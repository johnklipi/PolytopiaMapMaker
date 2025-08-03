using System.Text.Json.Serialization;
using BepInEx.Logging;
using PolytopiaBackendBase.Game;
using System.Text.Json;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;
using Polytopia.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static PopupBase;

namespace PolytopiaMapManager;

public static class MapMaker
{
    public class MapTile
    {
        [JsonInclude]
        public int climate = 0;
        [JsonInclude]
        [JsonConverter(typeof(EnumCacheJson<Polytopia.Data.TerrainData.Type>))]
        public Polytopia.Data.TerrainData.Type terrain = Polytopia.Data.TerrainData.Type.Field;
        [JsonInclude]
        [JsonConverter(typeof(EnumCacheJson<Polytopia.Data.ResourceData.Type>))]
        public ResourceData.Type? resource;
        [JsonInclude]
        [JsonConverter(typeof(EnumCacheJson<Polytopia.Data.ImprovementData.Type>))]
        public ImprovementData.Type? improvement;
        [JsonInclude]
        [JsonConverter(typeof(EnumCacheListJson<TileData.EffectType>))]
        public List<TileData.EffectType> effects = new();
    }

    public class MapData
    {
        [JsonInclude]
        public ushort size;
        [JsonInclude]
        public List<MapTile> map = new();
    }
    public enum MapGenerationType
    {
        Default,
        Custom
    }
    internal static ManualLogSource? modLogger;
    internal static readonly string MAPS_PATH = Path.Combine(PolyMod.Plugin.BASE_PATH, "Maps");
    internal static int chosenClimate = -1;

    public static void Load(ManualLogSource logger)
    {
        modLogger = logger;
        Harmony.CreateAndPatchAll(typeof(MapMaker));
        Harmony.CreateAndPatchAll(typeof(CustomPopup));
        PolyMod.Loader.AddGameModeButton("mapmaker", (UIButtonBase.ButtonAction)OnMapMaker, PolyMod.Registry.GetSprite("mapmaker"));
        PolyMod.Loader.AddPatchDataType("mapPreset", typeof(MapPreset));
        Directory.CreateDirectory(MAPS_PATH);

        void OnMapMaker(int id, BaseEventData eventData = null)
        {
            GameSettings gameSettings = new GameSettings();
            gameSettings.BaseGameMode = EnumCache<GameMode>.GetType("mapmaker");
            gameSettings.SetUnlockedTribes(GameManager.GetPurchaseManager().GetUnlockedTribes(false));
            gameSettings.mapPreset = MapPreset.Dryland;
            gameSettings.mapSize = 16;
            GameManager.StartingTribe = EnumCache<TribeData.Type>.GetType("mapmaker");
            GameManager.StartingTribeMix = TribeData.Type.None;
            GameManager.StartingSkin = SkinType.Default;
            GameManager.PreliminaryGameSettings = gameSettings;
            GameManager.PreliminaryGameSettings.OpponentCount = 0;
            GameManager.PreliminaryGameSettings.Difficulty = BotDifficulty.Frozen;
            //UIBlackFader.FadeIn(0.5f, DelegateSupport.ConvertDelegate<Il2CppSystem.Action>((Action)CreateGame), "gamesettings.creatingworld", null, null);

            GameManager.Instance.CreateSinglePlayerGame();

            int num = 0;
            for (int j = 0; j < (int)GameManager.GameState.Map.Height; j++)
            {
                for (int k = 0; k < (int)GameManager.GameState.Map.Width; k++)
                {
                    TileData tileData = new TileData
                    {
                        coordinates = new WorldCoordinates(k, j),
                        terrain = Polytopia.Data.TerrainData.Type.Field,
                        climate = 0,
                        altitude = 1,
                        improvement = null,
                        resource = null,
                        owner = 0
                    };
                    GameManager.GameState.Map.Tiles[num++] = tileData;
                }
            }
            for (int i = 0; i < GameManager.GameState.Map.Tiles.Length; i++)
            {
                GameManager.GameState.Map.Tiles[i].SetExplored(GameManager.LocalPlayer.Id, true);
            }
            MapRenderer.Current.Refresh(false);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HudButtonBar), nameof(HudButtonBar.Init))]
    public static void HudButtonBar_Init(HudButtonBar __instance, HudScreen hudScreen)
    {
        AddUiButtonToArray(__instance.menuButton, __instance.hudScreen, (UIButtonBase.ButtonAction)MenuButtonOnClicked, __instance.buttonArray, "Menu");
        // AddUiButtonToArray(__instance.menuButton, __instance.hudScreen, (UIButtonBase.ButtonAction)SaveMapButtonOnClicked, __instance.buttonArray, "Save Map");
        __instance.nextTurnButton.gameObject.SetActive(false);
        __instance.techTreeButton.gameObject.SetActive(false);
        __instance.statsButton.gameObject.SetActive(false);
        __instance.Show();
        __instance.Update();
        // __instance.buttonBar.statsButton.BlockButton = true;
        void MenuButtonOnClicked(int id, BaseEventData eventdata)
        {
            CustomPopup.Show();
        }

        void SaveMapButtonOnClicked(int id, BaseEventData eventdata)
        {
            // BuildMapFile(chosenMapName + ".json", (ushort)Math.Sqrt(GameManager.GameState.Map.Tiles.Length), GameManager.GameState.Map.Tiles.ToArray().ToList());
            NotificationManager.Notify($"Saved map.", "Map Maker", null, null);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SettingsUtils), nameof(SettingsUtils.UseCompactUI), MethodType.Getter)]
    private static void SettingsUtils_UseCompactUI_Get(ref bool __result)
    {
        // PlayerPrefsUtils.GetBoolValue("useCompactUI", false)
        // __result = IsMapMaker();
        __result = true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.CreateHorizontalList))]
    private static void GameSetupScreen_CreateHorizontalList(GameSetupScreen __instance, string headerKey, Il2CppStringArray items, Il2CppSystem.Action<int> indexChangedCallback, int selectedIndex, UnityEngine.RectTransform parent, int enabledItemCount, Il2CppSystem.Action onClickDisabledItemCallback)
    {
        Console.Write(headerKey);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.IsResourceVisibleToPlayer))]
    public static void GameLogicData_IsResourceVisibleToPlayer(ref bool __result, ResourceData.Type resourceType, PlayerState player)
    {
        if (!__result && IsMapMaker())
            __result = true;
    }

    private static void AddUiButtonToArray(UIRoundButton prefabButton, HudScreen hudScreen, UIButtonBase.ButtonAction action, UIRoundButton[] buttonArray, string? description = null)
    {
        UIRoundButton button = UnityEngine.GameObject.Instantiate(prefabButton, prefabButton.transform);
        button.transform.parent = hudScreen.buttonBar.transform;
        button.OnClicked += action;
        List<UIRoundButton> list = buttonArray.ToList();
        list.Add(button);
        list.ToArray();

        if(description != null){
            Transform child = button.gameObject.transform.Find("DescriptionText");

            if (child != null)
            {
                Console.Write("Found child: " + child.name);
                TMPLocalizer localizer = child.gameObject.GetComponent<TMPLocalizer>();
                localizer.Text = description;
            }
            else
            {
                Console.Write("Child not found.");
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
    private static void HudScreen_OnMatchStart(HudScreen __instance)
    {
        if (IsMapMaker())
        {
            Console.Write("IN MAP MAKERRRRRR");
            __instance.replayInterface.gameObject.SetActive(true);
            __instance.replayInterface.SetData(GameManager.GameState);
            __instance.replayInterface.timeline.gameObject.SetActive(false);
        }
        else
        {
            Console.Write("NOOOOOOOOOOOT IN MAP MAKERRRRRR");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ReplayInterface), nameof(ReplayInterface.ShowViewModePopup))]
    private static bool ReplayInterface_ShowViewModePopup(ReplayInterface __instance)
    {
        if (__instance.selectViewmodePopup != null && __instance.selectViewmodePopup.IsShowing())
        {
            return false;
        }
        __instance.selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
        // __instance.selectViewmodePopup.Header = Localization.Get("replay.viewmode.header", new Il2CppSystem.Object[] { });
        __instance.selectViewmodePopup.Header = Localization.Get("mapmaker.choose.climate", new Il2CppSystem.Object[] { });
        __instance.selectViewmodePopup.SetData(GameManager.GameState);
        __instance.selectViewmodePopup.buttonData = new PopupBase.PopupButtonData[]
        {
            new PopupBase.PopupButtonData("buttons.ok", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)exit, -1, true, null)
        };
        void exit(int id, BaseEventData eventData)
        {
            __instance.CloseViewModePopup();
        }
        __instance.selectViewmodePopup.Show(__instance.viewmodeSelectButton.rectTransform.position);
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SelectViewmodePopup), nameof(SelectViewmodePopup.SetData))]
    public static bool SelectViewmodePopup_SetData(SelectViewmodePopup __instance, GameState gameState)
    {
        if (IsMapMaker(gameState))
        {
            __instance.ClearButtons();
            __instance.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
            float num = 0f;
            foreach (int tribe in Enum.GetValues(typeof(TribeData.Type)))
            {
                TribeData.Type tribeType = (TribeData.Type)tribe;
                if (gameState.GameLogicData.TryGetData(tribeType, out TribeData tribeData))
                {
                    string tribeName = EnumCache<TribeData.Type>.GetName(tribeType);
                    UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(__instance.buttonPrefab, __instance.gridLayout.transform);
                    playerButton.id = tribe;
                    playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
                    playerButton.Outline.gameObject.SetActive(false);
                    playerButton.BG.color = ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(tribeData.color), 1f);
                    playerButton.text = tribeName[0].ToString().ToUpper() + tribeName.Substring(1);
                    playerButton.SetIconColor(Color.white);
                    playerButton.ButtonEnabled = true;
                    playerButton.OnClicked = (UIButtonBase.ButtonAction)OnClimateButtonClicked;
                    void OnClimateButtonClicked(int id, BaseEventData eventData)
                    {
                        Console.Write("Clicked i guess");
                    }
                    playerButton.iconSpriteHandle.SetCompletion((SpriteHandleCallback)TribeSpriteHandle);
                    void TribeSpriteHandle(SpriteHandle spriteHandleCallback)
                    {
                        playerButton.SetFaceIcon(spriteHandleCallback.sprite);
                    }
                    playerButton.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(tribeName));
                    if (playerButton.Label.PreferedValues.y > num)
                    {
                        num = playerButton.Label.PreferedValues.y;
                    }
                    __instance.buttons.Add(playerButton);
                }
            }
            __instance.gridLayout.spacing = new Vector2(__instance.gridLayout.spacing.x, num + 10f);
            __instance.gridLayout.padding.bottom = Mathf.RoundToInt(num + 10f);
            __instance.gridBottomSpacer.minHeight = num + 10f;
        }
        return !IsMapMaker(gameState);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(SelectViewmodePopup), nameof(SelectViewmodePopup.OnPlayerButtonClicked))]
    private static bool SelectViewmodePopup_OnPlayerButtonClicked(SelectViewmodePopup __instance, int id, BaseEventData eventData)
    {
        if (IsMapMaker())
            __instance.SetSelectedButton(id);
        return !IsMapMaker();
    }

    public static void BuildMapFile(string name, ushort size, List<TileData> tiles)
    {
        List<MapTile> mapTiles = new();
        foreach (TileData tileData in tiles)
        {
            MapTile mapTile = new MapTile
            {
                climate = tileData.climate,
                terrain = tileData.terrain,
            };
            if (tileData.resource != null)
            {
                mapTile.resource = tileData.resource.type;
            }
            if (tileData.improvement != null)
            {
                mapTile.improvement = tileData.improvement.type;
            }
            if (tileData.effects.Count > 0)
            {
                mapTile.effects = tileData.effects.ToArray().ToList();
            }
            mapTiles.Add(mapTile);
        }
        MapData mapData = new MapData
        {
            size = size,
            map = mapTiles
        };
        File.WriteAllTextAsync(
            Path.Combine(MAPS_PATH, name),
            JsonSerializer.Serialize(mapData, new JsonSerializerOptions { WriteIndented = true })
        );
    }
    public static bool IsMapMaker(GameMode gameMode)
    {
        return gameMode == EnumCache<GameMode>.GetType("mapmaker");
    }

    public static bool IsMapMaker(GameState gameState)
    {
        return IsMapMaker(gameState.Settings.BaseGameMode);
    }

    public static bool IsMapMaker()
    {
        if (GameManager.Instance.isLevelLoaded)
        {
            return IsMapMaker(GameManager.GameState);
        }
        return false;
    }
}