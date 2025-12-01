using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PolytopiaMapManager.Popup;

public static class CustomInput
{
    ///// TO DO
    ///     Normal functioning layout so inputtext is between description and buttons
    ///     Clean up class a bit, lots of useless commands there, problem is that I've no idea what does what atp
    ///     OnInputDone triggers two times?

    public static void OnInputDone(string value, BasicPopup popup)
    {
        Main.modLogger!.LogMessage(value);
        //MapMaker.MapName = value;
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