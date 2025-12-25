using System.Text.Json;
using Polytopia.Data;

namespace PolytopiaMapManager;
public static class IO
{
    internal static readonly string MAPS_PATH = Path.Combine(PolyMod.Plugin.BASE_PATH, "Maps");

    internal static Data.MapInfo BuildMap(ushort size, List<TileData> tiles, List<Data.Capital> capitals)
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
            map = mapTiles,
            capitals = capitals
        };
        return mapInfo;
    }

    public static bool SaveMap(string name, ushort size, List<TileData> tiles, List<Data.Capital> capitals)
    {
        string filePath = Path.Combine(MAPS_PATH, $"{name}.json");

        Data.MapInfo mapInfo = BuildMap(size, tiles, capitals);

        File.WriteAllText(
            filePath,
            JsonSerializer.Serialize
            (
                mapInfo,
                new JsonSerializerOptions 
                {
                    WriteIndented = true,
                    Converters = { new Data.WorldCoordinates2Json() }
                }
            )
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
        Data.MapInfo? mapInfo = null;
        string json = File.ReadAllText(filePath);
        try
        {
            mapInfo = JsonSerializer.Deserialize<Data.MapInfo>
            (
                json, new JsonSerializerOptions()
				{
					Converters = { new Data.WorldCoordinates2Json() },
				}
            );
        }
        catch (Exception ex)
        {
            string header = Localization.Get("mapmaker.failed.map", new Il2CppSystem.Object[] { name });
            NotificationManager.Notify(ex.Message, header);
            Main.modLogger!.LogInfo($"{header}\n{ex.Message}\n\n{ex.StackTrace}");
        }

        return mapInfo;
    }

    internal static string[] GetAllMaps()
    {
        return Directory.GetFiles(MAPS_PATH, "*.json");
    }
}