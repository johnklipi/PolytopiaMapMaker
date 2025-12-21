using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.Picker;
internal class ResourcePicker  : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.resource";
    internal override Vector3? Indent => new Vector3(90, 0, 0);
    internal static List<ResourceData.Type> excludedResources = new()
    {
        ResourceData.Type.Whale,
    };

    internal override Sprite GetIcon(GameLogicData gameLogicData)
    {
       return Manager.GetSprite((int)Brush.chosenResource, SpriteData.ResourceToString(Brush.chosenResource), gameLogicData);
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        foreach (ResourceData resourceData in gameState.GameLogicData.AllResourceData.Values)
        {
            ResourceData.Type resourceType = resourceData.type;
            if(excludedResources.Contains(resourceType))
                continue;
            string resourceName = Localization.Get(resourceData.displayName);
            Manager.CreateChoiceButton(selectViewmodePopup, resourceName,
                    (int)resourceType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetResourceIcon);

            void OnClick(int id)
            {
                Brush.chosenResource = (ResourceData.Type)id;
                Update(gameState.GameLogicData!);
            }

            void SetResourceIcon(UIRoundButton button, int type)
            {
                Manager.SetIcon(button, Manager.GetSprite(type, SpriteData.ResourceToString((ResourceData.Type)type), gameState.GameLogicData), 0.6f);
            }
        }
    }
}