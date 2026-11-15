using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;

namespace PolytopiaMapManager;

public static class Commands
{
    internal const string TILES_COMMAND = "mapmakertiles";
    internal const string RESIZE_COMMAND = "mapmakerresize";

    internal static CommandType TilesType => GetCommandType(TILES_COMMAND);
    internal static CommandType ResizeType => GetCommandType(RESIZE_COMMAND);
    internal static bool IsAvailable => TilesType != CommandType.None && ResizeType != CommandType.None;

    private static CommandType GetCommandType(string name)
    {
        return EnumCache<CommandType>.TryGetType(name, out CommandType type) ? type : CommandType.None;
    }

    internal static void Register()
    {
        ClassInjector.RegisterTypeInIl2Cpp<MapMakerTilesCommand>();
        ClassInjector.RegisterTypeInIl2Cpp<MapMakerResizeCommand>();
        RegisterCommandType(TILES_COMMAND);
        RegisterCommandType(RESIZE_COMMAND);
    }

    private static void RegisterCommandType(string name)
    {
        if(EnumCache<CommandType>.TryGetType(name, out _))
            return;

        CommandType value = (CommandType)PolyMod.Registry.GetAutoIndex(typeof(CommandType));
        PolyMod.Registry.IncrementAutoIndex(typeof(CommandType));
        EnumCache<CommandType>.AddMapping(name, value);
        EnumCache<CommandType>.AddMapping(name, value);
        Main.modLogger!.LogInfo($"Registered command type {name} with index {(int)value}");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameState), nameof(GameState.GetCommand))]
    private static bool GameState_GetCommand(ref CommandBase __result, CommandType type)
    {
        if(type == CommandType.None)
            return true;
        if(type == TilesType)
        {
            __result = new MapMakerTilesCommand();
            return false;
        }
        if(type == ResizeType)
        {
            __result = new MapMakerResizeCommand();
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CommandBase), nameof(CommandBase.Execute))]
    private static void CommandBase_Execute(CommandBase __instance, GameState state)
    {
        AsTiles(__instance)?.Apply(state);
        AsResize(__instance)?.Apply(state);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CommandBase), nameof(CommandBase.Serialize))]
    private static void CommandBase_Serialize(CommandBase __instance, Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        AsTiles(__instance)?.Write(writer, version);
        AsResize(__instance)?.Write(writer, version);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CommandBase), nameof(CommandBase.Deserialize))]
    private static void CommandBase_Deserialize(CommandBase __instance, Il2CppSystem.IO.BinaryReader reader, int version)
    {
        AsTiles(__instance)?.Read(reader, version);
        AsResize(__instance)?.Read(reader, version);
    }

    internal static MapMakerTilesCommand? AsTiles(CommandBase command)
    {
        return command.TryCast<MapMakerTilesCommand>();
    }

    internal static MapMakerResizeCommand? AsResize(CommandBase command)
    {
        return command.TryCast<MapMakerResizeCommand>();
    }
}

public class MapMakerTilesCommand : CommandBase
{
    private List<(int x, int y, byte[] data)> tiles = new();
    private List<Data.Capital> capitals = new();

    public MapMakerTilesCommand(IntPtr pointer) : base(pointer) {}

    public MapMakerTilesCommand() : base(ClassInjector.DerivedConstructorPointer<MapMakerTilesCommand>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public override CommandType GetCommandType()
    {
        return Commands.TilesType;
    }

    [HideFromIl2Cpp]
    internal List<Data.Capital> GetCapitalsAfter()
    {
        return capitals;
    }

    [HideFromIl2Cpp]
    internal static MapMakerTilesCommand Create(GameState state, IEnumerable<WorldCoordinates> coordinates, List<Data.Capital> capitalsAfter)
    {
        MapMakerTilesCommand command = new MapMakerTilesCommand();
        command.PlayerId = state.CurrentPlayer;
        foreach (WorldCoordinates coords in coordinates)
        {
            TileData? tile = state.Map.GetTile(coords);
            if(tile != null)
                command.tiles.Add((coords.x, coords.y, SerializeTile(tile, state.Version)));
        }
        command.capitals = History.CloneCapitals(capitalsAfter);
        return command;
    }

    [HideFromIl2Cpp]
    public void Apply(GameState state)
    {
        MapData map = state.Map;
        foreach (var (x, y, data) in tiles)
        {
            if(x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                continue;
            map.Tiles[x + y * map.Width] = DeserializeTile(data, state.Version);
        }
    }

    [HideFromIl2Cpp]
    public void Write(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(tiles.Count);
        foreach (var (x, y, data) in tiles)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(data.Length);
            writer.Write((Il2CppStructArray<byte>)data);
        }
        writer.Write(capitals.Count);
        foreach (Data.Capital capital in capitals)
        {
            writer.Write(capital.player);
            writer.Write(capital.coordinates.x);
            writer.Write(capital.coordinates.y);
        }
    }

    [HideFromIl2Cpp]
    public void Read(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        tiles = new();
        int tileCount = reader.ReadInt32();
        for (int i = 0; i < tileCount; i++)
        {
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            int length = reader.ReadInt32();
            tiles.Add((x, y, reader.ReadBytes(length).ToArray()));
        }
        capitals = new();
        int capitalCount = reader.ReadInt32();
        for (int i = 0; i < capitalCount; i++)
        {
            byte player = reader.ReadByte();
            int x = reader.ReadInt32();
            int y = reader.ReadInt32();
            capitals.Add(new Data.Capital { player = player, coordinates = new WorldCoordinates(x, y) });
        }
    }

    [HideFromIl2Cpp]
    private static byte[] SerializeTile(TileData tile, int version)
    {
        Il2CppSystem.IO.MemoryStream stream = new Il2CppSystem.IO.MemoryStream();
        Il2CppSystem.IO.BinaryWriter writer = new Il2CppSystem.IO.BinaryWriter(stream);
        tile.Serialize(writer, version);
        writer.Flush();
        return stream.ToArray().ToArray();
    }

    [HideFromIl2Cpp]
    private static TileData DeserializeTile(byte[] data, int version)
    {
        Il2CppSystem.IO.BinaryReader reader = new Il2CppSystem.IO.BinaryReader(new Il2CppSystem.IO.MemoryStream((Il2CppStructArray<byte>)data));
        TileData tile = new TileData();
        tile.Deserialize(reader, version);
        return tile;
    }
}

public class MapMakerResizeCommand : CommandBase
{
    private int size;

    public MapMakerResizeCommand(IntPtr pointer) : base(pointer) {}

    public MapMakerResizeCommand() : base(ClassInjector.DerivedConstructorPointer<MapMakerResizeCommand>())
    {
        ClassInjector.DerivedConstructorBody(this);
    }

    public override CommandType GetCommandType()
    {
        return Commands.ResizeType;
    }

    [HideFromIl2Cpp]
    internal static MapMakerResizeCommand Create(GameState state, int size)
    {
        MapMakerResizeCommand command = new MapMakerResizeCommand();
        command.PlayerId = state.CurrentPlayer;
        command.size = size;
        return command;
    }

    [HideFromIl2Cpp]
    public void Apply(GameState state)
    {
        Main.ResizeMap(ref state, size);
    }

    [HideFromIl2Cpp]
    public void Write(Il2CppSystem.IO.BinaryWriter writer, int version)
    {
        writer.Write(size);
    }

    [HideFromIl2Cpp]
    public void Read(Il2CppSystem.IO.BinaryReader reader, int version)
    {
        size = reader.ReadInt32();
    }
}
