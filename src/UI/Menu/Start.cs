using HarmonyLib;
using Il2CppInterop.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PolytopiaMapManager.UI.Menu;
public static class Start
{
    private static UIRoundButton_UI2? mapMakerButton = null;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartScreen_UI2), nameof(StartScreen_UI2.Init))]
    private static void StartScreen_UI2_Init(StartScreen_UI2 __instance, RectTransform transform)
    {
        mapMakerButton = UILibrary.NewRoundButton(transform).SetStyle(UIButtonBase_UI2.ButtonStyle.Suggested);
        mapMakerButton.bg.sprite = PolyMod.Registry.GetSprite("mapmaker");
        mapMakerButton.OnClickedSignal.Add(DelegateSupport.ConvertDelegate<Il2CppSystem.Action>(Main.Init));
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartScreen_UI2), nameof(StartScreen_UI2.RunLayout))]
    private static void StartScreen_UI2_RunLayout(StartScreen_UI2 __instance, ScreenBase_UI2.ScreenSize screenSize)
    {
        if(mapMakerButton == null)
        {
            Main.modLogger!.LogWarning("MapMaker button is null when running layout!");
            return;
        }
        mapMakerButton.iconContainer.gameObject.SetActive(false);
        mapMakerButton.outline.gameObject.SetActive(false);
        mapMakerButton.bg.color = Color.white;
        mapMakerButton.Text = Localization.Get("gamemode.mapmaker");
		float num = 50f;
		mapMakerButton.SetPosition(screenSize.safeRect.Left + (num * 2.5f), screenSize.safeRect.Top - num);
    }
}