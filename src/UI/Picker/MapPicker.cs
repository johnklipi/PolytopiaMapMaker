using PolytopiaMapManager.Data;
using UnityEngine;

namespace PolytopiaMapManager.UI.Picker;
internal class MapPicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.map";
    internal override Vector3? Indent => new Vector3(0, -110, 0);

    internal override Sprite GetIcon()
    {
       return PolyMod.Registry.GetSprite("map_icon")!;
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        string[] maps = IO.GetAllMaps();
        List<string> visualMaps = new();
        if (maps.Length > 0)
        {
            visualMaps = maps.Select(map => Path.GetFileNameWithoutExtension(map)).ToList();
            num++;
        }
        for (int index = 0; index < visualMaps.Count(); index++)
        {
            string name = visualMaps[index];

            base.CreateChoiceButton(selectViewmodePopup, name,
                    index, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.black, 0.6f), SetMapIcon);

            void OnClick(int id)
            {
                string mapName = visualMaps[id];
                Loader.chosenMap = IO.LoadMap(mapName);
                if(Loader.chosenMap == null)
                    return;

                Main.MapName = mapName;
                Loader.LoadMapInState(ref gameState, Loader.chosenMap);
                Main.currCapitals = Loader.chosenMap.capitals;
                GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
                base.Update(gameState.GameLogicData);
            }

            void SetMapIcon(UIRoundButton button, int type) 
            {
                MapInfo? mapInfo = IO.LoadMap(name);
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
                }
                base.SetIcon(button, icon, 0.6f);
            }
        }
    }
}