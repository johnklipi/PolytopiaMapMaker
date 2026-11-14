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
using PolytopiaMapManager.UI.Menu;

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
            "VerticalLayout",
            new Il2CppSystem.Type[] {Il2CppType.Of<RectTransform>()}
        );
        verticalLayouttGO.transform.SetParent(editorUi, false);
        verticalLayout = verticalLayouttGO.GetComponent<RectTransform>();

        verticalLayout.anchorMin = new Vector2(0f, 1f);
        verticalLayout.anchorMax = new Vector2(0f, 1f);
        verticalLayout.pivot     = new Vector2(0f, 1f);
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
        layout.childAlignment = TextAnchor.UpperLeft;

        layout.childControlWidth = false;
        layout.childControlHeight = false;

        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding.top = 75;
        return layout;
    }

    private static readonly Vector2 TOOL_BUTTON_SIZE = new Vector2(75f, 75f);
    private const float TOOL_ICON_SIZE = 0.6f;
    private static UIRoundButton? paintButton;

    private static UIRoundButton CreateToolButton(UIRoundButton referenceButton, Transform parent, Sprite? icon, string label, Action onClick)
    {
        UIRoundButton button = GameObject.Instantiate<UIRoundButton>(referenceButton, parent);
        button.gameObject.SetActive(true);
        button.OnClicked = (UIButtonBase.ButtonAction)((id, eventData) => onClick());
        button.rectTransform.sizeDelta = TOOL_BUTTON_SIZE;
        button.Outline.gameObject.SetActive(false);
        button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        PickerBase.SetIcon(button, icon, TOOL_ICON_SIZE);
        button.text = label;
        return button;
    }

    private static void CreateSaveButton(UIRoundButton referenceButton)
    {
        CreateToolButton(referenceButton, verticalLayout!, PolyMod.Registry.GetSprite("save_icon"), Localization.Get("mapmaker.map.save"), SaveMap);
    }

    private static void CreateRenameButton(UIRoundButton referenceButton)
    {
        CreateToolButton(referenceButton, verticalLayout!, PolyMod.Registry.GetSprite("rename_icon"), Localization.Get("mapmaker.map.rename.button"), ShowRenamePopup);
    }

    private static void CreatePaintButton(UIRoundButton referenceButton)
    {
        paintButton = CreateToolButton(referenceButton, horizontalLayout!, PolyMod.Registry.GetSprite("paint_icon"), string.Empty, TogglePaintMode);
        UpdatePaintButton();
    }

    private static void TogglePaintMode()
    {
        Brush.PaintMode = !Brush.PaintMode;
        if(Brush.PaintMode)
            InputEvents.SelectionCleared();
        UpdatePaintButton();
    }

    private static void UpdatePaintButton()
    {
        if(paintButton == null)
            return;
        paintButton.BG.color = Brush.PaintMode ? ColorConstants.blue : ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        paintButton.text = Localization.Get(Brush.PaintMode ? "mapmaker.paint.on" : "mapmaker.paint.off");
    }

    private static void CreateMapResizeButton(UIRoundButton referenceButton)
    {
        CreateToolButton(referenceButton, verticalLayout!, PolyMod.Registry.GetSprite("resize_icon"), Localization.Get("mapmaker.map.resize"), ShowMapPopup);

        void ShowMapPopup()
        {
            var popup = GameSetup_UI2.GetBasicPopupLegacy();
            popup.Header = Localization.Get("mapmaker.map.resize");
            popup.Description = string.Empty;
            popup.buttonData = new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (Action)delegate{CustomInput.RemoveInputFromPopup(popup);}, -1, true, null),
                new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (Action)delegate{Resize();}, -1, true, null)
            };
            //void Exit(int id, BaseEventData eventData)
            //{
            //   CustomInput.RemoveInputFromPopup(popup);
            //}
            void Resize(){
                var input = CustomInput.GetInputFromPopup(popup);
                if(input != null && int.TryParse(input.text, out int size))
                {
                    GameState gameState = GameManager.GameState;
                    if(size != gameState.Map.Width)
                        Main.UnsavedChanges = true;
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

    private static void CreateGenerateResourcesButton(UIRoundButton referenceButton)
    {
        string spriteName = SpriteData.ResourceToString(Polytopia.Data.ResourceData.Type.Fruit);
        Sprite icon = spriteAtlasManager.DoSpriteLookup(spriteName, PolytopiaBackendBase.Common.TribeType.Xinxi, PolytopiaBackendBase.Common.SkinType.Default).sprite;
        CreateToolButton(referenceButton, verticalLayout!, icon, Localization.Get("mapmaker.resources"), ShowPopup);

        void ShowPopup()
        {
            BasicPopup popup = PopupManager.GetBasicPopup();
            popup.Header = Localization.Get("mapmaker.resources");
            popup.Description = Localization.Get("mapmaker.resources.desc");
            popup.buttonData = new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (Action)delegate{}, -1, true, null),
                new PopupBase.PopupButtonData("mapmaker.resources.replace", PopupBase.PopupButtonData.States.None, (Action)delegate{ Generate(true); }, -1, true, null),
                new PopupBase.PopupButtonData("mapmaker.resources.fill", PopupBase.PopupButtonData.States.None, (Action)delegate{ Generate(false); }, -1, true, null)
            };
            popup.Show();
        }

        void Generate(bool replaceExisting)
        {
            GameState gameState = GameManager.GameState;
            int placed = ResourceGenerator.Generate(gameState, replaceExisting);
            if(placed > 0 || replaceExisting)
                Main.UnsavedChanges = true;
            GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
            NotificationManager.Notify(
                Localization.Get("mapmaker.resources.done", new Il2CppSystem.Object[] { placed }),
                Localization.Get("mapmaker.resources")
            );
        }
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
            if(picker.UseVerticalLayout)
            {
                picker.Create(referenceButton, verticalLayout);
            }
            else
            {
                picker.Create(referenceButton, horizontalLayout);
            }
            picker.Update(gameLogicData);
        }

        CreateSaveButton(referenceButton);
        CreateRenameButton(referenceButton);
        CreateMapResizeButton(referenceButton);
        CreateGenerateResourcesButton(referenceButton);
        CreatePaintButton(referenceButton);
        LayoutRebuilder.ForceRebuildLayoutImmediate(horizontalLayout);
        LayoutRebuilder.ForceRebuildLayoutImmediate(verticalLayout);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    private static void GameManager_Update()
    {
         if(!Main.isActive)
             return;

        if(Input.GetKey(KeyCode.LeftControl))
        {
            if(Input.GetKeyDown(KeyCode.S))
                SaveMap();
            if(Input.GetKeyDown(KeyCode.W))
                ShowRenamePopup();
        }
    }

    private static void SaveMap()
    {
        if(!Main.MapSaved && Main.MapName == Localization.Get("mapmaker.map.untitled"))
        {
            BasicPopupLegacy popup = GameSetup_UI2.GetBasicPopupLegacy();
            popup.Header = Localization.Get("mapmaker.map.rename");
            popup.Description = Localization.Get("mapmaker.map.rename.onsave");
            popup.buttonData = new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (Action)delegate{CustomInput.RemoveInputFromPopup(popup);}, -1, true, null),
                new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (Action)delegate{Save();}, -1, true, null)
            };
            void Save()
            {
                var input = CustomInput.GetInputFromPopup(popup);
                if(input != null)
                {
                    Main.MapName = input.text;
                    WriteMap();
                    CustomInput.RemoveInputFromPopup(popup);
                }
            }
            CustomInput.AddInputToPopup(popup, Main.MapName);
        }
        else
        {
            WriteMap();
        }
    }

    private static void WriteMap()
    {
        Main.MapSaved = IO.SaveMap(Main.MapName, (ushort)Math.Sqrt(GameManager.GameState.Map.Tiles.Length),
                                GameManager.GameState.Map.Tiles.ToArray().ToList(), Main.currCapitals);
        if(Main.MapSaved)
            Main.UnsavedChanges = false;
    }

    private static void ShowRenamePopup()
    {
        BasicPopupLegacy popup = GameSetup_UI2.GetBasicPopupLegacy();
        popup.Header = Localization.Get("mapmaker.map.rename");
        popup.Description = Localization.Get("mapmaker.map.rename.onclick");

        popup.buttonData = new PopupBase.PopupButtonData[]
        {
            new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (Action)delegate{CustomInput.RemoveInputFromPopup(popup);}, -1, true, null),
            new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (Action)delegate{SaveName();}, -1, true, null)
        };
        void SaveName()
        {
            var input = CustomInput.GetInputFromPopup(popup);
            if(input != null)
            {
                if(!string.IsNullOrEmpty(input.text) && input.text != Main.MapName)
                {
                    Main.MapName = input.text;
                    Main.UnsavedChanges = true;
                }
                NotificationManager.Notify(
                    Localization.Get("mapmaker.map.newname", new Il2CppSystem.Object[] { Main.MapName }),
                    Localization.Get("mapmaker.map.nameset")
                );
                CustomInput.RemoveInputFromPopup(popup);
            }
        }
        CustomInput.AddInputToPopup(popup, Main.MapName);
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

        text.GetComponent<TMPLocalizer>().Text = Main.DisplayMapName;
        text.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        mapNameContainer = text.transform;
    }

    public static void MapResizeValueChanged(string value, BasicPopupLegacy popup)
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

        UIRoundButton uiroundButton = __instance.CreateRoundBottomBarButton(Localization.Get("mapmaker.capital.set"), false);
        uiroundButton.sprite = PolyMod.Registry.GetSprite("capital_icon");
        uiroundButton.OnClicked += (UIButtonBase.ButtonAction)setcapitalmethod;
        void setcapitalmethod(int id, BaseEventData baseEventData)
        {
            BasicPopupLegacy popup = GameSetup_UI2.GetBasicPopupLegacy();
            popup.Header = Localization.Get("mapmaker.capital.header");
            popup.Description = Localization.Get("mapmaker.capital.desc", new Il2CppSystem.Object[]{1, 254, 0});
            popup.buttonData = new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (Action)delegate{Popup.CustomInput.RemoveInputFromPopup(popup);}, -1, true, null),
                new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (Action)delegate{Save();}, -1, true, null)
            };
            //void Exit(int id, BaseEventData eventData)
            //{
            //    Popup.CustomInput.RemoveInputFromPopup(popup);
            //}
            void Save()
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
        Capital? capitalById = Loader.GetCapital(ID, Main.currCapitals);
        if (ID == 0)
        { 
            if (capitalByCoords != null)
            {
                Main.currCapitals.Remove(capitalByCoords);
                Main.UnsavedChanges = true;
                NotificationManager.Notify(Localization.Get("mapmaker.capital.removed"));
            }
        }
        else if (capitalById != null) NotificationManager.Notify(Localization.Get("mapmaker.capital.already"));
        else
        {
            if(capitalByCoords != null)
            {
                NotificationManager.Notify(
                    Localization.Get(
                        "mapmaker.capital.override.desc",
                        new Il2CppSystem.Object[] {
                            capitalByCoords.player,
                            ID
                        }
                    ), 
                    Localization.Get("mapmaker.capital.override")
                );
                Main.currCapitals.Remove(capitalByCoords);
            }
            Capital newCapital = new();
            newCapital.player = ID;
            newCapital.coordinates = coords;
            Main.currCapitals.Add(newCapital);
            Main.UnsavedChanges = true;
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
            __instance.description.text = Localization.Get(
                "mapmaker.capital",
                new Il2CppSystem.Object[] {
                    capital.player
                }
            );
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnNextTurn))]
    internal static bool HudScreen_OnNextTurn(bool forceConfirmation)
    {
        return !Main.isActive;
    }
}