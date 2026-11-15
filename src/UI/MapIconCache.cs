using System.Collections.Concurrent;
using HarmonyLib;
using UnityEngine;

namespace PolytopiaMapManager.UI;

internal static class MapIconCache
{
    private const int ICONS_CREATED_PER_FRAME = 1;

    private class Entry
    {
        internal DateTime lastWrite;
        internal Sprite? icon;
    }

    private record LoadedIcon(string Name, DateTime LastWrite, byte[]? Bytes);

    private static readonly Dictionary<string, Entry> entries = new();
    private static readonly ConcurrentQueue<LoadedIcon> loaded = new();
    private static Task? preloadTask;

    internal static void StartPreload()
    {
        if(preloadTask != null && !preloadTask.IsCompleted)
            return;

        List<string> maps = IO.GetAllMaps();
        preloadTask = Task.Run(() =>
        {
            foreach (string name in maps)
                loaded.Enqueue(Read(name));
        });
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    private static void GameManager_Update()
    {
        for (int i = 0; i < ICONS_CREATED_PER_FRAME && loaded.TryDequeue(out LoadedIcon? icon); i++)
            Store(icon);
    }

    internal static Sprite? Get(string name)
    {
        while (loaded.TryDequeue(out LoadedIcon? pending))
            Store(pending);

        DateTime lastWrite = GetLastWrite(name);
        if(!entries.TryGetValue(name, out Entry? entry) || entry.lastWrite != lastWrite)
            Store(Read(name));
        return entries[name].icon;
    }

    internal static void Put(string name, byte[] bytes)
    {
        Store(new LoadedIcon(name, GetLastWrite(name), bytes));
    }

    internal static void RemoveMissing(List<string> maps)
    {
        foreach (string name in entries.Keys.Where(name => !maps.Contains(name)).ToList())
            Forget(name);
    }

    private static LoadedIcon Read(string name)
    {
        return new LoadedIcon(name, GetLastWrite(name), IO.LoadMapIcon(name));
    }

    private static DateTime GetLastWrite(string name)
    {
        return File.GetLastWriteTimeUtc(IO.GetMapPath(name));
    }

    private static void Store(LoadedIcon loadedIcon)
    {
        if(entries.TryGetValue(loadedIcon.Name, out Entry? entry) && entry.lastWrite >= loadedIcon.LastWrite)
            return;

        Forget(loadedIcon.Name);
        entries[loadedIcon.Name] = new Entry { lastWrite = loadedIcon.LastWrite, icon = CreateSprite(loadedIcon.Bytes) };
    }

    private static Sprite? CreateSprite(byte[]? bytes)
    {
        if(bytes == null || bytes.Length == 0)
            return null;

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if(!texture.LoadImage(bytes))
        {
            GameObject.Destroy(texture);
            return null;
        }
        texture.hideFlags = HideFlags.DontUnloadUnusedAsset;
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
        return sprite;
    }

    private static void Forget(string name)
    {
        if(!entries.TryGetValue(name, out Entry? entry))
            return;

        if(entry.icon != null)
        {
            GameObject.Destroy(entry.icon.texture);
            GameObject.Destroy(entry.icon);
        }
        entries.Remove(name);
    }
}
