using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OmniRemote.Integrations.Bluetooth;

/// <summary>
/// OmniRemote BLE Device Protocol v1 (ORDP/1).
/// A compatible peripheral exposes <see cref="ServiceUuid"/> with a write characteristic for commands and a
/// notify characteristic for responses. Every frame is authenticated with HMAC-SHA256 using a per-device key
/// provisioned out-of-band during pairing and kept in platform secure storage. Peripherals MUST:
///  • require an LE Secure Connections bond before accepting writes;
///  • verify the MAC; reject frames whose <c>seq</c> is not greater than the last accepted value (replay);
///  • validate the command and parameters independently of the app.
/// </summary>
public static class OrdpProtocol
{
    public static readonly Guid ServiceUuid = Guid.Parse("6f6d6e69-7265-4d6f-9465-000000000001");
    public static readonly Guid CommandCharacteristic = Guid.Parse("6f6d6e69-7265-4d6f-9465-000000000002");
    public static readonly Guid ResponseCharacteristic = Guid.Parse("6f6d6e69-7265-4d6f-9465-000000000003");

    public const int Version = 1;
    public const int RequiredMtu = 247;
    public const int MaxFrameBytes = RequiredMtu - 3;
    public const int KeyBytes = 32;

    public static byte[] Encode(OrdpRequest request, ReadOnlySpan<byte> key)
    {
        var mac = ComputeMac(Canonical(request), key);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(request with { Mac = mac }, OrdpJsonContext.Default.OrdpRequest);
        if (bytes.Length > MaxFrameBytes) throw new InvalidOperationException("Command frame exceeds BLE MTU.");
        return bytes;
    }

    public static OrdpRequest? DecodeRequest(ReadOnlySpan<byte> frame, ReadOnlySpan<byte> key)
    {
        OrdpRequest? r;
        try { r = JsonSerializer.Deserialize(frame, OrdpJsonContext.Default.OrdpRequest); }
        catch (JsonException) { return null; }
        if (r?.Mac is null) return null;
        var expected = ComputeMac(Canonical(r), key);
        return FixedTimeEquals(expected, r.Mac) ? r : null;
    }

    public static byte[] EncodeResponse(OrdpResponse response, ReadOnlySpan<byte> key)
    {
        var mac = ComputeMac(Canonical(response), key);
        return JsonSerializer.SerializeToUtf8Bytes(response with { Mac = mac }, OrdpJsonContext.Default.OrdpResponse);
    }

    /// <summary>Returns null when the frame is malformed or its MAC does not verify.</summary>
    public static OrdpResponse? DecodeResponse(ReadOnlySpan<byte> frame, ReadOnlySpan<byte> key)
    {
        if (frame.Length > 4096) return null;
        OrdpResponse? r;
        try { r = JsonSerializer.Deserialize(frame, OrdpJsonContext.Default.OrdpResponse); }
        catch (JsonException) { return null; }
        if (r?.Mac is null) return null;
        var expected = ComputeMac(Canonical(r), key);
        return FixedTimeEquals(expected, r.Mac) ? r : null;
    }

    /// <summary>
    /// Canonical MAC input: fields joined by '\n', parameters/state sorted by key as "k=v" joined by '&amp;'.
    /// Firmware reproduces this exactly; it does not depend on JSON formatting.
    /// </summary>
    internal static string Canonical(OrdpRequest r) =>
        string.Join('\n', "ORDP1-REQ", r.V.ToString(CultureInfo.InvariantCulture), r.Id, r.Seq.ToString(CultureInfo.InvariantCulture),
            r.Ts.ToString(CultureInfo.InvariantCulture), r.Cmd, Pairs(r.P));

    internal static string Canonical(OrdpResponse r) =>
        string.Join('\n', "ORDP1-RES", r.Id, r.Status, r.Err ?? "", Pairs(r.State));

    private static string Pairs(IReadOnlyDictionary<string, string>? d) =>
        d is null ? "" : string.Join('&', d.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}={kv.Value}"));

    private static string ComputeMac(string canonical, ReadOnlySpan<byte> key) =>
        Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(canonical)));

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));
}

public sealed record OrdpRequest(
    [property: JsonPropertyName("v")] int V,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("seq")] long Seq,
    [property: JsonPropertyName("ts")] long Ts,
    [property: JsonPropertyName("cmd")] string Cmd,
    [property: JsonPropertyName("p")] IReadOnlyDictionary<string, string> P,
    [property: JsonPropertyName("mac")] string? Mac = null);

/// <summary>Status: "ack" (received), "done" (applied and state reported), "error".</summary>
public sealed record OrdpResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("err")] string? Err = null,
    [property: JsonPropertyName("state")] IReadOnlyDictionary<string, string>? State = null,
    [property: JsonPropertyName("mac")] string? Mac = null);

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(OrdpRequest))]
[JsonSerializable(typeof(OrdpResponse))]
internal sealed partial class OrdpJsonContext : JsonSerializerContext;
