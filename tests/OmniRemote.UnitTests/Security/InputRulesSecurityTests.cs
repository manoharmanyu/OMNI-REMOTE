using OmniRemote.Domain.Validation;
using Xunit;

namespace OmniRemote.UnitTests.Security;

public sealed class InputRulesSecurityTests
{
    [Theory]
    [InlineData("Living Room AC", true)]
    [InlineData("Samsung TV #1", true)]
    [InlineData("Bedroom Light (West)", true)]
    [InlineData("Test & Demo - Main", true)]
    public void ValidDisplayNames_PassValidation(string name, bool expected)
    {
        var result = InputRules.ValidateDisplayName(name, 40);
        Assert.Equal(expected, result.IsValid);
    }

    [Theory]
    [InlineData("Bad\nName")] // Newline injection
    [InlineData("Bad\r\nName")] // CRLF injection
    [InlineData("Bad\0Name")] // Null byte
    [InlineData("Test\u202EOverride")] // Bidi override
    [InlineData("<script>alert(1)</script>")] // Script tag injection
    public void MaliciousOrControlChars_FailValidation(string invalidName)
    {
        var result = InputRules.ValidateDisplayName(invalidName, 40);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ForLog_SanitizesNewlinesAndControlChars()
    {
        var untrusted = "Status: OK\r\nADMIN_OVERRIDE=TRUE\nFakeLogLine";
        var sanitized = InputRules.ForLog(untrusted, 60);

        Assert.DoesNotContain("\r", sanitized);
        Assert.DoesNotContain("\n", sanitized);
    }

    [Fact]
    public void SafeIdentifier_RejectsPathTraversalAndShellChars()
    {
        Assert.True(InputRules.IsSafeIdentifier("valid-device-123", 32));
        Assert.False(InputRules.IsSafeIdentifier("../../../etc/passwd", 32));
        Assert.False(InputRules.IsSafeIdentifier("name; rm -rf /", 32));
        Assert.False(InputRules.IsSafeIdentifier("name && ls", 32));
    }
}
