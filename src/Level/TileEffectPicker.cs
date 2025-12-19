using Polytopia.Data;
using UnityEngine;

namespace PolytopiaMapManager.Level;
internal class TileEffectPicker : PickerBase
{
    internal static List<TileData.EffectType> excludedTileEffects = new()
    {
        TileData.EffectType.Swamped,
        TileData.EffectType.Tentacle,
        TileData.EffectType.Foam,
    };

    internal new void Create(UIRoundButton referenceButton, Transform parent)
    {
        button = Pickers.CreatePicker(button, referenceButton, parent, CreateTileEffectButtons, new Vector3(270, 0, 0), "mapmaker.choose.tileeffect");
        UIRoundButton? CreateTileEffectButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
        {
            if(picker != null)
            {
                foreach (TileData.EffectType tileEffect in System.Enum.GetValues(typeof(TileData.EffectType)))
                {
                    if(excludedTileEffects.Contains(tileEffect))
                        continue;

                    string tileEffectName = Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(tileEffect)}");

                    Pickers.CreateChoiceButton(selectViewmodePopup, tileEffectName,
                            (int)tileEffect, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetTileEffectIcon);

                    void OnClick(int id)
                    {
                        Brush.chosenTileEffect = (TileData.EffectType)id;
                        Update(gameState.GameLogicData);
                    }

                    void SetTileEffectIcon(UIRoundButton button, int type)
                    {
                        Pickers.SetIcon(button, Pickers.GetSprite(type, TileEffectToString((TileData.EffectType)type), gameState.GameLogicData), 0.6f);
                    }
                }
                return picker;
            }
            return null;
        }
    }

    internal new static Sprite GetIcon(GameLogicData gameLogicData)
    {
       return Pickers.GetSprite((int)Brush.chosenTileEffect, TileEffectToString(Brush.chosenTileEffect), gameLogicData);
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