#region
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Chaos.Client.Data;
using Chaos.Client.Systems;
using Microsoft.Xna.Framework.Graphics;
#endregion

namespace Chaos.Client;

/// <summary>
///     Static configuration for the client: version, data path, lobby host/port, and sampler state. Triggers all one-time
///     initialization (encoding providers, data archives, text colors) via the static constructor.
/// </summary>
public static class GlobalSettings
{
    public const string DEFAULT_LOBBY_HOST = "qa.hybrasyl.com";
    public const int DEFAULT_LOBBY_PORT = 2610;

    private const string CFG_FILE_NAME = "Darkages.cfg";
    private const string CFG_KEY_LOBBY_HOST = "LobbyHost";
    private const string CFG_KEY_LOBBY_PORT = "LobbyPort";

    private static readonly string[] PreLoadedAssemblies = ["Chaos.Networking"];
    private static readonly Type[] PreInitializedStatics = [typeof(DataContext), typeof(MachineIdentity)];
    public static readonly SamplerState Sampler = SamplerState.PointClamp; //SamplerState.LinearClamp;
    private static ushort ClientVersion => 741;

    public static string DataPath
        => @"E:\Games\Dark Ages";
            //Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, ".."));

    //LobbyHost/LobbyPort are overridable by Darkages.cfg at DataPath. See docs/epona-integration.md —
    //the sibling Epona launcher templates these keys per-profile before spawning the client.
    public static string LobbyHost { get; private set; } = DEFAULT_LOBBY_HOST;
    public static int LobbyPort { get; private set; } = DEFAULT_LOBBY_PORT;

    /// <summary>
    ///     When true, walking onto a water tile requires either the GM flag or the "Swimming" skill.
    ///     When false (default), any character can swim freely and pathfinding routes through water tiles.
    /// </summary>
    public static bool RequireSwimmingSkill => false;

    static GlobalSettings()
    {
        LoadEndpointFromCfg();
        InitializeOthers();
    }

    //read-only overlay: missing file, missing keys, malformed values → silently keep the defaults.
    //runs before DataContext.Initialize so the endpoint passed to the connection manager is the
    //one the user (or Epona) configured.
    private static void LoadEndpointFromCfg()
    {
        var path = Path.Combine(DataPath, CFG_FILE_NAME);

        if (!File.Exists(path))
            return;

        Dictionary<string, string> values;

        try
        {
            values = DarkagesCfg.ParseLines(File.ReadLines(path));
        } catch
        {
            //unreadable cfg → fall back to defaults silently (launch should not crash on this)
            return;
        }

        if (values.TryGetValue(CFG_KEY_LOBBY_HOST, out var host) && !string.IsNullOrWhiteSpace(host))
            LobbyHost = host;

        if (values.TryGetValue(CFG_KEY_LOBBY_PORT, out var portText)
            && int.TryParse(portText, out var port)
            && port is > 0 and <= 65535)
            LobbyPort = port;
    }

    private static void InitializeOthers()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        DataContext.Initialize(
            ClientVersion,
            DataPath,
            LobbyHost,
            LobbyPort);

        LegendColors.Initialize();
        TextColors.Initialize();

        foreach (var name in PreLoadedAssemblies)
            Assembly.Load(name);

        foreach (var type in PreInitializedStatics)
            RuntimeHelpers.RunClassConstructor(type.TypeHandle);
    }
}