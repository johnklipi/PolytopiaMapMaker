using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.Picker;
internal class ImprovementPicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.improvement";
    internal override Vector3? Indent => new Vector3(360, 0, 0);
    internal static List<ImprovementData.Type> allowedImprovements = new()
    {
        ImprovementData.Type.None,
        ImprovementData.Type.City,
        ImprovementData.Type.Ruin
    };

    internal override Sprite GetIcon(GameLogicData gameLogicData)
    {
       return Utils.GetSprite((int)Brush.chosenBuilding, SpriteData.ImprovementToString(Brush.chosenBuilding), gameLogicData);
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        foreach (ImprovementData improvementData in gameState.GameLogicData.AllImprovementData.Values)
        {
            ImprovementData.Type improvementType = improvementData.type;
            if(!allowedImprovements.Contains(improvementType))
                continue;
            string improvementName = Localization.Get(improvementData.displayName);
            Utils.CreateChoiceButton(selectViewmodePopup, improvementName,
                    (int)improvementType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetImprovementIcon);

            void OnClick(int id)
            {
                Brush.chosenBuilding = (ImprovementData.Type)id;
                Update(gameState.GameLogicData);
            }

            void SetImprovementIcon(UIRoundButton button, int type)
            {
                Utils.SetIcon(button, Utils.GetSprite(type, SpriteData.ImprovementToString((ImprovementData.Type)type), gameState.GameLogicData), 0.6f);
            }
        }
    }
}