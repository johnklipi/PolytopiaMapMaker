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
    internal static RectTransform? leftRow;
    internal static RectTransform? rightRow;
    internal static SpriteAtlasManager spriteAtlasManager = GameManager.GetSpriteAtlasManager();
    private static bool leaveConfirmed = false;

    internal static void ConfirmDiscard(Action proceed)
    {
        if(!Main.HasUnsavedEdits)
        {
            proceed();
            return;
        }

        BasicPopup popup = PopupManager.GetBasicPopup();
        popup.Header = Localization.Get("mapmaker.unsaved");
        popup.Description = Localization.Get("mapmaker.unsaved.desc");
        popup.buttonData = new PopupBase.PopupButtonData[]
        {
            new PopupBase.PopupButtonData("buttons.stay", PopupBase.PopupButtonData.States.None, (Action)delegate{}, -1, true, null),
            new PopupBase.PopupButtonData("buttons.leave", PopupBase.PopupButtonData.States.None, proceed, -1, true, null)
        };
        popup.Show();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.ReturnToMenu))]
    private static bool GameManager_ReturnToMenu_Prefix()
    {
        if(leaveConfirmed || !Main.HasUnsavedEdits)
        {
            leaveConfirmed = false;
            return true;
        }

        ConfirmDiscard(() =>
        {
            leaveConfirmed = true;
            GameManager.ReturnToMenu();
        });
        return false;
    }

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

    private const float INTERACTION_BAR_GAP = 50f;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HudButtonBar), nameof(HudButtonBar.UpdateRightAlignment))]
    private static bool HudButtonBar_UpdateRightAlignment(HudButtonBar __instance)
    {
        if(!Main.isActive)
            return true;

        InteractionBar interactionBar = __instance.hudScreen.InteractionBar;
        if(interactionBar.type != InteractionBar.Type.PcBar)
        {
            __instance.SetRightAlign(false);
            return false;
        }

        float menuButtonWidth = __instance.menuButton.rectTransform.WorldRect().width;
        float centeredMenuButtonStart = __instance.rectTransform.WorldRect().center.x - menuButtonWidth / 2f;
        bool overlapsInteractionBar = interactionBar.rectTransform.WorldRect().xMax > centeredMenuButtonStart - INTERACTION_BAR_GAP;
        __instance.SetRightAlign(overlapsInteractionBar);
        return false;
    }

    private const float ROW_ANCHOR_Y = 0.95f;
    private const float ROW_MARGIN = 20f;
    private const float ROW_SPACING = 20f;
    private const float COMPACT_NAME_TO_ROW = 80f;
    private const float COMPACT_ROW_STEP = 130f;
    private static bool compactLayout = false;

    internal static void CreateEditorUI(Transform hudScreen)
    {
        editorUi = CreateRect("EditorUI", hudScreen);
        editorUi.anchorMin = Vector2.zero;
        editorUi.anchorMax = Vector2.one;
        editorUi.offsetMin = Vector2.zero;
        editorUi.offsetMax = Vector2.zero;

        leftRow = CreateRow("LeftRow", TextAnchor.MiddleLeft);
        rightRow = CreateRow("RightRow", TextAnchor.MiddleRight);
        ApplyLayout(false);
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, new Il2CppSystem.Type[] { Il2CppType.Of<RectTransform>() });
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static RectTransform CreateRow(string name, TextAnchor alignment)
    {
        RectTransform row = CreateRect(name, editorUi!);
        row.sizeDelta = Vector2.zero;

        HorizontalLayoutGroup layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = ROW_SPACING;
        layout.childAlignment = alignment;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        row.gameObject.AddComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        return row;
    }

    private static void PlaceMapName()
    {
        if(mapNameContainer == null || editorUi == null)
            return;

        mapNameContainer.SetParent(editorUi, false);
        ApplyLayout(compactLayout);
    }

    private static void UpdateLayout()
    {
        if(editorUi == null || leftRow == null || rightRow == null || mapNameContainer == null)
            return;

        bool compact = !MapNameFitsBetweenRows();
        if(compact != compactLayout)
            ApplyLayout(compact);
    }

    private static bool MapNameFitsBetweenRows()
    {
        float screenWidth = editorUi!.rect.width;
        float nameHalfWidth = GetMapNameWidth() / 2f;
        float leftRowEnd = ROW_MARGIN + leftRow!.rect.width + ROW_SPACING;
        float rightRowStart = screenWidth - ROW_MARGIN - rightRow!.rect.width - ROW_SPACING;
        return screenWidth / 2f - nameHalfWidth >= leftRowEnd
            && screenWidth / 2f + nameHalfWidth <= rightRowStart;
    }

    private static float GetMapNameWidth()
    {
        TextMeshProUGUI text = mapNameContainer!.GetComponent<TextMeshProUGUI>();
        return Mathf.Min(text.preferredWidth, text.rectTransform.rect.width);
    }

    private static void ApplyLayout(bool compact)
    {
        compactLayout = compact;
        if(compact)
        {
            PlaceAtTop(leftRow, 0f, new Vector2(ROW_MARGIN, -COMPACT_NAME_TO_ROW));
            PlaceAtTop(rightRow, 0f, new Vector2(ROW_MARGIN, -COMPACT_NAME_TO_ROW - COMPACT_ROW_STEP));
            PlaceMapNameAtTop(0f, new Vector2(ROW_MARGIN, 0f), TextAlignmentOptions.Left);
        }
        else
        {
            PlaceAtTop(leftRow, 0f, new Vector2(ROW_MARGIN, 0f));
            PlaceAtTop(rightRow, 1f, new Vector2(-ROW_MARGIN, 0f));
            PlaceMapNameAtTop(0.5f, Vector2.zero, TextAlignmentOptions.Center);
        }
    }

    private static void PlaceMapNameAtTop(float anchorX, Vector2 position, TextAlignmentOptions alignment)
    {
        if(mapNameContainer == null)
            return;

        TextMeshProUGUI text = mapNameContainer.GetComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        PlaceAtTop(text.rectTransform, anchorX, position);
    }

    private static void PlaceAtTop(RectTransform? rect, float anchorX, Vector2 position)
    {
        if(rect == null)
            return;

        Vector2 anchor = new Vector2(anchorX, ROW_ANCHOR_Y);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(anchorX, 0.5f);
        rect.anchoredPosition = position;
    }

    private static readonly Vector2 TOOL_BUTTON_SIZE = new Vector2(75f, 75f);
    private const float TOOL_ICON_SIZE = 0.6f;
    private const float RESOURCE_POPUP_WIDTH_SHARE = 0.6f;
    private const float RESOURCE_POPUP_MAX_WIDTH = 1000f;
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
        CreateToolButton(referenceButton, rightRow!, PolyMod.Registry.GetSprite("save_icon"), Localization.Get("mapmaker.map.save"), SaveMap);
    }

    private static void CreateRenameButton(UIRoundButton referenceButton)
    {
        CreateToolButton(referenceButton, rightRow!, PolyMod.Registry.GetSprite("rename_icon"), Localization.Get("mapmaker.map.rename.button"), ShowRenamePopup);
    }

    private static void CreatePaintButton(UIRoundButton referenceButton)
    {
        paintButton = CreateToolButton(referenceButton, leftRow!, PolyMod.Registry.GetSprite("paint_icon"), string.Empty, TogglePaintMode);
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
        CreateToolButton(referenceButton, rightRow!, PolyMod.Registry.GetSprite("resize_icon"), Localization.Get("mapmaker.map.resize"), ShowMapPopup);

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
                    bool changed = size != gameState.Map.Width;
                    Main.ResizeMap(ref gameState, size);
                    if(changed)
                    {
                        Main.UnsavedChanges = true;
                        History.RecordResize(gameState, size);
                    }
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
        CreateToolButton(referenceButton, rightRow!, icon, Localization.Get("mapmaker.resourcegen"), ShowPopup);

        void ShowPopup()
        {
            BasicPopupLegacy popup = GameSetup_UI2.GetBasicPopupLegacy();
            popup.Header = Localization.Get("mapmaker.resourcegen");
            popup.Description = Localization.Get("mapmaker.resourcegen.desc");
            popup.buttonData = new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (Action)delegate{}, -1, true, null),
                new PopupBase.PopupButtonData("mapmaker.resourcegen.replace", PopupBase.PopupButtonData.States.None, (Action)delegate{ Generate(true); }, -1, true, null),
                new PopupBase.PopupButtonData("mapmaker.resourcegen.fill", PopupBase.PopupButtonData.States.None, (Action)delegate{ Generate(false); }, -1, true, null)
            };
            PolyMod.Managers.Visual.ShowSetWidth(popup, RESOURCE_POPUP_WIDTH_SHARE, RESOURCE_POPUP_MAX_WIDTH);
        }

        void Generate(bool replaceExisting)
        {
            GameState gameState = GameManager.GameState;
            TileData[] tiles = gameState.Map.Tiles;
            int[] before = tiles.Select(GetResourceId).ToArray();
            int placed = ResourceGenerator.Generate(gameState, replaceExisting);
            List<WorldCoordinates> changed = new();
            for (int i = 0; i < tiles.Length; i++)
            {
                if(GetResourceId(tiles[i]) != before[i])
                    changed.Add(tiles[i].coordinates);
            }
            if(changed.Count > 0)
            {
                Main.UnsavedChanges = true;
                History.RecordTiles(gameState, changed);
            }
            GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
            NotificationManager.Notify(
                Localization.Get("mapmaker.resourcegen.done", new Il2CppSystem.Object[] { placed }),
                Localization.Get("mapmaker.resourcegen")
            );
        }

        static int GetResourceId(TileData tile)
        {
            return tile.resource == null ? -1 : (int)tile.resource.type;
        }
    }

    private static void CreateRevertButton(UIRoundButton referenceButton)
    {
        CreateToolButton(referenceButton, leftRow!, PolyMod.Registry.GetSprite("revert_icon"), Localization.Get("mapmaker.revert"), Revert);
    }

    private static void Revert()
    {
        Brush.CommitStroke();
        if(History.RewindOne())
            Main.UnsavedChanges = true;
        else
            NotificationManager.Notify(Localization.Get("mapmaker.revert.empty"), Localization.Get("mapmaker.revert"));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
    private static void HudScreen_OnMatchStart(HudScreen __instance)
    {
        if (!Main.isActive)
            return;

        CreateEditorUI(__instance.transform);
        if(editorUi == null || leftRow == null || rightRow == null)
            return;
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
            picker.Create(referenceButton, picker.UseRightRow ? rightRow : leftRow);
            picker.Update(gameLogicData);
        }

        CreateSaveButton(referenceButton);
        CreateRenameButton(referenceButton);
        CreateMapResizeButton(referenceButton);
        CreateGenerateResourcesButton(referenceButton);
        // CreatePaintButton(referenceButton);
        CreateRevertButton(referenceButton);
        LayoutRebuilder.ForceRebuildLayoutImmediate(leftRow);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rightRow);
        PlaceMapName();
        History.Reset(GameManager.GameState);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    private static void GameManager_Update()
    {
        if(!Main.isActive)
            return;

        UpdateLayout();

        if(Input.GetKey(KeyCode.LeftControl))
        {
            if(Input.GetKeyDown(KeyCode.S))
                SaveMap();
            if(Input.GetKeyDown(KeyCode.W))
                ShowRenamePopup();
            if(Input.GetKeyDown(KeyCode.Z))
                Revert();
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

        if(mapNameContainer != null)
        {
            Main.RefreshMapNameLabel();
            return;
        }

        TextMeshProUGUI text = GameObject.Instantiate(__instance.currencyContainer.headerLabel, __instance.turnsContainer.transform.parent);
        text.name = "MapName";

        RectTransform rect = text.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, rect.sizeDelta.y);

        text.fontSize = 35;

        text.GetComponent<TMPLocalizer>().Text = Main.DisplayMapName;
        text.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        mapNameContainer = text.transform;
        PlaceMapName();
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
                History.RecordTiles(GameManager.GameState, new List<WorldCoordinates>());
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
            History.RecordTiles(GameManager.GameState, new List<WorldCoordinates>());
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