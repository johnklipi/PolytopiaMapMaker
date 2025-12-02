using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.Level
{
    internal static class TerrainPicker
    {
        internal static List<Polytopia.Data.TerrainData.Type> excludedTerrains = new()
        {
            Polytopia.Data.TerrainData.Type.Wetland,
            Polytopia.Data.TerrainData.Type.Mangrove
        };
        internal static UIRoundButton? terrainButton = null;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
        private static void HudScreen_OnMatchStart(HudScreen __instance)
        {
            if (MapLoader.inMapMaker)
            {
                terrainButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
                terrainButton.transform.position = terrainButton.transform.position + new Vector3(180, 0, 0);
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
                    foreach (Polytopia.Data.TerrainData terrainData in gameState.GameLogicData.AllTerrainData.Values)
                    {
                        Polytopia.Data.TerrainData.Type terrainType = terrainData.type;
                        if(excludedTerrains.Contains(terrainType))
                            continue;
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
            if (MapLoader.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                PickersHelper.SetIcon(button, PickersHelper.GetSprite((int)Brush.chosenTerrain, SpriteData.TerrainToString(Brush.chosenTerrain), gameLogicData), 0.6f);
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
            }
        }

        internal static void CreateTerrainChoiceButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, ref float num)
        {
            UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
            playerButton.id = (int)type;
            playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
            playerButton.Outline.gameObject.SetActive(false);
            playerButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
            playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
            playerButton.SetIconColor(Color.white);
            playerButton.ButtonEnabled = true;
            playerButton.OnClicked = (UIButtonBase.ButtonAction)OnTerrainButtonClicked;
            void OnTerrainButtonClicked(int id, BaseEventData eventData)
            {
                int type = id;
                Main.modLogger!.LogInfo("Clicked i guess");
                Main.modLogger!.LogInfo(id);
                Brush.chosenTerrain = (Polytopia.Data.TerrainData.Type)type;
                UpdateTerrainButton(terrainButton!);
                // viewmodePopup.Hide();
            }

            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
            PickersHelper.SetIcon(playerButton, PickersHelper.GetSprite(type, spriteName, gameLogicData), 0.6f);

            if (playerButton.Label.PreferedValues.y > num)
            {
                num = playerButton.Label.PreferedValues.y;
            }
            viewmodePopup.buttons.Add(playerButton);
        }
    }
}