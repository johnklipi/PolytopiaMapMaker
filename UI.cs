using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Game;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace PolytopiaMapManager
{
    public static class UI
    {
        private static GameSetupNameRow? mapSizeInputField = null;
        internal const float CAMERA_MAXZOOM_CONSTANT = 1000;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SelectViewmodePopup), nameof(SelectViewmodePopup.OnPlayerButtonClicked))]
        private static bool SelectViewmodePopup_OnPlayerButtonClicked(SelectViewmodePopup __instance, int id, BaseEventData eventData)
        {
            if (MapMaker.IsMapMaker())
                __instance.SetSelectedButton(id);
            return !MapMaker.IsMapMaker();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(SettingsUtils), nameof(SettingsUtils.UseCompactUI), MethodType.Getter)]
        private static void SettingsUtils_UseCompactUI_Get(ref bool __result)
        {
            // PlayerPrefsUtils.GetBoolValue("useCompactUI", false)
            // __result = IsMapMaker();
            __result = true;
        }

        internal static bool ContainsHorizontalList(GameSetupScreen __instance, string headerKey)
        {
            Console.Write("DestroyHorizontalList: " + headerKey);
            foreach (GameObject item in __instance.rows)
            {
                if (item.TryGetComponent<UIHorizontalList>(out UIHorizontalList list))
                {
                    if (list.HeaderKey != null)
                    {
                        if (list.HeaderKey == headerKey)
                        {
                            Console.Write("True");
                            return true;
                        }
                    }
                }
            }
            Console.Write("False");
            return false;
        }

        internal static void DestroyHorizontalList(GameSetupScreen __instance, string headerKey)
        {
            Console.Write("DestroyHorizontalList: " + headerKey);
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

        internal static void DestroyStartGameButton(GameSetupScreen __instance)
        {
            Console.Write("DestroyStartGameButton");
            if (__instance.continueButtonRow != null)
            {
                __instance.rows.Remove(__instance.continueButtonRow.gameObject);
                GameObject.Destroy(__instance.continueButtonRow.gameObject);
            }
        }

        internal static void ClearSpacers(GameSetupScreen __instance)
        {
            Console.Write("ClearSpacers");
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
                foreach (MapMaker.MapGenerationType value in Enum.GetValues(typeof(MapMaker.MapGenerationType)))
                {
                    types.Add(value.ToString());
                }
                __instance.CreateHorizontalList("gamesettings.generationtype", types.ToArray(), new Action<int>(OnMapGenTypeChanged));
            }
            return true;
        }

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CameraController), nameof(CameraController.Awake))]
		private static void CameraController_Awake()
		{
			CameraController.Instance.maxZoom = CAMERA_MAXZOOM_CONSTANT;
		}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.CreateHorizontalList))]
        private static bool GameSetupScreen_CreateHorizontalList(GameSetupScreen __instance, string headerKey, ref Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStringArray items, Il2CppSystem.Action<int> indexChangedCallback, int selectedIndex , RectTransform parent, int enabledItemCount, Il2CppSystem.Action onClickDisabledItemCallback)
        {
            Console.Write(headerKey);
            if (headerKey == "gamesettings.size")
            {
                List<string> list = items.ToList();
                list.Add("gamesettings.size.custom");
                items = list.ToArray();
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnMapSizeChanged))]
        private static void MapSizeExtensions_OnMapSizeChanged(GameSetupScreen __instance, int index)
        {
            Console.Write("MapSizeExtensions_OnMapSizeChanged");
            Console.Write(index);
            Console.Write(Enum.GetValues<MapSize>().Length);
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
                __instance.RefreshInfo();
                mapSizeInputField = CreateNameInputRowFix(__instance, "gamesettings.mapsize", "", null, new Action<string>(OnMapSizeChangedInput) );
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
                    if (mapSize > MapMaker.MAX_MAP_SIZE)
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
            Console.Write(value);
            if (int.TryParse(value, out int result))
            {
                GameManager.PreliminaryGameSettings.MapSize = result;
            }
        }

        private static Action<string> _dynamicValueChangedAction;
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
                _dynamicValueChangedAction = onValueChangedAction;
                gameSetupNameRow.inputField.onValueChanged.AddListener((UnityEngine.Events.UnityAction<string>)OnValueChangedHandler);
            }
            return gameSetupNameRow;
        }
        private static void OnValueChangedHandler(string newValue)
        {
            if (_dynamicValueChangedAction != null)
                _dynamicValueChangedAction(newValue);
        }

        private static void OnMapGenTypeChanged(int index)
        {
            Console.Write("OnMapGenTypeChanged: " + index);
            GameSetupScreen gameSetupScreen = UIManager.Instance.GetScreen(UIConstants.Screens.GameSetup).Cast<GameSetupScreen>();
            bool shouldBlockStart = false;
            ClearSpacers(gameSetupScreen);
            if (index != 0)
            {
                DestroyHorizontalList(gameSetupScreen, "gamesettings.difficulty");
                DestroyHorizontalList(gameSetupScreen, "gamesettings.map");
                DestroyHorizontalList(gameSetupScreen, "gamesettings.size");
                string[] maps = Directory.GetFiles(MapMaker.MAPS_PATH, "*.json");
                List<string> visualMaps = new();
                if (maps.Length > 0)
                {
                    visualMaps = maps.Select(map => Path.GetFileNameWithoutExtension(map)).ToList();
                }
                else
                {
                    visualMaps.Add("No maps found!");
                    shouldBlockStart = true;

                }
                gameSetupScreen.CreateHorizontalList("gamesettings.maps", visualMaps.ToArray(), new Action<int>(OnCustomMapChanged));
                GameManager.PreliminaryGameSettings.mapPreset = EnumCache<MapPreset>.GetType("custom");
            }
            else
            {
                gameSetupScreen.CreateDifficultyList();
                gameSetupScreen.CreateMapPresetList();
                gameSetupScreen.CreateMapSizeList();
                DestroyHorizontalList(gameSetupScreen, "gamesettings.maps");
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
            Console.Write("shouldBlockStart: " + shouldBlockStart);
            gameSetupScreen.continueButtonRow.buttonComp.ButtonEnabled = !shouldBlockStart; // I do not know why this fucking shit doesnt work
            gameSetupScreen.RefreshInfo();
        }

        private static void OnCustomMapChanged(int index)
        {
            Console.Write("OnCustomMapChanged: " + index);
            //_map = JObject.Parse(File.ReadAllText(Directory.GetFiles(MAPS_PATH, "*.json")[index]));
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.IsResourceVisibleToPlayer))]
        internal static void GameLogicData_IsResourceVisibleToPlayer(ref bool __result, ResourceData.Type resourceType, PlayerState player)
        {
            if (!__result && MapMaker.IsMapMaker())
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
            if (MapMaker.IsMapMaker())
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
            if (MapMaker.IsMapMaker())
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
            }
            return !MapMaker.IsMapMaker();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ReplayInterface), nameof(ReplayInterface.UpdateButton))]
        internal static bool ReplayInterface_UpdateButton(ReplayInterface __instance)
        {
            if (MapMaker.IsMapMaker())
            {
                __instance.viewmodeSelectButton.rectTransform.sizeDelta = new Vector2(75f, 75f);
                __instance.viewmodeSelectButton.iconSpriteHandle.SetCompletion((SpriteHandleCallback)TribeSpriteHandle);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                void TribeSpriteHandle(SpriteHandle spriteHandleCallback)
                {
                    __instance.viewmodeSelectButton.SetFaceIcon(spriteHandleCallback.sprite);
                }
                TribeData.Type tribeType = gameLogicData.GetTribeTypeFromStyle(MapMaker.chosenClimate);
                string spriteName;
                if (MapMaker.chosenSkinType == SkinType.Default)
                {
                    spriteName = EnumCache<TribeData.Type>.GetName(tribeType);
                }
                else
                {
                    spriteName = EnumCache<SkinType>.GetName(MapMaker.chosenSkinType);
                }
                __instance.viewmodeSelectButton.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(spriteName));
                __instance.viewmodeSelectButton.Outline.gameObject.SetActive(false);
                __instance.viewmodeSelectButton.BG.color = ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeType, MapMaker.chosenSkinType)), 1f);
            }
            return !MapMaker.IsMapMaker();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(SelectViewmodePopup), nameof(SelectViewmodePopup.SetData))]
        internal static bool SelectViewmodePopup_SetData(SelectViewmodePopup __instance, GameState gameState)
        {
            if (MapMaker.IsMapMaker(gameState))
            {
                __instance.ClearButtons();
                __instance.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                float num = 0f;
                GameLogicData gameLogicData = gameState.GameLogicData;
                List<TribeData> tribes = gameLogicData.GetTribes(TribeData.CategoryEnum.Human).ToArray().ToList().Concat(gameLogicData.GetTribes(TribeData.CategoryEnum.Special).ToArray().ToList()).ToList();
                foreach (TribeData tribeData in tribes)
                {
                    TribeData.Type tribeType = tribeData.type;
                    string tribeName = Localization.Get(tribeData.displayName);
                    CreatePlayerButton(__instance, gameState, tribeName, EnumCache<TribeData.Type>.GetName(tribeType), (int)tribeType, gameLogicData.GetTribeColor(tribeData.type, SkinType.Default), ref num);
                    foreach (SkinType skinType in tribeData.skins)
                    {
                        // gameLogicData.TryGetData(skinType, out SkinData data);
                        string skinHeader = string.Format(Localization.Get(SkinTypeExtensions.GetSkinNameKey(), new Il2CppSystem.Object[] { }), Localization.Get(skinType.GetLocalizationKey(), new Il2CppSystem.Object[] { }));
                        CreatePlayerButton(__instance, gameState, skinHeader, EnumCache<SkinType>.GetName(skinType), (int)skinType + 1000, gameLogicData.GetTribeColor(tribeData.type, skinType), ref num);
                    }
                }
                __instance.gridLayout.spacing = new Vector2(__instance.gridLayout.spacing.x, num + 10f);
                __instance.gridLayout.padding.bottom = Mathf.RoundToInt(num + 10f);
                __instance.gridBottomSpacer.minHeight = num + 10f;
            }
            return !MapMaker.IsMapMaker(gameState);
        }

        internal static void CreatePlayerButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, int color, ref float num)
        {
            UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
            playerButton.id = (int)type;
            playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
            playerButton.Outline.gameObject.SetActive(false);
            playerButton.BG.color = ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(color), 1f);
            playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
            playerButton.SetIconColor(Color.white);
            playerButton.ButtonEnabled = true;
            playerButton.OnClicked = (UIButtonBase.ButtonAction)OnClimateButtonClicked;
            void OnClimateButtonClicked(int id, BaseEventData eventData)
            {
                int type = id;
                Console.Write("Clicked i guess");
                Console.Write(id);
                if (type >= 1000)
                {
                    type -= 1000;
                    SkinType skinType = (SkinType)type;
                    MapMaker.chosenClimate = MapMaker.GetTribeClimateFromSkin(skinType, gameState.GameLogicData);
                    MapMaker.chosenSkinType = skinType;
                }
                else
                {
                    MapMaker.chosenClimate = MapMaker.GetTribeClimateFromType((TribeData.Type)type, gameState.GameLogicData);
                    MapMaker.chosenSkinType = SkinType.Default;
                }
                HudScreen hudScreen = UIManager.Instance.GetScreen(UIConstants.Screens.Hud).Cast<HudScreen>();
                hudScreen.replayInterface.UpdateButton();
                // viewmodePopup.Hide();
            }
            playerButton.iconSpriteHandle.SetCompletion((SpriteHandleCallback)TribeSpriteHandle);
            void TribeSpriteHandle(SpriteHandle spriteHandleCallback)
            {
                playerButton.SetFaceIcon(spriteHandleCallback.sprite);
            }
            playerButton.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(spriteName));
            if (playerButton.Label.PreferedValues.y > num)
            {
                num = playerButton.Label.PreferedValues.y;
            }
            viewmodePopup.buttons.Add(playerButton);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(HudButtonBar), nameof(HudButtonBar.Init))]
        internal static void HudButtonBar_Init(HudButtonBar __instance, HudScreen hudScreen)
        {
            if (MapMaker.IsMapMaker())
            {
                UI.AddUiButtonToArray(__instance.menuButton, __instance.hudScreen, (UIButtonBase.ButtonAction)MenuButtonOnClicked, __instance.buttonArray, "Menu");
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
        }
    }
}