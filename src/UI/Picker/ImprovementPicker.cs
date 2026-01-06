using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.UI.Picker;
internal class ImprovementPicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.improvement";
    internal static List<ImprovementData.Type> allowedImprovements = new()
    {
        ImprovementData.Type.None,
        ImprovementData.Type.City,
        ImprovementData.Type.Ruin
    };

    internal override Sprite GetIcon(GameLogicData gameLogicData)
    {
        if((ImprovementData.Type)chosenValue == ImprovementData.Type.City)
        {
            return PolyMod.Registry.GetSprite("city")!;
        }
        return GetSprite(chosenValue, SpriteData.ImprovementToString((ImprovementData.Type)chosenValue), gameLogicData);
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        foreach (ImprovementData improvementData in gameState.GameLogicData.AllImprovementData.Values)
        {
            ImprovementData.Type improvementType = improvementData.type;
            if(!allowedImprovements.Contains(improvementType))
                continue;
            string improvementName = Localization.Get(improvementData.displayName);
            CreateChoiceButton(selectViewmodePopup, improvementName,
                    (int)improvementType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetImprovementIcon);

            if(improvementType == ImprovementData.Type.None)
                CreateChoiceButton(selectViewmodePopup, Localization.Get("mapmaker.remove"),
                    DESTROY_OPTION_ID, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetImprovementIcon);

            void OnClick(int id)
            {
                chosenValue = id;
                Update(gameState.GameLogicData);
            }

            void SetImprovementIcon(UIRoundButton button, int type)
            {
                Sprite icon = GetSprite(type, SpriteData.ImprovementToString((ImprovementData.Type)type), gameState.GameLogicData);
                if((ImprovementData.Type)type == ImprovementData.Type.City)
                {
                    icon = PolyMod.Registry.GetSprite("city")!;
                }
                SetIcon(button, icon, 0.6f);
            }
        }
    }
}