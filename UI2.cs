using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

using TMPro;

namespace PolytopiaMapManager
{

    public static class CustomInput
    {
        ///// TO DO
        ///     Normal functioning layout so inputtext is between description and buttons
        ///     Clean up class a bit, lots of useless commands there, problem is that I've no idea what does what atp
        ///     OnInputDone triggers two times?

        public static void OnInputDone(string value, BasicPopup popup)
        {
            MapMaker.modLogger!.LogMessage(value);
            MapMaker.MapName = value;
        }

        public static void RemoveInputFromPopup(BasicPopup popup)
        {
            var input = GetInputFromPopup(popup);
            if (input == null) return;
            UnityEngine.Object.Destroy(input.gameObject);
        }

        public static TMP_InputField GetInputFromPopup(BasicPopup popup)
        {
            Transform t = popup.transform.Find("InputBox");
            if (t == null) return null;

            return t.GetComponent<TMPro.TMP_InputField>();
        }


        public static void AddInputToPopup(BasicPopup popup)
        {
            Transform parent = popup.transform; //Popup is parent

            GameObject go = new GameObject("InputBox");
            go.transform.SetParent(parent, false);


            // Body
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 50);
            rt.anchoredPosition = new Vector2(0, -150);



            // Background
            var bg = go.AddComponent<UnityEngine.UI.Image>();
            bg.color = Color.white;

            // Text
            GameObject textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<TMPro.TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 40;
            text.color = Color.black;

            // Text layout
            RectTransform txtRT = textGo.GetComponent<RectTransform>();
            txtRT.anchorMin = Vector2.zero;
            txtRT.anchorMax = Vector2.one;
            txtRT.offsetMin = new Vector2(10, 10);
            txtRT.offsetMax = new Vector2(-10, -10);

            // Textarea
            GameObject textArea = new GameObject("Text Area");
            textArea.transform.SetParent(go.transform, false);

            RectTransform textAreaRT = textArea.AddComponent<RectTransform>();
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(10, 10);
            textAreaRT.offsetMax = new Vector2(-10, -10);

            textArea.AddComponent<UnityEngine.UI.RectMask2D>();

            // Placeholder

            GameObject placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(textArea.transform, false);

            var placeholder = placeholderGO.AddComponent<TMPro.TextMeshProUGUI>();
            placeholder.text = "Type here...";
            placeholder.fontSize = 36;
            placeholder.color = new Color(0.6f, 0.6f, 0.6f);

            RectTransform placeholderRT = placeholderGO.GetComponent<RectTransform>();
            placeholderRT.anchorMin = Vector2.zero;
            placeholderRT.anchorMax = Vector2.one;
            placeholderRT.offsetMin = Vector2.zero;
            placeholderRT.offsetMax = Vector2.zero;


            // TMP InputField
            var input = go.AddComponent<TMPro.TMP_InputField>();
            input.textViewport = textAreaRT;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.interactable = true;
            input.text = MapMaker.MapName;
            popup.IsUnskippable = true;
            input.onSubmit.RemoveAllListeners();
            input.onSubmit.AddListener(new Action<string>(value => OnInputDone(value, popup)));
            popup.Show();
            UINavigationManager.Select(input);
            popup.currentSelectable = input;


        }
    }
    public static class UI2
    {
        private static List<string> visualMaps = new();
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.Show))]
        private static void GameSetupScreen_Show(GameSetupScreen __instance, bool instant)
        {
            int diffListIndex = GetHorizontalListIndex(__instance, "gamesettings.difficulty");

            List<string> types = new();
            foreach (MapMaker.MapGenerationType value in Enum.GetValues(typeof(MapMaker.MapGenerationType)))
            {
                types.Add(value.ToString());
            }
            string[] maps = Directory.GetFiles(MapMaker.MAPS_PATH, "*.json");
            visualMaps = new();
            int num = 1;
            if (maps.Length > 0)
            {
                visualMaps = maps.Select(map => Path.GetFileNameWithoutExtension(map)).ToList();
                num++;
            }

            diffListIndex++;
            UIHorizontalList horizontalList = CreateUIHorizontalList(__instance, "gamesettings.generationtype", types.ToArray(), new Action<int>(OnMapGenTypeChanged), 0, null, maps.Length + 1, (Il2CppSystem.Action)OnTriedSelectDisabledMapGenType, rowPosition: diffListIndex);
        }
        private static void OnTriedSelectDisabledMapGenType()
        {
            NotificationManager.Notify(Localization.Get("gamesettings.nomaps", new Il2CppSystem.Object[] { }), Localization.Get("gamesettings.notavailable", new Il2CppSystem.Object[] { }), null, null);
        }
        private static void OnMapGenTypeChanged(int index)
        { }

        private static UIHorizontalList CreateUIHorizontalList(GameSetupScreen gameSetupScreen, string headerKey, string[] items, Action<int> indexChangedCallback = null, int selectedIndex = 0, RectTransform parent = null, int enabledItemCount = -1, Il2CppSystem.Action onClickDisabledItemCallback = null, int rowPosition = -1)
        {
            UIHorizontalList uihorizontalList = GameObject.Instantiate<UIHorizontalList>(gameSetupScreen.horizontalListPrefab, parent ?? gameSetupScreen.VerticalListRectTr);
            uihorizontalList.HeaderKey = headerKey;
            uihorizontalList.IndexChangedCallback = indexChangedCallback;
            uihorizontalList.OnSelectDisabledItemCallback = onClickDisabledItemCallback;
            uihorizontalList.SetData(items, null, selectedIndex, false);
            uihorizontalList.UpdateScrollerOnHighlight = true;
            gameSetupScreen.totalHeight += uihorizontalList.rectTransform.sizeDelta.y;
            if (rowPosition != -1 || rowPosition <= gameSetupScreen.rows.Count)
            {
                gameSetupScreen.rows.Add(uihorizontalList.gameObject);
            }
            else
            {
                gameSetupScreen.rows.Insert(rowPosition, uihorizontalList.gameObject);
            }
            if (gameSetupScreen.topmostHorizontalList == null)
            {
                gameSetupScreen.topmostHorizontalList = uihorizontalList;
                PolytopiaInput.Omnicursor.AffixToUIElement(uihorizontalList.items[uihorizontalList.SelectedIndex].button.GetComponent<RectTransform>());
            }
            return uihorizontalList;
        }
        internal static int GetHorizontalListIndex(GameSetupScreen gameSetupScreen, string headerKey)
        {
            for (int i = 0; i < gameSetupScreen.rows.Count; i++)
            {
                GameObject item = gameSetupScreen.rows[i];
                Type? type = GetTypeByName(item.name);
                if (type != null && type == typeof(UIHorizontalList))
                {
                    if (item.TryGetComponent<UIHorizontalList>(out UIHorizontalList horizontalList))
                    {
                        if (horizontalList.HeaderKey == headerKey)
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        internal static Type? GetTypeByName(string name)
        {
            if (name.Contains("GameSetupNameRow"))
            {
                return typeof(GameSetupNameRow);
            }
            else if (name.Contains("Spacer"))
            {
                return typeof(UISpacer);
            }
            else if (name.Contains("HorizontalList"))
            {
                return typeof(UIHorizontalList);
            }
            else if (name.Contains("GameSetupInfoRow"))
            {
                return typeof(GameSetupInfoRow);
            }
            else if (name.Contains("Button"))
            {
                return typeof(ButtonRow);
            }
            return null;
        }
    }
}