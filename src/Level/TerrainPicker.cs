using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;
using DG.Tweening;

namespace PolytopiaMapManager.Level
{
    internal static class TerrainPicker
    {
        internal static List<Polytopia.Data.TerrainData.Type> availableTerrains = new()
        {
            Polytopia.Data.TerrainData.Type.Field,
            Polytopia.Data.TerrainData.Type.Forest,
            Polytopia.Data.TerrainData.Type.Mountain,
            Polytopia.Data.TerrainData.Type.Water,
            Polytopia.Data.TerrainData.Type.Ocean,
            Polytopia.Data.TerrainData.Type.Ice
        };
        internal static UIRoundButton? terrainButton = null;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
        private static void HudScreen_OnMatchStart(HudScreen __instance)
        {
            if (MapMaker.inMapMaker)
            {
                terrainButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
                terrainButton.transform.position = terrainButton.transform.position + new Vector3(90, 0, 0);
                terrainButton.gameObject.SetActive(true);
                terrainButton.OnClicked = (UIButtonBase.ButtonAction)ShowTerrainPopup;
                terrainButton.text = string.Empty;
                UpdateTerrainButton(terrainButton);

                void ShowTerrainPopup(int id, BaseEventData eventData)
                {
                    SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                    // __instance.selectViewmodePopup.Header = Localization.Get("replay.viewmode.header", new Il2CppSystem.Object[] { });
                    selectViewmodePopup.Header = Localization.Get("mapmaker.choose.terrain", new Il2CppSystem.Object[] { });
                    GameState gameState = GameManager.GameState;
                    // Set Data
                    selectViewmodePopup.ClearButtons();
                    selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                    float num = 0f;
                    foreach (Polytopia.Data.TerrainData.Type terrainType in availableTerrains)
                    {
                        string terrainName = Localization.Get(terrainType.GetDisplayName());
                        CreateTerrainChoiceButton(selectViewmodePopup, gameState, terrainName, SpriteData.TerrainToString(terrainType), (int)terrainType, ref num);
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
                    selectViewmodePopup.Show(terrainButton!.rectTransform.position);
                }
            }
        }

        internal static void UpdateTerrainButton(UIRoundButton button)
        {
            if (MapMaker.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                SpriteAtlasManager manager = GameManager.GetSpriteAtlasManager();
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                SpriteAtlasManager.SpriteLookupResult lookupResult = manager.DoSpriteLookup(SpriteData.TerrainToString(MapMaker.chosenTerrain), gameLogicData.GetTribeTypeFromStyle(MapMaker.chosenClimate), MapMaker.chosenSkinType, false);
                button.icon.sprite = lookupResult.sprite;
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 1f);
            }
        }

        internal static void CreateTerrainChoiceButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, ref float num)
        {
            UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
            playerButton.id = (int)type;
            playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
            playerButton.Outline.gameObject.SetActive(false);
            playerButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 1f);
            playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
            playerButton.SetIconColor(Color.white);
            playerButton.ButtonEnabled = true;
            playerButton.OnClicked = (UIButtonBase.ButtonAction)OnTerrainButtonClicked;
            void OnTerrainButtonClicked(int id, BaseEventData eventData)
            {
                int type = id;
                MapMaker.modLogger!.LogInfo("Clicked i guess");
                MapMaker.modLogger!.LogInfo(id);
                MapMaker.chosenTerrain = (Polytopia.Data.TerrainData.Type)type;
                UpdateTerrainButton(terrainButton!);
                // viewmodePopup.Hide();
            }
            SpriteAtlasManager manager = GameManager.GetSpriteAtlasManager();
            SpriteAtlasManager.SpriteLookupResult lookupResult = manager.DoSpriteLookup(spriteName, gameState.GameLogicData.GetTribeTypeFromStyle(MapMaker.chosenClimate), MapMaker.chosenSkinType, false);
            playerButton.icon.sprite = lookupResult.sprite;
            if (playerButton.Label.PreferedValues.y > num)
            {
                num = playerButton.Label.PreferedValues.y;
            }
            viewmodePopup.buttons.Add(playerButton);
        }
    }
}