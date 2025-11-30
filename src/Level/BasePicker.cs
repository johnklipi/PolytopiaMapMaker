using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.Level
{
    internal static class BasePicker
    {
        protected virtual int transformPositionOffsetX;
        protected virtual string popupHeaderLocalizationKey;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
        private static void HudScreen_OnMatchStart(HudScreen __instance)
        {
            if (MapMaker.inMapMaker)
            {
                ClimatePicker.OnMatchStart(__instance);
                ResourcePicker.OnMatchStart(__instance);
                ImprovementPicker.OnMatchStart(__instance);
                TerrainPicker.OnMatchStart(__instance);
                TileEffectPicker.OnMatchStart(__instance);
            }
        }

        protected static void OnMatchStart(HudScreen hudScreen)
        {
            UIRoundButton pickerButton = GameObject.Instantiate<UIRoundButton>(hudScreen.replayInterface.viewmodeSelectButton, hudScreen.transform);
            pickerButton.transform.position = pickerButton.transform.position + new Vector3(transformPositionOffsetX, 0, 0);
            pickerButton.gameObject.SetActive(true);
            pickerButton.OnClicked = (UIButtonBase.ButtonAction)ShowPopup;
            pickerButton.text = string.Empty;
            UpdateButton(pickerButton);

            void ShowPopup(int id, BaseEventData eventData)
            {
                SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                selectViewmodePopup.Header = Localization.Get("mapmaker.choose." + popupHeaderLocalizationKey, new Il2CppSystem.Object[] { });
                GameState gameState = GameManager.GameState;

                // Set Data
                selectViewmodePopup.ClearButtons();
                selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                float num = 0f;
                CreateButtons(selectViewmodePopup, gameState, ref num);
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
                selectViewmodePopup.Show(climateButton!.rectTransform.position);
            }
        }

        protected virtual void CreateButtons(SelectViewmodePopup viewmodePopup, GameState gameState, ref float num)
        {
            // Override in derived classes
        }

        protected virtual void UpdateButton(UIRoundButton button)
        {
            // Override in derived classes
        }

    //     internal static void CreateClimateChoiceButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, int color, ref float num)
    //     {
    //         UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
    //         playerButton.id = type;
    //         playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
    //         playerButton.Outline.gameObject.SetActive(false);
    //         playerButton.BG.color = ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(color), 1f);
    //         playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
    //         playerButton.SetIconColor(Color.white);
    //         playerButton.ButtonEnabled = true;
    //         playerButton.OnClicked = (UIButtonBase.ButtonAction)OnClimateButtonClicked;
    //         void OnClimateButtonClicked(int id, BaseEventData eventData)
    //         {
    //             int type = id;
    //             Main.modLogger!.LogInfo("Clicked i guess");
    //             Main.modLogger!.LogInfo(id);
    //             if (type >= SKINS_NUM)
    //             {
    //                 type -= SKINS_NUM;
    //                 SkinType skinType = (SkinType)type;
    //                 MapMaker.chosenClimate = MapMaker.GetTribeClimateFromSkin(skinType, gameState.GameLogicData);
    //                 MapMaker.chosenSkinType = skinType;
    //             }
    //             else
    //             {
    //                 MapMaker.chosenClimate = MapMaker.GetTribeClimateFromType((TribeType)type, gameState.GameLogicData);
    //                 MapMaker.chosenSkinType = SkinType.Default;
    //             }
    //             UpdateClimateButton(climateButton!);
    //             ImprovementPicker.UpdateImprovementButton(ImprovementPicker.improvementButton!);
    //             ResourcePicker.UpdateResourceButton(ResourcePicker.resourceButton!);
    //             TerrainPicker.UpdateTerrainButton(TerrainPicker.terrainButton!);
    //             TileEffectPicker.UpdateTileEffectButton(TileEffectPicker.tileEffectButton!);
    //             // viewmodePopup.Hide();
    //         }
    //         playerButton.iconSpriteHandle.SetCompletion((SpriteHandleCallback)TribeSpriteHandle);
    //         void TribeSpriteHandle(SpriteHandle spriteHandleCallback)
    //         {
    //             playerButton.SetFaceIcon(spriteHandleCallback.sprite);
    //         }
    //         playerButton.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(spriteName));
    //         if (playerButton.Label.PreferedValues.y > num)
    //         {
    //             num = playerButton.Label.PreferedValues.y;
    //         }
    //         viewmodePopup.buttons.Add(playerButton);
    //     }
    }
}