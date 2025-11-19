using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager
{
    internal static class ClimatePickerUI
    {
        internal const int SKINS_NUM = 1000;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SelectViewmodePopup), nameof(SelectViewmodePopup.OnPlayerButtonClicked))]
        private static bool SelectViewmodePopup_OnPlayerButtonClicked(SelectViewmodePopup __instance, int id, BaseEventData eventData)
        {
            if (MapMaker.inMapMaker)
                __instance.SetSelectedButton(id);
            return !MapMaker.inMapMaker;
        }
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
        private static void HudScreen_OnMatchStart(HudScreen __instance)
        {
            if (MapMaker.inMapMaker)
            {
                MapMaker.modLogger!.LogInfo("IN MAP MAKERRRRRR");
                __instance.replayInterface.gameObject.SetActive(true);
                __instance.replayInterface.SetData(GameManager.GameState);
                __instance.replayInterface.timeline.gameObject.SetActive(false);
            }
            else
            {
                MapMaker.modLogger!.LogInfo("NOOOOOOOOOOOT IN MAP MAKERRRRRR");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ReplayInterface), nameof(ReplayInterface.ShowViewModePopup))]
        private static bool ReplayInterface_ShowViewModePopup(ReplayInterface __instance)
        {
            if (MapMaker.inMapMaker)
            {
                if (__instance.selectViewmodePopup != null && __instance.selectViewmodePopup.IsShowing())
                {
                    return false;
                }
                __instance.selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                // __instance.selectViewmodePopup.Header = Localization.Get("replay.viewmode.header", new Il2CppSystem.Object[] { });
                __instance.selectViewmodePopup.Header = Localization.Get("mapmaker.choose.climate", new Il2CppSystem.Object[] { });
                __instance.selectViewmodePopup.SetData(GameManager.GameState);
                __instance.selectViewmodePopup.buttonData = new PopupBase.PopupButtonData[]
                {
            new PopupBase.PopupButtonData("buttons.ok", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)exit, -1, true, null)
                };
                void exit(int id, BaseEventData eventData)
                {
                    __instance.CloseViewModePopup();
                }
                __instance.selectViewmodePopup.Show(__instance.viewmodeSelectButton.rectTransform.position);
            }
            return !MapMaker.inMapMaker;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ReplayInterface), nameof(ReplayInterface.UpdateButton))]
        internal static bool ReplayInterface_UpdateButton(ReplayInterface __instance)
        {
            if (MapMaker.inMapMaker)
            {
                __instance.viewmodeSelectButton.rectTransform.sizeDelta = new Vector2(75f, 75f);
                __instance.viewmodeSelectButton.iconSpriteHandle.SetCompletion((SpriteHandleCallback)TribeSpriteHandle);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                void TribeSpriteHandle(SpriteHandle spriteHandleCallback)
                {
                    __instance.viewmodeSelectButton.SetFaceIcon(spriteHandleCallback.sprite);
                }
                TribeType tribeType = gameLogicData.GetTribeTypeFromStyle(MapMaker.chosenClimate);
                string spriteName;
                if (MapMaker.chosenSkinType == SkinType.Default)
                {
                    spriteName = EnumCache<TribeType>.GetName(tribeType);
                }
                else
                {
                    spriteName = EnumCache<SkinType>.GetName(MapMaker.chosenSkinType);
                }
                __instance.viewmodeSelectButton.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(spriteName));
                __instance.viewmodeSelectButton.Outline.gameObject.SetActive(false);
                __instance.viewmodeSelectButton.BG.color = ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeType, MapMaker.chosenSkinType)), 1f);
            }
            return !MapMaker.inMapMaker;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SelectViewmodePopup), nameof(SelectViewmodePopup.SetData))]
        internal static bool SelectViewmodePopup_SetData(SelectViewmodePopup __instance, GameState gameState)
        {
            if (MapMaker.inMapMaker)
            {
                __instance.ClearButtons();
                __instance.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                float num = 0f;
                GameLogicData gameLogicData = gameState.GameLogicData;
                List<TribeData> tribes = gameLogicData.GetTribes(TribeData.CategoryEnum.Human).ToArray().ToList().Concat(gameLogicData.GetTribes(TribeData.CategoryEnum.Special).ToArray().ToList()).ToList();
                foreach (TribeData tribeData in tribes)
                {
                    TribeType tribeType = tribeData.type;
                    string tribeName = Localization.Get(tribeData.displayName);
                    CreatePlayerButton(__instance, gameState, tribeName, EnumCache<TribeType>.GetName(tribeType), (int)tribeType, gameLogicData.GetTribeColor(tribeData.type, SkinType.Default), ref num);
                    foreach (SkinType skinType in tribeData.skins)
                    {
                        // gameLogicData.TryGetData(skinType, out SkinData data);
                        string skinHeader = string.Format(Localization.Get(SkinTypeExtensions.GetSkinNameKey(), new Il2CppSystem.Object[] { }), Localization.Get(skinType.GetLocalizationKey(), new Il2CppSystem.Object[] { }));
                        CreatePlayerButton(__instance, gameState, skinHeader, EnumCache<SkinType>.GetName(skinType), (int)skinType + 1000, gameLogicData.GetTribeColor(tribeData.type, skinType), ref num);
                    }
                }
                __instance.gridLayout.spacing = new Vector2(__instance.gridLayout.spacing.x, num + 10f);
                __instance.gridLayout.padding.bottom = Mathf.RoundToInt(num + 10f);
                __instance.gridBottomSpacer.minHeight = num + 10f;
            }
            return !MapMaker.inMapMaker;
        }

        internal static void CreatePlayerButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, int color, ref float num)
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
                MapMaker.modLogger!.LogInfo("Clicked i guess");
                MapMaker.modLogger!.LogInfo(id);
                if (type >= SKINS_NUM)
                {
                    type -= SKINS_NUM;
                    SkinType skinType = (SkinType)type;
                    MapMaker.chosenClimate = MapMaker.GetTribeClimateFromSkin(skinType, gameState.GameLogicData);
                    MapMaker.chosenSkinType = skinType;
                }
                else
                {
                    MapMaker.chosenClimate = MapMaker.GetTribeClimateFromType((TribeType)type, gameState.GameLogicData);
                    MapMaker.chosenSkinType = SkinType.Default;
                }
                HudScreen hudScreen = UIManager.Instance.GetScreen(UIConstants.Screens.Hud).Cast<HudScreen>();
                hudScreen.replayInterface.UpdateButton();
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