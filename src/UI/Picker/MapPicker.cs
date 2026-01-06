using PolytopiaMapManager.Data;
using UnityEngine;

namespace PolytopiaMapManager.UI.Picker;
internal class MapPicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.map";
    internal override bool UseVerticalLayout => true;
    private Dictionary<string, Sprite> cachedMaps = new();

    internal override Sprite GetIcon()
    {
       return PolyMod.Registry.GetSprite("map_icon")!;
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        List<string> maps = IO.GetAllMaps();
        if(maps.Count > cachedMaps.Count)
        {
            foreach (string mapName in maps)
            {
                if(!cachedMaps.ContainsKey(mapName))
                    CacheMap(mapName);
            }
        }
        else if(maps.Count < cachedMaps.Count)
        {
            foreach (var map in cachedMaps)
            {
                if(!maps.Contains(map.Key))
                    cachedMaps.Remove(map.Key);
            }
        }
        for (int index = 0; index < cachedMaps.Count; index++)
        {
            string name = cachedMaps.Keys.ToArray()[index];
            CreateChoiceButton(selectViewmodePopup, name,
                    index, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.black, 0.6f), SetMapIcon);

            void OnClick(int id)
            {
                string name = cachedMaps.Keys.ToArray()[id];
                MapInfo? mapInfo = IO.LoadMap(name);

                if(mapInfo == null)
                    return;

                Loader.chosenMap = mapInfo;
                Main.MapName = name;
                Loader.LoadMapInState(ref gameState, Loader.chosenMap);
                Main.currCapitals = Loader.chosenMap.capitals;
                GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
                base.Update(gameState.GameLogicData);
            }

            void SetMapIcon(UIRoundButton button, int type) 
            {
                string name = cachedMaps.Keys.ToArray()[type];

                SetIcon(button, cachedMaps[name]);
            }
        }
    }

    private void CacheMap(string mapName)
    {
        MapInfo? mapInfo = IO.LoadMap(mapName);
        if(mapInfo == null)
            return;

        Sprite icon = GetIcon();
        if(mapInfo.icon.Count > 0)
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
            texture.LoadImage(mapInfo.icon.ToArray());
            icon = Sprite.Create(
                texture,
                new(0, 0, texture.width, texture.height),
                new(0.5f, 0.5f),
                2112f
            );
            if(!Editor.spriteAtlasManager.cachedSprites["Heads"].ContainsKey(mapName))
            {
                Editor.spriteAtlasManager.cachedSprites.TryAdd("Heads", new());
                Editor.spriteAtlasManager.cachedSprites["Heads"].Add(mapName, icon);
            }
        }
        cachedMaps[mapName] = icon;
    }
}