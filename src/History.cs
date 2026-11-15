using Il2CppInterop.Runtime.InteropTypes.Arrays;
using PolytopiaBackendBase.Game;

namespace PolytopiaMapManager;

internal static class History
{
    internal const int MAX_STEPS = 30;

    private static Il2CppStructArray<byte>? baseState;
    private static int baseIndex;
    private static List<Data.Capital> baseCapitals = new();

    internal static int Count
    {
        get
        {
            GameState? state = GameManager.GameState;
            return baseState == null || state == null ? 0 : Math.Max(0, state.CommandStack.Count - baseIndex);
        }
    }

    internal static void Reset(GameState state)
    {
        baseState = SerializationHelpers.ToByteArray(state, state.Version);
        baseIndex = state.CommandStack.Count;
        baseCapitals = CloneCapitals(Main.currCapitals);
    }

    internal static void Clear()
    {
        baseState = null;
        baseIndex = 0;
        baseCapitals = new();
    }

    internal static void Record(CommandBase command)
    {
        GameState state = GameManager.GameState;
        if(baseState == null || state == null)
            return;
        if(!Commands.IsAvailable)
        {
            Main.modLogger!.LogWarning($"Command types {Commands.TILES_COMMAND} and {Commands.RESIZE_COMMAND} are not registered, check that patch.json is up to date. Revert is disabled.");
            return;
        }

        state.CommandStack.Add(command);
        state.CurrentCommand = (ushort)state.CommandStack.Count;
        if(Count > MAX_STEPS)
            Fold(state);
    }

    internal static void RecordTiles(GameState state, IEnumerable<WorldCoordinates> coordinates)
    {
        Record(MapMakerTilesCommand.Create(state, coordinates, Main.currCapitals));
    }

    internal static void RecordResize(GameState state, int size)
    {
        Record(MapMakerResizeCommand.Create(state, size));
    }

    internal static bool RewindOne()
    {
        return RewindTo(Count - 1);
    }

    internal static bool RewindTo(int steps)
    {
        if(steps < 0 || steps >= Count)
            return false;
        if(!TryBuildState(steps, out GameState state, out List<Data.Capital> capitals))
            return false;

        Main.currCapitals = capitals;
        GameManager.Client.UpdateGameState(state, StateUpdateReason.Unknown);
        return true;
    }

    internal static bool TryBuildState(int steps, out GameState state, out List<Data.Capital> capitals)
    {
        capitals = CloneCapitals(baseCapitals);
        state = null!;
        GameState live = GameManager.GameState;
        if(live == null || steps < 0 || steps > Count || !TryLoadBase(out state))
            return false;

        Il2CppSystem.Collections.Generic.List<CommandBase> replay = new();
        for (int i = baseIndex; i < baseIndex + steps; i++)
        {
            CommandBase command = live.CommandStack[i];
            replay.Add(command);
            List<Data.Capital>? after = Commands.AsTiles(command)?.GetCapitalsAfter();
            if(after != null)
                capitals = CloneCapitals(after);
        }
        if(replay.Count > 0)
            new ActionManager(state).ExecuteCommands(replay);
        return true;
    }

    internal static List<Data.Capital> CloneCapitals(List<Data.Capital> capitals)
    {
        return capitals.Select(capital => new Data.Capital { player = capital.player, coordinates = capital.coordinates }).ToList();
    }

    private static void Fold(GameState live)
    {
        if(!TryBuildState(1, out GameState state, out List<Data.Capital> capitals))
            return;

        baseState = SerializationHelpers.ToByteArray(state, state.Version);
        baseCapitals = capitals;
        baseIndex++;
    }

    private static bool TryLoadBase(out GameState state)
    {
        state = null!;
        return baseState != null && SerializationHelpers.FromByteArray<GameState>(baseState, out state);
    }
}
