using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.Level
{
    internal static class ClimatePicker : BasePicker
    {
        internal const int SKINS_NUM = 1000;
        internal static UIRoundButton? climateButton = null;
        protected override int transformPositionOffsetX = 0;
        protected override string popupHeaderLocalizationKey = "climate";

        protected override void CreateButtons(SelectViewmodePopup selectViewmodePopup, GameState gameState, ref float num)
        {
            GameLogicData gameLogicData = gameState.GameLogicData;
            List<TribeData> tribes = gameLogicData.GetTribes(TribeData.CategoryEnum.Human).ToArray().ToList().Concat(gameLogicData.GetTribes(TribeData.CategoryEnum.Special).ToArray().ToList()).ToList();
            foreach (TribeData tribeData in tribes)
            {
                TribeType tribeType = tribeData.type;
                string tribeName = Localization.Get(tribeData.displayName);
                CreateClimateChoiceButton(selectViewmodePopup, gameState, tribeName, EnumCache<TribeType>.GetName(tribeType), (int)tribeType, gameLogicData.GetTribeColor(tribeData.type, SkinType.Default), ref num);
                foreach (SkinType skinType in tribeData.skins)
                {
                    string skinHeader = string.Format(Localization.Get(SkinTypeExtensions.GetSkinNameKey(), new Il2CppSystem.Object[] { }), Localization.Get(skinType.GetLocalizationKey(), new Il2CppSystem.Object[] { }));
                    CreateClimateChoiceButton(selectViewmodePopup, gameState, skinHeader, EnumCache<SkinType>.GetName(skinType), (int)skinType + 1000, gameLogicData.GetTribeColor(tribeData.type, skinType), ref num);
                }
            }
        }

        protected override void UpdateButton(UIRoundButton button)
        {
            if (MapMaker.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                button.iconSpriteHandle.SetCompletion((SpriteHandleCallback)TribeSpriteHandle);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                void TribeSpriteHandle(SpriteHandle spriteHandleCallback)
                {
                    button.SetFaceIcon(spriteHandleCallback.sprite);
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
                button.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(spriteName));
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeType, MapMaker.chosenSkinType)), 1f);
                climateButton = button;
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
                    MapMaker.chosenClimate = MapMaker.GetTribeClimateFromSkin(skinType, gameState.GameLogicData);
                    MapMaker.chosenSkinType = skinType;
                }
                else
                {
                    MapMaker.chosenClimate = MapMaker.GetTribeClimateFromType((TribeType)type, gameState.GameLogicData);
                    MapMaker.chosenSkinType = SkinType.Default;
                }
                UpdateButton(climateButton!);
                ImprovementPicker.UpdateButton(ImprovementPicker.improvementButton!);
                ResourcePicker.UpdateButton(ResourcePicker.resourceButton!);
                TerrainPicker.UpdateButton(TerrainPicker.terrainButton!);
                TileEffectPicker.UpdateButton(TileEffectPicker.tileEffectButton!);
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