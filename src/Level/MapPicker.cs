using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using UnityEngine.Rendering;

namespace PolytopiaMapManager.Level
{
    internal static class MapPicker
    {
        internal static UIRoundButton? mapChoiceButton = null;
        internal static List<string> visualMaps = new();

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
        private static void HudScreen_OnMatchStart(HudScreen __instance)
        {
            if (MapLoader.inMapMaker)
            {
                mapChoiceButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
                mapChoiceButton.transform.position = mapChoiceButton.transform.position - new Vector3(0, 90, 0);
                mapChoiceButton.gameObject.SetActive(true);
                mapChoiceButton.OnClicked = (UIButtonBase.ButtonAction)ShowMapPopup;
                mapChoiceButton.text = string.Empty;

                UpdatemapChoiceButton(mapChoiceButton);

                void ShowMapPopup(int id, BaseEventData eventData)
                {
                    SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                    selectViewmodePopup.Header = Localization.Get("mapmaker.choose.map", new Il2CppSystem.Object[] { });
                    GameState gameState = GameManager.GameState;
                    // Set Data
                    selectViewmodePopup.ClearButtons();
                    selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                    float num = 0f;
                    string[] maps = Directory.GetFiles(MapLoader.MAPS_PATH, "*.json");
                    visualMaps = new();
                    if (maps.Length > 0)
                    {
                        visualMaps = maps.Select(map => Path.GetFileNameWithoutExtension(map)).ToList();
                        num++;
                    }
                    for (int index = 0; index < visualMaps.Count(); index++)
                    {
                        string name = visualMaps[index];
                        CreateMapChoiceButton(selectViewmodePopup, gameState.GameLogicData, name, index, ref num);
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
                    selectViewmodePopup.Show(mapChoiceButton!.rectTransform.position);
                }
            }
        }

        internal static void UpdatemapChoiceButton(UIRoundButton button)
        {
            if (MapLoader.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                // PickersHelper.SetIcon(button, PickersHelper.GetSprite((int)Brush.chosenBuilding, SpriteData.MapToString(Brush.chosenBuilding), gameLogicData));
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
            }
        }

        internal static void CreateMapChoiceButton(SelectViewmodePopup viewmodePopup, GameLogicData gameLogicData, string header, int idx, ref float num)
        {
            UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
            playerButton.id = idx;
            playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
            playerButton.Outline.gameObject.SetActive(false);
            playerButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
            playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
            playerButton.SetIconColor(Color.white);
            playerButton.ButtonEnabled = true;
            playerButton.OnClicked = (UIButtonBase.ButtonAction)OnmapChoiceButtonClicked;
            void OnmapChoiceButtonClicked(int id, BaseEventData eventData)
            {
                int type = id;
                Main.modLogger!.LogInfo("Clicked i guess");
                Main.modLogger!.LogInfo(id);
                GameState gameState = GameManager.GameState;
                MapLoader.chosenMap = MapLoader.LoadMapFile(visualMaps[id]);
                MapLoader.LoadMapInState(ref gameState);
                GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
                // Brush.chosenBuilding = (Polytopia.Data.MapData.Type)type;
                UpdatemapChoiceButton(mapChoiceButton!);
            }
            // PickersHelper.SetIcon(playerButton, PickersHelper.GetSprite(idx, "", gameLogicData));
            if (playerButton.Label.PreferedValues.y > num)
            {
                num = playerButton.Label.PreferedValues.y;
            }
            viewmodePopup.buttons.Add(playerButton);
        }
    }
}