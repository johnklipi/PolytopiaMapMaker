using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.Level;
internal class PickerBase
{
    internal UIRoundButton? button;

    internal void Create(UIRoundButton referenceButton, Transform parent){}

    internal void Update(GameLogicData gameLogicData)
    {
        button!.rectTransform.sizeDelta = new Vector2(75f, 75f);
        button!.Outline.gameObject.SetActive(false);
        button!.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        Pickers.SetIcon(button!, GetIcon(gameLogicData), 0.6f);
    }

    internal static Sprite? GetIcon(GameLogicData gameLogicData)
    {
        return null;
    }
}