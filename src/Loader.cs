using System.Text.Json;
using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Common;
using PolytopiaMapManager.Data;
using UnityEngine;
using UnityEngine.EventSystems;

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
    private static void MapGenerator_Generate_Postfix(MapGenerator __instance, ref GameState state, ref MapGeneratorSettings settings)
    {
        if(chosenMap != null)
        {
            LoadMapInState(ref state, chosenMap);
            if (!Main.isActive)
            {
                var capitals = chosenMap.capitals;
                GenerateCapitals(state, __instance, capitals);
            }
            chosenMap = null;
        }
    }

    # region CAPITAL SPAWNING
    /// ONLY ON VILLAGE TILES
    static bool FailedGen = false;

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
        List<TileData> validTiles = new List<TileData>();
        foreach (TileData tile in gameState.Map.Tiles)
        {
            if (GetCapital(tile.coordinates, capitals) == null && tile.improvement != null && tile.improvement.type == ImprovementData.Type.City)
            {
                validTiles.Add(tile);
            }
        }
        Console.Write("//////////////////////////////");
        Console.Write("//////////////////////////////");
        Console.Write(validTiles.Count);
        Console.Write(gameState.PlayerCount);
        Console.Write(capitals.Count);
        Console.Write("//////////////////////////////");
        Console.Write("//////////////////////////////");
        if (validTiles.Count < gameState.PlayerCount - capitals.Count)
        {
            Main.modLogger!.LogError("More players than cities!");
            FailedGen = true;
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
    }

    public static void SetTileAsCapital(MapGenerator mapGenerator, PlayerState playerState, GameState gameState, WorldCoordinates coordinates)
    {
        TileData tile = gameState.Map.GetTile(coordinates);
        mapGenerator.SetTileAsCapital(gameState, playerState, tile);
        int idx = tile.coordinates.X + tile.coordinates.Y * gameState.Map.width;
        tile.climate = chosenMap!.map[idx].climate;
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