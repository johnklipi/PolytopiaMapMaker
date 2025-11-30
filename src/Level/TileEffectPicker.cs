using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;
using DG.Tweening;

namespace PolytopiaMapManager.Level
{
    internal static class TileEffectPicker
    {
        internal static List<TileData.EffectType> excludedTileEffects = new()
        {
            TileData.EffectType.Swamped,
            TileData.EffectType.Tentacle,
        };
        internal static UIRoundButton? tileEffectButton = null;
        protected override int transformPositionOffsetX = 270;
        protected override string popupHeaderLocalizationKey = "tileeffect";

        protected override void CreateButtons(SelectViewmodePopup selectViewmodePopup, GameState gameState, ref float num)
        {
            foreach (TileData.EffectType tileEffect in Enum.GetValues(typeof(TileData.EffectType)))
            {
                if(excludedTileEffects.Contains(tileEffect))
                    continue;
                EnumCache<TileData.EffectType>.GetName(tileEffect);
                string tileEffectName = Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(tileEffect)}");
                CreateTileEffectChoiceButton(selectViewmodePopup, gameState, tileEffectName, TileEffectToString(tileEffect), (int)tileEffect, ref num);
            }
        }

        protected override void UpdateButton(UIRoundButton button)
        {
            if (MapMaker.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                button.icon.sprite = PickersHelper.GetSprite((int)MapMaker.chosenTileEffect, TileEffectToString(MapMaker.chosenTileEffect), gameLogicData);
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
                tileEffectButton = button;
            }
        }

        internal static void CreateTileEffectChoiceButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, ref float num)
        {
            UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
            playerButton.id = (int)type;
            playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
            playerButton.Outline.gameObject.SetActive(false);
            playerButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
            playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
            playerButton.SetIconColor(Color.white);
            playerButton.ButtonEnabled = true;
            playerButton.OnClicked = (UIButtonBase.ButtonAction)OnTileEffectButtonClicked;
            void OnTileEffectButtonClicked(int id, BaseEventData eventData)
            {
                int type = id;
                Main.modLogger!.LogInfo("Clicked i guess");
                Main.modLogger!.LogInfo(id);
                MapMaker.chosenTileEffect = (TileData.EffectType)type;
                UpdateButton(tileEffectButton!);
            }

            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
            playerButton.icon.sprite = PickersHelper.GetSprite(type, spriteName, gameLogicData);

            if (playerButton.Label.PreferedValues.y > num)
            {
                num = playerButton.Label.PreferedValues.y;
            }
            viewmodePopup.buttons.Add(playerButton);
        }

        internal static string TileEffectToString(TileData.EffectType tileEffect)
        {
            if(tileEffect == TileData.EffectType.Flooded)
            {
                return SpriteData.TILE_WETLAND;
            }
            if(tileEffect == TileData.EffectType.Algae)
            {
                return "algae";
            }
            return "";
        }
    }
}