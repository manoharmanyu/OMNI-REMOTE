using System.Security.Cryptography;
using OmniRemote.Integrations.Bluetooth;
using Xunit;

namespace OmniRemote.UnitTests.Security;

public sealed class BluetoothOrdpProtocolTests
{
    private static readonly byte[] TestKey = RandomNumberGenerator.GetBytes(32);

    [Fact]
    public void ValidFrame_EncodesAndDecodesSuccessfully()
    {
        var req = new OrdpRequest(
            V: 1,
            Id: Guid.NewGuid().ToString("N"),
            Seq: 1001,
            Ts: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Cmd: "power.set",
            P: new Dictionary<string, string> { ["on"] = "true" });

        var bytes = OrdpProtocol.Encode(req, TestKey);
        var decoded = OrdpProtocol.DecodeRequest(bytes, TestKey);

        Assert.NotNull(decoded);
        Assert.Equal(req.Id, decoded.Id);
        Assert.Equal(req.Cmd, decoded.Cmd);
        Assert.Equal("true", decoded.P["on"]);
    }

    [Fact]
    public void TamperedFrame_FailsMacVerification()
    {
        var req = new OrdpRequest(
            V: 1,
            Id: Guid.NewGuid().ToString("N"),
            Seq: 1002,
            Ts: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Cmd: "temperature.set",
            P: new Dictionary<string, string> { ["celsius"] = "24" });

        var bytes = OrdpProtocol.Encode(req, TestKey);

        // Tamper with byte payload
        bytes[bytes.Length - 10] ^= 0xFF;

        var decoded = OrdpProtocol.DecodeRequest(bytes, TestKey);

        // Must reject forged / tampered packet
        Assert.Null(decoded);
    }

    [Fact]
    public void WrongKey_FailsMacVerification()
    {
        var req = new OrdpRequest(
            V: 1,
            Id: Guid.NewGuid().ToString("N"),
            Seq: 1003,
            Ts: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Cmd: "volume.step",
            P: new Dictionary<string, string> { ["delta"] = "1" });

        var wrongKey = RandomNumberGenerator.GetBytes(32);
        var bytes = OrdpProtocol.Encode(req, TestKey);

        var decoded = OrdpProtocol.DecodeRequest(bytes, wrongKey);

        Assert.Null(decoded);
    }
}
