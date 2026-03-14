using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Common;
using PolytopiaMapManager.Data;
using UnityEngine.EventSystems;

namespace PolytopiaMapManager;

public static class Loader
{
    internal const uint MAX_MAP_SIZE = 100;
    internal const uint MIN_MAP_SIZE = 3;
    internal static List<Data.MapInfo> maps = new();
    internal static Data.MapInfo? chosenMap;
    internal const string DEFAULT_MAP_NAME_KEY = "mapmaker.map.untitled";

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

    public static int GetOldClimateFromTribeType(TribeType tribeType)
    {
        switch(tribeType)
        {
            case TribeType.Xinxi:
                return 1;
            case TribeType.Imperius:
                return 2;
            case TribeType.Bardur:
            case TribeType.None:
            case TribeType.Nature:
                return 3;
            case TribeType.Oumaji:
                return 4;
            case TribeType.Kickoo:
                return 5;
            case TribeType.Hoodrick:
                return 6;
            case TribeType.Luxidoor:
                return 7;
            case TribeType.Vengir:
                return 8;
            case TribeType.Zebasi:
                return 9;
            case TribeType.Aimo:
                return 10;
            case TribeType.Aquarion:
                return 11;
            case TribeType.Quetzali:
                return 12;
            case TribeType.Elyrion:
                return 13;
            case TribeType.Yadakk:
                return 14;
            case TribeType.Polaris:
                return 15;
            case TribeType.Cymanti:
                return 16;
            default:
                return MapTile.DEFAULT_CLIMATE;
        }
    }

    public static TribeType GetTribeTypeFromOldClimate(int oldClimate)
    {
        switch(oldClimate)
        {
            case 1:
                return TribeType.Xinxi;
            case 2:
                return TribeType.Imperius;
            case 3:
                return TribeType.Bardur;
            case 4:
                return TribeType.Oumaji;
            case 5:
                return TribeType.Kickoo;
            case 6:
                return TribeType.Hoodrick;
            case 7:
                return TribeType.Luxidoor;
            case 8:
                return TribeType.Vengir;
            case 9:
                return TribeType.Zebasi;
            case 10:
                return TribeType.Aimo;
            case 11:
                return TribeType.Aquarion;
            case 12:
                return TribeType.Quetzali;
            case 13:
                return TribeType.Elyrion;
            case 14:
                return TribeType.Yadakk;
            case 15:
                return TribeType.Polaris;
            case 16:
                return TribeType.Cymanti;
            default:
                return MapTile.DEFAULT_TRIBE;
        }
    }

    internal static TribeType GetTribeClimateFromSkin(SkinType skinType, GameLogicData gameLogicData)
    {
        foreach (TribeData tribeData in gameLogicData.GetAllTribes().ToArray())
        {
            if (tribeData.skins.Contains(skinType))
            {
                return tribeData.type;
            }
        }
        return MapTile.DEFAULT_TRIBE;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
    private static bool MapGenerator_Generate_Prefix(MapGenerator __instance, GameState state, MapGeneratorSettings settings)
    {
        if(chosenMap == null)
            return true;

        state.Seed = CreateSeed(chosenMap);

        LoadMapInState(ref state, chosenMap);
        GenerateCapitals(state, __instance, chosenMap.capitals);

        chosenMap = null;

        return false;
    }

    public static int CreateSeed(MapInfo mapInfo)
    {
        var options = new JsonSerializerOptions
        {
            IncludeFields = true,
            WriteIndented = false
        };

        string json = JsonSerializer.Serialize(mapInfo, options);

        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));

        return BitConverter.ToInt32(hash, 0);
    }

    # region CAPITAL SPAWNING
    /// ONLY ON VILLAGE TILES
    static bool FailedGen = false;
    static bool inCustomGen = false;

    public static Capital? GetCapital(WorldCoordinates coordinates, List<Data.Capital> capitals)
    {
        foreach (var capital in capitals)
        {
            if (capital.coordinates == coordinates) return capital;
        }
        return null;
    }

    public static Capital? GetCapital(byte player, List<Data.Capital> capitals)
    {
        foreach (var capital in capitals)
        {
            if (capital.player == player) return capital;
        }
        return null;
    }

    public static void GenerateCapitals(GameState gameState, MapGenerator mapGenerator, List<Data.Capital> capitals)
    {
        FailedGen = false;
        inCustomGen = true;
        List<TileData> validTiles = new List<TileData>();
        foreach (TileData tile in gameState.Map.Tiles)
        {
            if (GetCapital(tile.coordinates, capitals) == null && tile.improvement != null && tile.improvement.type == ImprovementData.Type.City)
            {
                validTiles.Add(tile);
            }
        }
        if (validTiles.Count < gameState.PlayerCount - capitals.Count)
        {
            Main.modLogger!.LogError("More players than cities!");
            FailedGen = true;
            inCustomGen = false;
            return;
        }
        foreach (PlayerState playerState in gameState.PlayerStates)
        {
            if (playerState == null || playerState.Id == 0 || playerState.Id == 255) continue;
            Capital? capital = GetCapital(playerState.Id, capitals);
            if (capital != null)
            {
                SetTileAsCapital(mapGenerator, playerState, gameState, capital.coordinates);
            }
            else
            {
                System.Random rng = new System.Random(gameState.Seed);
                int num = rng.Next(0, validTiles.Count);
                SetTileAsCapital(mapGenerator, playerState, gameState, validTiles[num].coordinates);
                validTiles.Remove(validTiles[num]);
            }
        }
        inCustomGen = false;
    }

    public static void SetTileAsCapital(MapGenerator mapGenerator, PlayerState playerState, GameState gameState, WorldCoordinates coordinates)
    {
        TileData tile = gameState.Map.GetTile(coordinates);
        mapGenerator.SetTileAsCapital(gameState, playerState, tile);
        int idx = tile.coordinates.X + tile.coordinates.Y * gameState.Map.width;
        tile.climate = chosenMap!.map[idx].tribeType;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartMatchReaction), nameof(StartMatchReaction.DoWelcomeCinematic))]
    public static bool FailedGenSkip(Il2CppSystem.Action onComplete)
    {
        if (FailedGen && !Main.isActive)
        {
            BasicPopup popup = PopupManager.GetBasicPopup();
            popup.Header = "Not enough cities!";
            popup.Description = "You must have at least as many cities on the map as players!";
            popup.buttonData = new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null)
            };
            void Exit(int id, BaseEventData eventData)
            {
                GameManager.ReturnToMenu();
            }
            popup.Show();
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerExtensions), nameof(PlayerExtensions.GetScore), typeof(PlayerState), typeof(GameState))]
    public static void ScoreFix(this PlayerState player, GameState state, ref ScoreDetails __result)
    {
        if(chosenMap != null && !inCustomGen) __result.totalScore = 0;
    }


    #endregion

    public static void LoadMapInState(ref GameState gameState, Data.MapInfo map)
    {
        List<TileData> tiles = new();
        for (int y = 0; y < map.size; y++)
        {
            for (int x = 0; x < map.size; x++)
            {
                WorldCoordinates worldCoordinates = new WorldCoordinates(x, y);
                int index = worldCoordinates.X + worldCoordinates.Y * map.size;
                Data.MapTile customTile = map.map[index];

                TileData tileData = customTile.ToTileData();
                tileData.coordinates = worldCoordinates;
                tiles.Add(tileData);
            }
        }

        gameState.Map.width = map.size;
        gameState.Map.height = map.size;
        gameState.Map.tiles = tiles
            .OrderBy(t => t.coordinates.y)
            .ThenBy(t => t.coordinates.x)
            .ToList().ToArray();

        SetLighthouses(gameState);
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
}