using UnityEngine;

namespace PolytopiaMapManager.UI.Picker;
internal class MapPicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.map";
    internal override Vector3? Indent => new Vector3(0, -90, 0);

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
            Manager.CreateChoiceButton(selectViewmodePopup, name,
                    index, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f));

            void OnClick(int id)
            {
                Main.MapName = visualMaps[id];
                Loader.chosenMap = IO.LoadMap(Main.MapName);
                Loader.LoadMapInState(ref gameState, Loader.chosenMap!);
                GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
                Loader.RevealMap(GameManager.LocalPlayer.Id);
                base.Update(gameState.GameLogicData);
            }
        }
    }
}