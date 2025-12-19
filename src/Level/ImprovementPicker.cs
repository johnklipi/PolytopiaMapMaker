using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.Level;
internal class ImprovementPicker : PickerBase
{
    internal static List<Polytopia.Data.ImprovementData.Type> allowedImprovements = new()
    {
        Polytopia.Data.ImprovementData.Type.None,
        Polytopia.Data.ImprovementData.Type.City,
        Polytopia.Data.ImprovementData.Type.Ruin
    };

    internal override void Create(UIRoundButton referenceButton, Transform parent)
    {
        button = Pickers.CreatePicker(button, referenceButton, parent, CreateImprovementButtons, new Vector3(360, 0, 0), "mapmaker.choose.improvement");
        UIRoundButton? CreateImprovementButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
        {
            if(picker != null)
            {
                foreach (Polytopia.Data.ImprovementData improvementData in gameState.GameLogicData.AllImprovementData.Values)
                {
                    Polytopia.Data.ImprovementData.Type improvementType = improvementData.type;
                    if(!allowedImprovements.Contains(improvementType))
                        continue;
                    string improvementName = Localization.Get(improvementData.displayName);
                    Pickers.CreateChoiceButton(selectViewmodePopup, improvementName,
                            (int)improvementType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetImprovementIcon);

                    void OnClick(int id)
                    {
                        Brush.chosenBuilding = (ImprovementData.Type)id;
                        Update(gameState.GameLogicData);
                    }

                    void SetImprovementIcon(UIRoundButton button, int type)
                    {
                        Pickers.SetIcon(button, Pickers.GetSprite(type, SpriteData.ImprovementToString((ImprovementData.Type)type), gameState.GameLogicData), 0.6f);
                    }
                }
                return picker;
            }
            return null;
        }
    }

    internal override Sprite GetIcon(GameLogicData gameLogicData)
    {
       return Pickers.GetSprite((int)Brush.chosenBuilding, SpriteData.ImprovementToString(Brush.chosenBuilding), gameLogicData);
    }
}