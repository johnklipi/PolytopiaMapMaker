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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
        private static void HudScreen_OnMatchStart(HudScreen __instance)
        {
            if (MapLoader.inMapMaker)
            {
                resourceButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
                resourceButton.transform.position = resourceButton.transform.position + new Vector3(90, 0, 0);
                resourceButton.gameObject.SetActive(true);
                resourceButton.OnClicked = (UIButtonBase.ButtonAction)ShowResourcePopup;
                resourceButton.text = string.Empty;
                UpdateResourceButton(resourceButton);

                void ShowResourcePopup(int id, BaseEventData eventData)
                {
                    SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                    // __instance.selectViewmodePopup.Header = Localization.Get("replay.viewmode.header", new Il2CppSystem.Object[] { });
                    selectViewmodePopup.Header = Localization.Get("mapmaker.choose.resource", new Il2CppSystem.Object[] { });
                    GameState gameState = GameManager.GameState;
                    // Set Data
                    selectViewmodePopup.ClearButtons();
                    selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                    float num = 0f;
                    foreach (Polytopia.Data.ResourceData resourceData in gameState.GameLogicData.AllResourceData.Values)
                    {
                        Polytopia.Data.ResourceData.Type resourceType = resourceData.type;
                        if(excludedResources.Contains(resourceType))
                            continue;
                        string resourceName = Localization.Get(resourceData.displayName);
                        CreateResourceChoiceButton(selectViewmodePopup, gameState, resourceName, SpriteData.ResourceToString(resourceType), (int)resourceType, ref num);
                    }
                    selectViewmodePopup.gridLayout.spacing = new Vector2(selectViewmodePopup.gridLayout.spacing.x, num + 10f);
                    selectViewmodePopup.gridLayout.padding.bottom = Mathf.RoundToInt(num + 10f);
                    selectViewmodePopup.gridBottomSpacer.minHeight = num + 10f;


                    selectViewmodePopup.buttonData = new PopupBase.PopupButtonData[]
                    {
                        new PopupBase.PopupButtonData("buttons.ok", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null)
                    };

                    void Exit(int id, BaseEventData eventData)
                    {
                        selectViewmodePopup.Hide();
                    }
                    selectViewmodePopup.Show(resourceButton!.rectTransform.position);
                }
            }
        }

        internal static void UpdateResourceButton(UIRoundButton button)
        {
            if (MapLoader.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                button.Outline.gameObject.SetActive(false);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                button.icon.sprite = PickersHelper.GetSprite((int)Brush.chosenResource, SpriteData.ResourceToString(Brush.chosenResource), gameLogicData);
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
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
                Brush.chosenResource = (Polytopia.Data.ResourceData.Type)type;
                UpdateResourceButton(resourceButton!);
                // viewmodePopup.Hide();
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