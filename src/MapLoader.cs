using System.Text.Json.Serialization;
using PolytopiaBackendBase.Game;
using System.Text.Json;
using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Common;
using PolyMod.Managers;
using PolyMod;
using Il2CppInterop.Runtime;
using Unity.Collections;

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
        [JsonInclude]
        public List<string> capitals = new(); // Format: "(1,1).ID"
    }
    internal const uint MAX_MAP_SIZE = 100;
    internal const uint MIN_MAP_SIZE = 3;
    internal static readonly string MAPS_PATH = Path.Combine(PolyMod.Plugin.BASE_PATH, "Maps");
    internal static List<MapInfo> maps = new();
    internal static MapInfo? chosenMap;
    internal static bool inMapMaker = false; //my stuff was failing due to level not being loaded, so uhhhh, thats a problem though
    internal const string DEFAULT_MAP_NAME_KEY = "mapmaker.map.untitled";

    public static void Init()
    {
        inMapMaker = true;
        MapMaker.currCapitals = new Dictionary<byte, WorldCoordinates>();
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
        // UIBlackFader.FadeIn(0.5f, DelegateSupport.ConvertDelegate<Il2CppSystem.Action>((Action)CreateGame), "gamesettings.creatingworld", null, null); Can someone make this thing working pls?

        GameManager.Instance.CreateSinglePlayerGame();

        int num = 0;
        for (int y = 0; y < (int)GameManager.GameState.Map.Height; y++)
        {
            for (int x = 0; x < (int)GameManager.GameState.Map.Width; x++)
            {
                GameManager.GameState.Map.Tiles[num++] = GetBasicTile(x, y);
            }
        }
        SetLighthouses(GameManager.GameState);
        RevealMap(GameManager.LocalPlayer.Id);
        UIManager.Instance.BlockHints(); // Uhhhh shouldnt it block suggestions but it doesnt. Later...
    }

    public static void RevealMap(byte playerId)
    {
        for (int i = 0; i < GameManager.GameState.Map.Tiles.Length; i++)
        {
            GameManager.GameState.Map.Tiles[i].SetExplored(playerId, true);
        }
        GameManager.GameState.Map.GenerateShoreLines();
        MapRenderer.Current.Refresh(false);
    }

    public static TileData GetBasicTile(int x, int y)
    {
        return new TileData
        {
            coordinates = new WorldCoordinates(x, y),
            terrain = Polytopia.Data.TerrainData.Type.Field,
            climate = 0,
            altitude = 1,
            improvement = null,
            resource = null,
            owner = 0
        };
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ReturnToMenu))]
    private static void GameManager_ReturnToMenu()
    {
        if(inMapMaker)
        {
            inMapMaker = false;
            UIManager.Instance.UnblockHints();
        }
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
        chosenMap = null;
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

    public static void LoadMapInState(ref GameState gameState)
    {
        if (chosenMap == null)
        {
            return;
        }
        List<TileData> newTiles = new();
        for (int y = 0; y < chosenMap.size; y++)
        {
            for (int x = 0; x < chosenMap.size; x++)
            {
                WorldCoordinates worldCoordinates = new WorldCoordinates(x, y);
                int index = worldCoordinates.X + worldCoordinates.Y * chosenMap.size;
                MapTile customTile = chosenMap.map[index];
                TileData tile = new TileData();
                tile.coordinates = worldCoordinates;
                tile.climate = (customTile.climate < 0 || customTile.climate > 16) ? 1 : customTile.climate;
                tile.Skin = customTile.skinType;
                tile.terrain = customTile.terrain;
                tile.resource = customTile.resource == null ? null : new() { type = (Polytopia.Data.ResourceData.Type)customTile.resource };
                tile.effects = new Il2CppSystem.Collections.Generic.List<TileData.EffectType>();
                foreach (TileData.EffectType effect in customTile.effects)
                {
                    tile.effects.Add(effect);
                }
                switch(customTile.improvement)
                {
                    case null:
                    case ImprovementData.Type.LightHouse:
                        tile.improvement = null;
                        break;
                    case ImprovementData.Type.City:
                        tile.improvement = new ImprovementState
                        {
                            type = ImprovementData.Type.City,
                            founded = 0,
                            level = 1,
                            borderSize = 1,
                            production = 1
                        };
                        break;
                    default:
                        tile.improvement = new() { type = (Polytopia.Data.ImprovementData.Type)customTile.improvement };
                        break;
                }
                newTiles.Add(tile);
            }
            gameState.Map.width = chosenMap.size;
            gameState.Map.height = chosenMap.size;
            gameState.Map.tiles = newTiles
                .OrderBy(t => t.coordinates.y)
                .ThenBy(t => t.coordinates.x)
                .ToList().ToArray();

            SetLighthouses(gameState);
        }
    }

    public static void SetLighthouses(GameState gameState)
    {
        foreach(TileData tileData in gameState.Map.Tiles)
        {
            if(tileData.HasImprovement(ImprovementData.Type.LightHouse))
                tileData.improvement = null;
        }
        WorldCoordinates[] corners = ExploreLightHouseTask.GetCorners(gameState);
        foreach (var corner in corners)
        {
            TileData lighthouseTile = gameState.Map.GetTile(corner);
            if(lighthouseTile != null)
            {
                lighthouseTile.improvement = new()
                {
                    type = ImprovementData.Type.LightHouse,
                    borderSize = 0,
                    level = 1,
                    production = 0,
                    founded = 0
                };

                if (!lighthouseTile.IsWater)
                {
                    lighthouseTile.terrain = Polytopia.Data.TerrainData.Type.Field;
                }
            }
        }
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
        
        if(mapInfo?.capitals != null) MapMaker.currCapitals = ConvertCapitalList(mapInfo.capitals);
        foreach(var kvp in MapMaker.currCapitals)
        {
            Main.modLogger!.LogMessage(kvp.Key + " | "+ kvp.Value);
        }
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
            if (tileData.improvement != null && !tileData.HasImprovement(ImprovementData.Type.LightHouse))
            {
                mapTile.improvement = tileData.improvement.type;
            }
            if (tileData.effects.Count > 0)
            {
                mapTile.effects = tileData.effects.ToArray().ToList();
            }
            mapTiles.Add(mapTile);
        }
        List<string> Capitals = CapitalsToString(MapMaker.currCapitals);
        MapInfo mapInfo = new MapInfo
        {
            size = size,
            map = mapTiles,
            capitals = Capitals
        };
        File.WriteAllTextAsync(
            Path.Combine(MAPS_PATH, name),
            JsonSerializer.Serialize(mapInfo, new JsonSerializerOptions { WriteIndented = true })
        );
    }

    #region CapitalUtils
    ////
    /// Capitals are saved into a string list in this exact format:
    /// COORDS.PlayerByte
    /// ex: (1,1).1
    public static Dictionary<byte, WorldCoordinates> ConvertCapitalList(List<string> entries)
    {
        Dictionary<byte, WorldCoordinates> kvp = new Dictionary<byte, WorldCoordinates>();
        foreach(string entry in entries)
        {
            string[] entrysplit = entry.Split('.');
            if(entrysplit.Length != 2) continue;
            WorldCoordinates coords = InverseToString(entrysplit[0]);
            if(coords == WorldCoordinates.NULL_COORDINATES) continue;
            if(byte.TryParse(entrysplit[1], out byte res))
                kvp[res] = coords;
            else {Main.modLogger!.LogError("Invalid player id found in capitals. Ignoring...");}
        }
        return kvp;
    }

    public static WorldCoordinates InverseToString(string coords)
    {
        var xyUnclean = coords.Split(',');
        if(xyUnclean.Length != 2) return WorldCoordinates.NULL_COORDINATES;
        var x = xyUnclean[0].Trim('(');
        var y = xyUnclean[1].Trim(')');
        if(int.TryParse(x, out int X) && int.TryParse(y, out int Y))
        {
            WorldCoordinates res;
            res.x = X;
            res.y = Y;
            return res;
        }
        return WorldCoordinates.NULL_COORDINATES;
    }

    public static List<string> CapitalsToString(Dictionary<byte, WorldCoordinates> dict)
    {
        List<string> entries = new List<string>();
        foreach(var kvp in dict)
        {
            entries.Add(kvp.Value.ToString() + "." + kvp.Key.ToString());
        }
        return entries;
    }
    #endregion
}