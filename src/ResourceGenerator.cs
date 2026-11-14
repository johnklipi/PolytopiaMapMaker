using Polytopia.Data;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager;

public static class ResourceGenerator
{
    private const float CITY_NEIGHBOUR_CHANCE = 1f;
    private const float BASE_CHANCE = 1f / 3f;
    private const int PLACEMENT_ROLL = 0;
    private const int PICK_ROLL = 1;
    private const int FIRST_RESOURCE_ROLL = 2;

    public static int Generate(GameState gameState, bool replaceExisting)
    {
        MapData map = gameState.Map;
        GameLogicData gameLogicData = gameState.GameLogicData;

        List<TileData> tiles = map.Tiles.ToArray().ToList();
        XXHash hash = new XXHash(Loader.CreateSeed(IO.BuildMap(map.Width, tiles, Main.currCapitals)));

        List<ResourceData> resources = new();
        foreach (ResourceData resource in gameLogicData.GetAllResources())
            resources.Add(resource);

        if (replaceExisting)
        {
            foreach (TileData tile in tiles)
            {
                if (tile.improvement == null)
                    tile.resource = null;
            }
        }

        HashSet<int> cityNeighbours = new();
        foreach (TileData tile in tiles)
        {
            if (!tile.HasImprovement(ImprovementData.Type.City))
                continue;
            foreach (TileData neighbour in map.GetTileNeighbors(tile.coordinates))
                cityNeighbours.Add(neighbour.coordinates.X + neighbour.coordinates.Y * map.Width);
        }

        Dictionary<TribeType, WorldContinent> climateOdds = new();
        int placed = 0;
        foreach (TileData tile in tiles)
        {
            if (tile.improvement != null || tile.resource != null)
                continue;

            int x = tile.coordinates.X;
            int y = tile.coordinates.Y;
            float chance = cityNeighbours.Contains(x + y * map.Width) ? CITY_NEIGHBOUR_CHANCE : BASE_CHANCE;
            if (hash.Value(x, y, PLACEMENT_ROLL) >= chance)
                continue;

            ResourceData? resource = PickResource(map, tile, resources, GetClimateOdds(gameLogicData, climateOdds, tile.climate), hash);
            if (resource == null)
                continue;

            tile.resource = new ResourceState { type = resource.type };
            placed++;
        }
        return placed;
    }

    private static WorldContinent GetClimateOdds(GameLogicData gameLogicData, Dictionary<TribeType, WorldContinent> cache, TribeType tribe)
    {
        if (cache.TryGetValue(tribe, out WorldContinent? odds))
            return odds;

        odds = new WorldContinent();
        if (tribe != TribeType.None && gameLogicData.TryGetData(tribe, out TribeData tribeData))
            odds.SetResourceModifiers(tribeData.resourceModifier);
        cache[tribe] = odds;
        return odds;
    }

    private static ResourceData? PickResource(MapData map, TileData tile, List<ResourceData> resources, WorldContinent climateOdds, XXHash hash)
    {
        int x = tile.coordinates.X;
        int y = tile.coordinates.Y;
        List<ResourceData> candidates = new();
        for (int i = 0; i < resources.Count; i++)
        {
            ResourceData resource = resources[i];
            float modifier = climateOdds.GetModifier(resource.type);
            if (modifier <= 0f || hash.Value(x, y, FIRST_RESOURCE_ROLL + i) > modifier)
                continue;

            var requirements = resource.resourceTerrainRequirements;
            if (requirements != null && requirements.Count > 0 && !requirements.Contains(tile.terrain))
                continue;

            if (resource.type == ResourceData.Type.Whale && HasWhaleOrCityNearby(map, tile))
                continue;

            candidates.Add(resource);
        }
        return candidates.Count > 0 ? candidates[hash.Range(0, candidates.Count, x, y, PICK_ROLL)] : null;
    }

    private static bool HasWhaleOrCityNearby(MapData map, TileData tile)
    {
        foreach (TileData neighbour in map.GetTileNeighbors(tile.coordinates))
        {
            if (neighbour.HasResource(ResourceData.Type.Whale) || neighbour.HasImprovement(ImprovementData.Type.City))
                return true;
        }
        return false;
    }
}
