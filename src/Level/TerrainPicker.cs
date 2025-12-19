using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.Level;
internal class TerrainPicker : PickerBase
{
    internal static List<Polytopia.Data.TerrainData.Type> excludedTerrains = new()
    {
        Polytopia.Data.TerrainData.Type.Wetland,
        Polytopia.Data.TerrainData.Type.Mangrove
    };

    internal override void Create(UIRoundButton referenceButton, Transform parent)
    {
        button = Pickers.CreatePicker(button, referenceButton, parent, CreateTerrainButtons, new Vector3(180, 0, 0), "mapmaker.choose.terrain");
        UIRoundButton? CreateTerrainButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
        {
            if(picker != null)
            {
                foreach (Polytopia.Data.TerrainData terrainData in gameState.GameLogicData.AllTerrainData.Values)
                {
                    Polytopia.Data.TerrainData.Type terrainType = terrainData.type;
                    if(excludedTerrains.Contains(terrainType))
                        continue;
                    string terrainName = Localization.Get(terrainType.GetDisplayName());
                    Pickers.CreateChoiceButton(selectViewmodePopup, terrainName,
                            (int)terrainType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetTerrainIcon);

                    void OnClick(int id)
                    {
                        Brush.chosenTerrain = (Polytopia.Data.TerrainData.Type)id;
                        Update(gameState.GameLogicData);
                    }

                    void SetTerrainIcon(UIRoundButton button, int type)
                    {
                        Pickers.SetIcon(button, Pickers.GetSprite(type, SpriteData.TerrainToString((Polytopia.Data.TerrainData.Type)type), gameState.GameLogicData), 0.6f);
                    }
                }
                return picker;
            }
            return null;
        }
    }

    internal override Sprite GetIcon(GameLogicData gameLogicData)
    {
       return Pickers.GetSprite((int)Brush.chosenTerrain, SpriteData.TerrainToString(Brush.chosenTerrain), gameLogicData);
    }
}