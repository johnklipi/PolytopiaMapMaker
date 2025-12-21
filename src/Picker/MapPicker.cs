using UnityEngine;

namespace PolytopiaMapManager.Picker;
internal class MapPicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.map";
    internal override Vector3? Indent => new Vector3(0, -90, 0);

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
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
            Manager.CreateChoiceButton(selectViewmodePopup, name,
                    index, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f));

            void OnClick(int id)
            {
                MapMaker.MapName = visualMaps[id];
                MapLoader.chosenMap = MapLoader.LoadMapFile(MapMaker.MapName);
                MapLoader.LoadMapInState(ref gameState);
                GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
                MapLoader.RevealMap(GameManager.LocalPlayer.Id);
                base.Update(gameState.GameLogicData);
            }
        }
    }
}