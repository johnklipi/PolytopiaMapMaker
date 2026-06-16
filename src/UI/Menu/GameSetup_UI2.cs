using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace PolytopiaMapManager.UI.Menu;
public static class GameSetup_UI2
{
    private static GameSetupScreen_UI2? gameSetupScreen_UI2 = null; // i dont like it
    private static UIHorizontalList_UI2? testList = null;
    private static UIHorizontalListData? testListData = null;
    private static MapGenerationType chosenMapGenType = MapGenerationType.Default; 
    public enum MapGenerationType
    {
        Default,
        Custom
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
        testListData = new UIHorizontalListData(2, __instance.Loc("your.localization.key"));
        testListData.AddItem("ITEM1", 0);
        testListData.AddItem("ITEM2", 1);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSetupScreenView), nameof(GameSetupScreenView.Init))]
    private static void GameSetupScreenView_Init(GameSetupScreenView __instance, UnityEngine.RectTransform holder)
    {
        testList = UILibrary.NewHorizontalList(__instance.scroller.content);
        testList.gameObject.name = "test list";
        testList.OnItemSelected().Add(DelegateSupport.ConvertDelegate<Il2CppSystem.Action<int>>(OnTestListChanged));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSetupScreenView), nameof(GameSetupScreenView.RunLayout))]
    private static void GameSetupScreenView_RunLayout(GameSetupScreenView __instance, ScreenBase_UI2.ScreenSize screenSize)
    {
        testListData = new UIHorizontalListData(2, "testtt");
        testListData.AddItem("ITEM1", 0);
        testListData.AddItem("ITEM2", 1);

        if (testList != null && testListData != null)
        {
            testList.SetData(testListData.header, testListData.GetLabels(), 0);
            testList.SetWidth(screenSize.screenRect.Width);
            testList.UpdateLayout();
            testList.ActiveSelf = true;
            
            if (__instance.listOpponentCount != null)
            {
                var opponentRect = __instance.listOpponentCount.GetComponent<RectTransform>();
                var opponentHeight = __instance.listOpponentCount.GetHeight();
                float spacing = 10f;
                float newY = opponentRect.anchoredPosition.y - opponentHeight - spacing;
                testList.SetPositionTopY(0f, newY);
            }
            
            if (__instance.listDifficulty != null)
            {
                float testListHeight = testList.GetHeight() + 50f;
                bool foundDifficulty = false;
                
                foreach (var component in __instance.allComponents)
                {
                    if (!foundDifficulty)
                    {
                        UIHorizontalList_UI2? castedList = component.TryCast<UIHorizontalList_UI2>();
                        if (castedList != null && castedList == __instance.listDifficulty)
                        {
                            foundDifficulty = true;
                        }
                    }
                    
                    if (foundDifficulty && component.ActiveSelf)
                    {
                        float currentY = component.GetY();
                        component.SetY(currentY - testListHeight);
                    }
                }
            }
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
        if(testListData == null)
            return;

        int id = testListData.GetId(index);
        chosenMapGenType = (MapGenerationType)id;
        Console.Write("ON TEST LIST CHANGED");
        if(gameSetupScreen_UI2 != null)
        {
            gameSetupScreen_UI2.UpdateLayout();
        }
        else
        {
            Console.Write("GAME SETUP SCREEN IS NULL SDASIFI0DSUFGUESFHGFUIERFHGUIEDFHFGURFE");
        }
    }
}