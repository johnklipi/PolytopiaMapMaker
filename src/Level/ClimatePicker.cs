using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.Level
{
    internal static class ClimatePicker
    {
        internal const int SKINS_NUM = 1000;
        internal static UIRoundButton? climateButton = null;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
        private static void HudScreen_OnMatchStart(HudScreen __instance)
        {
            if (MapLoader.inMapMaker)
            {
                climateButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
                climateButton.gameObject.SetActive(true);
                climateButton.OnClicked = (UIButtonBase.ButtonAction)ShowClimatePopup;
                climateButton.text = string.Empty;
                UpdateClimateButton(climateButton);

                void ShowClimatePopup(int id, BaseEventData eventData)
                {
                    SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                    // __instance.selectViewmodePopup.Header = Localization.Get("replay.viewmode.header", new Il2CppSystem.Object[] { });
                    selectViewmodePopup.Header = Localization.Get("mapmaker.choose.climate", new Il2CppSystem.Object[] { });
                    GameState gameState = GameManager.GameState;

                    // Set Data
                    selectViewmodePopup.ClearButtons();
                    selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                    float num = 0f;
                    GameLogicData gameLogicData = gameState.GameLogicData;
                    List<TribeData> tribes = gameLogicData.GetTribes(TribeData.CategoryEnum.Human).ToArray().ToList().Concat(gameLogicData.GetTribes(TribeData.CategoryEnum.Special).ToArray().ToList()).ToList();
                    foreach (TribeData tribeData in tribes)
                    {
                        TribeType tribeType = tribeData.type;
                        string tribeName = Localization.Get(tribeData.displayName);
                        CreateClimateChoiceButton(selectViewmodePopup, gameState, tribeName, EnumCache<TribeType>.GetName(tribeType), (int)tribeType, gameLogicData.GetTribeColor(tribeData.type, SkinType.Default), ref num);
                        foreach (SkinType skinType in tribeData.skins)
                        {
                            // gameLogicData.TryGetData(skinType, out SkinData data);
                            string skinHeader = string.Format(Localization.Get(SkinTypeExtensions.GetSkinNameKey(), new Il2CppSystem.Object[] { }), Localization.Get(skinType.GetLocalizationKey(), new Il2CppSystem.Object[] { }));
                            CreateClimateChoiceButton(selectViewmodePopup, gameState, skinHeader, EnumCache<SkinType>.GetName(skinType), (int)skinType + 1000, gameLogicData.GetTribeColor(tribeData.type, skinType), ref num);
                        }
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
                    selectViewmodePopup.Show(climateButton!.rectTransform.position);
                }
            }
        }

        internal static void UpdateClimateButton(UIRoundButton button)
        {
            if (MapLoader.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                button.iconSpriteHandle.SetCompletion((SpriteHandleCallback)TribeSpriteHandle);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                void TribeSpriteHandle(SpriteHandle spriteHandleCallback)
                {
                    button.SetFaceIcon(spriteHandleCallback.sprite);
                }
                TribeType tribeType = gameLogicData.GetTribeTypeFromStyle(Brush.chosenClimate);
                string spriteName;
                if (Brush.chosenSkinType == SkinType.Default)
                {
                    spriteName = EnumCache<TribeType>.GetName(tribeType);
                }
                else
                {
                    spriteName = EnumCache<SkinType>.GetName(Brush.chosenSkinType);
                }
                button.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(spriteName));
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeType, Brush.chosenSkinType)), 1f);
            }
        }

        internal static void CreateClimateChoiceButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, int color, ref float num)
        {
            UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
            playerButton.id = (int)type;
            playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
            playerButton.Outline.gameObject.SetActive(false);
            playerButton.BG.color = ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(color), 1f);
            playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
            playerButton.SetIconColor(Color.white);
            playerButton.ButtonEnabled = true;
            playerButton.OnClicked = (UIButtonBase.ButtonAction)OnClimateButtonClicked;
            void OnClimateButtonClicked(int id, BaseEventData eventData)
            {
                int type = id;
                Main.modLogger!.LogInfo("Clicked i guess");
                Main.modLogger!.LogInfo(id);
                if (type >= SKINS_NUM)
                {
                    type -= SKINS_NUM;
                    SkinType skinType = (SkinType)type;
                    Brush.chosenClimate = MapLoader.GetTribeClimateFromSkin(skinType, gameState.GameLogicData);
                    Brush.chosenSkinType = skinType;
                }
                else
                {
                    Brush.chosenClimate = MapLoader.GetTribeClimateFromType((TribeType)type, gameState.GameLogicData);
                    Brush.chosenSkinType = SkinType.Default;
                }
                UpdateClimateButton(climateButton!);
                ImprovementPicker.UpdateImprovementButton(ImprovementPicker.improvementButton!);
                ResourcePicker.UpdateResourceButton(ResourcePicker.resourceButton!);
                TerrainPicker.UpdateTerrainButton(TerrainPicker.terrainButton!);
                TileEffectPicker.UpdateTileEffectButton(TileEffectPicker.tileEffectButton!);
                // viewmodePopup.Hide();
            }
            playerButton.iconSpriteHandle.SetCompletion((SpriteHandleCallback)TribeSpriteHandle);
            void TribeSpriteHandle(SpriteHandle spriteHandleCallback)
            {
                playerButton.SetFaceIcon(spriteHandleCallback.sprite);
            }
            playerButton.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(spriteName));
            if (playerButton.Label.PreferedValues.y > num)
            {
                num = playerButton.Label.PreferedValues.y;
            }
            viewmodePopup.buttons.Add(playerButton);
        }
    }
}