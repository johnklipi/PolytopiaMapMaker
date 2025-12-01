using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PolytopiaMapManager;

public static class MapMaker
{
    private static string mapName = "Untitled Map";
    public static bool MapSaved = false; // Can also use this to indicate unsaved maps in UI?
    public static string MapName
    {
        set
        {
            if(!string.IsNullOrEmpty(value))
            {
                mapName = value;
                if(mapNameContainer != null)
                {
                    mapNameContainer.GetComponent<TMPLocalizer>().Text = value;
                }
            }
        }
        get
        {
            return mapName;
        }
    }
    private static Transform? mapNameContainer;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    public static void MapRename()
    {
        if(Input.GetKeyDown(KeyCode.W) && Input.GetKey(KeyCode.LeftControl)){
        BasicPopup popup = PopupManager.GetBasicPopup();
        popup.Header = "Rename Map";
        popup.Description = "Naming your map is an exciting step! Click SET if done, don't forget to save!";
        popup.buttonData = new PopupBase.PopupButtonData[]
        {
            new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)exit, -1, true, null),
            new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)savename, -1, true, null)
        };
        void exit(int id, BaseEventData eventData)
        {
            Popup.CustomInput.RemoveInputFromPopup(popup);
        }
        void savename(int id, BaseEventData eventData){
            MapName = Popup.CustomInput.GetInputFromPopup(popup).text;
            NotificationManager.Notify($"New name is {MapName}", "Map name set!");
            Popup.CustomInput.RemoveInputFromPopup(popup);
        }
        Popup.CustomInput.AddInputToPopup(popup);
        }
    }

    public static void SaveMap()
    {
        MapLoader.BuildMapFile(MapName + ".json", (ushort)Math.Sqrt(GameManager.GameState.Map.Tiles.Length), GameManager.GameState.Map.Tiles.ToArray().ToList());
        NotificationManager.Notify(MapName + " has been saved.");
        MapSaved = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    private static void GameManager_Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Tab))
        {
            for (int i = 0; i < GameManager.GameState.Map.Tiles.Length; i++)
            {
                GameManager.GameState.Map.Tiles[i].SetExplored(GameManager.LocalPlayer.Id, true);
            }
            MapRenderer.Current.Refresh(false);
            NotificationManager.Notify("Map has been revealed.");
        }
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S))
        {
            if(!MapSaved && MapName == "Untitled Map"){
            BasicPopup popup = PopupManager.GetBasicPopup();
            popup.Header = "Rename Your Map!";
            popup.Description = "Before saving, please name your map!";
            popup.buttonData = new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)exit, -1, true, null)
            };
            void exit(int id, BaseEventData eventData)
            {
                MapName = Popup.CustomInput.GetInputFromPopup(popup).text;
                SaveMap();
                Popup.CustomInput.RemoveInputFromPopup(popup);
            }
            Popup.CustomInput.AddInputToPopup(popup);
            }
            else
            {
                SaveMap();
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ResourceBar), nameof(ResourceBar.OnEnable))]
    internal static void OnEnable(ResourceBar __instance)
    {
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

        text.GetComponent<TMPLocalizer>().Text = MapName;
        text.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        mapNameContainer = text.transform;
    }
}