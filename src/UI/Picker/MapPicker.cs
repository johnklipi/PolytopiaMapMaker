using PolytopiaMapManager.Data;
using UnityEngine;

namespace PolytopiaMapManager.UI.Picker;
internal class MapPicker : PickerBase
{
    private const float PREVIEW_SIZE = 140f;
    private const float PREVIEW_ICON_SIZE = 0.95f;

    internal override string HeaderKey => "mapmaker.choose.map";
    internal override bool UseRightRow => true;
    internal override Vector2 ChoiceButtonSize => new Vector2(PREVIEW_SIZE, PREVIEW_SIZE);
    internal override Vector2? ChoiceCellSize => new Vector2(PREVIEW_SIZE, PREVIEW_SIZE);

    internal override Sprite GetIcon()
    {
       return PolyMod.Registry.GetSprite("map_icon")!;
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        List<string> maps = IO.GetAllMaps();
        MapIconCache.RemoveMissing(maps);

        for (int index = 0; index < maps.Count; index++)
        {
            Sprite icon = MapIconCache.Get(maps[index]) ?? GetIcon();
            CreateChoiceButton(selectViewmodePopup, maps[index], index, ref num,
                id => Editor.ConfirmDiscard(() => LoadMap(maps[id], gameState)),
                ColorUtil.SetAlphaOnColor(Color.black, 0.6f),
                (button, id) => SetIcon(button, icon, PREVIEW_ICON_SIZE));
        }
    }

    private void LoadMap(string name, GameState gameState)
    {
        MapInfo? mapInfo = IO.LoadMap(name);
        if(mapInfo == null)
            return;

        Loader.chosenMap = mapInfo;
        Main.MapName = name;
        Main.MapSaved = true;
        Main.UnsavedChanges = false;
        Loader.LoadMapInState(ref gameState, Loader.chosenMap);
        Main.currCapitals = Loader.chosenMap.capitals;
        GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
        History.Reset(gameState);
        base.Update(gameState.GameLogicData);
    }
}
