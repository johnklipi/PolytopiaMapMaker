using Polytopia.Data;
using PolytopiaBackendBase.Common;
using UnityEngine;

namespace PolytopiaMapManager.Level
{
    public static class PickersHelper
    {
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
        public static void SetIcon(UIRoundButton button, Sprite icon, float iconSizeMultiplier = 0.8f)
        {
            if(string.IsNullOrEmpty(icon.name))
                iconSizeMultiplier = 0.8f;
            button.faceIconSizeMultiplier = iconSizeMultiplier;
            button.icon.sprite = icon;
            button.icon.useSpriteMesh = true;
            button.icon.SetNativeSize();
            Vector2 sizeDelta = button.icon.rectTransform.sizeDelta;
            button.icon.rectTransform.sizeDelta = sizeDelta * button.faceIconSizeMultiplier;
            button.icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            button.icon.gameObject.SetActive(true);
        }
    }
}