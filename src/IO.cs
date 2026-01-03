using System.Text.Json;
using Polytopia.Data;
using UnityEngine;

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

    public static List<byte> CreateMapIcon(ushort size)
    {
        if(!GameManager.Instance.isLevelLoaded)
            return new();

        var oldZoom = CameraController.Instance.zoom;
        var oldPosition = CameraController.Instance.previousPosition;

        int center = (size - 1) / 2;
        WorldCoordinates centerCoordinates = new(center, center);

        CameraController.Instance.SetCameraZoom(GetIconCameraZoom(size));
        CameraController.Instance.SetPosition(centerCoordinates.ToPosition());
        CameraController.Instance.UpdateCameraSize();

        int resWidth = Screen.currentResolution.width;
        int resHeight = Screen.currentResolution.height;
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);

        Camera camera = CameraController.Camera;
        camera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        camera.Render();

        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        camera.targetTexture = null;
        RenderTexture.active = null;
        GameObject.Destroy(rt);

        byte[] bytes = screenShot.EncodeToPNG();

        CameraController.Instance.SetCameraZoom(oldZoom);
        CameraController.Instance.SetPosition(oldPosition);

        return bytes.ToList();
    }

    public static float GetIconCameraZoom(int mapSize)
    {
        float zoom = 0f;
        var value = mapSize / 10f;
        float multiplier = 0.003f;

        if(mapSize > 10)
        {
            zoom += (value - 1) * multiplier;
        }

        return zoom;
    }

    public static bool SaveMap(string name, ushort size, List<TileData> tiles, List<Data.Capital> capitals)
    {
        string filePath = Path.Combine(MAPS_PATH, $"{name}.json");

        Data.MapInfo mapInfo = BuildMap(size, tiles, capitals);
        mapInfo.icon = CreateMapIcon(size);
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

    internal static List<string> GetAllMaps()
    {
        List<string> names = new();
        foreach (var rawName in Directory.GetFiles(MAPS_PATH, "*.json"))
        {
            names.Add(Path.GetFileNameWithoutExtension(rawName));
        }
        return names;
    }
}