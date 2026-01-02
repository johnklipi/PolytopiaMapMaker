using Polytopia.Data;
using PolytopiaBackendBase.Common;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PolytopiaMapManager.UI.Picker;
internal class PickerBase
{
    internal UIRoundButton? button;

    internal Vector2 buttonSize = new Vector2(75f, 75f);
    internal Color baseColor = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
    internal float iconSize = 0.6f;
    internal virtual string HeaderKey => "";
    internal virtual Vector3? Indent => null;

    internal virtual void Create(UIRoundButton referenceButton, Transform parent)
    {
        button = CreatePicker(referenceButton, parent, CreatePopupButtons, Indent, HeaderKey);
    }

    internal virtual void Update(GameLogicData gameLogicData)
    {
        if(button == null)
            return;

        button.rectTransform.sizeDelta = buttonSize;
        button.Outline.gameObject.SetActive(false);
        button.BG.color = baseColor;
        SetIcon(button!, GetIcon(gameLogicData), iconSize);
    }

    internal virtual Sprite GetIcon()
    {
        return PolyMod.Registry.GetSprite("none")!;
    }

    internal virtual Sprite GetIcon(GameLogicData gameLogicData)
    {
        return GetSprite(0, "none", gameLogicData);
    }

    internal virtual void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState) {}

    public Sprite GetSprite(int type, string spriteName, GameLogicData gameLogicData)
    {
        Sprite? sprite = null;
        if(type >= 1000)
            return PolyMod.Registry.GetSprite("remove_icon")!;

        if(type != 0)
        {
            TribeType tribeType = TribeType.Xinxi;
            if(Brush.chosenClimate != 0)
            {
                tribeType = gameLogicData.GetTribeTypeFromStyle(Brush.chosenClimate);
            }
            SpriteAtlasManager.SpriteLookupResult lookupResult = Editor.spriteAtlasManager.DoSpriteLookup(spriteName, tribeType, Brush.chosenSkinType, false);
            sprite = lookupResult.sprite;
        }
        if(sprite == null)
        {
            sprite = GetIcon();
        }
        return sprite;
    }

    public void SetIcon(UIRoundButton button, Sprite? icon, float iconSizeMultiplier = 0.8f)
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

    internal delegate void UtilshowAction(ref float num,
                                            SelectViewmodePopup selectViewmodePopup, GameState gameState);

    internal UIRoundButton CreatePicker(UIRoundButton referenceButton, Transform parent, UtilshowAction showAction, Vector3? indent = null, string headerKey = "")
    {
        UIRoundButton picker = GameObject.Instantiate<UIRoundButton>(referenceButton, parent);
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

            showAction(ref num, selectViewmodePopup, gameState);

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

    internal void CreateChoiceButton(
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
}