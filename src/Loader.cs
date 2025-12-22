using System.Text.Json;
using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager;

public static class Loader
{
    internal const uint MAX_MAP_SIZE = 100;
    internal const uint MIN_MAP_SIZE = 3;
    internal static List<Data.MapInfo> maps = new();
    internal static Data.MapInfo? chosenMap;
    internal const string DEFAULT_MAP_NAME_KEY = "mapmaker.map.untitled";

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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
    private static void MapGenerator_Generate_Postfix(ref GameState state, ref MapGeneratorSettings settings)
    {
        if(chosenMap != null)
        {
            LoadMapInState(ref state, chosenMap);
            chosenMap = null;
        }
    }

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