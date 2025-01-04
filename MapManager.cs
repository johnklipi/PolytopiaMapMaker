﻿using DG.Tweening;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Newtonsoft.Json.Linq;
using Polytopia.Data;
using PolytopiaBackendBase;
using PolytopiaBackendBase.Game;
using UnityEngine;
using UnityEngine.EventSystems;


namespace PolytopiaMapManager
{
    public class MapManager
    {
        internal static string versionOfMapManager = "0.0.0";
        internal const int MAP_MAX_PLAYERS = 100;
		internal const float CAMERA_MAXZOOM_CONSTANT = 1000;
		internal static readonly string BASE_PATH = Path.Combine(BepInEx.Paths.BepInExRootPath, "..");
		internal static readonly string MAPS_PATH = Path.Combine(BASE_PATH, "Maps");
        public static bool isInMapMaker = false;
        public static int chosenClimate = 1;
		public static int mapMakerButtonIndex;
		//public static UnityEngine.UI.Image mapMakerIcon;
		private static JObject? _map;
		private static bool _isListInstantiated = false;
		private static UIHorizontalList _customMapsList = new() { };

        public static void Load()
		{
			Console.WriteLine("Loading MapManager of version " + versionOfMapManager + "...");
            Init();
			Harmony.CreateAndPatchAll(typeof(MapManager));
			Console.WriteLine("MapManager loaded successfully!");
		}

        [HarmonyPrefix]
		[HarmonyPatch(typeof(GameModeScreen), nameof(GameModeScreen.UpdateListLayout))]
		private static void GameModeScreen_UpdateListLayout(GameModeScreen __instance)
		{
			__instance.buttons[mapMakerButtonIndex].text = "MAP MAKER";
			__instance.buttons[mapMakerButtonIndex].description.Text = "Create your own game maps and let your imagination flourish!";
			//__instance.buttons[__instance.buttons.Length - 1].icon = mapMakerIcon; 
		}
	
		[HarmonyPrefix]
		[HarmonyPatch(typeof(GameModeScreen), nameof(GameModeScreen.Init))]
		private static void GameModeScreen_Init(GameModeScreen __instance)
		{
			GamemodeButton prefab = __instance.buttons[2];
			GamemodeButton button = UnityEngine.GameObject.Instantiate(prefab);
			button.transform.localScale = new(1.1f, 1.1f);
			mapMakerButtonIndex = __instance.buttons.Length;
			List<GamemodeButton> list = __instance.buttons.ToList();
			list.Add(button);
			__instance.buttons = list.ToArray();
			__instance.buttons[mapMakerButtonIndex].OnClicked += (UIButtonBase.ButtonAction)MapMakerButton_OnClicked;
	
			void MapMakerButton_OnClicked(int id, BaseEventData eventData = null)
			{
				StartMapMaker();
			}
		}
	
		[HarmonyPostfix]
		[HarmonyPatch(typeof(BuildAction), nameof(BuildAction.ExecuteDefault))]
		private static void BuildAction_ExecuteDefault(BuildAction __instance, GameState gameState)
		{
			TileData tile = gameState.Map.GetTile(__instance.Coordinates);
			ImprovementData improvementData;
			PlayerState playerState;
			if (tile != null && gameState.GameLogicData.TryGetData(__instance.Type, out improvementData) && gameState.TryGetPlayer(__instance.PlayerId, out playerState))
			{
				if (improvementData.type != ImprovementData.Type.Road)
				{
					if (improvementData.type == ImprovementData.Type.City)
					{
						tile.improvement.level = 1;
					}
					if (improvementData.HasAbility((ImprovementAbility.Type)600))
					{
						tile.climate = chosenClimate;
					}
				}
			}
		}
		private static void StartMapMaker()
		{
			isInMapMaker = true;
			GameSettings gameSettings = new GameSettings();
			gameSettings.BaseGameMode = GameMode.Custom;
			gameSettings.SetUnlockedTribes(GameManager.GetPurchaseManager().GetUnlockedTribes(false));
			gameSettings.mapPreset = MapPreset.Dryland;
			gameSettings.mapSize = 16;
			GameManager.StartingTribe = (TribeData.Type)815;
			GameManager.StartingTribeMix = TribeData.Type.None;
			GameManager.StartingSkin = SkinType.Default;
			GameManager.PreliminaryGameSettings = gameSettings;
			GameManager.PreliminaryGameSettings.OpponentCount = 0;
			GameManager.PreliminaryGameSettings.Difficulty = GameSettings.Difficulties.Frozen;
			//UIBlackFader.FadeIn(0.5f, async delegate
			//{
			//    DOTween.KillAll(false);
			//    await GameManager.Instance.CreateSinglePlayerGame();
			//}, "gamesettings.creatingworld", null, null);
			GameManager.Instance.CreateSinglePlayerGame();
		}

		public static void BuildMapFile(string name) //this method is polniy pizdec
		{
			string mapString = "{\n";
			mapString += "\t" + "\"size\": " + Math.Sqrt(GameManager.GameState.Map.Tiles.Length).ToString() + "," + "\n";
			mapString += "\t\"map\": [\n";
			for (int i = 0; i < GameManager.GameState.Map.Tiles.Length; i++)
			{
				mapString += "\t\t{\n";
				//Console.Write(i);
				if (GameManager.GameState.Map.Tiles[i].coordinates != null)
				{
					Console.Write(GameManager.GameState.Map.Tiles[i].coordinates);
				}
				if (GameManager.GameState.Map.Tiles[i].terrain != null)
				{
					mapString += "\n";
					// Console.Write(GameManager.GameState.Map.Tiles[i].terrain);
					mapString += "\t\t\t" + "\"terrain\": " + "\"" + GameManager.GameState.Map.Tiles[i].terrain.ToString().ToLower() + "\"";
				}
				if (GameManager.GameState.Map.Tiles[i].climate != null)
				{
					mapString += ",\n";
					//Console.Write(GameManager.GameState.Map.Tiles[i].climate);
					mapString += "\t\t\t" + "\"climate\": " + GameManager.GameState.Map.Tiles[i].climate.ToString().ToLower();
				}
				if (GameManager.GameState.Map.Tiles[i].improvement != null)
				{
					if (GameManager.GameState.Map.Tiles[i].improvement.type.ToString().ToLower() != "lighthouse")
					{
					mapString += ",\n";
					//Console.Write(GameManager.GameState.Map.Tiles[i].improvement.type);
					mapString += "\t\t\t" + "\"improvement\": " + "\"" + GameManager.GameState.Map.Tiles[i].improvement.type.ToString().ToLower() + "\"";
					}
				}
				if (GameManager.GameState.Map.Tiles[i].resource != null)
				{
					mapString += ",\n";
					//Console.Write(GameManager.GameState.Map.Tiles[i].resource.type);
					mapString += "\t\t\t" + "\"resource\": " + "\"" + GameManager.GameState.Map.Tiles[i].resource.type.ToString().ToLower() + "\"";
				}
				if (i == GameManager.GameState.Map.Tiles.Length - 1)
				{
					mapString += "\n";
					mapString += "\t\t}\n";
				}
				else
				{
					mapString += "\n";
					mapString += "\t\t},\n";
				}
			}
			mapString += "\t]\n";
			mapString += "}";
			File.WriteAllText(Path.Combine(MAPS_PATH, name), mapString);
			//UI.active = false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
		private static void MapGenerator_Generate(ref GameState state, ref MapGeneratorSettings settings)
		{
			_map = JObject.Parse(File.ReadAllText(Path.Combine(MAPS_PATH, "map.json")));
			PreGenerate(ref state, ref settings);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.Generate))]
		private static void MapGenerator_Generate_(ref GameState state)
		{
			_map = JObject.Parse(File.ReadAllText(Path.Combine(MAPS_PATH, "map.json")));
			PostGenerate(ref state);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GeneratePlayerCapitalPositions))]
		private static void MapGenerator_GeneratePlayerCapitalPositions(ref Il2CppSystem.Collections.Generic.List<int> __result)
		{
			if (isInMapMaker)
			{
				Il2CppSystem.Collections.Generic.List<int> list = __result;
				list.Clear();
				list.Add(-1);
				__result = list;
			}
			__result = GetCapitals(__result);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameManager), nameof(GameManager.GetMaxOpponents))]
		private static void GameManager_GetMaxOpponents(ref int __result)
		{
			__result = MAP_MAX_PLAYERS - 1;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MapDataExtensions), nameof(MapDataExtensions.GetMaximumOpponentCountForMapSize))]
		private static void MapDataExtensions_GetMaximumOpponentCountForMapSize(ref int __result)
		{
			__result = MAP_MAX_PLAYERS;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PurchaseManager), nameof(PurchaseManager.GetUnlockedTribeCount))]
		private static void PurchaseManager_GetUnlockedTribeCount(ref int __result)
		{
			__result = MAP_MAX_PLAYERS + 2;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(CameraController), nameof(CameraController.Awake))]
		private static void CameraController_Awake()
		{
			CameraController.Instance.maxZoom = CAMERA_MAXZOOM_CONSTANT;
			CameraController.Instance.techViewBounds = new(
				new(CAMERA_MAXZOOM_CONSTANT, CAMERA_MAXZOOM_CONSTANT), CameraController.Instance.techViewBounds.size
			);
			UnityEngine.GameObject.Find("TechViewWorldSpace").transform.position = new(CAMERA_MAXZOOM_CONSTANT, CAMERA_MAXZOOM_CONSTANT);
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnStartGameClicked))]
		private static void GameSetupScreen_OnStartGameClicked()
		{
			_isListInstantiated = false;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.RedrawScreen))]
		private static void GameSetupScreen_RedrawScreen()
		{
			_map = null;
		}

		private static Il2CppSystem.Collections.Generic.List<int> GetCapitals(Il2CppSystem.Collections.Generic.List<int> originalCapitals)
		{
			if (_map == null || _map["capitals"] == null)
			{
				return originalCapitals;
			}
			JArray jcapitals = _map["capitals"].Cast<JArray>();
			Il2CppSystem.Collections.Generic.List<int> capitals = new();
			for (int i = 0; i < jcapitals.Count; i++)
			{
				capitals.Add((int)jcapitals[i]);
			}

			if (capitals.Count < originalCapitals.Count)
			{
				throw new Exception("Too few capitals provided");
			}

			return capitals.GetRange(0, originalCapitals.Count);
		}

		private static void PreGenerate(ref GameState state, ref MapGeneratorSettings settings)
		{
			if (_map == null)
			{
				return;
			}
			ushort size = (ushort)_map["size"];
			state.Map = new(size, size);
			settings.mapType = PolytopiaBackendBase.Game.MapPreset.Dryland;
		}

		public static void Init()
		{
			EnumCache<ImprovementAbility.Type>.AddMapping("climatesetter", (ImprovementAbility.Type)600);
			EnumCache<ImprovementAbility.Type>.AddMapping("climatesetter", (ImprovementAbility.Type)600);
            EnumCache<TribeData.Type>.AddMapping("mapmaker", (TribeData.Type)815);
            EnumCache<TribeData.Type>.AddMapping("mapmaker", (TribeData.Type)815);
		}

		private static void PostGenerate(ref GameState state)
		{
			if (_map == null)
			{
				return;
			}
			MapData originalMap = state.Map;

			for (int i = 0; i < originalMap.tiles.Length; i++)
			{
				TileData tile = originalMap.tiles[i];
				JToken tileJson = _map["map"][i];

				if (tileJson["skip"] != null && (bool)tileJson["skip"]) continue;

				tile.climate = (tileJson["climate"] == null || (int)tileJson["climate"] < 0 || (int)tileJson["climate"] > 16) ? 1 : (int)tileJson["climate"];
				tile.Skin = tileJson["skinType"] == null ? SkinType.Default : EnumCache<SkinType>.GetType((string)tileJson["skinType"]);
				tile.terrain = tileJson["terrain"] == null ? Polytopia.Data.TerrainData.Type.None : EnumCache<Polytopia.Data.TerrainData.Type>.GetType((string)tileJson["terrain"]);
				tile.resource = tileJson["resource"] == null ? null : new() { type = EnumCache<ResourceData.Type>.GetType((string)tileJson["resource"]) };
				tile.effects = new Il2CppSystem.Collections.Generic.List<TileData.EffectType>();

				if (tile.rulingCityCoordinates != tile.coordinates)
				{
					tile.improvement = tileJson["improvement"] == null ? null : new() { type = EnumCache<ImprovementData.Type>.GetType((string)tileJson["improvement"]) };
					if (tile.improvement != null && tile.improvement.type == ImprovementData.Type.City)
					{
						tile.improvement = new ImprovementState
						{
							type = ImprovementData.Type.City,
							founded = 0,
							level = 1,
							borderSize = 1,
							production = 1
						};
					}
				}
				else
				{
					if (_map["autoTribe"] != null && (bool)_map["autoTribe"])
					{
						state.TryGetPlayer(tile.owner, out PlayerState player);
						if (player == null)
						{
							throw new Exception($"Player {tile.owner} does not exist");
						}
						foreach (var tribe in PolytopiaDataManager.currentVersion.tribes.Values)
						{
							if (tile.climate == tribe.climate)
							{
								player.tribe = tribe.type;
							}
						}
					}
				}

				originalMap.tiles[i] = tile;
			}

			_map = null;
		}

		private static void OnCustomMapChanged(int index)
		{
			_map = JObject.Parse(File.ReadAllText(Directory.GetFiles(MAPS_PATH, "*.json")[index]));
		}

		// WILL GO TO POLYTOPIAUI FOR API STUFF.
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
		[HarmonyPatch(typeof(HudButtonBar), nameof(HudButtonBar.Init))]
		private static void HudButtonBar_Init(HudButtonBar __instance, HudScreen hudScreen)
		{
			AddUiButtonToArray(__instance.menuButton, __instance.hudScreen, (UIButtonBase.ButtonAction)MapRevealButton_OnClicked, __instance.buttonArray, "Reveal Map");
			AddUiButtonToArray(__instance.menuButton, __instance.hudScreen, (UIButtonBase.ButtonAction)AddPlayerButton_OnClicked, __instance.buttonArray, "Add Player");
            AddUiButtonToArray(__instance.menuButton, __instance.hudScreen, (UIButtonBase.ButtonAction)MapSaveButton_OnClicked, __instance.buttonArray, "Save Map");
			__instance.Show();
			__instance.Update();

			// UNUSED
			void MapRevealButton_OnClicked(int id, BaseEventData eventdata)
			{
				for (int i = 0; i < GameManager.GameState.Map.Tiles.Length; i++)
				{
					GameManager.GameState.Map.Tiles[i].SetExplored(GameManager.LocalPlayer.Id, true);
				}
				MapRenderer.Current.Refresh(false);
				NotificationManager.Notify("Map has been revealed.", "Mythopia Toolbox", null, null);
			}

			void MapSaveButton_OnClicked(int id, BaseEventData eventdata)
			{
				MapManager.BuildMapFile("map.json");
				NotificationManager.Notify("Map has been saved.", "Mythopia Toolbox", null, null);
			}
			
			// FOR MYTHOPIA AND WILL BE REMOVED LATER.
			void AddPlayerButton_OnClicked(int id, BaseEventData eventdata)
			{
                if(GameManager.PreliminaryGameSettings.GameType == GameType.PassAndPlay)
                {
                    Console.Write("Im Going to... WHAT!");
                    //GameManager.LocalPlayer.Currency += 100;
                    Console.Write(GameManager.LocalPlayer.UserName);
                    NotificationManager.Notify("Creating new player", "Mythopia Toolbox", null, null);
                    //Plugin.logger.LogMessage(GameManager.GameState.PlayerStates[0].AccountId);
                    int num = (int)(GameManager.GameState.PlayerStates[GameManager.GameState.PlayerStates.Count - 1].Id + 1);
                    PlayerState playerState = new PlayerState
                    {
                        Id = (byte)num,
                        AccountId = new Il2CppSystem.Nullable<Il2CppSystem.Guid>(Il2CppSystem.Guid.Empty),
                        AutoPlay = true,
                        startTile = new WorldCoordinates(0, 0),
                        hasChosenTribe = (GameManager.LocalPlayer.tribe > TribeData.Type.None),
                        tribe = GameManager.LocalPlayer.tribe,
                        skinType = ((GameManager.LocalPlayer.skinType == SkinType.Default) ? GameManager.GameState.Settings.GetSelectedSkin(GameManager.LocalPlayer.tribe) : GameManager.LocalPlayer.skinType),
                        handicap = 0,
						currency = 0,
						score = 0,
						cities = 0,
						kills = 0,
						casualities = 0,
						wipeOuts = 0,
                    };
                    GameManager.GameState.PlayerStates.Add(playerState);
                    Console.Write("Created player");
                    // GameManager.GameState.PlayerStates.Add(new PlayerState 
                    // { 
                    //     Id = 99,
                    //     UserName = "Test",
                    //     //AccountId = Il2CppSystem.Guid.Parse("06624b2c-f5af-4dd2-babf-f4bc66fec9f8"); 
                    //     AutoPlay = false,
                    //     Currency = 12
                    // });
                }
			}
		}
    }
}