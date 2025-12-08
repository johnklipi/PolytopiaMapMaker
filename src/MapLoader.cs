using System.Text.Json.Serialization;
using PolytopiaBackendBase.Game;
using System.Text.Json;
using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Common;
using PolyMod.Managers;

namespace PolytopiaMapManager;

public static class MapLoader
{
    public class MapTile
    {
        [JsonInclude]
        public int climate = 0;
        [JsonInclude]
        [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<SkinType>))]
        public SkinType skinType = SkinType.Default;
        [JsonInclude]
        [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<Polytopia.Data.TerrainData.Type>))]
        public Polytopia.Data.TerrainData.Type terrain = Polytopia.Data.TerrainData.Type.Field;
        [JsonInclude]
        [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<Polytopia.Data.ResourceData.Type>))]
        public Polytopia.Data.ResourceData.Type? resource;
        [JsonInclude]
        [JsonConverter(typeof(PolyMod.Json.EnumCacheJson<Polytopia.Data.ImprovementData.Type>))]
        public Polytopia.Data.ImprovementData.Type? improvement;
        [JsonInclude]
        [JsonConverter(typeof(PolyMod.Json.EnumCacheListJson<TileData.EffectType>))]
        public List<TileData.EffectType> effects = new();
    }

    public class MapInfo
    {
        [JsonInclude]
        public ushort size;
        [JsonInclude]
        public List<MapTile> map = new();
    }
    internal const uint MAX_MAP_SIZE = 100;
    internal static readonly string MAPS_PATH = Path.Combine(PolyMod.Plugin.BASE_PATH, "Maps");
    internal static List<MapInfo> maps = new();
    internal static MapInfo? chosenMap;
    internal static bool inMapMaker = false; //my stuff was failing due to level not being loaded, so uhhhh, thats a problem though
    internal const string DEFAULT_MAP_NAME_KEY = "mapmaker.map.untitled";

    public static void Init()
    {
        inMapMaker = true;
        MapMaker.MapName = Localization.Get(DEFAULT_MAP_NAME_KEY);
        MapMaker.MapSaved = false;
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
        //UIBlackFader.FadeIn(0.5f, DelegateSupport.ConvertDelegate<Il2CppSystem.Action>((Action)CreateGame), "gamesettings.creatingworld", null, null);

        GameManager.Instance.CreateSinglePlayerGame();

        int num = 0;
        for (int j = 0; j < (int)GameManager.GameState.Map.Height; j++)
        {
            for (int k = 0; k < (int)GameManager.GameState.Map.Width; k++)
            {
                TileData tileData = new TileData
                {
                    coordinates = new WorldCoordinates(k, j),
                    terrain = Polytopia.Data.TerrainData.Type.Field,
                    climate = 0,
                    altitude = 1,
                    improvement = null,
                    resource = null,
                    owner = 0
                };
                GameManager.GameState.Map.Tiles[num++] = tileData;
            }
        }
        for (int i = 0; i < GameManager.GameState.Map.Tiles.Length; i++)
        {
            GameManager.GameState.Map.Tiles[i].SetExplored(GameManager.LocalPlayer.Id, true);
        }
        MapRenderer.Current.Refresh(false);
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ReturnToMenu))]
    private static void GameManager_ReturnToMenu()
    {
        inMapMaker = false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CommandUtils), nameof(CommandUtils.GetBuildableImprovements))]
    private static bool CommandUtils_GetBuildableImprovements(ref Il2CppSystem.Collections.Generic.List<CommandBase> __result, GameState gameState, PlayerState player, TileData tile, bool includeUnavailable)
    {
        return true;
    }

    internal static int GetTribeClimateFromType(TribeType type, GameLogicData gameLogicData)
    {
        gameLogicData.TryGetData(type, out TribeData data);
        return data.climate;
    }

    internal static int GetTribeClimateFromSkin(SkinType skinType, GameLogicData gameLogicData)
    {
        List<TribeData> tribes = gameLogicData.GetTribes(TribeData.CategoryEnum.Human).ToArray().ToList().Concat(gameLogicData.GetTribes(TribeData.CategoryEnum.Special).ToArray().ToList()).ToList();
        foreach (TribeData tribeData in tribes)
        {
            if (tribeData.skins.Contains(skinType))
            {
                return tribeData.climate;
            }
        }
        return 1;
    }

    // [HarmonyPrefix]
    // [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
    // private static bool MapGenerator_Generate(ref GameState state, ref MapGeneratorSettings settings)
    // {
    //     return true;
    // }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
    private static void MapGenerator_Generate_Postfix(ref GameState state, ref MapGeneratorSettings settings)
    {
        LoadMapInState(ref state);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HudButtonBar), nameof(HudButtonBar.Init))]
    internal static void HudButtonBar_Init(HudButtonBar __instance, HudScreen hudScreen)
    {
        if (inMapMaker)
        {
            __instance.nextTurnButton.gameObject.SetActive(false);
            __instance.techTreeButton.gameObject.SetActive(false);
            __instance.statsButton.gameObject.SetActive(false);
            __instance.Show();
            __instance.Update();
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

    public static void LoadMapInState(ref GameState state)
    {
        if (chosenMap == null)
        {
            return;
        }
        MapData originalMap = state.Map;
        for (int i = 0; i < chosenMap.map.Count; i++)
        {
            TileData tile = originalMap.tiles[i];
            MapTile customTile = chosenMap.map[i];

            tile.climate = (customTile.climate < 0 || customTile.climate > 16) ? 1 : customTile.climate;
            tile.Skin = customTile.skinType;
            tile.terrain = customTile.terrain;
            tile.resource = customTile.resource == null ? null : new() { type = (Polytopia.Data.ResourceData.Type)customTile.resource };
            tile.effects = new Il2CppSystem.Collections.Generic.List<TileData.EffectType>();
            foreach (TileData.EffectType effect in customTile.effects)
            {
                tile.effects.Add(effect);
            }
            if (tile.rulingCityCoordinates != tile.coordinates)
            {
                tile.improvement = customTile.improvement == null ? null : new() { type = (Polytopia.Data.ImprovementData.Type)customTile.improvement };
                if (tile.improvement != null && tile.improvement.type == ImprovementData.Type.City)
                {
                    tile.improvement = new ImprovementState
                    {
                        type = ImprovementData.Type.City,
                        founded = 0,
                        level = 1,
                        borderSize = 1,
                        production = 1
                    };
                }
            }
            originalMap.tiles[i] = tile;
        }
        WorldCoordinates[] corners = ExploreLightHouseTask.GetCorners(state);
        for (int i = 0; i < corners.Length; i++)
        {
            TileData tile = originalMap.GetTile(corners[i]);
            tile.improvement = new ImprovementState
            {
                type = ImprovementData.Type.LightHouse,
                borderSize = 0,
                level = 1,
                production = 0,
                founded = 0
            };
            if (!tile.IsWater)
            {
                tile.terrain = Polytopia.Data.TerrainData.Type.Field;
            }
        }
        originalMap.GenerateShoreLines();
        chosenMap = null;
    }

    internal static MapInfo? LoadMapFile(string name)
    {
        string filePath = Path.Combine(MAPS_PATH, $"{name}.json");

        if (!File.Exists(filePath))
        {
            return null;
        }

        string json = File.ReadAllText(filePath);

        MapInfo? mapInfo = JsonSerializer.Deserialize<MapInfo>(json);

        return mapInfo;
    }

    internal static void BuildMapFile(string name, ushort size, List<TileData> tiles)
    {
        List<MapTile> mapTiles = new();
        foreach (TileData tileData in tiles)
        {
            MapTile mapTile = new MapTile
            {
                climate = tileData.climate,
                skinType = tileData.Skin,
                terrain = tileData.terrain,
            };
            if (tileData.resource != null)
            {
                mapTile.resource = tileData.resource.type;
            }
            if (tileData.improvement != null)
            {
                mapTile.improvement = tileData.improvement.type;
            }
            if (tileData.effects.Count > 0)
            {
                mapTile.effects = tileData.effects.ToArray().ToList();
            }
            mapTiles.Add(mapTile);
        }
        MapInfo mapInfo = new MapInfo
        {
            size = size,
            map = mapTiles
        };
        File.WriteAllTextAsync(
            Path.Combine(MAPS_PATH, name),
            JsonSerializer.Serialize(mapInfo, new JsonSerializerOptions { WriteIndented = true })
        );
    }
}