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
                tribeType = tileData.climate,
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

    internal enum IconSource
    {
        GameSnapshot,
        MainCamera,
        Legacy
    }

    internal static IconSource ICON_SOURCE = IconSource.GameSnapshot;
    private const int ICON_WIDTH = 512;
    private const float ICON_MARGIN = 0.6f;

    public static List<byte> CreateMapIcon(ushort size)
    {
        if(!GameManager.Instance.isLevelLoaded)
            return new();

        if(ICON_SOURCE == IconSource.Legacy)
            return CreateMapIconLegacy(size);

        if(ICON_SOURCE == IconSource.GameSnapshot)
        {
            List<byte>? snapshot = CreateMapIconWithSpriteCamera();
            if(snapshot != null)
                return snapshot;
            Main.modLogger!.LogInfo("Game snapshot camera is not available, using the main camera for the map icon");
        }
        return CreateMapIconWithMainCamera();
    }

    private static List<byte>? CreateMapIconWithSpriteCamera()
    {
        SpriteCamera spriteCamera = SpriteCamera.instance;
        if(spriteCamera == null || spriteCamera.spriteCamera == null)
            return null;

        GameState source = GameManager.GameState;
        if(!SerializationHelpers.FromByteArray<GameState>(SerializationHelpers.ToByteArray(source, source.Version), out GameState copy))
            return null;

        Camera camera = spriteCamera.spriteCamera;
        Vector3 oldPosition = camera.transform.position;
        float oldSize = camera.orthographicSize;
        Transform unitHolder = LevelManager.UnitHolder;
        try
        {
            spriteCamera.transform.localPosition = Vector3.zero;
            Vector2Int iconSize = FrameMap(camera, spriteCamera.tileHolderMap, copy.Map);

            SpriteCamera.SnapshotMapData snapshot = new SpriteCamera.SnapshotMapData();
            snapshot.LoadMapState(copy);
            Texture2D texture = SpriteCamera.TakeSnapshotOfMapState(snapshot, iconSize.x / 2f, iconSize.y / 2f, PolytopiaBackendBase.Common.TribeType.None, PolytopiaBackendBase.Common.SkinType.Default, PolytopiaBackendBase.Common.TribeType.None, 0, true);
            if(texture == null)
                return null;

            byte[] bytes = texture.EncodeToPNG();
            GameObject.Destroy(texture);
            return bytes.ToList();
        }
        catch(Exception ex)
        {
            Main.modLogger!.LogWarning($"Game snapshot failed: {ex.Message}");
            GameManager.ClearFakeSession();
            GameManager.debugShowGameUI = true;
            return null;
        }
        finally
        {
            LevelManager.UnitHolder = unitHolder;
            camera.transform.position = oldPosition;
            camera.orthographicSize = oldSize;
            camera.ResetAspect();
        }
    }

    private static List<byte> CreateMapIconWithMainCamera()
    {
        Camera camera = CameraController.Camera;
        Vector3 oldPosition = camera.transform.position;
        float oldSize = camera.orthographicSize;
        RenderTexture oldTarget = camera.targetTexture;
        try
        {
            Vector2Int iconSize = FrameMap(camera, null, GameManager.GameState.Map);
            RenderTexture rt = new RenderTexture(iconSize.x, iconSize.y, 24);
            camera.targetTexture = rt;
            camera.Render();

            RenderTexture.active = rt;
            Texture2D texture = new Texture2D(iconSize.x, iconSize.y, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, iconSize.x, iconSize.y), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            camera.targetTexture = oldTarget;
            GameObject.Destroy(rt);

            byte[] bytes = texture.EncodeToPNG();
            GameObject.Destroy(texture);
            return bytes.ToList();
        }
        finally
        {
            camera.targetTexture = oldTarget;
            camera.transform.position = oldPosition;
            camera.orthographicSize = oldSize;
            camera.ResetAspect();
        }
    }

    private static Vector2Int FrameMap(Camera camera, Transform? tileHolder, MapData map)
    {
        float left = new WorldCoordinates(0, map.Height - 1).ToPosition().x;
        float right = new WorldCoordinates(map.Width - 1, 0).ToPosition().x;
        float top = new WorldCoordinates(map.Width - 1, map.Height - 1).ToPosition().y;
        Vector3 center = new((left + right) / 2f, top / 2f, 0f);
        float halfWidth = (right - left) / 2f + ICON_MARGIN;
        float halfHeight = top / 2f + ICON_MARGIN;

        Vector3 worldCenter = tileHolder != null ? tileHolder.TransformPoint(center) : center;
        float scale = tileHolder != null ? tileHolder.lossyScale.y : 1f;
        camera.transform.position = new Vector3(worldCenter.x, worldCenter.y, camera.transform.position.z);
        camera.orthographicSize = halfHeight * scale;
        camera.aspect = halfWidth / halfHeight;

        return new Vector2Int(ICON_WIDTH, Mathf.Max(1, Mathf.RoundToInt(ICON_WIDTH * halfHeight / halfWidth)));
    }

    private static List<byte> CreateMapIconLegacy(ushort size)
    {

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
        UI.MapIconCache.Put(name, mapInfo.icon.ToArray());

        NotificationManager.Notify($"{name} has been saved.");

        return true;
    }

    internal static string GetMapPath(string name)
    {
        return Path.Combine(MAPS_PATH, $"{name}.json");
    }

    internal static byte[]? LoadMapIcon(string name)
    {
        string filePath = GetMapPath(name);
        if (!File.Exists(filePath))
            return null;

        try
        {
            Utf8JsonReader reader = new Utf8JsonReader(File.ReadAllBytes(filePath));
            if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
                return null;

            while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
            {
                bool isIcon = reader.ValueTextEquals("icon");
                reader.Read();
                if (isIcon)
                    return Data.ByteListBase64Json.ReadBytes(ref reader);
                reader.Skip();
            }
        }
        catch (Exception ex)
        {
            Main.modLogger!.LogInfo($"Failed to read icon of map {name}: {ex.Message}");
        }
        return null;
    }

    internal static Data.MapInfo? LoadMap(string name)
    {
        string filePath = GetMapPath(name);

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
