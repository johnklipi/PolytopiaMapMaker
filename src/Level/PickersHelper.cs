using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            Sprite sprite;
            if(type != 0)
            {
                SpriteAtlasManager.SpriteLookupResult lookupResult = manager.DoSpriteLookup(spriteName, gameLogicData.GetTribeTypeFromStyle(MapMaker.chosenClimate), MapMaker.chosenSkinType, false);
                sprite = lookupResult.sprite;
            }
            else
            {
                sprite = PolyMod.Registry.GetSprite("none")!;
            }
            return sprite;
        }
    }
}