namespace Chaos.Client.Systems;

/// <summary>
///     Pure parser for the retail-format <c>Darkages.cfg</c>. Each non-blank line is split on its first
///     <c>:</c>; the left side (trimmed) is the key, the right side (trimmed, unquoted-as-written) is the value.
///     Last-write-wins on duplicate keys. Keys are case-insensitive so Epona-written <c>LobbyHost</c> matches
///     <c>lobbyhost</c>, <c>LOBBYHOST</c>, etc. Values are returned raw — callers that need quote stripping or
///     numeric parsing do it themselves.
/// </summary>
public static class DarkagesCfg
{
    /// <summary>
    ///     Parses cfg lines into a case-insensitive key → value dictionary. Blank lines and lines with no
    ///     colon are skipped. Values containing further colons (e.g. <c>Tel1: "Nexus","1"</c>) are kept intact.
    /// </summary>
    public static Dictionary<string, string> ParseLines(IEnumerable<string> lines)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in lines)
        {
            var colonIndex = line.IndexOf(':');

            if (colonIndex < 0)
                continue;

            var key = line[..colonIndex]
                .Trim();

            if (key.Length == 0)
                continue;

            var value = line[(colonIndex + 1)..]
                .Trim();

            result[key] = value;
        }

        return result;
    }
}
