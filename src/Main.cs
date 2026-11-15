using BepInEx.Logging;
using HarmonyLib;
using PolytopiaBackendBase.Game;
using PolytopiaMapManager.Popup;
using PolytopiaBackendBase.Common;
using Il2CppInterop.Runtime;
using DG.Tweening;
using Polytopia.Data;

namespace PolytopiaMapManager;
public static class Main
{
    private static string mapName = "";
    private static bool unsavedChanges = false;
    public static bool MapSaved = false;
    public static string MapName
    {
        set
        {
            if(!string.IsNullOrEmpty(value))
            {
                mapName = value;
                RefreshMapNameLabel();
            }
        }
        get
        {
            return mapName;
        }
    }
    private static bool edited = false;
    internal static bool HasUnsavedEdits => isActive && unsavedChanges && edited;
    public static bool UnsavedChanges
    {
        set
        {
            edited = value;
            if(unsavedChanges == value)
                return;
            unsavedChanges = value;
            RefreshMapNameLabel();
        }
        get
        {
            return unsavedChanges;
        }
    }
    public static string DisplayMapName => unsavedChanges ? mapName + "*" : mapName;

    internal static void RefreshMapNameLabel()
    {
        if(UI.Editor.mapNameContainer != null)
            UI.Editor.mapNameContainer.GetComponent<TMPLocalizer>().Text = DisplayMapName;
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
        Directory.CreateDirectory(IO.MAPS_PATH);
        Harmony.CreateAndPatchAll(typeof(Main));
        Harmony.CreateAndPatchAll(typeof(Loader));
        Harmony.CreateAndPatchAll(typeof(Brush));
        Harmony.CreateAndPatchAll(typeof(UI.Editor));
        Harmony.CreateAndPatchAll(typeof(UI.CapitalMarkers));
        Harmony.CreateAndPatchAll(typeof(UI.Menu.Start));
        Harmony.CreateAndPatchAll(typeof(UI.Menu.GameSetup));
        Harmony.CreateAndPatchAll(typeof(UI.Menu.GameSetup_UI2));
        Harmony.CreateAndPatchAll(typeof(CustomInput));
        PolyMod.Loader.AddPatchDataType("mapPreset", typeof(MapPreset));
        PolyMod.Loader.AddPatchDataType("mapSize", typeof(MapSize));
        PolyMod.Loader.AddPatchDataType("gameType", typeof(GameType));
        PolyMod.Loader.AddPatchDataType("gameMode", typeof(GameMode));
        Harmony.CreateAndPatchAll(typeof(UI.MapIconCache));
        Commands.Register();
        Harmony.CreateAndPatchAll(typeof(Commands));
        UI.MapIconCache.StartPreload();
    }

    public static void Init()
    {
        isActive = true;
        MapName = Localization.Get(Loader.DEFAULT_MAP_NAME_KEY);
        MapSaved = false;
        UnsavedChanges = true;
        edited = false;
        Brush.PaintMode = false;
        GameSettings gameSettings = new GameSettings();
        gameSettings.GameName = "Map Maker";
        gameSettings.GameType = EnumCache<GameType>.GetType("mapmaker");
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
        Il2CppSystem.Nullable<Il2CppSystem.Guid> awaitedLobbyId = new();

        UIBlackFader.FadeIn(
            0.5f, DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(StartGame),
            "gamesettings.creatingworld", null, color, awaitedLobbyId
        );

        void StartGame()
        {
            DOTween.KillAll(false);
            if (!GameManager.Instance.isLoadingGame)
            {
                GameManager.Instance.SetLoadingGame(isLoading: true);
                GameManager.Instance.client = new LocalClient();
                GameManager.Instance.settings.mapPreset = (
                    (GameManager.Instance.settings.mapPreset == MapPreset.None) ?
                    MapPreset.Continents : GameManager.Instance.settings.mapPreset
                );
                if (
                    CreateMapMakerSession(
                        GameManager.Instance.client,
                        GameManager.Instance.settings,
                        Il2CppSystem.Guid.NewGuid()
                    ) == CreateSessionResult.Success)
                {
                    GameManager.Instance.LoadLevel();
                    return;
                }
            }
        }
    }

    private static CreateSessionResult CreateMapMakerSession(ClientBase client, GameSettings settings, Il2CppSystem.Guid gameId)
    {
        client.Reset();
        GameManager.Client.gameId = gameId;

        modLogger!.LogInfo($"Creating map maker session with key: {gameId.ToString()}");
        PlayerState ownPlayerState = new PlayerState
        {
            Id = 1,
            AccountId = new(Il2CppSystem.Guid.Empty),
            AutoPlay = false,
            UserName = AccountManager.AliasInternal,
            tribe = GameManager.StartingTribe,
            tribeMix = GameManager.StartingTribeMix,
            skinType = GameManager.StartingSkin,
            hasChosenTribe = true
        };
        
        GameState gameState = CreateMapMakerGame(VersionManager.GameVersion, settings, ownPlayerState);
        SerializationHelpers.FromByteArray<GameState>(SerializationHelpers.ToByteArray(gameState, gameState.Version), out GameState initialGameState);
        GameManager.Client.initialGameState = initialGameState;
        modLogger!.LogInfo("Session with map maker created successfully");
        gameState.CommandStack.Add(new StartMatchCommand(1));
        GameManager.Client.hasInitializedSaveData = true;
        GameManager.Client.UpdateGameStateImmediate(gameState, StateUpdateReason.GameCreated);
        GameManager.Client.PrepareSession();

        return CreateSessionResult.Success;
	}

	public static GameState CreateMapMakerGame(int gameVersion, GameSettings settings, PlayerState ownPlayerState)
	{
		GameState gameState = new GameState
		{
			Version = gameVersion,
			Settings = settings,
			PlayerStates = new Il2CppSystem.Collections.Generic.List<PlayerState>(),
            Seed = 0
		};

		gameState.PlayerStates.Add(ownPlayerState);
		GameStateUtils.SetPlayerColors(gameState);
		GameStateUtils.AddNaturePlayer(gameState);
		ushort mapWidth = (ushort)Math.Max(settings.MapSize, MapDataExtensions.GetMinimumMapSize(gameState.PlayerCount));
		gameState.Map = new MapData(mapWidth, mapWidth);
        int tileCount = 0;
        for (int y = 0; y < (int)gameState.Map.Height; y++)
        {
            for (int x = 0; x < (int)gameState.Map.Width; x++)
            {
                gameState.Map.Tiles[tileCount++] = Loader.GetBasicTile(x, y);
            }
        }
        Loader.SetLighthouses(gameState);
		return gameState;
	}

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    private static void GameManager_Update()
    {
        if(isActive && GameManager.Instance.isLevelLoaded
            && !UIManager.Instance.advisorManager.areHintsBlocked)
            UIManager.Instance.BlockHints();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartMatchReaction), nameof(StartMatchReaction.DoWelcomeCinematic))]
    private static bool StartMatchReaction_DoWelcomeCinematic(StartMatchReaction __instance, Il2CppSystem.Action onComplete)
    {
        if (!isActive)
            return true;

        MapData map = GameManager.GameState.Map;
        CameraController.Instance.CenterOnPosition(new WorldCoordinates(map.Width / 2, map.Height / 2).ToPosition(), 0f);
        __instance.StartGame(onComplete);
        return false;
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
    private static void GameManager_ReturnToMenu(bool __runOriginal)
    {
        if(!__runOriginal)
            return;

        UI.Editor.pickers.Clear();

        if(!isActive)
            return;

        isActive = false;
        Loader.chosenMap = null;
        currCapitals.Clear();
        History.Clear();
        UIManager.Instance.UnblockHints();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ClientBase), nameof(ClientBase.SaveSession))]
    private static bool ClientBase_SaveSession(ClientBase __instance, string gameId, bool showSaveErrorPopup = false)
    {
        if (__instance.GameState == null || __instance.GameState.Settings == null)
            return true;
        return __instance.GameState.Settings.BaseGameMode != EnumCache<GameMode>.GetType("mapmaker");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.TryGenerateMapFromIslands))]
    private static bool MapGenerator_TryGenerateMapFromIslands(ref bool __result, MapData map, MapGeneratorSettings settings)
    {
        if (settings.mapType != MapPreset.Continents)
            return true;

        int tileCount = map.Tiles.Length;
        for (int continents = 2; continents < 8; continents++)
        {
            float landPerContinent = (float)(tileCount / continents) * (1f - settings.wetness);
            if (landPerContinent >= 30f && landPerContinent <= 200f)
                return true;
        }

        modLogger!.LogWarning($"Map {map.Width}x{map.Height} is too big for the Continents generator, falling back to the default generator");
        __result = false;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LocalSaveFileUtils), nameof(LocalSaveFileUtils.DeleteAllSaveFilesOfType))]
	private static bool LocalSaveFileUtils_DeleteAllSaveFilesOfType(GameType gameType, bool localOnly)
	{
		return gameType != EnumCache<GameType>.GetType("mapmaker");
	}

    [HarmonyPrefix]
    [HarmonyPatch(typeof(LocalSaveFileUtils), nameof(LocalSaveFileUtils.DeleteSaveFile))]
	private static bool LocalSaveFileUtils_DeleteSaveFile(GameType gameType, string gameId, bool localOnly)
	{
		return gameType != EnumCache<GameType>.GetType("mapmaker");
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