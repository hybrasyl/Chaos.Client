using Chaos.Client.Systems;
using Xunit;

namespace Chaos.Client.Tests;

public sealed class DarkagesCfgTests
{
    [Fact]
    public void EmptyInput_ReturnsEmptyDictionary()
    {
        var result = DarkagesCfg.ParseLines([]);

        Assert.Empty(result);
    }

    [Fact]
    public void SingleKeyValue_Parsed()
    {
        var result = DarkagesCfg.ParseLines(["LobbyHost: foo.com"]);

        Assert.Single(result);
        Assert.Equal("foo.com", result["LobbyHost"]);
    }

    [Fact]
    public void MultipleKeys_AllParsed()
    {
        var result = DarkagesCfg.ParseLines(
        [
            "LobbyHost: foo.com",
            "LobbyPort: 4200",
            "Speed: 100"
        ]);

        Assert.Equal(3, result.Count);
        Assert.Equal("foo.com", result["LobbyHost"]);
        Assert.Equal("4200",    result["LobbyPort"]);
        Assert.Equal("100",     result["Speed"]);
    }

    [Fact]
    public void WhitespaceAroundColon_Trimmed()
    {
        var result = DarkagesCfg.ParseLines(["  LobbyHost : foo.com  "]);

        Assert.Single(result);
        Assert.Equal("foo.com", result["LobbyHost"]);
    }

    [Fact]
    public void CaseInsensitiveKeyLookup()
    {
        var result = DarkagesCfg.ParseLines(["lobbyhost: foo.com"]);

        Assert.Equal("foo.com", result["LobbyHost"]);
        Assert.Equal("foo.com", result["LOBBYHOST"]);
        Assert.Equal("foo.com", result["lobbyhost"]);
    }

    [Fact]
    public void DuplicateKey_LastWins()
    {
        var result = DarkagesCfg.ParseLines(
        [
            "LobbyHost: first.com",
            "LobbyHost: second.com"
        ]);

        Assert.Single(result);
        Assert.Equal("second.com", result["LobbyHost"]);
    }

    [Fact]
    public void LineWithoutColon_Ignored()
    {
        var result = DarkagesCfg.ParseLines(
        [
            "LobbyHost: foo.com",
            "just some noise without a delimiter",
            "LobbyPort: 4200"
        ]);

        Assert.Equal(2, result.Count);
        Assert.False(result.ContainsKey("just some noise without a delimiter"));
    }

    [Fact]
    public void EmptyValue_PreservedAsEmptyString()
    {
        var result = DarkagesCfg.ParseLines(["LobbyHost:"]);

        Assert.Single(result);
        Assert.Equal(string.Empty, result["LobbyHost"]);
    }

    [Fact]
    public void ValueContainingColon_KeptIntact()
    {
        //retail cfg has Tel1: "Nexus","1" style lines; some other cfgs might have URLs with ports.
        //the parser splits only on the first colon, so the rest of the value is preserved verbatim.
        var result = DarkagesCfg.ParseLines(["Tel1: \"Nexus\",\"1\""]);

        Assert.Equal("\"Nexus\",\"1\"", result["Tel1"]);
    }

    [Fact]
    public void BlankLine_Ignored()
    {
        var result = DarkagesCfg.ParseLines(
        [
            "LobbyHost: foo.com",
            "",
            "   ",
            "LobbyPort: 4200"
        ]);

        Assert.Equal(2, result.Count);
    }
}
