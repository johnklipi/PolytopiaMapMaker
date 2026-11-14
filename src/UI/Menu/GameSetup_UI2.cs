using HarmonyLib;
using Il2CppInterop.Runtime;
using PolytopiaBackendBase.Game;
using UnityEngine;
using PolytopiaMapManager.Popup;

namespace PolytopiaMapManager.UI.Menu;

public static class GameSetup_UI2
{
    private static GameSetupScreen_UI2? gameSetupScreen_UI2 = null;
    private static UIHorizontalList_UI2? generationSelectorList = null;
    private static UIHorizontalListData? generationSelectorListData = null;
    private static UIHorizontalList_UI2? mapSelectorList = null;
    private static UIHorizontalListData? mapSelectorListData = null;
    private static MapGenerationType chosenMapGenType = MapGenerationType.Default;
    private static List<string> visualMaps = new();
    private static int chosenMapIndex = 0;
    private static string? loadedMapName = null;
    private static TextField_UI2? customWidthHeader = null;
    private static PolytopiaInputTextField_UI2? customWidthInput = null;
    private const int DEFAULT_CUSTOM_WIDTH = 40;
    private static int lastCustomWidth = DEFAULT_CUSTOM_WIDTH;

    private static int CustomSizeId => (int)EnumCache<MapSize>.GetType("custom");

    private static bool IsCustomWidth(int width)
    {
        if (width <= 0)
            return false;
        foreach (MapSize size in new[] { MapSize.Tiny, MapSize.Small, MapSize.Normal, MapSize.Large, MapSize.Huge, MapSize.Massive })
        {
            if (size.ToMapWidth() == width)
                return false;
        }
        return true;
    }

    public enum MapGenerationType
    {
        Default,
        Custom
    }

    internal static bool CustomMapsSupported(GameSettings settings)
    {
        return settings.GameType == GameType.PassAndPlay
            || (settings.GameType == GameType.SinglePlayer && settings.BaseGameMode == GameMode.Custom);
    }

    private static bool UsingCustomMap(GameSettings settings)
    {
        return chosenMapGenType == MapGenerationType.Custom
            && visualMaps.Count > 0
            && CustomMapsSupported(settings);
    }

    internal static int GetMaxPlayers(Data.MapInfo map)
    {
        HashSet<(int, int)> cityTiles = new();
        for (int i = 0; i < map.map.Count; i++)
        {
            if (map.map[i].improvement == Polytopia.Data.ImprovementData.Type.City)
                cityTiles.Add((i % map.size, i / map.size));
        }
        foreach (Data.Capital capital in map.capitals)
            cityTiles.Add((capital.coordinates.x, capital.coordinates.y));
        return cityTiles.Count;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.Init))]
    private static void GameSetupScreen_UI2_Init(GameSetupScreen_UI2 __instance, UnityEngine.RectTransform holder)
    {
        gameSetupScreen_UI2 = __instance;
    }

    #region New Lists

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSetupScreenView), nameof(GameSetupScreenView.Init))]
    private static void GameSetupScreenView_Init(GameSetupScreenView __instance, UnityEngine.RectTransform holder)
    {
        generationSelectorList = UILibrary.NewHorizontalList(__instance.scroller.content);
        generationSelectorList.ActiveSelf = false;
        generationSelectorList.gameObject.name = "generationSelectorList";
        generationSelectorList.OnItemSelected().Add(DelegateSupport.ConvertDelegate<Il2CppSystem.Action<int>>(OnGenerationSelectorChanged));

        mapSelectorList = UILibrary.NewHorizontalList(__instance.scroller.content);
        mapSelectorList.ActiveSelf = false;
        mapSelectorList.gameObject.name = "mapSelectorList";
        mapSelectorList.OnItemSelected().Add(DelegateSupport.ConvertDelegate<Il2CppSystem.Action<int>>(OnMapSelectorChanged));

        customWidthHeader = UILibrary.NewHeaderText(__instance.scroller.content, "customWidthHeader");
        customWidthHeader.ActiveSelf = false;
        customWidthInput = UILibrary.NewInputField(__instance.scroller.content, "customWidthInput");
        customWidthInput.ActiveSelf = false;
        customWidthInput.SetWidth(300f);
        customWidthInput.inputField.contentType = TMPro.TMP_InputField.ContentType.IntegerNumber;
        customWidthInput.inputField.characterLimit = 3;
        customWidthInput.SetPlaceholderText(DEFAULT_CUSTOM_WIDTH.ToString());
        customWidthInput.inputField.onEndEdit.AddListener((UnityEngine.Events.UnityAction<string>)OnCustomWidthEntered);
    }

    private static void OnCustomWidthEntered(string value)
    {
        GameSettings settings = GameManager.PreliminaryGameSettings;
        int width = int.TryParse(value, out int parsed) ? parsed : lastCustomWidth;
        width = Math.Clamp(width, (int)Loader.MIN_MAP_SIZE, (int)Loader.MAX_MAP_SIZE);
        if (IsCustomWidth(width))
            lastCustomWidth = width;
        customWidthInput?.SetText(width.ToString());
        if (settings.MapSize == width)
            return;
        settings.MapSize = width;
        settings.SaveToDisk();
        gameSetupScreen_UI2?.UpdateLayout();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.RunLayout))]
    private static void GameSetupScreen_UI2_RunLayout_Prefix()
    {
        visualMaps = IO.GetAllMaps();
        if (chosenMapIndex >= visualMaps.Count)
            chosenMapIndex = 0;

        GameSettings settings = GameManager.PreliminaryGameSettings;
        if (settings.MapSize > Loader.MAX_MAP_SIZE)
            settings.MapSize = lastCustomWidth;

        if (!UsingCustomMap(settings))
        {
            Loader.chosenMap = null;
            loadedMapName = null;
            return;
        }

        string mapName = visualMaps[chosenMapIndex];
        if (Loader.chosenMap == null || loadedMapName != mapName)
        {
            Loader.chosenMap = IO.LoadMap(mapName);
            loadedMapName = mapName;
        }
        if (Loader.chosenMap == null)
            return;

        settings.MapSize = Loader.chosenMap.size;

        if (settings.GameType == GameType.SinglePlayer)
        {
            int maxOpponents = Math.Max(GetMaxPlayers(Loader.chosenMap) - 1, 0);
            if (settings.OpponentCount > maxOpponents)
                settings.OpponentCount = maxOpponents;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSetupScreenView), nameof(GameSetupScreenView.SetShowOpponents))]
    private static void GameSetupScreenView_SetShowOpponents(GameSetupScreenView __instance, Il2CppSystem.Collections.Generic.List<string> labels)
    {
        GameSettings settings = GameManager.PreliminaryGameSettings;
        if (Loader.chosenMap == null || !UsingCustomMap(settings) || settings.GameType != GameType.SinglePlayer)
            return;

        int maxOpponents = GetMaxPlayers(Loader.chosenMap) - 1;
        for (int i = 0; i < labels.Count; i++)
        {
            if (int.TryParse(labels[i], out int count) && count > maxOpponents)
                __instance.listOpponentCount.SetItemEnabled(i, false);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameSetupScreenView), nameof(GameSetupScreenView.RunLayout))]
    private static bool GameSetupScreenView_RunLayout(GameSetupScreenView __instance, ScreenBase_UI2.ScreenSize screenSize)
    {
        GameSettings settings = GameManager.PreliminaryGameSettings;
        GameSetupScreenView.Show whatToShow = __instance.whatToShow;
        bool showGenerationRows = generationSelectorList != null && mapSelectorList != null
            && visualMaps.Count > 0
            && CustomMapsSupported(settings)
            && whatToShow.HasFlag(GameSetupScreenView.Show.MapSizeList);
        bool usingCustomMap = showGenerationRows && UsingCustomMap(settings) && Loader.chosenMap != null;
        bool showWidthInput = customWidthHeader != null && customWidthInput != null
            && !usingCustomMap
            && whatToShow.HasFlag(GameSetupScreenView.Show.MapSizeList)
            && IsCustomWidth(settings.MapSize);

        if (generationSelectorList != null) generationSelectorList.ActiveSelf = false;
        if (mapSelectorList != null) mapSelectorList.ActiveSelf = false;
        if (!showWidthInput)
        {
            if (customWidthHeader != null) customWidthHeader.ActiveSelf = false;
            if (customWidthInput != null) customWidthInput.ActiveSelf = false;
        }
        if (!showGenerationRows && !showWidthInput)
            return true;

        float width = screenSize.screenRect.Width;
        float height = screenSize.safeRect.Height;
        float textWidth = width - 20f;

        if (showGenerationRows)
        {
            generationSelectorListData = new UIHorizontalListData(2, Localization.Get("gamesettings.generationtype"));
            generationSelectorListData.AddItem(Localization.Get("gamesettings.generationtype.default"), (int)MapGenerationType.Default);
            generationSelectorListData.AddItem(Localization.Get("gamesettings.generationtype.custom"), (int)MapGenerationType.Custom);
            generationSelectorList!.SetData(generationSelectorListData.header, generationSelectorListData.GetLabels(), usingCustomMap ? 1 : 0);
            generationSelectorList.SetWidth(width);

            mapSelectorListData = new UIHorizontalListData(visualMaps.Count, Localization.Get("gamesettings.maps"));
            for (int i = 0; i < visualMaps.Count; i++)
                mapSelectorListData.AddItem(visualMaps[i], i);
            mapSelectorList!.SetData(mapSelectorListData.header, mapSelectorListData.GetLabels(), chosenMapIndex);
            mapSelectorList.SetWidth(width);
        }
        if (showWidthInput)
        {
            customWidthHeader!.Text = string.Format(Localization.Get("gamesettings.mapwidth.range"), Loader.MIN_MAP_SIZE, Loader.MAX_MAP_SIZE);
            customWidthHeader.SetWidth(textWidth);
            customWidthHeader.UpdateSize();
            if (!customWidthInput!.inputField.isFocused)
                customWidthInput.SetText(settings.MapSize.ToString());
        }

        __instance.holder.SetSize(width, height);
        foreach (IUILayoutable component in __instance.allComponents)
            component.ActiveSelf = false;
        foreach (UIHorizontalList_UI2 list in __instance.allLists)
            list.SetWidth(width);

        bool Has(GameSetupScreenView.Show flag) => whatToShow.HasFlag(flag);
        List<IUILayoutable> rows = new(64);
        void Add(Il2CppSystem.Object item) => rows.Add(item.Cast<IUILayoutable>());
        void AddText(TextField_UI2 text)
        {
            text.SetWidth(textWidth);
            text.UpdateSize();
            Add(text);
            rows.Add(__instance.GetSpacer(1));
        }

        if (Has(GameSetupScreenView.Show.GameNameField))
        {
            Add(__instance.gameNameComponent);
            rows.Add(__instance.GetSpacer(1));
        }
        if (Has(GameSetupScreenView.Show.NetworkList))
            Add(__instance.listNetwork);
        if (Has(GameSetupScreenView.Show.NetworkDescriptionText))
            AddText(__instance.networkDescriptionText);
        if (Has(GameSetupScreenView.Show.TurnTimeLimitList) && !Has(GameSetupScreenView.Show.TurnTimeLimitListAtBottom))
            Add(__instance.listTurnTimeLimit);
        if (Has(GameSetupScreenView.Show.GameModeList))
            Add(__instance.listGameMode);
        if (Has(GameSetupScreenView.Show.ScoreLimitList))
            Add(__instance.listScoreLimit);
        if (Has(GameSetupScreenView.Show.GameModeDescriptionText))
            AddText(__instance.gameModeDescriptionText);
        if (Has(GameSetupScreenView.Show.OpponentsList))
            Add(__instance.listOpponentCount);
        if (Has(GameSetupScreenView.Show.DifficultyList))
            Add(__instance.listDifficulty);
        if (Has(GameSetupScreenView.Show.GameConfigurationDescription))
            AddText(__instance.gameConfigurationDescriptionText);
        if (Has(GameSetupScreenView.Show.AdvancedSettingsToggle))
        {
            rows.Add(__instance.GetSpacer(3));
            Add(__instance.advancedSettingsToggle);
            rows.Add(__instance.GetSpacer(1));
        }

        if (showGenerationRows)
            Add(generationSelectorList!);
        if (usingCustomMap)
        {
            Add(mapSelectorList!);
            Data.MapInfo map = Loader.chosenMap!;
            __instance.mapSizeDescriptionText.Text = string.Format(
                Localization.Get("gamesettings.custommap.info"),
                map.size,
                LocalizationUtils.FormatNumber(map.size * map.size),
                GetMaxPlayers(map)
            );
            AddText(__instance.mapSizeDescriptionText);
        }
        else
        {
            if (Has(GameSetupScreenView.Show.MapTypeList))
                Add(__instance.listMapType);
            if (Has(GameSetupScreenView.Show.MapSizeList))
                Add(__instance.listMapSize);
            if (showWidthInput)
            {
                Add(customWidthHeader!);
                Add(customWidthInput!);
                rows.Add(__instance.GetSpacer(1));
            }
            if (Has(GameSetupScreenView.Show.MapSizeDescriptionText))
                AddText(__instance.mapSizeDescriptionText);
            else
                rows.Add(__instance.GetSpacer(1));
        }

        if (Has(GameSetupScreenView.Show.TurnTimeLimitList) && Has(GameSetupScreenView.Show.TurnTimeLimitListAtBottom))
            Add(__instance.listTurnTimeLimit);
        if (Has(GameSetupScreenView.Show.ContinueButton))
        {
            rows.Add(__instance.GetSpacer(2));
            Add(__instance.continueButton);
        }

        float totalHeight = 0f;
        foreach (IUILayoutable row in rows)
            totalHeight += row.GetHeight() + 10f;
        totalHeight = __instance.scroller.LayoutPageScroller(ref screenSize, totalHeight, Has(GameSetupScreenView.Show.ScrollToTop));
        __instance.scroller.LayoutTopFade(screenSize);

        float y = totalHeight / 2f;
        foreach (IUILayoutable row in rows)
        {
            row.ActiveSelf = true;
            row.SetPositionTopY(0f, y);
            y -= row.GetHeight() + 10f;
            UIHorizontalList_UI2? horizontalList = row.TryCast<UIHorizontalList_UI2>();
            if (horizontalList != null)
                horizontalList.UpdateLayout();
        }
        return false;
    }

    private static void OnGenerationSelectorChanged(int index)
    {
        if (generationSelectorListData == null)
            return;

        chosenMapGenType = (MapGenerationType)generationSelectorListData.GetId(index);
        GameSettings settings = GameManager.PreliminaryGameSettings;
        if (chosenMapGenType == MapGenerationType.Default)
        {
            Loader.chosenMap = null;
            loadedMapName = null;
            settings.MapSize = settings.FigureOutPreferredMapSize();
        }
        settings.SaveToDisk();
        gameSetupScreen_UI2?.UpdateLayout();
    }

    private static void OnMapSelectorChanged(int index)
    {
        Main.modLogger!.LogInfo("OnCustomMapChanged: " + index);
        if (index < 0 || index >= visualMaps.Count)
            return;

        chosenMapIndex = index;
        gameSetupScreen_UI2?.UpdateLayout();
    }
    #endregion


    #region Custom Map Size

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIHorizontalListData), nameof(UIHorizontalListData.AddItem))]
    private static void AddItem_Postfix(UIHorizontalListData __instance, string label, int id)
    {
        if (__instance == null) return;
        if (label == null || __instance.labels == null || label != Localization.Get("gamesettings.size.massive")) return;
        if (__instance.header != Localization.Get("gamesettings.size")) return;

        for (int i = 0; i < __instance.labels.Count; i++)
        {
            if (__instance.labels[i] == Localization.Get("gamesettings.size.custom")) return;
        }
        int Id = (int)EnumCache<MapSize>.GetType("custom");
        __instance.AddItem(Localization.Get("gamesettings.size.custom"), Id);

    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.OnMapSizeChanged))]
    private static bool OnMapSizeChanged(GameSetupScreen_UI2 __instance, int index)
    {
        if (__instance.mapSizeData == null || __instance.mapSizeData.GetId(index) != CustomSizeId)
            return true;

        GameSettings settings = GameManager.PreliminaryGameSettings;
        if (!IsCustomWidth(settings.MapSize) || settings.MapSize > Loader.MAX_MAP_SIZE)
            settings.MapSize = lastCustomWidth;
        settings.SaveToDisk();
        __instance.UpdateLayout();
        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIHorizontalListData), nameof(UIHorizontalListData.IndexFromId))]
    private static void ByPassVerification(UIHorizontalListData __instance, ref int __result, int id)
    {
        if (__result != -1 || gameSetupScreen_UI2 == null || gameSetupScreen_UI2.mapSizeData == null)
            return;
        if (__instance.header == gameSetupScreen_UI2.mapSizeData.header)
        {
            int i = 0;
            foreach (var label in __instance.labels)
            {
                if (label == Localization.Get("gamesettings.size.custom"))
                {
                    __result = i;
                    return;
                }
                i++;
            }
        }
    }

    #endregion

    #region LegacyPopup
    private const int BASIC_POPUP_LEGACY_ID = 29;
    internal static BasicPopupLegacy GetBasicPopupLegacy()
    {
        WhatsNewPopup whatsNewPopup = PopupManager.GetWhatsNewPopup();
        BasicPopupLegacy original = PopupManager.instance.popupPrefabs[BASIC_POPUP_LEGACY_ID].Cast<BasicPopupLegacy>();
        BasicPopupLegacy basicPopupLegacy = UnityEngine.Object.Instantiate(original, PopupManager.instance.transform);
        basicPopupLegacy.buttonContainer = GameObject.Instantiate(whatsNewPopup.buttonContainer, basicPopupLegacy.transform);
        basicPopupLegacy.popupId = "basicPopupLegacy";
        basicPopupLegacy.popupManager = PopupManager.instance;
        basicPopupLegacy.Init();
        basicPopupLegacy.identifier = null;
        basicPopupLegacy.rectTransform.SetAsLastSibling();
        return basicPopupLegacy;
    }

    #endregion
}