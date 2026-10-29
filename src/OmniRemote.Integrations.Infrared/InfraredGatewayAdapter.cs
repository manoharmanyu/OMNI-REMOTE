using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OmniRemote.Core.Abstractions;
using OmniRemote.Core.Net;
using OmniRemote.Domain.Commands;
using OmniRemote.Domain.Devices;

namespace OmniRemote.Integrations.Infrared;

/// <summary>
/// Transport adapter for external Infrared gateways (e.g. ESP32, Tasmota, or Broadlink-style LAN gateways).
/// Phones do not possess built-in IR blasters; an external hardware bridge is required.
/// Because raw IR transmission is inherently one-way, successfully sent commands are reported as Sent (unconfirmed).
/// </summary>
public sealed class InfraredGatewayAdapter : IDeviceAdapter
{
    public const string GatewayUrlKey = "gatewayUrl";
    public const string ProtocolKey = "irProtocol"; // e.g. "NEC", "SONY", "RC5", "RAW"
    public const string DeviceCodeKey = "deviceCode";

    private readonly HttpClient _httpClient;
    private readonly ISecretStore _secrets;
    private readonly ILogger<InfraredGatewayAdapter> _logger;

    public InfraredGatewayAdapter(HttpClient httpClient, ISecretStore secrets, ILogger<InfraredGatewayAdapter> logger)
    {
        _httpClient = httpClient;
        _secrets = secrets;
        _logger = logger;
    }

    public ConnectionKind Kind => ConnectionKind.InfraredGateway;
    public bool IsAvailable => true;
    public string VerificationNote => "Verified against mock gateway API; requires external ESP32/Tasmota hardware";

    public ConnectionState GetConnectionState(Guid deviceId) => ConnectionState.Disconnected;

    public async Task<ConnectionState> ConnectAsync(RemoteDevice device, CancellationToken cancellationToken)
    {
        if (!TryGetValidBaseUri(device, out var baseUri, out _))
            return ConnectionState.Unavailable;

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, new Uri(baseUri, "/api/ir/health"));
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
            return CommandResult.Fail(CommandErrorCode.NotConfigured, urlError ?? "Invalid IR Gateway URL.");

        var payload = new IrTransmitPayload
        {
            RequestId = command.Command.RequestId.ToString("N"),
            Protocol = device.Connection.Get(ProtocolKey) ?? "NEC",
            DeviceCode = device.Connection.Get(DeviceCodeKey) ?? device.BrandId,
            Command = command.Name,
            Parameters = command.Values.ToDictionary(kv => kv.Key, kv => kv.Value),
        };

        var json = JsonSerializer.Serialize(payload, IrJsonContext.Default.IrTransmitPayload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var req = new HttpRequestMessage(HttpMethod.Post, new Uri(baseUri, "/api/ir/send"))
        {
            Content = content,
        };

        if (!string.IsNullOrEmpty(device.Connection.SecretRef))
        {
            var token = await _secrets.GetAsync(device.Connection.SecretRef);
            if (!string.IsNullOrEmpty(token))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        try
        {
            using var res = await _httpClient.SendAsync(req, cancellationToken);
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("IR Gateway returned HTTP error {StatusCode}", (int)res.StatusCode);
                return CommandResult.Fail(CommandErrorCode.TransportError, $"IR Gateway returned HTTP {(int)res.StatusCode}.");
            }

            // CRITICAL: Infrared is one-way blind transmission.
            // Status must be Sent (unconfirmed), not Confirmed, unless independent sensor feedback exists.
            return new CommandResult
            {
                Status = CommandStatus.Sent,
                ErrorCode = CommandErrorCode.None,
                Message = "IR signal emitted by gateway (one-way unconfirmed).",
                Simulated = false,
                ReportedState = null,
            };
        }
        catch (TaskCanceledException)
        {
            return CommandResult.TimedOut();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("IR gateway connection error: {Message}", ex.Message);
            return CommandResult.Fail(CommandErrorCode.TransportError, "Failed to communicate with IR Gateway.");
        }
    }

    public Task<DeviceState?> ReadStateAsync(RemoteDevice device, CancellationToken cancellationToken) =>
        Task.FromResult<DeviceState?>(null); // One-way IR cannot read state

    private static bool TryGetValidBaseUri(RemoteDevice device, out Uri uri, out string? error)
    {
        var rawUrl = device.Connection.Get(GatewayUrlKey);
        return EndpointPolicy.TryValidateBaseUrl(rawUrl, out uri, out error);
    }
}

public sealed class IrTransmitPayload
{
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = "";

    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = "";

    [JsonPropertyName("deviceCode")]
    public string DeviceCode { get; set; } = "";

    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("parameters")]
    public Dictionary<string, string> Parameters { get; set; } = [];
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(IrTransmitPayload))]
internal sealed partial class IrJsonContext : JsonSerializerContext;
