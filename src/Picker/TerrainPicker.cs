using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.Picker;
internal class TerrainPicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.terrain";
    internal override Vector3? Indent => new Vector3(180, 0, 0);
    internal static List<Polytopia.Data.TerrainData.Type> excludedTerrains = new()
    {
        Polytopia.Data.TerrainData.Type.Wetland,
        Polytopia.Data.TerrainData.Type.Mangrove
    };

    internal override Sprite GetIcon(GameLogicData gameLogicData)
    {
       return Utils.GetSprite((int)Brush.chosenTerrain, SpriteData.TerrainToString(Brush.chosenTerrain), gameLogicData);
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        foreach (Polytopia.Data.TerrainData terrainData in gameState.GameLogicData.AllTerrainData.Values)
        {
            Polytopia.Data.TerrainData.Type terrainType = terrainData.type;
            if(excludedTerrains.Contains(terrainType))
                continue;
            string terrainName = Localization.Get(terrainType.GetDisplayName());
            Utils.CreateChoiceButton(selectViewmodePopup, terrainName,
                    (int)terrainType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetTerrainIcon);

            void OnClick(int id)
            {
                Brush.chosenTerrain = (Polytopia.Data.TerrainData.Type)id;
                Update(gameState.GameLogicData);
            }

            void SetTerrainIcon(UIRoundButton button, int type)
            {
                Utils.SetIcon(button, Utils.GetSprite(type, SpriteData.TerrainToString((Polytopia.Data.TerrainData.Type)type), gameState.GameLogicData), 0.6f);
            }
        }
    }
}