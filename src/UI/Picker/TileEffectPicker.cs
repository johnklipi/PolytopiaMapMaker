using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.UI.Picker;
internal class TileEffectPicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.tileeffect";
    internal static List<TileData.EffectType> excludedTileEffects = new()
    {
        TileData.EffectType.Swamped,
        TileData.EffectType.Tentacle,
        TileData.EffectType.Foam,
    };

    internal override Sprite GetIcon(GameLogicData gameLogicData)
    {
       return base.GetSprite(chosenValue, TileEffectToString(chosenValue), gameLogicData);
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        foreach (TileData.EffectType tileEffect in System.Enum.GetValues(typeof(TileData.EffectType)))
        {
            if(excludedTileEffects.Contains(tileEffect))
                continue;

            string tileEffectName = Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(tileEffect)}");

            base.CreateChoiceButton(selectViewmodePopup, tileEffectName,
                    (int)tileEffect, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetTileEffectIcon);

            if(tileEffect == TileData.EffectType.None)
                base.CreateChoiceButton(selectViewmodePopup, Localization.Get("mapmaker.remove"),
                    PickerBase.DESTROY_OPTION_ID, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetTileEffectIcon);

            void OnClick(int id)
            {
                chosenValue = id;
                Update(gameState.GameLogicData);
            }

            void SetTileEffectIcon(UIRoundButton button, int type)
            {
                base.SetIcon(button, base.GetSprite(type, TileEffectToString(type), gameState.GameLogicData), 0.6f);
            }
        }
    }

    internal static string TileEffectToString(int effectIdx)
    {
        TileData.EffectType tileEffect = (TileData.EffectType)effectIdx;
        if(tileEffect == TileData.EffectType.Flooded)
        {
            return SpriteData.TILE_WETLAND;
        }
        if(tileEffect == TileData.EffectType.Algae)
        {
            return "algae";
        }
        return "";
    }
}