using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OmniRemote.Core.Abstractions;
using OmniRemote.Core.Net;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Integrations.Network;

/// <summary>
/// Transport adapter for local Wi-Fi devices exposing a documented REST API.
/// Adheres strictly to EndpointPolicy: target must be HTTPS or a private LAN IP.
/// </summary>
public sealed class WifiDeviceAdapter : IDeviceAdapter
{
    public const string EndpointKey = "endpoint";
    public const string DeviceIdentifierKey = "deviceIdentifier";

    private readonly HttpClient _httpClient;
    private readonly ISecretStore _secrets;
    private readonly ILogger<WifiDeviceAdapter> _logger;

    public WifiDeviceAdapter(HttpClient httpClient, ISecretStore secrets, ILogger<WifiDeviceAdapter> logger)
    {
        _httpClient = httpClient;
        _secrets = secrets;
        _logger = logger;
    }

    public ConnectionKind Kind => ConnectionKind.Wifi;
    public bool IsAvailable => true;
    public string VerificationNote => "Verified against mock HTTP endpoint; unverified on proprietary vendor hardware";

    public ConnectionState GetConnectionState(Guid deviceId) => ConnectionState.Disconnected;

    public async Task<ConnectionState> ConnectAsync(RemoteDevice device, CancellationToken cancellationToken)
    {
        if (!TryGetValidBaseUri(device, out var baseUri, out _))
            return ConnectionState.Unavailable;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/api/v1/status"));
            await ApplyAuthAsync(device, req);
            using var res = await _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            return res.IsSuccessStatusCode ? ConnectionState.Connected : ConnectionState.Unavailable;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogDebug("Wi-Fi health check failed: {Error}", ex.Message);
            return ConnectionState.Unavailable;
        }
    }

    public Task DisconnectAsync(RemoteDevice device, CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task<CommandResult> ExecuteAsync(RemoteDevice device, ValidatedCommand command, CancellationToken cancellationToken)
    {
        if (!TryGetValidBaseUri(device, out var baseUri, out var urlError))
            return CommandResult.Fail(CommandErrorCode.NotConfigured, urlError ?? "Invalid endpoint URL.");

        var payload = new WifiCommandPayload
        {
            RequestId = command.Command.RequestId.ToString("N"),
            DeviceId = device.Connection.Get(DeviceIdentifierKey) ?? device.Id.ToString(),
            Command = command.Name,
            Parameters = command.Values.ToDictionary(kv => kv.Key, kv => kv.Value),
        };

        var json = JsonSerializer.Serialize(payload, WifiJsonContext.Default.WifiCommandPayload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/api/v1/control"))
        {
            Content = content,
        };

        await ApplyAuthAsync(device, req);

        try
        {
            using var res = await _httpClient.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Wi-Fi device returned status code {StatusCode}", (int)res.StatusCode);
                return CommandResult.Fail(CommandErrorCode.RejectedByDevice, $"Device returned HTTP {(int)res.StatusCode}.");
            }

            var respBytes = await res.Content.ReadAsByteArrayAsync(cancellationToken);
            var resultObj = JsonSerializer.Deserialize(respBytes, WifiJsonContext.Default.WifiResponsePayload);

            if (resultObj is null)
                return CommandResult.Fail(CommandErrorCode.TransportError, "Invalid response from Wi-Fi device.");

            if (resultObj.Success)
            {
                return new CommandResult
                {
                    Status = resultObj.Confirmed ? CommandStatus.Confirmed : CommandStatus.Acknowledged,
                    ReportedState = resultObj.State,
                    Message = resultObj.Message ?? "Command accepted by Wi-Fi device.",
                    Simulated = false,
                };
            }

            return CommandResult.Fail(CommandErrorCode.RejectedByDevice, resultObj.Message ?? "Wi-Fi device reported execution failure.");
        }
        catch (TaskCanceledException)
        {
            return CommandResult.TimedOut();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Wi-Fi communication error: {Message}", ex.Message);
            return CommandResult.Fail(CommandErrorCode.TransportError, "Network communication error.");
        }
    }

    public async Task<DeviceState?> ReadStateAsync(RemoteDevice device, CancellationToken cancellationToken)
    {
        if (!TryGetValidBaseUri(device, out var baseUri, out _))
            return null;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/api/v1/state"));
            await ApplyAuthAsync(device, req);
            using var res = await _httpClient.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode) return null;

            var respBytes = await res.Content.ReadAsByteArrayAsync(cancellationToken);
            var resultObj = JsonSerializer.Deserialize(respBytes, WifiJsonContext.Default.WifiStatePayload);
            if (resultObj?.State is null) return null;

            return new DeviceState
            {
                Values = resultObj.State,
                Origin = StateOrigin.Reported,
                UpdatedAt = DateTimeOffset.UtcNow,
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool TryGetValidBaseUri(RemoteDevice device, out Uri uri, out string? error)
    {
        var rawUrl = device.Connection.Get(EndpointKey);
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

public sealed class WifiCommandPayload
{
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = "";

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; } = "";

    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("parameters")]
    public Dictionary<string, string> Parameters { get; set; } = [];
}

public sealed class WifiResponsePayload
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("confirmed")]
    public bool Confirmed { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("state")]
    public Dictionary<string, string>? State { get; set; }
}

public sealed class WifiStatePayload
{
    [JsonPropertyName("state")]
    public Dictionary<string, string> State { get; set; } = [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WifiCommandPayload))]
[JsonSerializable(typeof(WifiResponsePayload))]
[JsonSerializable(typeof(WifiStatePayload))]
internal sealed partial class WifiJsonContext : JsonSerializerContext;
