using System.Linq.Expressions;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PolytopiaMapManager;

public static class MapMaker
{
    private static string mapName = "";
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
    [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
    private static void HudScreen_OnMatchStart(HudScreen __instance)
    {
        if (MapLoader.inMapMaker)
        {
            UIRoundButton mapSizeButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
            mapSizeButton.transform.position = mapSizeButton.transform.position - new Vector3(0, 180, 0);
            mapSizeButton.gameObject.SetActive(true);
            mapSizeButton.OnClicked = (UIButtonBase.ButtonAction)ShowMapPopup;
            mapSizeButton.text = string.Empty;

            void ShowMapPopup(int id, BaseEventData eventData)
            {
                BasicPopup popup = PopupManager.GetBasicPopup();
                popup.Header = "Resize Map";
                popup.Description = "";
                popup.buttonData = new PopupBase.PopupButtonData[]
                {
                    new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null),
                    new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Resize, -1, true, null)
                };
                void Exit(int id, BaseEventData eventData)
                {
                    Popup.CustomInput.RemoveInputFromPopup(popup);
                }
                void Resize(int id, BaseEventData eventData){
                    if(int.TryParse(Popup.CustomInput.GetInputFromPopup(popup).text, out int size))
                    {
                        GameState gameState = GameManager.GameState;
                        ResizeMap(ref gameState, size);
                        GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
                        MapLoader.RevealMap(GameManager.LocalPlayer.Id);
                        NotificationManager.Notify($"New size is {size}x{size}", "Map size set!");
                    }
                    else
                    {
                        NotificationManager.Notify("Only numbers are allowed", "Error");
                    }
                    Popup.CustomInput.RemoveInputFromPopup(popup);
                }
                Popup.CustomInput.AddInputToPopup(popup);
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerExtensions), nameof(PlayerExtensions.CountCapitals))]
	public static bool PlayerExtensions_CountCapitals(ref int __result, PlayerState player, GameState gameState)
	{
        if(MapLoader.inMapMaker)
        {
            __result = 0;
        }
		return !MapLoader.inMapMaker;
	}

    internal static void ResizeMap(ref GameState gameState, int size)
    {
        gameState.Settings.MapSize = size;
        List<TileData> tiles = gameState.Map.Tiles.ToList();

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                WorldCoordinates worldCoordinates = new WorldCoordinates(x, y);
                TileData? tileData = gameState.Map.GetTile(worldCoordinates);
                if(tileData == null)
                {
                    TileData tile = MapLoader.GetBasicTile(worldCoordinates.x, worldCoordinates.y);
                    tiles.Add(tile);
                }
            }
        }
        gameState.Map.tiles = tiles
            .OrderBy(t => t.coordinates.y)
            .ThenBy(t => t.coordinates.x)
            .ToList().ToArray();
        gameState.Map.width = (ushort)size;
        gameState.Map.height = (ushort)size;
    }

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
                new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null),
                new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)SaveName, -1, true, null)
            };
            void Exit(int id, BaseEventData eventData)
            {
                Popup.CustomInput.RemoveInputFromPopup(popup);
            }
            void SaveName(int id, BaseEventData eventData){
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
            MapLoader.RevealMap(GameManager.LocalPlayer.Id);
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
                    new PopupBase.PopupButtonData("buttons.exit", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null),
                    new PopupBase.PopupButtonData("buttons.set", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Save, -1, true, null)
                };
                void Exit(int id, BaseEventData eventData)
                {
                    Popup.CustomInput.RemoveInputFromPopup(popup);
                }
                void Save(int id, BaseEventData eventData)
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

    public static WorldCoordinates GetTileCoordinates(int index, int width)
    {
        int x = index % width;
        int y = index / width;
        return new WorldCoordinates(x, y);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ResourceBar), nameof(ResourceBar.OnEnable))]
    internal static void ResourceBar_OnEnable(ResourceBar __instance)
    {
        if(MapLoader.inMapMaker)
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
}