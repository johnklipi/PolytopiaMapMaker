using Polytopia.Data;
using UnityEngine;

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
        button = Manager.CreatePicker(referenceButton, parent, CreatePopupButtons, Indent, HeaderKey);
    }

    internal virtual void Update(GameLogicData gameLogicData)
    {
        if(button == null)
            return;

        button.rectTransform.sizeDelta = buttonSize;
        button.Outline.gameObject.SetActive(false);
        button.BG.color = baseColor;
        Console.Write("////////////////////////");
        Console.Write(button.rectTransform.sizeDelta.y);
        Console.Write("////////////////////////");
        Manager.SetIcon(button!, GetIcon(gameLogicData), iconSize);
    }
    internal virtual Sprite GetIcon(GameLogicData gameLogicData)
    {
        return Manager.GetSprite(0, "none", gameLogicData);
    }

    internal virtual void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState) {}
}