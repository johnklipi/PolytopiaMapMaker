using BepInEx.Logging;
using HarmonyLib;
using PolytopiaBackendBase.Game;
using UnityEngine.EventSystems;
using PolytopiaMapManager.Popup;
using PolytopiaBackendBase.Common;
using Il2CppInterop.Runtime;
using DG.Tweening;
using Polytopia.Data;

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
    internal const int BASE_MAP_WIDTH = 16;
    internal const float CAMERA_MAXZOOM_CONSTANT = 1000;
    public static List<Data.Capital> currCapitals = new List<Data.Capital>();
    public static void Load(ManualLogSource logger)
    {
        modLogger = logger;
        Harmony.CreateAndPatchAll(typeof(Main));
        Harmony.CreateAndPatchAll(typeof(Loader));
        Harmony.CreateAndPatchAll(typeof(Brush));
        Harmony.CreateAndPatchAll(typeof(UI.Editor));
        Harmony.CreateAndPatchAll(typeof(UI.Menu.Start));
        Harmony.CreateAndPatchAll(typeof(UI.Menu.GameSetup));
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
        gameSettings.MapSize = BASE_MAP_WIDTH;
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
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    private static void GameManager_Update()
    {
        if(isActive && GameManager.Instance.isLevelLoaded
            && !UIManager.Instance.advisorManager.areHintsBlocked)
            UIManager.Instance.BlockHints();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Tile), nameof(Tile.TileIsHidden))]
    [HarmonyPatch(typeof(Tile), nameof(Tile.IsHidden), MethodType.Getter)]
    private static void Tile_TileIsHidden__IsHidden_Getter(ref bool __result)
    {
        if(__result && Main.isActive)
            __result = false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(CameraController), nameof(CameraController.Awake))]
    private static void CameraController_Awake()
    {
        CameraController.Instance.maxZoom = CAMERA_MAXZOOM_CONSTANT;
        CameraController.Instance.techViewBounds = new(
            new(CAMERA_MAXZOOM_CONSTANT, CAMERA_MAXZOOM_CONSTANT), CameraController.Instance.techViewBounds.size
        );
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TechView), nameof(TechView.OnEnable))]
    private static void TechView_OnEnable(TechView __instance)
    {
        __instance.techTreeContainer.parent.transform.position = new(CAMERA_MAXZOOM_CONSTANT, CAMERA_MAXZOOM_CONSTANT);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ReturnToMenu))]
    private static void GameManager_ReturnToMenu()
    {
        UI.Editor.pickers.Clear();

        if(!isActive)
            return;

        isActive = false;
        Loader.chosenMap = null;
        currCapitals.Clear();
        UIManager.Instance.UnblockHints();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ClientBase), nameof(ClientBase.SaveSession))]
    private static bool ClientBase_SaveSession(ClientBase __instance, string gameId, bool showSaveErrorPopup = false)
    {
        return __instance.GameState.Settings.BaseGameMode != EnumCache<GameMode>.GetType("mapmaker");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LocalSaveFileUtils), nameof(LocalSaveFileUtils.DeleteAllSaveFilesOfType))]
	private static bool LocalSaveFileUtils_DeleteAllSaveFilesOfType(GameType gameType, bool localOnly)
	{
        if(GameManager.GameState != null && GameManager.GameState.Settings != null)
		    return GameManager.GameState.Settings.BaseGameMode != EnumCache<GameMode>.GetType("mapmaker");
        return true;
	}

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LocalSaveFileUtils), nameof(LocalSaveFileUtils.DeleteSaveFile))]
	private static bool LocalSaveFileUtils_DeleteSaveFile(GameType gameType, string gameId, bool localOnly)
	{
		return GameManager.GameState.Settings.BaseGameMode != EnumCache<GameMode>.GetType("mapmaker");
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.GetAllTribes))]
    private static void GameLogicData_GetAllTribes(ref Il2CppSystem.Collections.Generic.List<TribeData> __result)
    {
        for (int i = 0; i < __result.Count; i++)
        {
            TribeData data = __result[i];
            if(data.type == EnumCache<TribeType>.GetType("mapmaker"))
            {
                __result.Remove(data);
                break;
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.GetAllTribeTypes))]
    private static void GameLogicData_GetAllTribeTypes(ref Il2CppSystem.Collections.Generic.List<TribeType> __result)
    {
        __result.Remove(EnumCache<TribeType>.GetType("mapmaker"));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.IsResourceVisibleToPlayer))]
    internal static void GameLogicData_IsResourceVisibleToPlayer(ref bool __result, ResourceData.Type resourceType, PlayerState player)
    {
        if (!__result && isActive)
            __result = true;
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