using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.Level;
internal static class Pickers
{
    private static ClimatePicker climatePicker = new();
    private static MapPicker mapPicker = new();
    private static ResourcePicker resourcePicker = new();
    private static TerrainPicker terrainPicker = new();
    private static TileEffectPicker tileEffectPicker = new();
    private static ImprovementPicker improvementPicker = new();
    private static SpriteAtlasManager manager = GameManager.GetSpriteAtlasManager();
    public static Sprite GetSprite(int type, string spriteName, GameLogicData gameLogicData)
    {
        Sprite? sprite = null;
        if(type != 0)
        {
            TribeType tribeType = TribeType.Xinxi;
            if(Brush.chosenClimate != 0)
            {
                tribeType = gameLogicData.GetTribeTypeFromStyle(Brush.chosenClimate);
            }
            SpriteAtlasManager.SpriteLookupResult lookupResult = manager.DoSpriteLookup(spriteName, tribeType, Brush.chosenSkinType, false);
            sprite = lookupResult.sprite;
        }
        if(sprite == null)
        {
            sprite = PolyMod.Registry.GetSprite("none")!;
        }
        return sprite;
    }

    public static void SetIcon(UIRoundButton button, Sprite? icon, float iconSizeMultiplier = 0.8f)
    {
        button.faceIconSizeMultiplier = iconSizeMultiplier;
        button.icon.sprite = icon;
        button.icon.useSpriteMesh = true;
        button.icon.SetNativeSize();
        Vector2 sizeDelta = button.icon.rectTransform.sizeDelta;
        button.icon.rectTransform.sizeDelta = sizeDelta * button.faceIconSizeMultiplier;
        button.icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        button.icon.gameObject.SetActive(true);
    }

    internal delegate UIRoundButton PickerShowAction(UIRoundButton? picker, ref float num,
                                            SelectViewmodePopup selectViewmodePopup, GameState gameState);

    internal static UIRoundButton CreatePicker(UIRoundButton? picker, UIRoundButton referenceButton, Transform parent, PickerShowAction showAction, Vector3? indent = null, string headerKey = "")
    {
        picker = GameObject.Instantiate<UIRoundButton>(referenceButton, parent);
        if(indent != null)
        {
            picker.transform.position = picker.transform.position + (Vector3)indent;
        }
        picker.gameObject.SetActive(true);
        picker.OnClicked = (UIButtonBase.ButtonAction)ShowPopup;
        picker.text = string.Empty;

        void ShowPopup(int id, BaseEventData eventData)
        {
            SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
            selectViewmodePopup.Header = Localization.Get(headerKey, new Il2CppSystem.Object[] { });
            GameState gameState = GameManager.GameState;

            selectViewmodePopup.ClearButtons();
            selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
            float num = 0f;

            picker = showAction(picker, ref num, selectViewmodePopup, gameState);

            selectViewmodePopup.gridLayout.spacing = new Vector2(selectViewmodePopup.gridLayout.spacing.x, num + 10f);
            selectViewmodePopup.gridLayout.padding.bottom = Mathf.RoundToInt(num + 10f);
            selectViewmodePopup.gridBottomSpacer.minHeight = num + 10f;


            selectViewmodePopup.buttonData = new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData("buttons.ok", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null)
            };

            void Exit(int id, BaseEventData eventData)
            {
                selectViewmodePopup.Hide();
            }
            selectViewmodePopup.Show(picker!.rectTransform.position);
        }
        return picker;
    }

    internal static void CreateChoiceButton(
        SelectViewmodePopup viewmodePopup,
        string header,
        int id,
        ref float maxLabelHeight,
        System.Action<int> onClick,
        Color? backgroundColor = null,
        System.Action<UIRoundButton, int>? setIcon = null
    )
    {
        Color bg = backgroundColor ?? ColorUtil.SetAlphaOnColor(Color.white, 0.5f);

        UIRoundButton button = GameObject.Instantiate(
            viewmodePopup.buttonPrefab,
            viewmodePopup.gridLayout.transform
        );

        button.id = id;
        button.rectTransform.sizeDelta = new Vector2(56f, 56f);
        button.Outline.gameObject.SetActive(false);
        button.BG.color = bg;

        button.text = char.ToUpper(header[0]) + header.Substring(1);
        button.SetIconColor(Color.white);
        button.ButtonEnabled = true;

        button.OnClicked = (UIButtonBase.ButtonAction)((btnId, eventData) =>
        {
            onClick(btnId);
        });

        if(setIcon != null)
            setIcon(button, id);

        if (button.Label.PreferedValues.y > maxLabelHeight)
        {
            maxLabelHeight = button.Label.PreferedValues.y;
        }

        viewmodePopup.buttons.Add(button);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
    private static void HudScreen_OnMatchStart(HudScreen __instance)
    {
        if (MapLoader.inMapMaker)
        {
            UIRoundButton referenceButton = __instance.replayInterface.viewmodeSelectButton;
            climatePicker.Create(referenceButton, __instance.transform);
            mapPicker.Create(referenceButton, __instance.transform);
            resourcePicker.Create(referenceButton, __instance.transform);
            terrainPicker.Create(referenceButton, __instance.transform);
            tileEffectPicker.Create(referenceButton, __instance.transform);
            improvementPicker.Create(referenceButton, __instance.transform);

            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
            climatePicker.Update(gameLogicData);
            mapPicker.Update(gameLogicData);
            resourcePicker.Update(gameLogicData);
            terrainPicker.Update(gameLogicData);
            tileEffectPicker.Update(gameLogicData);
            improvementPicker.Update(gameLogicData);
        }
    }
}
