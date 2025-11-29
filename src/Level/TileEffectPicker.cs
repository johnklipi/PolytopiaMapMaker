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

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
        private static void HudScreen_OnMatchStart(HudScreen __instance)
        {
            if (MapMaker.inMapMaker)
            {
                tileEffectButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
                tileEffectButton.transform.position = tileEffectButton.transform.position + new Vector3(270, 0, 0);
                tileEffectButton.gameObject.SetActive(true);
                tileEffectButton.OnClicked = (UIButtonBase.ButtonAction)ShowTileEffectPopup;
                tileEffectButton.text = string.Empty;
                UpdateTileEffectButton(tileEffectButton);

                void ShowTileEffectPopup(int id, BaseEventData eventData)
                {
                    SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                    // __instance.selectViewmodePopup.Header = Localization.Get("replay.viewmode.header", new Il2CppSystem.Object[] { });
                    selectViewmodePopup.Header = Localization.Get("mapmaker.choose.tileeffect", new Il2CppSystem.Object[] { });
                    GameState gameState = GameManager.GameState;
                    // Set Data
                    selectViewmodePopup.ClearButtons();
                    selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                    float num = 0f;
                    foreach (TileData.EffectType tileEffect in Enum.GetValues(typeof(TileData.EffectType)))
                    {
                        if(excludedTileEffects.Contains(tileEffect))
                            continue;
                        EnumCache<TileData.EffectType>.GetName(tileEffect);
                        string tileEffectName = Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(tileEffect)}");
                        CreateTileEffectChoiceButton(selectViewmodePopup, gameState, tileEffectName, TileEffectToString(tileEffect), (int)tileEffect, ref num);
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
                    selectViewmodePopup.Show(tileEffectButton!.rectTransform.position);
                }
            }
        }

        internal static void UpdateTileEffectButton(UIRoundButton button)
        {
            if (MapMaker.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                button.icon.sprite = PickersHelper.GetSprite((int)MapMaker.chosenTileEffect, TileEffectToString(MapMaker.chosenTileEffect), gameLogicData);
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
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
                UpdateTileEffectButton(tileEffectButton!);
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