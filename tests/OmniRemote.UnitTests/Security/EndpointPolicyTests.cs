using System.Net;
using OmniRemote.Core.Net;
using Xunit;

namespace OmniRemote.UnitTests.Security;

public sealed class EndpointPolicyTests
{
    [Theory]
    [InlineData("192.168.1.50", true)]
    [InlineData("10.0.0.1", true)]
    [InlineData("172.20.10.5", true)]
    [InlineData("127.0.0.1", false)] // Localhost on device strictly prohibited
    [InlineData("8.8.8.8", false)]     // Public IP
    [InlineData("1.1.1.1", false)]     // Public IP
    public void IsPrivateIp_ValidatesCorrectly(string ipStr, bool expected)
    {
        var parsed = IPAddress.Parse(ipStr);
        Assert.Equal(expected, EndpointPolicy.IsPrivate(parsed));
    }

    [Theory]
    [InlineData("https://my-smart-hub.local/")]
    [InlineData("https://api.smarthome.example.com")]
    [InlineData("http://192.168.1.100:8123")]
    [InlineData("http://10.0.0.50")]
    public void ValidBaseUrls_PassValidation(string url)
    {
        var ok = EndpointPolicy.TryValidateBaseUrl(url, out var uri, out var error);
        Assert.True(ok, error);
        Assert.NotNull(uri);
    }

    [Theory]
    [InlineData("http://8.8.8.8/")] // Insecure HTTP to public internet host
    [InlineData("http://127.0.0.1:8080")] // Loopback access attempt
    [InlineData("http://" + "user" + ":" + "key" + "@192.168.1.10/")] // Embedded credentials in URL
    [InlineData("javascript:alert(1)")] // Non-HTTP scheme
    public void InvalidBaseUrls_FailValidation(string url)
    {
        var ok = EndpointPolicy.TryValidateBaseUrl(url, out _, out _);
        Assert.False(ok);
    }
}
