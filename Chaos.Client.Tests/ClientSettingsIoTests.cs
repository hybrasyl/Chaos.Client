using Chaos.Client.Systems;
using Xunit;

namespace Chaos.Client.Tests;

//ClientSettings is a static class with mutable state — tests mutate shared state and cannot run in parallel
//with each other. A single-class collection with DisableParallelization scopes this to just these tests.
[CollectionDefinition(nameof(ClientSettingsCollection), DisableParallelization = true)]
public sealed class ClientSettingsCollection;

[Collection(nameof(ClientSettingsCollection))]
public sealed class ClientSettingsIoTests
{
    private static string LoadThenSave(string input)
    {
        using var reader = new StringReader(input);
        ClientSettings.Load(reader);

        using var writer = new StringWriter();
        ClientSettings.Save(writer);

        return writer.ToString();
    }

    [Fact]
    public void LoadThenSave_KnownKeysWrittenWithCurrentFormat()
    {
        //regression: the retail-compatible lines Save emits today must keep emitting regardless of input.
        var output = LoadThenSave(string.Empty);

        //spot-check the fixed header Save emits unconditionally
        Assert.Contains("Version: 9728", output);
        Assert.Contains("Tel1: \"Nexus\",\"1\"", output);

        //and a couple of the known setting lines with current default values
        Assert.Contains("Sound Volume : 5",  output);
        Assert.Contains("Music Volume : 5",  output);
        Assert.Contains("Speed: 100",        output);
    }

    [Fact]
    public void LoadThenSave_UnknownKeysPreservedVerbatim()
    {
        //Epona writes these; Chaos.Client doesn't know about them, but Save must not drop them.
        var input = "LobbyHost: foo.com" + Environment.NewLine
                                         + "LobbyPort: 4200"
                                         + Environment.NewLine;

        var output = LoadThenSave(input);

        Assert.Contains("LobbyHost: foo.com", output);
        Assert.Contains("LobbyPort: 4200",    output);
    }

    [Fact]
    public void LoadThenSave_Idempotent()
    {
        //once the file is in a steady state, repeated Load→Save cycles produce identical bytes.
        //guards against drift where Save emits a shape Load then mutates into a different shape.
        var input = "LobbyHost: foo.com" + Environment.NewLine
                                         + "LobbyPort: 4200"
                                         + Environment.NewLine;

        var firstPass  = LoadThenSave(input);
        var secondPass = LoadThenSave(firstPass);

        Assert.Equal(firstPass, secondPass);
    }

    [Fact]
    public void LoadThenSave_UnknownKeysWithOddSpacing_Preserved()
    {
        //external writers (or hand edits) may use non-standard spacing. Save must preserve the line verbatim.
        var input = "  LobbyHost:foo.com" + Environment.NewLine;

        var output = LoadThenSave(input);

        Assert.Contains("  LobbyHost:foo.com", output);
    }
}
