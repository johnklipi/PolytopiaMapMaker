using EnumsNET;
using HarmonyLib;
using PolyMod.Managers;
using PolytopiaMapManager.Popup;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PolytopiaMapManager.UI;

public static class Editor
{
    internal static Transform? mapNameContainer;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HudButtonBar), nameof(HudButtonBar.Init))]
    internal static void HudButtonBar_Init(HudButtonBar __instance, HudScreen hudScreen)
    {
        if(!Main.isActive)
            return;

        __instance.nextTurnButton.gameObject.SetActive(false);
        __instance.techTreeButton.gameObject.SetActive(false);
        __instance.statsButton.gameObject.SetActive(false);
        __instance.Show();
        __instance.Update();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    private static void GameManager_Update()
    {
        if(!Main.isActive)
            return;

        if(Input.GetKey(KeyCode.LeftControl))
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Loader.RevealMap(GameManager.LocalPlayer.Id);
                NotificationManager.Notify("Map has been revealed.");
            }
            if (Input.GetKeyDown(KeyCode.S))
            {
                if(!Main.MapSaved && Main.MapName == "Untitled Map")
                {
                    BasicPopup popup = PopupManager.GetBasicPopup();
                    popup.Header = "Rename Your Map!";
                    popup.Description = "Before saving, please name your map!";
                    popup.buttonData = new PopupBase.PopupButtonData[]
                    {
                        new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null),
                        new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Save, -1, true, null)
                    };
                    void Exit(int id, BaseEventData eventData)
                    {
                        CustomInput.RemoveInputFromPopup(popup);
                    }
                    void Save(int id, BaseEventData eventData)
                    {
                        var input = CustomInput.GetInputFromPopup(popup);
                        if(input != null)
                        {
                            Main.MapName = input.text;
                            Main.MapSaved = IO.SaveMap(Main.MapName, (ushort)Math.Sqrt(GameManager.GameState.Map.Tiles.Length),
                                                    GameManager.GameState.Map.Tiles.ToArray().ToList());
                            CustomInput.RemoveInputFromPopup(popup);
                        }
                    }
                    CustomInput.AddInputToPopup(popup, Main.MapName);
                }
                else
                {
                    Main.MapSaved = IO.SaveMap(Main.MapName, (ushort)Math.Sqrt(GameManager.GameState.Map.Tiles.Length),
                                            GameManager.GameState.Map.Tiles.ToArray().ToList());
                }
            }
            if(Input.GetKeyDown(KeyCode.W))
            {
                BasicPopup popup = PopupManager.GetBasicPopup();
                popup.Header = "Rename Map";
                popup.Description = "Naming your map is an exciting step! Click SET if done, don't forget to save!";
                popup.buttonData = new PopupBase.PopupButtonData[]
                {
                    new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null),
                    new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)SaveName, -1, true, null)
                };
                void Exit(int id, BaseEventData eventData)
                {
                    CustomInput.RemoveInputFromPopup(popup);
                }
                void SaveName(int id, BaseEventData eventData)
                {
                    var input = CustomInput.GetInputFromPopup(popup);
                    if(input != null)
                    {
                        Main.MapName = input.text;
                        NotificationManager.Notify($"New name is {Main.MapName}", "Map name set!");
                        CustomInput.RemoveInputFromPopup(popup);
                    }
                }
                CustomInput.AddInputToPopup(popup, Main.MapName);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ResourceBar), nameof(ResourceBar.OnEnable))]
    internal static void ResourceBar_OnEnable(ResourceBar __instance)
    {
        if(Main.isActive)
        {
            __instance.currencyContainer.gameObject.SetActive(false);
            __instance.scoreContainer.gameObject.SetActive(false);
            __instance.turnsContainer.gameObject.SetActive(false);

            TextMeshProUGUI text = GameObject.Instantiate(__instance.currencyContainer.headerLabel, __instance.turnsContainer.transform.parent);
            text.name = "MapName";

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, rect.sizeDelta.y);
            rect.anchoredPosition = new Vector2((Screen.width / 2) - (500 / 8), 40);
            rect.anchorMax = Vector2.zero;
            rect.anchorMin = Vector2.zero;

            text.fontSize = 35;
            text.alignment = TextAlignmentOptions.Center;

            text.GetComponent<TMPLocalizer>().Text = Main.MapName;
            text.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            mapNameContainer = text.transform;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
    private static void HudScreen_OnMatchStart(HudScreen __instance)
    {
        if (Main.isActive)
        {
            UIRoundButton mapSizeButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
            mapSizeButton.transform.position = mapSizeButton.transform.position - new Vector3(0, 180, 0);
            mapSizeButton.gameObject.SetActive(true);
            mapSizeButton.OnClicked = (UIButtonBase.ButtonAction)ShowMapPopup;
            mapSizeButton.text = string.Empty;

            void ShowMapPopup(int id, BaseEventData eventData)
            {
                BasicPopup popup = PopupManager.GetBasicPopup();
                popup.Header = "Resize Map";
                popup.Description = "";
                popup.buttonData = new PopupBase.PopupButtonData[]
                {
                    new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null),
                    new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Resize, -1, true, null)
                };
                void Exit(int id, BaseEventData eventData)
                {
                    CustomInput.RemoveInputFromPopup(popup);
                }
                void Resize(int id, BaseEventData eventData){
                    var input = CustomInput.GetInputFromPopup(popup);
                    if(input != null && int.TryParse(input.text, out int size))
                    {
                        GameState gameState = GameManager.GameState;
                        Main.ResizeMap(ref gameState, size);
                        GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
                        Loader.RevealMap(GameManager.LocalPlayer.Id);
                        NotificationManager.Notify($"New size is {size}x{size}", "Map size set!");
                    }
                    else
                    {
                        NotificationManager.Notify("Only numbers are allowed", "Error");
                    }
                    CustomInput.RemoveInputFromPopup(popup);
                }
                CustomInput.AddInputToPopup(popup, GameManager.GameState.Map.Width.ToString(), onValueChanged: new Action<string>(value => MapResizeValueChanged(value, popup)));
            }
        }
    }

    public static void MapResizeValueChanged(string value, BasicPopup popup)
    {
        if(!string.IsNullOrEmpty(value) && value.Length > 0)
        {
            bool couldntParse = !int.TryParse(value, out int result);
            bool isTooBig = result > Loader.MAX_MAP_SIZE;
            // bool isTooSmall = result < Loader.MIN_MAP_SIZE;
            if(couldntParse || isTooBig) // || isTooSmall
            {
                string message = "";
                if(couldntParse)
                {
                    message = Localization.Get("mapmaker.numbers.only");
                }
                else if (isTooBig)
                {
                    message = Localization.Get("mapmaker.size.big", new Il2CppSystem.Object[] { Loader.MAX_MAP_SIZE });
                }
                // else if (isTooSmall)
                // {
                //     message = Localization.Get("mapmaker.size.small", new Il2CppSystem.Object[] { Loader.MIN_MAP_SIZE });
                // }
                value = value.Remove(value.Length - 1);
                var input = CustomInput.GetInputFromPopup(popup);
                if(input != null)
                {
                    input.text = value;
                }
                NotificationManager.Notify(message, Localization.Get("gamemode.mapmaker"));
            }
        }
    }
}