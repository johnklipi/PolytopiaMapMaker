using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.UI.Picker;
internal class TerrainPicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.terrain";
    internal static List<Polytopia.Data.TerrainData.Type> excludedTerrains = new()
    {
        Polytopia.Data.TerrainData.Type.Wetland,
        Polytopia.Data.TerrainData.Type.Mangrove
    };

    internal override Sprite GetIcon(GameLogicData gameLogicData)
    {
       return base.GetSprite(chosenValue, SpriteData.TerrainToString((Polytopia.Data.TerrainData.Type)chosenValue), gameLogicData);
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        foreach (Polytopia.Data.TerrainData terrainData in gameState.GameLogicData.AllTerrainData.Values)
        {
            Polytopia.Data.TerrainData.Type terrainType = terrainData.type;
            if(excludedTerrains.Contains(terrainType))
                continue;
            string terrainName = Localization.Get(terrainType.GetDisplayName());
            base.CreateChoiceButton(selectViewmodePopup, terrainName,
                    (int)terrainType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetTerrainIcon);

            void OnClick(int id)
            {
                chosenValue = id;
                Update(gameState.GameLogicData);
            }

            void SetTerrainIcon(UIRoundButton button, int type)
            {
                base.SetIcon(button, base.GetSprite(type, SpriteData.TerrainToString((Polytopia.Data.TerrainData.Type)type), gameState.GameLogicData), 0.6f);
            }
        }
    }
}