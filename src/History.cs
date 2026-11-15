using Il2CppInterop.Runtime.InteropTypes.Arrays;
using PolytopiaBackendBase.Game;

namespace PolytopiaMapManager;

internal abstract class EditorCommand
{
    internal abstract void Execute(GameState state, List<Data.Capital> capitals);
}

internal class TileChangesCommand : EditorCommand
{
    private readonly List<(WorldCoordinates coordinates, Il2CppStructArray<byte> data)> tiles = new();
    private readonly List<Data.Capital> capitals;
    private readonly int version;

    internal TileChangesCommand(GameState state, IEnumerable<WorldCoordinates> coordinates, List<Data.Capital> capitalsAfter)
    {
        version = state.Version;
        foreach (WorldCoordinates coords in coordinates)
        {
            TileData? tile = state.Map.GetTile(coords);
            if(tile != null)
                tiles.Add((coords, SerializeTile(tile, version)));
        }
        capitals = History.CloneCapitals(capitalsAfter);
    }

    internal override void Execute(GameState state, List<Data.Capital> capitals)
    {
        MapData map = state.Map;
        foreach (var (coordinates, data) in tiles)
        {
            if(coordinates.x < 0 || coordinates.y < 0 || coordinates.x >= map.Width || coordinates.y >= map.Height)
                continue;
            map.Tiles[coordinates.x + coordinates.y * map.Width] = DeserializeTile(data, version);
        }
        capitals.Clear();
        capitals.AddRange(History.CloneCapitals(this.capitals));
    }

    private static Il2CppStructArray<byte> SerializeTile(TileData tile, int version)
    {
        Il2CppSystem.IO.MemoryStream stream = new Il2CppSystem.IO.MemoryStream();
        Il2CppSystem.IO.BinaryWriter writer = new Il2CppSystem.IO.BinaryWriter(stream);
        tile.Serialize(writer, version);
        writer.Flush();
        return stream.ToArray();
    }

    private static TileData DeserializeTile(Il2CppStructArray<byte> data, int version)
    {
        Il2CppSystem.IO.BinaryReader reader = new Il2CppSystem.IO.BinaryReader(new Il2CppSystem.IO.MemoryStream(data));
        TileData tile = new TileData();
        tile.Deserialize(reader, version);
        return tile;
    }
}

internal class ResizeCommand : EditorCommand
{
    private readonly int size;

    internal ResizeCommand(int size)
    {
        this.size = size;
    }

    internal override void Execute(GameState state, List<Data.Capital> capitals)
    {
        Main.ResizeMap(ref state, size);
    }
}

internal static class History
{
    internal const int MAX_STEPS = 30;

    private static Il2CppStructArray<byte>? baseState;
    private static List<Data.Capital> baseCapitals = new();
    private static readonly List<EditorCommand> commands = new();

    internal static int Count => commands.Count;

    internal static void Reset(GameState state)
    {
        baseState = SerializationHelpers.ToByteArray(state, state.Version);
        baseCapitals = CloneCapitals(Main.currCapitals);
        commands.Clear();
    }

    internal static void Clear()
    {
        baseState = null;
        baseCapitals = new();
        commands.Clear();
    }

    internal static void Record(EditorCommand command)
    {
        if(baseState == null)
            return;

        commands.Add(command);
        if(commands.Count <= MAX_STEPS)
            return;

        if(!TryLoadBase(out GameState state, out List<Data.Capital> capitals))
            return;
        commands[0].Execute(state, capitals);
        commands.RemoveAt(0);
        baseState = SerializationHelpers.ToByteArray(state, state.Version);
        baseCapitals = capitals;
    }

    internal static void RecordTiles(GameState state, IEnumerable<WorldCoordinates> coordinates)
    {
        Record(new TileChangesCommand(state, coordinates, Main.currCapitals));
    }

    internal static bool RewindOne()
    {
        return RewindTo(commands.Count - 1);
    }

    internal static bool RewindTo(int commandCount)
    {
        if(commandCount < 0 || commandCount >= commands.Count)
            return false;
        if(!TryBuildState(commandCount, out GameState state, out List<Data.Capital> capitals))
            return false;

        commands.RemoveRange(commandCount, commands.Count - commandCount);
        Apply(state, capitals);
        return true;
    }

    internal static bool TryBuildState(int commandCount, out GameState state, out List<Data.Capital> capitals)
    {
        if(commandCount < 0 || commandCount > commands.Count || !TryLoadBase(out state, out capitals))
        {
            state = null!;
            capitals = new();
            return false;
        }

        for (int i = 0; i < commandCount; i++)
            commands[i].Execute(state, capitals);
        return true;
    }

    private static void Apply(GameState state, List<Data.Capital> capitals)
    {
        Main.currCapitals = capitals;
        GameManager.Client.UpdateGameState(state, StateUpdateReason.Unknown);
    }

    internal static List<Data.Capital> CloneCapitals(List<Data.Capital> capitals)
    {
        return capitals.Select(capital => new Data.Capital { player = capital.player, coordinates = capital.coordinates }).ToList();
    }

    private static bool TryLoadBase(out GameState state, out List<Data.Capital> capitals)
    {
        capitals = CloneCapitals(baseCapitals);
        state = null!;
        return baseState != null && SerializationHelpers.FromByteArray<GameState>(baseState, out state);
    }
}
