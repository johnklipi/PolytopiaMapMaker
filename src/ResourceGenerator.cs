using Polytopia.Data;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager;

public static class ResourceGenerator
{
    private const float CITY_NEIGHBOUR_CHANCE = 1f;
    private const float CITY_OUTER_RING_CHANCE = 1f / 3f;
    private const int CITY_REACH = 2;
    private const float STARFISH_PER_TILE = 0.45f / 25f;
    private const float AQUACROP_CHANCE = 0.3f;
    private const float LEGACY_WHALE_CHANCE = 0.8f;
    private const int PLACEMENT_ROLL = 0;
    private const int PICK_ROLL = 1;
    private const int STARFISH_ROLL = 2;
    private const int FIRST_RESOURCE_ROLL = 3;

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

        int[] cityDistances = GetCityDistances(map, tiles);
        Dictionary<TribeType, WorldContinent> climateOdds = new();
        int placed = 0;
        for (int i = 0; i < tiles.Count; i++)
        {
            TileData tile = tiles[i];
            if (tile.improvement != null || tile.resource != null || cityDistances[i] > CITY_REACH)
                continue;

            int x = tile.coordinates.X;
            int y = tile.coordinates.Y;
            float chance = cityDistances[i] == 1 ? CITY_NEIGHBOUR_CHANCE : CITY_OUTER_RING_CHANCE;
            if (hash.Value(x, y, PLACEMENT_ROLL) >= chance)
                continue;

            ResourceData? resource = PickResource(map, tile, resources, GetClimateOdds(gameLogicData, gameState.Version, climateOdds, tile.climate), hash);
            if (resource == null)
                continue;

            tile.resource = new ResourceState { type = resource.type };
            placed++;
        }
        return placed + AddStarfish(map, tiles, hash);
    }

    private static int[] GetCityDistances(MapData map, List<TileData> tiles)
    {
        int[] distances = Enumerable.Repeat(int.MaxValue, tiles.Count).ToArray();
        foreach (TileData city in tiles.Where(tile => tile.HasImprovement(ImprovementData.Type.City)))
        {
            for (int dy = -CITY_REACH; dy <= CITY_REACH; dy++)
            {
                for (int dx = -CITY_REACH; dx <= CITY_REACH; dx++)
                {
                    int x = city.coordinates.X + dx;
                    int y = city.coordinates.Y + dy;
                    if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
                        continue;

                    int index = x + y * map.Width;
                    distances[index] = Math.Min(distances[index], Math.Max(Math.Abs(dx), Math.Abs(dy)));
                }
            }
        }
        return distances;
    }

    private static int AddStarfish(MapData map, List<TileData> tiles, XXHash hash)
    {
        int existing = tiles.Count(tile => tile.HasResource(ResourceData.Type.Starfish));
        int target = (int)(tiles.Count * STARFISH_PER_TILE) - existing;
        IEnumerable<TileData> candidates = tiles
            .Where(tile => tile.IsWater && tile.improvement == null && tile.resource == null)
            .OrderBy(tile => hash.Value(tile.coordinates.X, tile.coordinates.Y, STARFISH_ROLL));

        int placed = 0;
        foreach (TileData tile in candidates)
        {
            if (placed >= target)
                break;
            if (HasCityOrStarfishNearby(map, tile))
                continue;

            tile.resource = new ResourceState { type = ResourceData.Type.Starfish };
            placed++;
        }
        return placed;
    }

    private static bool HasCityOrStarfishNearby(MapData map, TileData tile)
    {
        foreach (TileData neighbour in map.GetTileNeighbors(tile.coordinates))
        {
            if (neighbour.HasResource(ResourceData.Type.Starfish) || neighbour.HasImprovement(ImprovementData.Type.City))
                return true;
        }
        return false;
    }

    private static WorldContinent GetClimateOdds(GameLogicData gameLogicData, int version, Dictionary<TribeType, WorldContinent> cache, TribeType tribe)
    {
        if (cache.TryGetValue(tribe, out WorldContinent? odds))
            return odds;

        odds = new WorldContinent();
        if (version < 99)
            odds.SetModifier(ResourceData.Type.Whale, LEGACY_WHALE_CHANCE);
        if (version < 104 || version >= 105)
            odds.SetModifier(ResourceData.Type.AquaCrop, AQUACROP_CHANCE);
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
