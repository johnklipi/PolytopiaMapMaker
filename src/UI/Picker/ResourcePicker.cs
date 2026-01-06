using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.UI.Picker;
internal class ResourcePicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.resource";
    internal static List<ResourceData.Type> excludedResources = new()
    {
        ResourceData.Type.Whale,
    };

    internal override Sprite GetIcon(GameLogicData gameLogicData)
    {
       return GetSprite(chosenValue, SpriteData.ResourceToString((ResourceData.Type)chosenValue), gameLogicData);
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        foreach (ResourceData resourceData in gameState.GameLogicData.AllResourceData.Values)
        {
            ResourceData.Type resourceType = resourceData.type;
            if(excludedResources.Contains(resourceType))
                continue;
            string resourceName = Localization.Get(resourceData.displayName);
            CreateChoiceButton(selectViewmodePopup, resourceName,
                    (int)resourceType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetResourceIcon);

            if(resourceType == ResourceData.Type.None)
                CreateChoiceButton(selectViewmodePopup, Localization.Get("mapmaker.remove"),
                    DESTROY_OPTION_ID, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetResourceIcon);

            void OnClick(int id)
            {
                chosenValue = id;
                Update(gameState.GameLogicData!);
            }

            void SetResourceIcon(UIRoundButton button, int type)
            {
                SetIcon(button, GetSprite(type, SpriteData.ResourceToString((ResourceData.Type)type), gameState.GameLogicData), 0.6f);
            }
        }
    }
}