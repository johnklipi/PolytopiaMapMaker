using HarmonyLib;
using PolytopiaMapManager;
using PolytopiaMapManager.Data;
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
        // if(!Main.isActive)
        //     return;

        if(Input.GetKey(KeyCode.LeftControl))
        {
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
                                                    GameManager.GameState.Map.Tiles.ToArray().ToList(), Main.currCapitals);
                            CustomInput.RemoveInputFromPopup(popup);
                        }
                    }
                    CustomInput.AddInputToPopup(popup, Main.MapName);
                }
                else
                {
                    Main.MapSaved = IO.SaveMap(Main.MapName, (ushort)Math.Sqrt(GameManager.GameState.Map.Tiles.Length),
                                            GameManager.GameState.Map.Tiles.ToArray().ToList(), Main.currCapitals);
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

            UIRoundButton brushSizeButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
            brushSizeButton.transform.position = mapSizeButton.transform.position + new Vector3(60, 0, 0);
            brushSizeButton.gameObject.SetActive(true);
            brushSizeButton.OnClicked = (UIButtonBase.ButtonAction)ShowBrushPopup;
            brushSizeButton.text = string.Empty;

            void ShowBrushPopup(int id, BaseEventData eventData)
            {
                BasicPopup popup = PopupManager.GetBasicPopup();
                popup.Header = "Brush Size";
                popup.Description = "Set brush radius (0 = single tile, 1 = 3x3, etc).";
                popup.buttonData = new PopupBase.PopupButtonData[]
                {
                    new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null),
                    new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)SetBrush, -1, true, null)
                };
                void Exit(int id, BaseEventData eventData)
                {
                    CustomInput.RemoveInputFromPopup(popup);
                }
                void SetBrush(int id, BaseEventData eventData)
                {
                    var input = CustomInput.GetInputFromPopup(popup);
                    if(input != null && int.TryParse(input.text, out int size))
                    {
                        if(size < 0) size = 0;
                        if(size > Loader.MAX_BRUSH_SIZE) size = (int)Loader.MAX_BRUSH_SIZE;
                        Brush.brushSize = size;
                        NotificationManager.Notify($"Brush radius set to {2*size+1}x{2*size+1}", "Brush");
                    }
                    else
                    {
                        NotificationManager.Notify("Only numbers are allowed", "Error");
                    }
                    CustomInput.RemoveInputFromPopup(popup);
                }
                CustomInput.AddInputToPopup(popup, Brush.brushSize.ToString());
            }

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


    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractionBar), nameof(InteractionBar.AddAbilityButtons))]
    public static void AddSetCapitalButton(InteractionBar __instance, Tile tile)
    {
        if (!Main.isActive || tile == null) return;
        TileData tile1 = GameManager.GameState.Map.GetTile(tile.Coordinates);
        if (tile1 == null || tile1.improvement == null || tile1.improvement.type != Polytopia.Data.ImprovementData.Type.City) return;

        UIRoundButton uiroundButton = __instance.CreateRoundBottomBarButton(Localization.Get("setcapital"), false);
        //uiroundButton.sprite = PolyMod.Registry.GetSprite("anything");
        uiroundButton.OnClicked += (UIButtonBase.ButtonAction)setcapitalmethod;
        void setcapitalmethod(int id, BaseEventData baseEventData)
        {
            BasicPopup popup = PopupManager.GetBasicPopup();
            popup.Header = "Whose capital should this be?";
            popup.Description = "Input a number from 1 to 254! The associated player will spawn here. Type in 0 to mark this tile as an ordinary (noncapital) city!";
            popup.buttonData = new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null),
                new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Save, -1, true, null)
            };
            void Exit(int id, BaseEventData eventData)
            {
                Popup.CustomInput.RemoveInputFromPopup(popup);
            }
            void Save(int id, BaseEventData eventData)
            {
                var input = Popup.CustomInput.GetInputFromPopup(popup);
                if (input != null)
                {
                    if (byte.TryParse(input.text, out byte ID))
                    {
                        ChangeCapitalForTile(ID, tile.Coordinates);
                    }
                }
                Popup.CustomInput.RemoveInputFromPopup(popup);
            }
            Popup.CustomInput.AddInputToPopup(popup);
        }
    }

    public static void ChangeCapitalForTile(byte ID, WorldCoordinates coords)
    {
        Capital? capitalByCoords = Loader.GetCapital(coords, Main.currCapitals);
        Capital? capitalById = Loader.GetCapital(coords, Main.currCapitals);
        if (ID == 0)
        { 
            if (capitalByCoords != null)
            {
                Main.currCapitals.Remove(capitalByCoords);
                NotificationManager.Notify("City is no longer a capital!");
            }
        }
        else if (capitalById != null) NotificationManager.Notify("Player already has their capital set!");
        else
        {
            if(capitalByCoords != null)
            {
                NotificationManager.Notify("From " + capitalByCoords.player + " to " + ID, "Overwritten capital location!");
                Main.currCapitals.Remove(capitalByCoords);
            }
            Capital newCapital = new();
            newCapital.player = ID;
            newCapital.coordinates = coords;
            Main.currCapitals.Add(newCapital);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InteractionBar), nameof(InteractionBar.Show))]
    public static void ShowCapitalStatus(InteractionBar __instance, bool instant, bool force)
    {
        if(!Main.isActive || __instance == null || __instance.description == null || __instance.tile == null) return;
        TileData tile = GameManager.GameState.Map.GetTile(__instance.tile.Coordinates);
        if(tile == null || tile.improvement == null || tile.improvement.type != Polytopia.Data.ImprovementData.Type.City) return;
        Capital? capital = Loader.GetCapital(tile.coordinates, Main.currCapitals);
        if(capital != null)
        {
            __instance.description.text = "Capital City of Player "+ capital.player;
        }
    }
}