using HarmonyLib;
using Il2CppInterop.Runtime.Runtime;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PolytopiaMapManager.Menu
{
    public static class GameSetup
    {
        public enum MapGenerationType
        {
            Default,
            Custom
        }

        // This code is fucking stupid. Half of it is due to me not being able to move hl's.
        // Like, even if i change the position in the rows array, it gets placed in the end and i tbh do not get it.
        // Im too lazy to manually change ts but i have to when i will refactor this shitcode
        internal const float CAMERA_MAXZOOM_CONSTANT = 1000;
        private static GameSetupNameRow? mapSizeInputField = null;
        private static Action<string>? dynamicValueChangedAction;
        private static List<string> visualMaps = new();

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CameraController), nameof(CameraController.Awake))]
		private static void CameraController_Awake()
		{
			CameraController.Instance.maxZoom = CAMERA_MAXZOOM_CONSTANT;
		}

        #region Horizontal Lists

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.CreateHorizontalList))]
        private static bool GameSetupScreen_CreateHorizontalList(GameSetupScreen __instance, string headerKey, ref Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray items, Il2CppSystem.Action<int> indexChangedCallback, int selectedIndex , RectTransform parent, int enabledItemCount, Il2CppSystem.Action onClickDisabledItemCallback)
        {
            Main.modLogger!.LogInfo(headerKey);
            if (headerKey == "gamesettings.size")
            {
                List<string> list = items.ToList();
                list.Add(Localization.Get("gamesettings.size.custom", new Il2CppSystem.Object[]{}));
                items = list.ToArray();
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.CreateHorizontalList))] // TODO: Well, if u have chosen custom type before, when u open game setup screen again it wont create the input field. Idk how to properly manage it there.
        private static void GameSetupScreen_CreateHorizontalList_Postfix(GameSetupScreen __instance, string headerKey, ref Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray items, Il2CppSystem.Action<int> indexChangedCallback, int selectedIndex, RectTransform parent, int enabledItemCount, Il2CppSystem.Action onClickDisabledItemCallback)
        {
            int num = selectedIndex;
            if (GameManager.PreliminaryGameSettings.GameType != GameType.Matchmaking)
            {
                num++;
            }
            if (num >= Enum.GetValues<MapSize>().Length && headerKey == "gamesettings.size")
            {
                mapSizeInputField = CreateNameInputRowFix(__instance, Localization.Get("gamesettings.mapwidth", new Il2CppSystem.Object[]{ MapLoader.MAX_MAP_SIZE }), GameManager.PreliminaryGameSettings.MapSize.ToString(), null, new Action<string>(OnMapSizeChangedInput));
            }
            else if (mapSizeInputField != null)
            {
                __instance.rows.Remove(mapSizeInputField.gameObject);
                GameObject.Destroy(mapSizeInputField.gameObject);
                mapSizeInputField = null;
            }
        }

        internal static bool ContainsHorizontalList(GameSetupScreen __instance, string headerKey)
        {
            Main.modLogger!.LogInfo("DestroyHorizontalList: " + headerKey);
            foreach (GameObject item in __instance.rows)
            {
                if (item.TryGetComponent<UIHorizontalList>(out UIHorizontalList list))
                {
                    if (list.HeaderKey != null)
                    {
                        if (list.HeaderKey == headerKey)
                        {
                            Main.modLogger!.LogInfo("True");
                            return true;
                        }
                    }
                }
            }
            Main.modLogger!.LogInfo("False");
            return false;
        }

        internal static void DestroyHorizontalList(GameSetupScreen __instance, string headerKey)
        {
            Main.modLogger!.LogInfo("DestroyHorizontalList: " + headerKey);
            GameObject? toDestroy = null;
            foreach (GameObject item in __instance.rows)
            {
                if (item.TryGetComponent<UIHorizontalList>(out UIHorizontalList list))
                {
                    if (list.HeaderKey != null)
                    {
                        if (list.HeaderKey == headerKey)
                        {
                            toDestroy = item;
                        }
                    }
                }
            }
            if (toDestroy != null)
            {
                __instance.rows.Remove(toDestroy);
                GameObject.Destroy(toDestroy);
            }
        }

        #endregion

        internal static void DestroyStartGameButton(GameSetupScreen __instance)
        {
            Main.modLogger!.LogInfo("DestroyStartGameButton");
            if (__instance.continueButtonRow != null)
            {
                __instance.rows.Remove(__instance.continueButtonRow.gameObject);
                GameObject.Destroy(__instance.continueButtonRow.gameObject);
            }
        }

        internal static void ClearSpacers(GameSetupScreen __instance)
        {
            Main.modLogger!.LogInfo("ClearSpacers");
            List<GameObject> toDestroy = new();
            foreach (GameObject item in __instance.rows)
            {
                if (item.TryGetComponent<UISpacer>(out UISpacer spacer))
                {
                    toDestroy.Add(item);
                }
            }
            foreach (var item in toDestroy)
            {
                __instance.rows.Remove(item);
                GameObject.Destroy(item);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.CreateDifficultyList))]
        private static bool GameSetupScreen_CreateDifficultyList(GameSetupScreen __instance)
        {
            if (!ContainsHorizontalList(__instance, "gamesettings.generationtype") && GameManager.PreliminaryGameSettings.BaseGameMode == GameMode.Custom)
            {
                List<string> types = new();
                foreach (MapGenerationType value in Enum.GetValues(typeof(MapGenerationType)))
                {
                    types.Add(value.ToString());
                }
                string[] maps = Directory.GetFiles(MapLoader.MAPS_PATH, "*.json");
                visualMaps = new();
                int num = 1;
                if (maps.Length > 0)
                {
                    visualMaps = maps.Select(map => Path.GetFileNameWithoutExtension(map)).ToList();
                    num++;
                }
                __instance.CreateHorizontalList("gamesettings.generationtype", types.ToArray(), new Action<int>(OnMapGenTypeChanged), 0, null, maps.Length + 1, (Il2CppSystem.Action)OnTriedSelectDisabledMapGenType);
            }
            return true;
        }

        private static void OnTriedSelectDisabledMapGenType()
        {
            NotificationManager.Notify(Localization.Get("gamesettings.nomaps", new Il2CppSystem.Object[]{}), Localization.Get("gamesettings.notavailable", new Il2CppSystem.Object[]{}), null, null);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapSizeExtensions), nameof(MapSizeExtensions.MapSizeFromInt))]
        private static void MapSizeExtensions_MapSizeFromInt(ref MapSize __result, int value)
        {
            if (value > MapSizeExtensions.MAX_MAP_SIZE)
            {
                __result = EnumCache<MapSize>.GetType("custom");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnMapSizeChanged))]
        private static void GameSetupScreen_OnMapSizeChanged(GameSetupScreen __instance, int index)
        {
            Main.modLogger!.LogInfo("MapSizeExtensions_OnMapSizeChanged");
            Main.modLogger!.LogInfo(index);
            Main.modLogger!.LogInfo(Enum.GetValues<MapSize>().Length);
            int num = index;
            if (GameManager.PreliminaryGameSettings.GameType != GameType.Matchmaking)
            {
                num++;
            }
            if (num >= Enum.GetValues<MapSize>().Length)
            {
                GameManager.PreliminaryGameSettings.MapSize = 50;
                __instance.UpdateOpponentList();
                GameManager.PreliminaryGameSettings.SaveToDisk();
                mapSizeInputField = CreateNameInputRowFix(__instance, Localization.Get("gamesettings.mapwidth", new Il2CppSystem.Object[]{ MapLoader.MAX_MAP_SIZE }), GameManager.PreliminaryGameSettings.MapSize.ToString(), null, new Action<string>(OnMapSizeChangedInput));
                if (__instance.singlePlayerInfoRow != null)
                {
                    __instance.rows.Remove(__instance.singlePlayerInfoRow.gameObject);
                    GameObject.Destroy(__instance.singlePlayerInfoRow.gameObject);
                }
                __instance.singlePlayerInfoRow = __instance.CreateInfoRow(null);
                ClearSpacers(__instance);
                __instance.CreateSpacer(20f, null);
                DestroyStartGameButton(__instance);
                __instance.CreateStartGameButton();
                __instance.RefreshInfo();
            }
            else if (mapSizeInputField != null)
            {
                __instance.rows.Remove(mapSizeInputField.gameObject);
                GameObject.Destroy(mapSizeInputField.gameObject);
                mapSizeInputField = null;
            }
        }

        private static void OnMapSizeChangedInput(string value)
        {
            if (mapSizeInputField != null)
            {
                string numericalInput = new string(value.Where(char.IsDigit).ToArray());
                if (int.TryParse(numericalInput, out int mapSize))
                {
                    if (mapSize > MapLoader.MAX_MAP_SIZE)
                    {
                        numericalInput = numericalInput.Remove(numericalInput.Length - 1);
                        NotificationManager.Notify("Your MapSize should not exceed 100x100.");
                    }
                    else
                    {
                        GameManager.PreliminaryGameSettings.MapSize = mapSize;
                    }
                }
                mapSizeInputField.inputField.text = numericalInput;
            }
        }

        private static void OnMapSizeFinishedInput(string value)
        {
            Main.modLogger!.LogInfo(value);
            if (int.TryParse(value, out int result))
            {
                GameManager.PreliminaryGameSettings.MapSize = result;
            }
        }

        private static GameSetupNameRow CreateNameInputRowFix(GameSetupScreen __instance, string headerKey, string name, Action<string>? inputDoneCallback = null, Action<string>? onValueChangedAction = null, RectTransform parent = null)
        {
            GameSetupNameRow gameSetupNameRow = GameObject.Instantiate<GameSetupNameRow>(__instance.nameRowPrefab, parent ?? __instance.VerticalListRectTr);
            gameSetupNameRow.HeaderKey = headerKey;
            gameSetupNameRow.inputDoneCallback = inputDoneCallback;
            gameSetupNameRow.Name = name;
            __instance.totalHeight += gameSetupNameRow.rectTransform.sizeDelta.y;
            __instance.rows.Add(gameSetupNameRow.gameObject);
            if (onValueChangedAction != null)
            {
                dynamicValueChangedAction = onValueChangedAction;
                gameSetupNameRow.inputField.onValueChanged.AddListener((UnityEngine.Events.UnityAction<string>)OnValueChangedHandler);
            }
            return gameSetupNameRow;
        }
        private static void OnValueChangedHandler(string newValue)
        {
            if (dynamicValueChangedAction != null)
                dynamicValueChangedAction(newValue);
        }

        private static void OnMapGenTypeChanged(int index)
        {
            Main.modLogger!.LogInfo("OnMapGenTypeChanged: " + index);
            GameSetupScreen gameSetupScreen = UIManager.Instance.GetScreen(UIConstants.Screens.GameSetup).Cast<GameSetupScreen>();
            bool shouldBlockStart = false;
            ClearSpacers(gameSetupScreen);
            if (visualMaps.Count > 0)
            {
                if (index != 0)
                {
                    DestroyHorizontalList(gameSetupScreen, "gamesettings.difficulty");
                    DestroyHorizontalList(gameSetupScreen, "gamesettings.map");
                    DestroyHorizontalList(gameSetupScreen, "gamesettings.size");
                    gameSetupScreen.CreateHorizontalList("gamesettings.maps", visualMaps.ToArray(), new Action<int>(OnCustomMapChanged));
                    GameManager.PreliminaryGameSettings.mapPreset = EnumCache<MapPreset>.GetType("custom");
                    if (mapSizeInputField != null)
                    {
                        gameSetupScreen.rows.Remove(mapSizeInputField.gameObject);
                        GameObject.Destroy(mapSizeInputField.gameObject);
                        mapSizeInputField = null;
                    }
                    OnCustomMapChanged(0);
                }
                else
                {
                    gameSetupScreen.CreateDifficultyList();
                    gameSetupScreen.CreateMapPresetList();
                    gameSetupScreen.CreateMapSizeList();
                    DestroyHorizontalList(gameSetupScreen, "gamesettings.maps");
                }   
            }
            if (gameSetupScreen.singlePlayerInfoRow != null)
            {
                gameSetupScreen.rows.Remove(gameSetupScreen.singlePlayerInfoRow.gameObject);
                GameObject.Destroy(gameSetupScreen.singlePlayerInfoRow.gameObject);
            }
            gameSetupScreen.singlePlayerInfoRow = gameSetupScreen.CreateInfoRow(null);
            gameSetupScreen.CreateSpacer(20f, null);
            DestroyStartGameButton(gameSetupScreen);
            gameSetupScreen.CreateStartGameButton();
            Main.modLogger!.LogInfo("shouldBlockStart: " + shouldBlockStart);
            gameSetupScreen.continueButtonRow.buttonComp.ButtonEnabled = !shouldBlockStart; // I do not know why this fucking shit doesnt work
            gameSetupScreen.RefreshInfo();
        }

        private static void OnCustomMapChanged(int index)
        {
            Main.modLogger!.LogInfo("OnCustomMapChanged: " + index);
            MapLoader.chosenMap = MapLoader.LoadMapFile(visualMaps[index]);
            Console.Write(visualMaps[index]);
            Console.Write(MapLoader.chosenMap != null);
            if (MapLoader.chosenMap != null)
            {
                GameManager.PreliminaryGameSettings.MapSize = MapLoader.chosenMap.size;
                GameManager.PreliminaryGameSettings.mapPreset = EnumCache<MapPreset>.GetType("custom");
                GameSetupScreen gameSetupScreen = UIManager.Instance.GetScreen(UIConstants.Screens.GameSetup).Cast<GameSetupScreen>();
                gameSetupScreen.UpdateOpponentList(); // i dont understand though? why doesnt it adapt properly but instead changes map size
                gameSetupScreen.RefreshInfo();
                Console.Write((int)Math.Pow((double)(GameManager.PreliminaryGameSettings.MapSize / 3), 2.0) - 1);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.IsResourceVisibleToPlayer))]
        internal static void GameLogicData_IsResourceVisibleToPlayer(ref bool __result, ResourceData.Type resourceType, PlayerState player)
        {
            if (!__result && MapLoader.inMapMaker)
                __result = true;
        }

        internal static void AddUiButtonToArray(UIRoundButton prefabButton, HudScreen hudScreen, UIButtonBase.ButtonAction action, UIRoundButton[] buttonArray, string? description = null)
        {
            UIRoundButton button = UnityEngine.GameObject.Instantiate(prefabButton, prefabButton.transform);
            button.transform.parent = hudScreen.buttonBar.transform;
            button.OnClicked += action;
            List<UIRoundButton> list = buttonArray.ToList();
            list.Add(button);
            list.ToArray();

            if (description != null)
            {
                Transform child = button.gameObject.transform.Find("DescriptionText");

                if (child != null)
                {
                    Main.modLogger!.LogInfo("Found child: " + child.name);
                    TMPLocalizer localizer = child.gameObject.GetComponent<TMPLocalizer>();
                    localizer.Text = description;
                }
                else
                {
                    Main.modLogger!.LogInfo("Child not found.");
                }
            }
        }
    }
}