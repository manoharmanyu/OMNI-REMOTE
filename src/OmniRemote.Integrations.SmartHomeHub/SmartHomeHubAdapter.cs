using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OmniRemote.Core.Abstractions;
using OmniRemote.Core.Net;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Integrations.SmartHomeHub;

/// <summary>
/// Transport adapter for smart-home hubs (specifically Home Assistant REST API).
/// Translates OmniRemote domain commands into Home Assistant service calls
/// (e.g. /api/services/climate/set_temperature) and reads live state from /api/states/{entity_id}.
/// </summary>
public sealed class SmartHomeHubAdapter : IDeviceAdapter
{
    public const string HubUrlKey = "hubUrl";
    public const string EntityIdKey = "entityId";

    private readonly HttpClient _httpClient;
    private readonly ISecretStore _secrets;
    private readonly ILogger<SmartHomeHubAdapter> _logger;

    public SmartHomeHubAdapter(HttpClient httpClient, ISecretStore secrets, ILogger<SmartHomeHubAdapter> logger)
    {
        _httpClient = httpClient;
        _secrets = secrets;
        _logger = logger;
    }

    public ConnectionKind Kind => ConnectionKind.SmartHomeHub;
    public bool IsAvailable => true;
    public string VerificationNote => "Verified against Home Assistant REST schema; unverified on live instance";

    public ConnectionState GetConnectionState(Guid deviceId) => ConnectionState.Disconnected;

    public async Task<ConnectionState> ConnectAsync(RemoteDevice device, CancellationToken cancellationToken)
    {
        if (!TryGetValidBaseUri(device, out var baseUri, out _))
            return ConnectionState.Unavailable;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/api/"));
            await ApplyAuthAsync(device, req);
            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return res.IsSuccessStatusCode ? ConnectionState.Connected : ConnectionState.Unavailable;
        }
        catch
        {
            return ConnectionState.Unavailable;
        }
    }

    public Task DisconnectAsync(RemoteDevice device, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<CommandResult> ExecuteAsync(RemoteDevice device, ValidatedCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetValidBaseUri(device, out var baseUri, out var urlError))
            return CommandResult.Fail(CommandErrorCode.NotConfigured, urlError ?? "Invalid Hub URL.");

        var entityId = device.Connection.Get(EntityIdKey);
        if (string.IsNullOrWhiteSpace(entityId))
            return CommandResult.Fail(CommandErrorCode.NotConfigured, "Home Assistant entity_id is not configured.");

        var (domain, service, serviceData) = MapToHomeAssistant(device, command, entityId);
        if (string.IsNullOrEmpty(service))
            return CommandResult.Fail(CommandErrorCode.UnsupportedCapability, "Hub command mapping not supported for this action.");

        var endpoint = new Uri(baseUri, $"/api/services/{domain}/{service}");
        var json = JsonSerializer.Serialize(serviceData, HubJsonContext.Default.DictionaryStringObject);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = content,
        };

        await ApplyAuthAsync(device, req);

        try
        {
            using var res = await _httpClient.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Home Assistant returned HTTP error {StatusCode}", (int)res.StatusCode);
                return CommandResult.Fail(CommandErrorCode.RejectedByDevice, $"Home Assistant rejected request (HTTP {(int)res.StatusCode}).");
            }

            // Command succeeded. Fetch updated state from entity to confirm.
            var state = await ReadStateInternalAsync(baseUri, device, entityId, cancellationToken);

            return new CommandResult
            {
                Status = state is not null ? CommandStatus.Confirmed : CommandStatus.Acknowledged,
                ReportedState = state?.Values,
                Message = "Command executed via Home Assistant.",
                Simulated = false,
            };
        }
        catch (TaskCanceledException)
        {
            return CommandResult.TimedOut();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Home Assistant hub connection error: {Message}", ex.Message);
            return CommandResult.Fail(CommandErrorCode.TransportError, "Failed to communicate with Home Assistant.");
        }
    }

    public async Task<DeviceState?> ReadStateAsync(RemoteDevice device, CancellationToken cancellationToken)
    {
        if (!TryGetValidBaseUri(device, out var baseUri, out _))
            return null;

        var entityId = device.Connection.Get(EntityIdKey);
        if (string.IsNullOrWhiteSpace(entityId)) return null;

        return await ReadStateInternalAsync(baseUri, device, entityId, cancellationToken);
    }

    private async Task<DeviceState?> ReadStateInternalAsync(Uri baseUri, RemoteDevice device, string entityId, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, $"/api/states/{entityId}"));
            await ApplyAuthAsync(device, req);
            using var res = await _httpClient.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return null;

            var bytes = await res.Content.ReadAsByteArrayAsync(ct);
            var stateObj = JsonSerializer.Deserialize(bytes, HubJsonContext.Default.HaEntityState);
            if (stateObj is null) return null;

            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (stateObj.State is not null)
            {
                values[StateKeys.Power] = stateObj.State.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                                          stateObj.State.Equals("playing", StringComparison.OrdinalIgnoreCase) ? "on" : "off";

                if (device.Type == DeviceType.TemperatureHumiditySensor)
                    values[StateKeys.SensorTemperature] = stateObj.State;
                else if (device.Type == DeviceType.MotionSensor)
                    values[StateKeys.SensorMotion] = stateObj.State.Equals("on", StringComparison.OrdinalIgnoreCase) ? "true" : "false";
            }

            if (stateObj.Attributes is not null)
            {
                if (stateObj.Attributes.TryGetValue("temperature", out var tempEl) && tempEl is not null)
                    values[StateKeys.Temperature] = tempEl.ToString() ?? "";
                if (stateObj.Attributes.TryGetValue("volume_level", out var volEl) && volEl is not null && double.TryParse(volEl.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var volDbl))
                    values[StateKeys.Volume] = ((int)(volDbl * 100)).ToString(CultureInfo.InvariantCulture);
                if (stateObj.Attributes.TryGetValue("is_volume_muted", out var muteEl) && muteEl is not null)
                    values[StateKeys.Muted] = (muteEl.ToString() ?? "false").ToLowerInvariant();
                if (stateObj.Attributes.TryGetValue("brightness", out var brightEl) && brightEl is not null && int.TryParse(brightEl.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var bVal))
                    values[StateKeys.Brightness] = ((int)(bVal * 100 / 255.0)).ToString(CultureInfo.InvariantCulture);
            }

            return new DeviceState
            {
                Values = values,
                Origin = StateOrigin.Reported,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
        }
        catch
        {
            return null;
        }
    }

    private static (string Domain, string Service, Dictionary<string, object> ServiceData) MapToHomeAssistant(RemoteDevice device, ValidatedCommand cmd, string entityId)
    {
        var domain = entityId.Split('.')[0];
        var data = new Dictionary<string, object> { ["entity_id"] = entityId };

        switch (cmd.Name)
        {
            case CommandNames.PowerSet:
                var on = cmd.GetBool("on");
                return (domain, on ? "turn_on" : "turn_off", data);

            case CommandNames.TemperatureSet:
                data["temperature"] = cmd.GetInt("celsius");
                return ("climate", "set_temperature", data);

            case CommandNames.ModeSet:
                data["hvac_mode"] = cmd.GetString("mode");
                return ("climate", "set_hvac_mode", data);

            case CommandNames.FanSpeedSet:
                data["fan_mode"] = cmd.GetString("speed");
                return ("climate", "set_fan_mode", data);

            case CommandNames.VolumeStep:
                var delta = cmd.GetInt("delta");
                return ("media_player", delta > 0 ? "volume_up" : "volume_down", data);

            case CommandNames.MuteToggle:
                return ("media_player", "volume_mute", data);

            case CommandNames.PlaybackPress:
                var action = cmd.GetString("action");
                var s = action switch
                {
                    "play" => "media_play",
                    "pause" => "media_pause",
                    "stop" => "media_stop",
                    "next" => "media_next_track",
                    "previous" => "media_previous_track",
                    _ => "media_play_pause",
                };
                return ("media_player", s, data);

            case CommandNames.BrightnessSet:
                data["brightness_pct"] = cmd.GetInt("percent");
                return ("light", "turn_on", data);

            case CommandNames.ColorSet:
                data["rgb_color"] = ParseHexToRgb(cmd.GetString("hex"));
                return ("light", "turn_on", data);

            default:
                return (domain, "", data);
        }
    }

    private static int[] ParseHexToRgb(string hex)
    {
        if (hex.StartsWith('#')) hex = hex[1..];
        if (hex.Length == 6 && int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgb))
        {
            return [(rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF];
        }
        return [255, 255, 255];
    }

    private static bool TryGetValidBaseUri(RemoteDevice device, out Uri uri, out string? error)
    {
        var rawUrl = device.Connection.Get(HubUrlKey);
        return EndpointPolicy.TryValidateBaseUrl(rawUrl, out uri, out error);
    }

    private async Task ApplyAuthAsync(RemoteDevice device, HttpRequestMessage req)
    {
        if (!string.IsNullOrEmpty(device.Connection.SecretRef))
        {
            var token = await _secrets.GetAsync(device.Connection.SecretRef);
            if (!string.IsNullOrEmpty(token))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
    }
}

public sealed class HaEntityState
{
    [JsonPropertyName("entity_id")]
    public string EntityId { get; set; } = "";

    [JsonPropertyName("state")]
    public string State { get; set; } = "";

    [JsonPropertyName("attributes")]
    public Dictionary<string, object>? Attributes { get; set; }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(Dictionary<string, object>))]
[JsonSerializable(typeof(HaEntityState))]
internal sealed partial class HubJsonContext : JsonSerializerContext;
