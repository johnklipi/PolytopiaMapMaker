using HarmonyLib;
using Il2CppInterop.Runtime;
using PolyMod.Managers;
using PolytopiaBackendBase.Game;
using UnityEngine;
using PolytopiaMapManager.Popup;

namespace PolytopiaMapManager.UI.Menu;

public static class GameSetup_UI2
{
    private static GameSetupScreen_UI2? gameSetupScreen_UI2 = null; // i dont like it // don't like what?
    private static UIHorizontalList_UI2? generationSelectorList = null;
    private static UIHorizontalListData? generationSelectorListData = null;
    private static UIHorizontalList_UI2? mapSelectorList = null;
    private static UIHorizontalListData? mapSelectorListData = null;
    private static MapGenerationType chosenMapGenType = MapGenerationType.Default;
    private static List<string> visualMaps = new();
    public enum MapGenerationType
    {
        Default,
        Custom
    }

    // (Hopefully) generic method for inserting list after another
    private static void InsertListAt(GameSetupScreenView view, UIHorizontalList_UI2 list, UIHorizontalList_UI2 insertAfter, bool shouldPush = true, float padding = 50f)
    {
        if (insertAfter == null || !insertAfter.ActiveSelf)
        {
            list.ActiveSelf = false;
            Main.modLogger!.LogError("Could not find insertAfter for horizontal list");
            return;
        }

        list.ActiveSelf = true;

        float y = insertAfter.GetY() - list.GetHeight() - padding;

        //Push everything beneath lower
        if (shouldPush)
            foreach (var iuilayoutable in view.allComponents)
            {
                if (iuilayoutable != null)
                {
                    if (iuilayoutable.GetY() <= y && iuilayoutable.Pointer != list.Pointer && iuilayoutable.ActiveSelf)
                    {
                        iuilayoutable.SetY(iuilayoutable.GetY() - list.GetHeight());
                    }
                }
            }

        list.SetPosition(insertAfter.GetX(), y);
        list.UpdateLayout();
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
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSetupScreenView), nameof(GameSetupScreenView.RunLayout))]
    private static void GameSetupScreenView_RunLayout(GameSetupScreenView __instance, ScreenBase_UI2.ScreenSize screenSize)
    {
        visualMaps = IO.GetAllMaps();
        generationSelectorListData = new UIHorizontalListData(2, "Map Generation"); // Needs to be localized
        generationSelectorListData.AddItem("Default", 0);
        generationSelectorListData.AddItem("Custom", 1);



        mapSelectorListData = new UIHorizontalListData(visualMaps.Count + 1, "Choose map!"); //idk the text, also localize it pls
        for (int i = 0; i < visualMaps.Count; i++)
        {
            mapSelectorListData.AddItem(visualMaps[i], i);
        }

        bool inCreativeMode = __instance.listGameMode != null && __instance.listGameMode.ActiveSelf; // Custom maps only in creative mode

        if (inCreativeMode && visualMaps.Count <= 0)
        {
            if (generationSelectorList != null) generationSelectorList.ActiveSelf = false;
            if (__instance.listMapType != null) __instance.listMapType.ActiveSelf = true;
            if (__instance.listMapSize != null) __instance.listMapSize.ActiveSelf = true;
            return;
        }
        if (!inCreativeMode || visualMaps.Count <= 0)
        {
            if (generationSelectorList != null) generationSelectorList.ActiveSelf = false;
            if (__instance.listMapType != null) __instance.listMapType.ActiveSelf = false;
            if (__instance.listMapSize != null) __instance.listMapSize.ActiveSelf = false;

            return;
        }

        if (generationSelectorList != null && generationSelectorListData != null && mapSelectorList != null && mapSelectorListData != null)
        {
            generationSelectorList.SetData(generationSelectorListData.header, generationSelectorListData.GetLabels(), 0);
            generationSelectorList.SetWidth(screenSize.screenRect.Width);
            generationSelectorList.UpdateLayout();
            generationSelectorList.SelectedIndex = chosenMapGenType == MapGenerationType.Default ? 0 : 1;

            mapSelectorList.SetData(mapSelectorListData.header, mapSelectorListData.GetLabels(), 0);
            mapSelectorList.SetWidth(screenSize.screenRect.Width);
            mapSelectorList.UpdateLayout();
            // Umm nevermind, to my knowledge there isnt anything from mapData that can easily get the mapname string.
            // Not the end of the world anyways
            /*if(Loader.chosenMap != null)
                for(int j=0; j<visualMaps.Count; j++)
                {
                    if(visualMaps[j] == Loader.chosenMap.)
                        mapSelectorList.SelectedIndex = j;
                }*/

            InsertListAt(__instance, generationSelectorList, __instance.listDifficulty, true);
            InsertListAt(__instance, mapSelectorList, generationSelectorList, false, 25f);
        }
        if (chosenMapGenType == MapGenerationType.Custom)
        {
            __instance.listMapType.ActiveSelf = false;
            __instance.listMapSize.ActiveSelf = false;
            if (mapSelectorList != null) mapSelectorList.ActiveSelf = true;
        }
        else
        {
            __instance.listMapType.ActiveSelf = true;
            __instance.listMapSize.ActiveSelf = true;
            if (mapSelectorList != null) mapSelectorList.ActiveSelf = false;
        }
    }

    private static void OnGenerationSelectorChanged(int index)
    {
        if (generationSelectorListData == null)
            return;

        int id = generationSelectorListData.GetId(index);
        chosenMapGenType = (MapGenerationType)id;
        Console.Write("ON TEST LIST CHANGED");
        if (gameSetupScreen_UI2 != null)
        {
            gameSetupScreen_UI2.UpdateLayout();
        }
        else
        {
            Console.Write("GAME SETUP SCREEN IS NULL");
        }
    }

    private static void OnMapSelectorChanged(int index)
    {
        Main.modLogger!.LogInfo("OnCustomMapChanged: " + index);
        Loader.chosenMap = IO.LoadMap(visualMaps[index]);

        if (Loader.chosenMap != null)
        {
            GameManager.PreliminaryGameSettings.MapSize = Loader.chosenMap.size;
            GameManager.PreliminaryGameSettings.mapPreset = EnumCache<PolytopiaBackendBase.Game.MapPreset>.GetType("custom");
            GameSetupScreen_UI2 gameSetupScreenUI2 = UIManager.Instance.GetScreen(UIConstants.Screens.GameSetup).Cast<GameSetupScreen_UI2>();
            gameSetupScreenUI2.UpdateLayout();
            mapSelectorList!.SelectedIndex = index;
        }
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.OnMapSizeChanged))]
    private static void OnMapSizeChanged(int index)
    {
        if (gameSetupScreen_UI2 == null) return; // we're cooked

        if (gameSetupScreen_UI2.mapSizeData.ids[index] == (int)EnumCache<MapSize>.GetType("custom"))
        {
            BasicPopupLegacy popup = GameSetup_UI2.GetBasicPopupLegacy();
            popup.Header = Localization.Get("gamesettings.mapwidth", new Il2CppSystem.Object[] { Loader.MAX_MAP_SIZE });
            popup.Description = string.Empty;
            popup.buttonData = new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (Action)delegate{GameManager.PreliminaryGameSettings.MapSize = 16; CustomInput.RemoveInputFromPopup(popup); gameSetupScreen_UI2.UpdateLayout();}, -1, true, null),
                new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (Action)delegate{Resize();}, -1, true, null)
            };

            void Resize()
            {
                var input = CustomInput.GetInputFromPopup(popup);
                if (input != null && int.TryParse(input.text, out int width))
                {
                    if (width > (int)Loader.MAX_MAP_SIZE)
                    {
                        width = (int)Loader.MAX_MAP_SIZE;
                        NotificationManager.Notify(Localization.Get("mapmaker.text.long",
                                            new Il2CppSystem.Object[] { Loader.MAX_MAP_SIZE }), Localization.Get("gamemode.mapmaker"));
                    }
                    if (width < (int)Loader.MIN_MAP_SIZE)
                    {
                        NotificationManager.Notify("Too small map!"); //Add more sensible localization
                        width = (int)Loader.MIN_MAP_SIZE;
                    }
                    GameManager.PreliminaryGameSettings.MapSize = width;
                }
                else
                {
                    NotificationManager.Notify("Only numbers are allowed", "Error");
                    GameManager.PreliminaryGameSettings.MapSize = 16;
                }
                CustomInput.RemoveInputFromPopup(popup);
                gameSetupScreen_UI2!.UpdateLayout();
            }

            CustomInput.AddInputToPopup(popup, "16");
            popup.Show();

            gameSetupScreen_UI2.UpdateLayout();
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIHorizontalListData), nameof(UIHorizontalListData.IndexFromId))]
    private static void ByPassVerification(UIHorizontalListData __instance, ref int __result, int id)
    {
        if (__instance.header == gameSetupScreen_UI2!.mapSizeData.header && __result == -1)
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