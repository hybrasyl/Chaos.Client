namespace Chaos.Client.Systems;

/// <summary>
///     Reads and writes the client settings file (Darkages.cfg) in the original DarkAges format. File is a line-delimited
///     key-value format: "Key : Value" or "Key: Value". Saved next to the executable. Unknown keys encountered during
///     Load are preserved verbatim and re-emitted by Save so that externally-written keys (notably LobbyHost/LobbyPort
///     written by the Epona launcher — see docs/epona-integration.md) survive a user-triggered settings save.
/// </summary>
public static class ClientSettings
{
    private const string FILE_NAME = "Darkages.cfg";

    //every key Save() emits. Load treats anything not in this set as a caller-owned line we must
    //round-trip verbatim. OrdinalIgnoreCase so "lobbyhost" (non-canonical casing) doesn't get double-written.
    private static readonly HashSet<string> KnownKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "Version",
        "Port",
        "Speed",
        "KeyBoard",
        "Tel",
        "HanFont",
        "EngFont",
        "Tel1",
        "Tel2",
        "Tel3",
        "Tel4",
        "Chatting Mode",
        "doGroundAnimation",
        "Sound Volume",
        "Music Volume",
        "SkillSpellSelectByToggle",
        "GroupAnswer",
        "ScrollLevel",
        "UserClickMode",
        "MonsterSayRecordMode",
        "GroupObjectOption"
    };

    //verbatim lines for any key KnownKeys doesn't recognize. populated by Load, re-emitted by Save.
    private static List<string> UnknownLines = [];

    public static bool UseGroupWindow { get; set; } = true;
    public static int ChattingMode { get; set; }
    public static bool DoGroundAnimation { get; set; } = true;
    public static bool EnableProfileClick { get; set; }
    public static bool GroupOpen { get; set; }
    public static int MusicVolume { get; set; } = 5;
    public static bool RecordNpcChat { get; set; } = true;
    public static int ScrollLevel { get; set; }

    //defaults match the original client
    public static int SoundVolume { get; set; } = 5;
    public static int Speed { get; set; } = 100;
    public static bool UseShiftKeyForAltPanels { get; set; } = true;

    private static string FilePath => Path.Combine(GlobalSettings.DataPath, FILE_NAME);

    /// <summary>
    ///     Loads settings from Darkages.cfg into static properties. Uses defaults if the file doesn't exist or is corrupt.
    /// </summary>
    public static void Load()
    {
        if (!File.Exists(FilePath))
            return;

        try
        {
            using var reader = new StreamReader(FilePath);
            Load(reader);
        } catch
        {
            //corrupted file — use whatever defaults/partial state was already set
        }
    }

    /// <summary>
    ///     Stream-based Load, split out so tests can round-trip without touching disk. Unknown-key lines are
    ///     appended to <see cref="UnknownLines" /> in original order and with original whitespace/quoting.
    /// </summary>
    internal static void Load(TextReader reader)
    {
        UnknownLines.Clear();

        string? line;

        while ((line = reader.ReadLine()) is not null)
        {
            var colonIndex = line.IndexOf(':');

            if (colonIndex < 0)
                continue;

            var key = line[..colonIndex]
                .Trim();

            var value = line[(colonIndex + 1)..]
                .Trim();

            if (!KnownKeys.Contains(key))
            {
                //preserve raw line — external writer may rely on exact spacing / quoting
                UnknownLines.Add(line);

                continue;
            }

            switch (key)
            {
                case "Sound Volume":
                    if (int.TryParse(value, out var sv))
                        SoundVolume = Math.Clamp(sv, 0, 10);

                    break;

                case "Music Volume":
                    if (int.TryParse(value, out var mv))
                        MusicVolume = Math.Clamp(mv, 0, 10);

                    break;

                case "doGroundAnimation":
                    DoGroundAnimation = value == "1";

                    break;

                case "SkillSpellSelectByToggle":
                    UseShiftKeyForAltPanels = value != "1";

                    break;

                case "GroupAnswer":
                    GroupOpen = value == "1";

                    break;

                case "ScrollLevel":
                    if (int.TryParse(value, out var sl))
                        ScrollLevel = sl;

                    break;

                case "UserClickMode":
                    EnableProfileClick = value == "1";

                    break;

                case "MonsterSayRecordMode":
                    RecordNpcChat = value == "1";

                    break;

                case "GroupObjectOption":
                    UseGroupWindow = value == "1";

                    break;

                case "Chatting Mode":
                    if (int.TryParse(value, out var cm))
                        ChattingMode = cm;

                    break;

                case "Speed":
                    if (int.TryParse(value, out var spd))
                        Speed = spd;

                    break;
            }
        }
    }

    /// <summary>
    ///     Saves the current settings to Darkages.cfg in the original format.
    /// </summary>
    public static void Save()
    {
        try
        {
            using var writer = new StreamWriter(FilePath, false);
            Save(writer);
        } catch
        {
            //best effort — don't crash on save failure
        }
    }

    /// <summary>
    ///     Stream-based Save, split out so tests can round-trip without touching disk. Writes the fixed set of
    ///     known keys in retail order, then appends any <see cref="UnknownLines" /> captured by the most recent
    ///     <see cref="Load(TextReader)" /> so externally-written entries (e.g. Epona's LobbyHost/LobbyPort)
    ///     survive this round-trip.
    /// </summary>
    internal static void Save(TextWriter writer)
    {
        writer.WriteLine("Version: 9728");
        writer.WriteLine("Port: 5");
        writer.WriteLine($"Speed: {Speed}");
        writer.WriteLine("KeyBoard: 0");
        writer.WriteLine("Tel: 1");
        writer.WriteLine("HanFont: 0");
        writer.WriteLine("EngFont: 0");
        writer.WriteLine("Tel1: \"Nexus\",\"1\"");
        writer.WriteLine("Tel2: \"Nexus\",\"2\"");
        writer.WriteLine("Tel3: \"Nexus\",\"3\"");
        writer.WriteLine("Tel4: \"Nexus\",\"4\"");
        writer.WriteLine($"Chatting Mode : {ChattingMode}");
        writer.WriteLine($"doGroundAnimation : {(DoGroundAnimation ? 1 : 0)}");
        writer.WriteLine($"Sound Volume : {SoundVolume}");
        writer.WriteLine($"Music Volume : {MusicVolume}");
        writer.WriteLine($"SkillSpellSelectByToggle : {(UseShiftKeyForAltPanels ? 0 : 1)}");
        writer.WriteLine($"GroupAnswer : {(GroupOpen ? 1 : 0)}");
        writer.WriteLine($"ScrollLevel : {ScrollLevel}");
        writer.WriteLine($"UserClickMode : {(EnableProfileClick ? 1 : 0)}");
        writer.WriteLine($"MonsterSayRecordMode : {(RecordNpcChat ? 1 : 0)}");
        writer.WriteLine($"GroupObjectOption : {(UseGroupWindow ? 1 : 0)}");

        foreach (var unknown in UnknownLines)
            writer.WriteLine(unknown);
    }
}
