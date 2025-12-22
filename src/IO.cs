using System.Text.Json;
using Polytopia.Data;

namespace PolytopiaMapManager;
public static class IO
{
    internal static readonly string MAPS_PATH = Path.Combine(PolyMod.Plugin.BASE_PATH, "Maps");

    internal static Data.MapInfo BuildMap(ushort size, List<TileData> tiles)
    {
        List<Data.MapTile> mapTiles = new();
        foreach (TileData tileData in tiles)
        {
            Data.MapTile mapTile = new Data.MapTile
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
        Data.MapInfo mapInfo = new Data.MapInfo
        {
            size = size,
            map = mapTiles
        };
        return mapInfo;
    }

    public static bool SaveMap(string name, ushort size, List<TileData> tiles)
    {
        string filePath = Path.Combine(MAPS_PATH, $"{name}.json");

        Data.MapInfo mapInfo = BuildMap(size, tiles);

        File.WriteAllTextAsync(
            filePath,
            JsonSerializer.Serialize(mapInfo, new JsonSerializerOptions { WriteIndented = true })
        );

        NotificationManager.Notify($"{name} has been saved.");

        return true;
    }

    internal static Data.MapInfo? LoadMap(string name)
    {
        string filePath = Path.Combine(MAPS_PATH, $"{name}.json");

        if (!File.Exists(filePath))
        {
            return null;
        }

        string json = File.ReadAllText(filePath);

        Data.MapInfo? mapInfo = JsonSerializer.Deserialize<Data.MapInfo>(json);

        return mapInfo;
    }

    internal static string[] GetAllMaps()
    {
        return Directory.GetFiles(MAPS_PATH, "*.json");
    }
}