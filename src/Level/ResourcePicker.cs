using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.Level;
internal class ResourcePicker  : PickerBase
{
    internal static List<Polytopia.Data.ResourceData.Type> excludedResources = new()
    {
        Polytopia.Data.ResourceData.Type.Whale,
    };

    internal new void Create(UIRoundButton referenceButton, Transform parent)
    {
        button = Pickers.CreatePicker(button, referenceButton, parent, CreateResourceButtons, new Vector3(90, 0, 0), "mapmaker.choose.resource");
        UIRoundButton? CreateResourceButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
        {
            if(picker != null)
            {
                foreach (Polytopia.Data.ResourceData resourceData in gameState.GameLogicData.AllResourceData.Values)
                {
                    Polytopia.Data.ResourceData.Type resourceType = resourceData.type;
                    if(excludedResources.Contains(resourceType))
                        continue;
                    string resourceName = Localization.Get(resourceData.displayName);
                    Pickers.CreateChoiceButton(selectViewmodePopup, resourceName,
                            (int)resourceType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetResourceIcon);

                    void OnClick(int id)
                    {
                        Brush.chosenResource = (Polytopia.Data.ResourceData.Type)id;
                        Update(gameState.GameLogicData!);
                    }

                    void SetResourceIcon(UIRoundButton button, int type)
                    {
                        Pickers.SetIcon(button, Pickers.GetSprite(type, SpriteData.ResourceToString((ResourceData.Type)type), gameState.GameLogicData), 0.6f);
                    }
                }
                return picker;
            }
            return null;
        }
    }

    internal static new Sprite GetIcon(GameLogicData gameLogicData)
    {
       return Pickers.GetSprite((int)Brush.chosenResource, SpriteData.ResourceToString(Brush.chosenResource), gameLogicData);
    }
}