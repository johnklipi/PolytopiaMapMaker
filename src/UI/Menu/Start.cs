using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.EventSystems;

namespace PolytopiaMapManager.UI.Menu
{
    public static class Start
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartScreen_UI2), nameof(StartScreen_UI2.RunLayout))]
        private static void StartScreen_Start(StartScreen_UI2 __instance)
        {
            GameObject originalButton = __instance.newsButton.gameObject;
            GameObject button = GameObject.Instantiate(originalButton, originalButton.transform.parent);
            button.name = "MapMakerButton";
            button.transform.position = __instance.weeklyChallengeButton.transform.position + new Vector3(90, 0, 0);

            UIRoundButton_UI2 buttonComponent = button.GetComponent<UIRoundButton_UI2>();
            buttonComponent.bg.sprite = PolyMod.Registry.GetSprite("mapmaker");
            buttonComponent.bg.transform.localScale = new Vector3(1.2f, 1.2f, 0);
            buttonComponent.bg.color = Color.white;

            GameObject.Destroy(buttonComponent.icon.gameObject);
            GameObject.Destroy(buttonComponent.outline.gameObject);

            var buttonui2 = button.GetComponent<UIButtonBase_UI2>();
            if(buttonui2 != null) buttonui2.Text = Localization.Get("gamemode.mapmaker");


            buttonComponent.OnClicked += (UIButtonBase_UI2.ButtonAction)OnMapMaker;

            static void OnMapMaker(int id, BaseEventData eventData)
            {
                Main.Init();
            }
        }
    }
}