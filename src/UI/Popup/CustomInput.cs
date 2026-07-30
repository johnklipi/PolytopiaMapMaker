using HarmonyLib;
using TMPro;
using UnityEngine;

namespace PolytopiaMapManager.Popup;

public static class CustomInput
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BasicPopupLegacy), nameof(BasicPopupLegacy.Show))] //Doesnt even work anymore
    private static void BasicPopup_Show(BasicPopupLegacy __instance)
    {
        var input = GetInputFromPopup(__instance);
        if (input != null)
        {
            var inputHeight = input.m_RectTransform.GetHeight();
            __instance.rectTransform.SetHeight(__instance.content.GetHeight() + (inputHeight * 1.5f));
        }
    }

    public static void RemoveInputFromPopup(BasicPopupLegacy popup)
    {
        var input = GetInputFromPopup(popup);
        if (input == null) return;
        UnityEngine.Object.Destroy(input.gameObject);
        UnityEngine.EventSystems.EventSystem.current.sendNavigationEvents = true;
        InputManager.EnableInput(InputManager.InputType.Camera | InputManager.InputType.Map | InputManager.InputType.Input);
    }

    public static TMP_InputField? GetInputFromPopup(PopupBase popup)
    {
        Transform t = popup.transform.Find("InputBox");

        if (t == null && popup is BasicPopupLegacy legacyPopup && legacyPopup.content != null)
        {
            t = legacyPopup.content.transform.Find("InputBox");
        }

        if (t == null) return null;

        return t.GetComponent<TMPro.TMP_InputField>();
    }

    /*public static TMP_InputField? GetInputFromPopup(PopupBase popup)
    {
        Transform t = popup.transform.Find("InputBox");
        if (t == null) return null;

        return t.GetComponent<TMPro.TMP_InputField>();
    }*/


    public static void AddInputToPopup(BasicPopupLegacy popup, string baseValue = "", Action<string>? onSubmit = null, Action<string>? onValueChanged = null)
    {
        UnityEngine.EventSystems.EventSystem.current.sendNavigationEvents = false;
        InputManager.DisableInput(InputManager.InputType.Camera | InputManager.InputType.Map | InputManager.InputType.Input);
        Transform parent = popup.content != null ? popup.content.transform : popup.transform;

        GameObject go = new GameObject("InputBox");
        go.transform.SetParent(parent, false);


        // Body
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 50);
        rt.anchoredPosition = new Vector2(0, 0);

        var layoutElement = go.AddComponent<UnityEngine.UI.LayoutElement>();
        layoutElement.minWidth = 400;
        layoutElement.minHeight = 50;
        layoutElement.preferredWidth = 400;
        layoutElement.preferredHeight = 50;

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
        input.text = baseValue;
        popup.IsUnskippable = true;
        if (onSubmit != null)
        {
            input.onSubmit.RemoveAllListeners();
            input.onSubmit.AddListener(onSubmit);
        }
        if (onValueChanged != null)
        {
            input.onValueChanged.RemoveAllListeners();
            input.onValueChanged.AddListener(onValueChanged);
        }

        popup.Show();

        UINavigationManager.Select(input);
        popup.currentSelectable = input;
    }
}