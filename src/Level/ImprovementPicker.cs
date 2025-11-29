using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;
using DG.Tweening;

namespace PolytopiaMapManager.Level
{
    internal static class ImprovementPicker
    {
        internal static List<Polytopia.Data.ImprovementData.Type> allowedImprovements = new()
        {
            Polytopia.Data.ImprovementData.Type.None,
            Polytopia.Data.ImprovementData.Type.City,
            Polytopia.Data.ImprovementData.Type.Ruin
        };
        internal static UIRoundButton? improvementButton = null;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
        private static void HudScreen_OnMatchStart(HudScreen __instance)
        {
            if (MapMaker.inMapMaker)
            {
                improvementButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
                improvementButton.transform.position = improvementButton.transform.position + new Vector3(360, 0, 0);
                improvementButton.gameObject.SetActive(true);
                improvementButton.OnClicked = (UIButtonBase.ButtonAction)ShowImprovementPopup;
                improvementButton.text = string.Empty;
                UpdateImprovementButton(improvementButton);

                void ShowImprovementPopup(int id, BaseEventData eventData)
                {
                    SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                    selectViewmodePopup.Header = Localization.Get("mapmaker.choose.improvement", new Il2CppSystem.Object[] { });
                    GameState gameState = GameManager.GameState;
                    // Set Data
                    selectViewmodePopup.ClearButtons();
                    selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                    float num = 0f;
                    foreach (Polytopia.Data.ImprovementData improvementData in gameState.GameLogicData.AllImprovementData.Values)
                    {
                        Polytopia.Data.ImprovementData.Type improvementType = improvementData.type;
                        if(!allowedImprovements.Contains(improvementType))
                            continue;
                        string improvementName = Localization.Get(improvementType.GetDisplayName());
                        CreateImprovementChoiceButton(selectViewmodePopup, gameState.GameLogicData, improvementName, SpriteData.ImprovementToString(improvementType), (int)improvementType, ref num);
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
                    selectViewmodePopup.Show(improvementButton!.rectTransform.position);
                }
            }
        }

        internal static void UpdateImprovementButton(UIRoundButton button)
        {
            if (MapMaker.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                button.icon.sprite = PickersHelper.GetSprite((int)MapMaker.chosenBuilding, SpriteData.ImprovementToString(MapMaker.chosenBuilding), gameLogicData);
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
            }
        }

        internal static void CreateImprovementChoiceButton(SelectViewmodePopup viewmodePopup, GameLogicData gameLogicData, string header, string spriteName, int type, ref float num)
        {
            UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
            playerButton.id = type;
            playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
            playerButton.Outline.gameObject.SetActive(false);
            playerButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
            playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
            playerButton.SetIconColor(Color.white);
            playerButton.ButtonEnabled = true;
            playerButton.OnClicked = (UIButtonBase.ButtonAction)OnImprovementButtonClicked;
            void OnImprovementButtonClicked(int id, BaseEventData eventData)
            {
                int type = id;
                Main.modLogger!.LogInfo("Clicked i guess");
                Main.modLogger!.LogInfo(id);
                MapMaker.chosenBuilding = (Polytopia.Data.ImprovementData.Type)type;
                UpdateImprovementButton(improvementButton!);
            }
            playerButton.icon.sprite = PickersHelper.GetSprite(type, spriteName, gameLogicData);
            if (playerButton.Label.PreferedValues.y > num)
            {
                num = playerButton.Label.PreferedValues.y;
            }
            viewmodePopup.buttons.Add(playerButton);
        }
    }
}