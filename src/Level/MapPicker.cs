using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.Level;
internal class MapPicker : PickerBase
{

    internal new void Create(UIRoundButton referenceButton, Transform parent)
    {
        button = Pickers.CreatePicker(button, referenceButton, parent, CreateMapButtons, new Vector3(0, -90, 0), "mapmaker.choose.map");
        UIRoundButton? CreateMapButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
        {
            if(picker != null)
            {
                string[] maps = Directory.GetFiles(MapLoader.MAPS_PATH, "*.json");
                List<string> visualMaps = new();
                if (maps.Length > 0)
                {
                    visualMaps = maps.Select(map => Path.GetFileNameWithoutExtension(map)).ToList();
                    num++;
                }
                for (int index = 0; index < visualMaps.Count(); index++)
                {
                    string name = visualMaps[index];
                    Pickers.CreateChoiceButton(selectViewmodePopup, name,
                            index, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f));

                    void OnClick(int id)
                    {
                        MapMaker.MapName = visualMaps[id];
                        MapLoader.chosenMap = MapLoader.LoadMapFile(MapMaker.MapName);
                        MapLoader.LoadMapInState(ref gameState);
                        GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
                        MapLoader.RevealMap(GameManager.LocalPlayer.Id);
                        Update(gameState.GameLogicData);
                    }
                    // CreateMapChoiceButton(selectViewmodePopup, gameState.GameLogicData, name, index, ref num);
                }
                return picker;
            }
            return null;
        }
    }
}