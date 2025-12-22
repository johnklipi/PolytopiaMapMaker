using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PolytopiaMapManager.UI.Menu
{
    public static class Start
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartScreen), nameof(StartScreen.Start))]
        private static void StartScreen_Start(StartScreen __instance)
        {
            GameObject originalButton = __instance.newsButton.gameObject;
            GameObject button = GameObject.Instantiate(originalButton, originalButton.transform.parent);
            button.name = "MapMakerButton";
            button.transform.position = __instance.weeklyChallengesButton.transform.position + new Vector3(90, 0, 0);

            UIRoundButton buttonComponent = button.GetComponent<UIRoundButton>();
            buttonComponent.bg.sprite = PolyMod.Registry.GetSprite("mapmaker");
            buttonComponent.bg.transform.localScale = new Vector3(1.2f, 1.2f, 0);
            buttonComponent.bg.color = Color.white;

            GameObject.Destroy(buttonComponent.icon.gameObject);
            GameObject.Destroy(buttonComponent.outline.gameObject);

            Transform descriptionText = button.transform.Find("DescriptionText");
            descriptionText.gameObject.SetActive(true);
            descriptionText.GetComponentInChildren<TMPLocalizer>().Key = "gamemode.mapmaker";

            buttonComponent.OnClicked += (UIButtonBase.ButtonAction)OnMapMaker;

            static void OnMapMaker(int id, BaseEventData eventData)
            {
                Core.Init();
            }
        }
    }
}