using PolytopiaMapManager.Data;
using UnityEngine;

namespace PolytopiaMapManager.UI.Picker;
internal class MapPicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.map";
    internal override Vector3? Indent => new Vector3(0, -110, 0);
    internal record CachedMap(string name, MapInfo map, Sprite icon);
    private List<CachedMap> cachedMaps = new();

    internal override Sprite GetIcon()
    {
       return PolyMod.Registry.GetSprite("map_icon")!;
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        if(cachedMaps.Count == 0)
        {
            string[] maps = IO.GetAllMaps();
            foreach (string mapName in maps)
            {
                string cleanMapName = Path.GetFileNameWithoutExtension(mapName);
                MapInfo? mapInfo = IO.LoadMap(cleanMapName);
                if(mapInfo == null)
                    continue;

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
                }

                CachedMap cachedMap = new(cleanMapName, mapInfo, icon);
                cachedMaps.Add(cachedMap);
            }
        }
        for (int index = 0; index < cachedMaps.Count; index++)
        {
            CachedMap cachedMap = cachedMaps[index];
            string name = cachedMap.name;
            base.CreateChoiceButton(selectViewmodePopup, name,
                    index, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.black, 0.6f), SetMapIcon);

            void OnClick(int id)
            {
                CachedMap cachedMap = cachedMaps[id];

                Loader.chosenMap = cachedMap.map;
                Main.MapName = cachedMap.name;
                Loader.LoadMapInState(ref gameState, Loader.chosenMap);
                Main.currCapitals = Loader.chosenMap.capitals;
                GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
                base.Update(gameState.GameLogicData);
            }

            void SetMapIcon(UIRoundButton button, int type) 
            {
                CachedMap cachedMap = cachedMaps[type];

                base.SetIcon(button, cachedMap.icon);
            }
        }
    }
}