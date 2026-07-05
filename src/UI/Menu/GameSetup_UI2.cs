using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace PolytopiaMapManager.UI.Menu;

public static class GameSetup_UI2
{
    private static GameSetupScreen_UI2? gameSetupScreen_UI2 = null; // i dont like it // don't like what?
    private static UIHorizontalList_UI2? generationSelectorList = null;
    private static UIHorizontalListData? generationSelectorListData = null;
    private static MapGenerationType chosenMapGenType = MapGenerationType.Default;
    public enum MapGenerationType
    {
        Default,
        Custom
    }

    // (Hopefully) generic method for inserting list after another
    private static void InsertListAt(GameSetupScreenView view, UIHorizontalList_UI2 list, UIHorizontalList_UI2 insertAfter)
    {
        if (insertAfter == null || !insertAfter.ActiveSelf)
        {
            list.ActiveSelf = false;
            Main.modLogger!.LogError("Could not find insertAfter for horizontal list");
            return;
        }
        list.ActiveSelf = true;

        float padding = 50f;
        float y = insertAfter.GetY() - list.GetHeight() - padding;

        //Push everything beneath lower
        for (int i = 0; i < view.allComponents.Count; i++)
        {
            var iuilayoutable = view.allComponents[i];
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.RunLayout))]
    private static void GameSetupScreen_UI2_RunLayout(GameSetupScreen_UI2 __instance, ScreenBase_UI2.ScreenSize screenSize)
    {
        generationSelectorListData = new UIHorizontalListData(2, __instance.Loc("your.localization.key"));
        generationSelectorListData.AddItem("ITEM1", 0);
        generationSelectorListData.AddItem("ITEM2", 1);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSetupScreenView), nameof(GameSetupScreenView.Init))]
    private static void GameSetupScreenView_Init(GameSetupScreenView __instance, UnityEngine.RectTransform holder)
    {
        generationSelectorList = UILibrary.NewHorizontalList(__instance.scroller.content);
        generationSelectorList.ActiveSelf = false;
        generationSelectorList.gameObject.name = "test list";
        generationSelectorList.OnItemSelected().Add(DelegateSupport.ConvertDelegate<Il2CppSystem.Action<int>>(OnTestListChanged));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSetupScreenView), nameof(GameSetupScreenView.RunLayout))]
    private static void GameSetupScreenView_RunLayout(GameSetupScreenView __instance, ScreenBase_UI2.ScreenSize screenSize)
    {
        generationSelectorListData = new UIHorizontalListData(2, "Map Generation"); // Needs to be localized
        generationSelectorListData.AddItem("Default", 0);
        generationSelectorListData.AddItem("Custom", 1);

        bool inCreativeMode = __instance.listGameMode != null && __instance.listGameMode.ActiveSelf; // Custom maps only in creative mode
        if (!inCreativeMode)
        {
            if (generationSelectorList != null)
            {
                generationSelectorList.ActiveSelf = false;
            }
            if (__instance.listMapType != null) __instance.listMapType.ActiveSelf = false;
            if (__instance.listMapSize != null) __instance.listMapSize.ActiveSelf = false;

            return;
        }

        if (generationSelectorList != null && generationSelectorListData != null)
        {
            generationSelectorList.SetData(generationSelectorListData.header, generationSelectorListData.GetLabels(), 0);
            generationSelectorList.SetWidth(screenSize.screenRect.Width);
            generationSelectorList.UpdateLayout();
            InsertListAt(__instance, generationSelectorList, __instance.listDifficulty);
        }
        if (chosenMapGenType == MapGenerationType.Custom)
        {
            __instance.listMapType.ActiveSelf = false;
            __instance.listMapSize.ActiveSelf = false;
        }
        else
        {
            __instance.listMapType.ActiveSelf = true;
            __instance.listMapSize.ActiveSelf = true;
        }
    }

    private static void OnTestListChanged(int index)
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
}