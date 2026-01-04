using HarmonyLib;
using Il2CppInterop.Runtime;
using Polytopia.Data;
using PolytopiaMapManager.Data;
using PolytopiaMapManager.Popup;
using PolytopiaMapManager.UI.Picker;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PolytopiaMapManager.UI;

public static class Editor
{
    internal static Transform? mapNameContainer;
    internal static BiomePicker biomePicker = new();
    internal static MapPicker mapPicker = new();
    internal static ResourcePicker resourcePicker = new();
    internal static TerrainPicker terrainPicker = new();
    internal static TileEffectPicker tileEffectPicker = new();
    internal static ImprovementPicker improvementPicker = new();
    internal static List<PickerBase> pickers = new();
    internal static RectTransform? editorUi;
    internal static RectTransform? horizontalLayout;
    internal static RectTransform? verticalLayout;
    internal static SpriteAtlasManager spriteAtlasManager = GameManager.GetSpriteAtlasManager();

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

    internal static void CreateEditorUI(Transform hudScreen)
    {
        GameObject editorGO = new GameObject(
            "EditorUI",
            new Il2CppSystem.Type[] {Il2CppType.Of<RectTransform>()}
        );

        editorUi = editorGO.GetComponent<RectTransform>();
        editorGO.transform.SetParent(hudScreen, false);

        editorUi.anchorMin = new Vector2(0f, 0.95f);
        editorUi.anchorMax = new Vector2(0f, 0.95f);
        editorUi.pivot     = new Vector2(0f, 0.95f);
        editorUi.anchoredPosition = new Vector2(20f, 0f);
        editorUi.sizeDelta = Vector2.zero;

        GameObject horizontalLayoutGO = new GameObject(
            "HorizontalLayout",
            new Il2CppSystem.Type[] {Il2CppType.Of<RectTransform>()}
        );
        horizontalLayoutGO.transform.SetParent(editorUi, false);
        horizontalLayout = horizontalLayoutGO.GetComponent<RectTransform>();

        horizontalLayout.anchorMin = new Vector2(0f, 1f);
        horizontalLayout.anchorMax = new Vector2(0f, 1f);
        horizontalLayout.pivot     = new Vector2(0f, 1f);
        horizontalLayout.anchoredPosition = Vector2.zero;
        horizontalLayout.sizeDelta = Vector2.zero;

        GameObject verticalLayouttGO = new GameObject(
            "verticalLayout",
            new Il2CppSystem.Type[] {Il2CppType.Of<RectTransform>()}
        );
        verticalLayouttGO.transform.SetParent(editorUi, false);
        verticalLayout = verticalLayouttGO.GetComponent<RectTransform>();

        verticalLayout.anchorMin = new Vector2(0f, 0.8f);
        verticalLayout.anchorMax = new Vector2(0f, 0.8f);
        verticalLayout.pivot     = new Vector2(0f, 0.8f);
        verticalLayout.anchoredPosition = Vector2.zero;
        verticalLayout.sizeDelta = Vector2.zero;
    }

    internal static HorizontalLayoutGroup EnsureHorizontalLayout(
        RectTransform parent,
        float spacing = 20f)
    {
        HorizontalLayoutGroup layout =
            parent.GetComponent<HorizontalLayoutGroup>();

        if (layout == null)
        {
            layout = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;

        layout.childControlWidth = false;
        layout.childControlHeight = false;

        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return layout;
    }

    internal static VerticalLayoutGroup EnsureVerticalLayout(
        RectTransform parent,
        float spacing = 20f)
    {
        VerticalLayoutGroup layout =
            parent.GetComponent<VerticalLayoutGroup>();

        if (layout == null)
        {
            layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleLeft;

        layout.childControlWidth = false;
        layout.childControlHeight = false;

        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return layout;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
    private static void HudScreen_OnMatchStart(HudScreen __instance)
    {
        if (!Main.isActive)
            return;

        CreateEditorUI(__instance.transform);
        if(editorUi == null || horizontalLayout == null || verticalLayout == null)
            return;
        EnsureHorizontalLayout(horizontalLayout);
        EnsureVerticalLayout(verticalLayout);
        pickers.Add(biomePicker);
        pickers.Add(mapPicker);
        pickers.Add(resourcePicker);
        pickers.Add(terrainPicker);
        pickers.Add(tileEffectPicker);
        pickers.Add(improvementPicker);

        UIRoundButton referenceButton = __instance.replayInterface.viewmodeSelectButton;
        GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
        foreach (var picker in pickers)
        {
            if(picker.GetType().ToString() == "PolytopiaMapManager.UI.Picker.MapPicker")
            {
                picker.Create(referenceButton, verticalLayout);
            }
            else
            {
                picker.Create(referenceButton, horizontalLayout);
            }
            picker.Update(gameLogicData);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(horizontalLayout);
        UIRoundButton mapSizeButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
        mapSizeButton.transform.position = mapSizeButton.transform.position - new Vector3(0, 220, 0);
        mapSizeButton.gameObject.SetActive(true);
        mapSizeButton.OnClicked = (UIButtonBase.ButtonAction)ShowMapPopup;
        mapSizeButton.text = string.Empty;

        mapSizeButton.rectTransform.sizeDelta = new Vector2(75f, 75f);
        mapSizeButton.Outline.gameObject.SetActive(false);
        mapSizeButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);

        mapSizeButton.faceIconSizeMultiplier = 0.6f;
        mapSizeButton.icon.sprite = PolyMod.Registry.GetSprite("resize_icon");
        mapSizeButton.icon.useSpriteMesh = true;
        mapSizeButton.icon.SetNativeSize();
        Vector2 sizeDelta = mapSizeButton.icon.rectTransform.sizeDelta;
        mapSizeButton.icon.rectTransform.sizeDelta = sizeDelta * mapSizeButton.faceIconSizeMultiplier;
        mapSizeButton.icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        mapSizeButton.icon.gameObject.SetActive(true);
        mapSizeButton.text = Localization.Get("mapmaker.resize");

        void ShowMapPopup(int id, BaseEventData eventData)
        {
            BasicPopup popup = PopupManager.GetBasicPopup();
            popup.Header = Localization.Get("mapmaker.resize");
            popup.Description = string.Empty;
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    private static void GameManager_Update()
    {
         if(!Main.isActive)
             return;

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
        if(!Main.isActive)
            return;

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

        UIRoundButton uiroundButton = __instance.CreateRoundBottomBarButton(Localization.Get("mapmaker.capitals.set"), false);
        uiroundButton.sprite = PolyMod.Registry.GetSprite("capital_icon");
        uiroundButton.OnClicked += (UIButtonBase.ButtonAction)setcapitalmethod;
        void setcapitalmethod(int id, BaseEventData baseEventData)
        {
            BasicPopup popup = PopupManager.GetBasicPopup();
            popup.Header = Localization.Get("mapmaker.capitals.header");
            popup.Description = Localization.Get("mapmaker.capitals.desc", new Il2CppSystem.Object[]{1, 254, 0});
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnNextTurn))]
    internal static bool HudScreen_OnNextTurn(bool forceConfirmation)
    {
        return !Main.isActive;
    }
}