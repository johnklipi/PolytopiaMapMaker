using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.Picker;
internal class ClimatePicker : PickerBase
{
    internal override string HeaderKey => "mapmaker.choose.climate";

    internal override void Update(GameLogicData gameLogicData)
    {
        base.Update(gameLogicData);

        if(button == null)
            return;

        if(Brush.chosenClimate != 0)
        {
            TribeType tribeType = gameLogicData.GetTribeTypeFromStyle(Brush.chosenClimate);
            string spriteName;
            if (Brush.chosenSkinType == SkinType.Default)
            {
                spriteName = EnumCache<TribeType>.GetName(tribeType);
            }
            else
            {
                spriteName = EnumCache<SkinType>.GetName(Brush.chosenSkinType);
            }
            button.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(spriteName));
            button.BG.color = ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeType, Brush.chosenSkinType)), 1f);
        }
        else
        {
            button.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(SpriteData.SpecialFaceIcon.neutral));
            button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 1f);
        }
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        Utils.CreateChoiceButton(selectViewmodePopup, "none",
                (int)TribeType.None, ref num, OnClick, ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(16777215), 1f), SetHeadIcon);

        void OnClick(int id)
        {
            if (id < 0)
            {
                id *= -1;
                SkinType skinType = (SkinType)id;
                Brush.chosenClimate = MapLoader.GetTribeClimateFromSkin(skinType, gameState.GameLogicData);
                Brush.chosenSkinType = skinType;
            }
            else
            {
                if((TribeType)id != TribeType.None)
                {
                    Brush.chosenClimate = MapLoader.GetTribeClimateFromType((TribeType)id, gameState.GameLogicData);
                }
                else
                {
                    Brush.chosenClimate = 0;
                }
                Brush.chosenSkinType = SkinType.Default;
            }
            this.Update(gameState.GameLogicData);
            Utils.resourcePicker.Update(gameState.GameLogicData);
            Utils.terrainPicker.Update(gameState.GameLogicData);
            Utils.tileEffectPicker.Update(gameState.GameLogicData);
            Utils.improvementPicker.Update(gameState.GameLogicData);
        }

        void SetHeadIcon(UIRoundButton button, int type)
        {
            string spriteName = EnumCache<TribeType>.GetName((TribeType)type);
            if(type < 0)
            {
                type *= -1;
                spriteName = EnumCache<SkinType>.GetName((SkinType)type);
            }
            button.iconSpriteHandle.SetCompletion((SpriteHandleCallback)TribeSpriteHandle);
            void TribeSpriteHandle(SpriteHandle spriteHandleCallback)
            {
                button.SetFaceIcon(spriteHandleCallback.sprite);
            }
            if((TribeType)type != TribeType.None)
            {
                button.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(spriteName));
            }
            else
            {
                button.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(SpriteData.SpecialFaceIcon.neutral));
            }
        }

        GameLogicData gameLogicData = gameState.GameLogicData;
        List<TribeData> tribes = gameLogicData.GetTribes(TribeData.CategoryEnum.Human).ToArray().ToList().Concat(gameLogicData.GetTribes(TribeData.CategoryEnum.Special).ToArray().ToList()).ToList();
        foreach (TribeData tribeData in tribes)
        {
            TribeType tribeType = tribeData.type;
            string tribeName = Localization.Get(tribeData.displayName);

            Utils.CreateChoiceButton(selectViewmodePopup, tribeName,
                (int)tribeType, ref num, OnClick, ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeData.type, SkinType.Default)), 1f), SetHeadIcon);

            foreach (SkinType skinType in tribeData.skins)
            {
                string skinHeader = string.Format(Localization.Get(SkinTypeExtensions.GetSkinNameKey(), new Il2CppSystem.Object[] { }), Localization.Get(skinType.GetLocalizationKey(), new Il2CppSystem.Object[] { }));

                Utils.CreateChoiceButton(selectViewmodePopup, skinHeader,
                    -(int)skinType, ref num, OnClick, ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeData.type, skinType)), 1f), SetHeadIcon);
            }
        }
    }
}