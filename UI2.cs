// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using HarmonyLib;
// using UnityEngine;

// using TMPro;

// namespace PolytopiaMapManager
// {
//     public static class UI2
//     {
//         private static List<string> visualMaps = new();
//         // [HarmonyPostfix]
//         // [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.Show))]
//         private static void GameSetupScreen_Show(GameSetupScreen __instance, bool instant)
//         {
//             int diffListIndex = GetHorizontalListIndex(__instance, "gamesettings.difficulty");

//             List<string> types = new();
//             foreach (MapMaker.MapGenerationType value in Enum.GetValues(typeof(MapMaker.MapGenerationType)))
//             {
//                 types.Add(value.ToString());
//             }
//             string[] maps = Directory.GetFiles(MapMaker.MAPS_PATH, "*.json");
//             visualMaps = new();
//             int num = 1;
//             if (maps.Length > 0)
//             {
//                 visualMaps = maps.Select(map => Path.GetFileNameWithoutExtension(map)).ToList();
//                 num++;
//             }

//             diffListIndex++;
//             UIHorizontalList horizontalList = CreateUIHorizontalList(__instance, "gamesettings.generationtype", types.ToArray(), new Action<int>(OnMapGenTypeChanged), 0, null, maps.Length + 1, (Il2CppSystem.Action)OnTriedSelectDisabledMapGenType, rowPosition: diffListIndex);
//         }
//         private static void OnTriedSelectDisabledMapGenType()
//         {
//             NotificationManager.Notify(Localization.Get("gamesettings.nomaps", new Il2CppSystem.Object[] { }), Localization.Get("gamesettings.notavailable", new Il2CppSystem.Object[] { }), null, null);
//         }
//         private static void OnMapGenTypeChanged(int index)
//         { }

//         private static UIHorizontalList CreateUIHorizontalList(GameSetupScreen gameSetupScreen, string headerKey, string[] items, Action<int> indexChangedCallback = null, int selectedIndex = 0, RectTransform parent = null, int enabledItemCount = -1, Il2CppSystem.Action onClickDisabledItemCallback = null, int rowPosition = -1)
//         {
//             UIHorizontalList uihorizontalList = GameObject.Instantiate<UIHorizontalList>(gameSetupScreen.horizontalListPrefab, parent ?? gameSetupScreen.VerticalListRectTr);
//             uihorizontalList.HeaderKey = headerKey;
//             uihorizontalList.IndexChangedCallback = indexChangedCallback;
//             uihorizontalList.OnSelectDisabledItemCallback = onClickDisabledItemCallback;
//             uihorizontalList.SetData(items, null, selectedIndex, false);
//             uihorizontalList.UpdateScrollerOnHighlight = true;
//             gameSetupScreen.totalHeight += uihorizontalList.rectTransform.sizeDelta.y;
//             if (rowPosition != -1 || rowPosition <= gameSetupScreen.rows.Count)
//             {
//                 gameSetupScreen.rows.Add(uihorizontalList.gameObject);
//             }
//             else
//             {
//                 gameSetupScreen.rows.Insert(rowPosition, uihorizontalList.gameObject);
//             }
//             if (gameSetupScreen.topmostHorizontalList == null)
//             {
//                 gameSetupScreen.topmostHorizontalList = uihorizontalList;
//                 PolytopiaInput.Omnicursor.AffixToUIElement(uihorizontalList.items[uihorizontalList.SelectedIndex].button.GetComponent<RectTransform>());
//             }
//             return uihorizontalList;
//         }
//         internal static int GetHorizontalListIndex(GameSetupScreen gameSetupScreen, string headerKey)
//         {
//             for (int i = 0; i < gameSetupScreen.rows.Count; i++)
//             {
//                 GameObject item = gameSetupScreen.rows[i];
//                 Type? type = GetTypeByName(item.name);
//                 if (type != null && type == typeof(UIHorizontalList))
//                 {
//                     if (item.TryGetComponent<UIHorizontalList>(out UIHorizontalList horizontalList))
//                     {
//                         if (horizontalList.HeaderKey == headerKey)
//                         {
//                             return i;
//                         }
//                     }
//                 }
//             }
//             return -1;
//         }

//         internal static Type? GetTypeByName(string name)
//         {
//             if (name.Contains("GameSetupNameRow"))
//             {
//                 return typeof(GameSetupNameRow);
//             }
//             else if (name.Contains("Spacer"))
//             {
//                 return typeof(UISpacer);
//             }
//             else if (name.Contains("HorizontalList"))
//             {
//                 return typeof(UIHorizontalList);
//             }
//             else if (name.Contains("GameSetupInfoRow"))
//             {
//                 return typeof(GameSetupInfoRow);
//             }
//             else if (name.Contains("Button"))
//             {
//                 return typeof(ButtonRow);
//             }
//             return null;
//         }
//     }
// }