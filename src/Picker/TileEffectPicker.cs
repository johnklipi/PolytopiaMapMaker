using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.Picker;
internal class TileEffectPicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.tileeffect";
    internal override Vector3? Indent => new Vector3(270, 0, 0);
    internal static List<TileData.EffectType> excludedTileEffects = new()
    {
        TileData.EffectType.Swamped,
        TileData.EffectType.Tentacle,
        TileData.EffectType.Foam,
    };

    internal override Sprite GetIcon(GameLogicData gameLogicData)
    {
       return Utils.GetSprite((int)Brush.chosenTileEffect, TileEffectToString(Brush.chosenTileEffect), gameLogicData);
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        foreach (TileData.EffectType tileEffect in System.Enum.GetValues(typeof(TileData.EffectType)))
        {
            if(excludedTileEffects.Contains(tileEffect))
                continue;

            string tileEffectName = Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(tileEffect)}");

            Utils.CreateChoiceButton(selectViewmodePopup, tileEffectName,
                    (int)tileEffect, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetTileEffectIcon);

            void OnClick(int id)
            {
                Brush.chosenTileEffect = (TileData.EffectType)id;
                Update(gameState.GameLogicData);
            }

            void SetTileEffectIcon(UIRoundButton button, int type)
            {
                Utils.SetIcon(button, Utils.GetSprite(type, TileEffectToString((TileData.EffectType)type), gameState.GameLogicData), 0.6f);
            }
        }
    }

    internal static string TileEffectToString(TileData.EffectType tileEffect)
    {
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