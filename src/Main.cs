using BepInEx.Logging;
using HarmonyLib;
using PolytopiaBackendBase.Game;
using UnityEngine.EventSystems;
using PolytopiaMapManager.Popup;
using PolytopiaBackendBase.Common;
using Il2CppInterop.Runtime;
using DG.Tweening;


namespace PolytopiaMapManager;
public static class Main
{
    private static string mapName = "";
    public static bool MapSaved = false;
    public static string MapName
    {
        set
        {
            if(!string.IsNullOrEmpty(value))
            {
                mapName = value;
                if(UI.Editor.mapNameContainer != null)
                {
                    UI.Editor.mapNameContainer.GetComponent<TMPLocalizer>().Text = value;
                }
            }
        }
        get
        {
            return mapName;
        }
    }
    internal static bool isActive = false;
    internal static ManualLogSource? modLogger;
    internal const int MAP_NAME_MAX_LENGTH = 12;
    public static void Load(ManualLogSource logger)
    {
        modLogger = logger;
        Harmony.CreateAndPatchAll(typeof(Main));
        Harmony.CreateAndPatchAll(typeof(Loader));
        Harmony.CreateAndPatchAll(typeof(Brush));
        Harmony.CreateAndPatchAll(typeof(UI.Editor));
        Harmony.CreateAndPatchAll(typeof(UI.Menu.Start));
        Harmony.CreateAndPatchAll(typeof(UI.Menu.GameSetup));
        Harmony.CreateAndPatchAll(typeof(UI.Picker.Manager));
        Harmony.CreateAndPatchAll(typeof(CustomInput));
        PolyMod.Loader.AddGameMode("mapmaker", (UIButtonBase.ButtonAction)OnMapMaker, false);
        PolyMod.Loader.AddPatchDataType("mapPreset", typeof(MapPreset));
        PolyMod.Loader.AddPatchDataType("mapSize", typeof(MapSize));
        Directory.CreateDirectory(IO.MAPS_PATH);

        static void OnMapMaker(int id, BaseEventData eventData)
        {
            Init();
        }
    }

    public static void Init()
    {
        isActive = true;
        MapName = Localization.Get(Loader.DEFAULT_MAP_NAME_KEY);
        MapSaved = false;
        GameSettings gameSettings = new GameSettings();
        gameSettings.BaseGameMode = EnumCache<GameMode>.GetType("mapmaker");
        gameSettings.SetUnlockedTribes(GameManager.GetPurchaseManager().GetUnlockedTribes(false));
        gameSettings.mapPreset = MapPreset.Dryland;
        gameSettings.MapSize = 16;
        GameManager.StartingTribe = EnumCache<TribeType>.GetType("mapmaker");
        GameManager.StartingTribeMix = TribeType.None;
        GameManager.StartingSkin = SkinType.Default;
        GameManager.PreliminaryGameSettings = gameSettings;
        GameManager.PreliminaryGameSettings.OpponentCount = 0;
        GameManager.PreliminaryGameSettings.Difficulty = BotDifficulty.Frozen;
        Il2CppSystem.Nullable<UnityEngine.Color> color = new(UnityEngine.Color.black);

        UIBlackFader.FadeIn(0.5f, DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(StartGame), "gamesettings.creatingworld", null, color);

        void StartGame()
        {
            DOTween.KillAll(false);
            GameManager.Instance.CreateSinglePlayerGame();
            int num = 0;
            for (int y = 0; y < (int)GameManager.GameState.Map.Height; y++)
            {
                for (int x = 0; x < (int)GameManager.GameState.Map.Width; x++)
                {
                    GameManager.GameState.Map.Tiles[num++] = Loader.GetBasicTile(x, y);
                }
            }
            Loader.SetLighthouses(GameManager.GameState);
            Loader.RevealMap(GameManager.LocalPlayer.Id);
            UIManager.Instance.BlockHints(); // Uhhhh it should block suggestions but it doesnt. Later...
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapDataExtensions), nameof(MapDataExtensions.GetMinimumMapSize))]
    public static void MapDataExtensions_GetMinimumMapSize(ref ushort __result, int players)
    {
        int blocksNeeded = (int)Math.Ceiling(Math.Sqrt(players));
        Console.Write("/////");
        Console.Write(blocksNeeded * 3);
        __result = (ushort)(blocksNeeded * 3);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ReturnToMenu))]
    private static void GameManager_ReturnToMenu()
    {
        if(isActive)
        {
            isActive = false;
            UIManager.Instance.UnblockHints();
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerExtensions), nameof(PlayerExtensions.CountCapitals))] // Game crashes otherwise.
	public static bool PlayerExtensions_CountCapitals(ref int __result, PlayerState player, GameState gameState)
	{
        if(isActive)
        {
            __result = 0;
        }
		return !isActive;
	}

    internal static void ResizeMap(ref GameState gameState, int size)
    {
        gameState.Settings.MapSize = size;
        List<TileData> tiles = new();
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                WorldCoordinates worldCoordinates = new WorldCoordinates(x, y);
                TileData? tileData = gameState.Map.GetTile(worldCoordinates);
                if(tileData == null)
                {
                    tileData = Loader.GetBasicTile(worldCoordinates.x, worldCoordinates.y);
                }
                tiles.Add(tileData);
            }
        }
        gameState.Map.tiles = tiles
            .OrderBy(t => t.coordinates.y)
            .ThenBy(t => t.coordinates.x)
            .ToList().ToArray();
        gameState.Map.width = (ushort)size;
        gameState.Map.height = (ushort)size;

        Loader.SetLighthouses(gameState);
    }

    public static void MapRenameValueChanged(string value, BasicPopup popup)
    {
        if(!string.IsNullOrEmpty(value))
        {
            bool isTooLong = value.Length > MAP_NAME_MAX_LENGTH;
            if (isTooLong)
            {
                value = value.Remove(value.Length - 1);
                var input = CustomInput.GetInputFromPopup(popup);
                if(input != null)
                {
                    input.text = value;
                }

                NotificationManager.Notify(Localization.Get("mapmaker.text.long", new Il2CppSystem.Object[]{ MAP_NAME_MAX_LENGTH }), Localization.Get("gamemode.mapmaker"));
            }
        }
    }

    public static WorldCoordinates GetTileCoordinates(int index, int width)
    {
        int x = index % width;
        int y = index / width;
        return new WorldCoordinates(x, y);
    }
}