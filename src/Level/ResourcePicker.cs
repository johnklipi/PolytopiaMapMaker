using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;
using DG.Tweening;

namespace PolytopiaMapManager.Level
{
    internal static class ResourcePicker
    {
        internal static List<Polytopia.Data.ResourceData.Type> excludedResources = new()
        {
            Polytopia.Data.ResourceData.Type.Whale,
        };
        internal static UIRoundButton? resourceButton = null;
        protected override int transformPositionOffsetX = 90;
        protected override string popupHeaderLocalizationKey = "resource";

        protected override void CreateButtons(SelectViewmodePopup selectViewmodePopup, GameState gameState, ref float num)
        {
            foreach (Polytopia.Data.ResourceData resourceData in gameState.GameLogicData.AllResourceData.Values)
            {
                Polytopia.Data.ResourceData.Type resourceType = resourceData.type;
                if(excludedResources.Contains(resourceType))
                    continue;
                string resourceName = Localization.Get(resourceData.displayName);
                CreateResourceChoiceButton(selectViewmodePopup, gameState, resourceName, SpriteData.ResourceToString(resourceType), (int)resourceType, ref num);
            }
        }

        protected override void UpdateButton(UIRoundButton button)
        {
            if (MapMaker.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                button.Outline.gameObject.SetActive(false);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                button.icon.sprite = PickersHelper.GetSprite((int)MapMaker.chosenResource, SpriteData.ResourceToString(MapMaker.chosenResource), gameLogicData);
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
                resourceButton = button;
            }
        }

        internal static void CreateResourceChoiceButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, ref float num)
        {
            UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
            playerButton.id = (int)type;
            playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
            playerButton.Outline.gameObject.SetActive(false);
            playerButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
            playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
            playerButton.SetIconColor(Color.white);
            playerButton.ButtonEnabled = true;
            playerButton.OnClicked = (UIButtonBase.ButtonAction)OnResourceButtonClicked;
            void OnResourceButtonClicked(int id, BaseEventData eventData)
            {
                int type = id;
                Main.modLogger!.LogInfo("Clicked i guess");
                Main.modLogger!.LogInfo(id);
                MapMaker.chosenResource = (Polytopia.Data.ResourceData.Type)type;
                UpdateButton(resourceButton!);
            }

            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
            playerButton.icon.sprite = PickersHelper.GetSprite(type, spriteName, gameLogicData);

            if (playerButton.Label.PreferedValues.y > num)
            {
                num = playerButton.Label.PreferedValues.y;
            }
            viewmodePopup.buttons.Add(playerButton);
        }
    }
}