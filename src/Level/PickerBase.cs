using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.Level;
internal class PickerBase
{
    internal UIRoundButton? button;

    internal Vector2 buttonSize = new Vector2(75f, 75f);
    internal Color baseColor = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);

    internal virtual void Create(UIRoundButton referenceButton, Transform parent){}
    internal virtual void Update(GameLogicData gameLogicData)
    {
        button!.rectTransform.sizeDelta = buttonSize;
        button!.Outline.gameObject.SetActive(false);
        button!.BG.color = baseColor;
        Pickers.SetIcon(button!, GetIcon(gameLogicData), 0.6f);
    }
    internal virtual Sprite GetIcon(GameLogicData gameLogicData)
    {
        return Pickers.GetSprite(0, "none", gameLogicData);
    }
}