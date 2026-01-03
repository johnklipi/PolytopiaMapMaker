using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

namespace PolytopiaMapManager.UI.Picker;
internal class BiomePicker : PickerBase
{
    internal SkinType chosenSkinType = SkinType.Default;
    internal override string HeaderKey => "mapmaker.choose.climate";
    internal static List<TribeType> excludedTribes = new()
    {
        TribeType.Polaris,
    };

    internal static List<SkinType> excludedSkins = new()
    {
        SkinType.DarkElf,
        SkinType.Magma,
    };

    internal override void Update(GameLogicData gameLogicData)
    {
        base.Update(gameLogicData);

        if(button == null)
            return;

        if(chosenValue != 0)
        {
            TribeType tribeType = gameLogicData.GetTribeTypeFromStyle(chosenValue);
            string spriteName;
            if (chosenSkinType == SkinType.Default)
            {
                spriteName = EnumCache<TribeType>.GetName(tribeType);
            }
            else
            {
                spriteName = EnumCache<SkinType>.GetName(chosenSkinType);
            }
            button.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(spriteName));
            button.BG.color = ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeType, chosenSkinType)), 1f);
        }
        else
        {
            button.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(SpriteData.SpecialFaceIcon.neutral));
            button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 1f);
        }
    }

    internal override void CreatePopupButtons(ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
    {
        base.CreateChoiceButton(selectViewmodePopup, "none",
                (int)TribeType.None, ref num, OnClick, ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(16777215), 1f), SetHeadIcon);

        void OnClick(int id)
        {
            if (id < 0)
            {
                id *= -1;
                SkinType skinType = (SkinType)id;
                chosenValue = Loader.GetTribeClimateFromSkin(skinType, gameState.GameLogicData);
                chosenSkinType = skinType;
            }
            else
            {
                if((TribeType)id != TribeType.None)
                {
                    chosenValue = Loader.GetTribeClimateFromType((TribeType)id, gameState.GameLogicData);
                }
                else
                {
                    chosenValue = 0;
                }
                chosenSkinType = SkinType.Default;
            }
            this.Update(gameState.GameLogicData);
            Editor.resourcePicker.Update(gameState.GameLogicData);
            Editor.terrainPicker.Update(gameState.GameLogicData);
            Editor.tileEffectPicker.Update(gameState.GameLogicData);
            Editor.improvementPicker.Update(gameState.GameLogicData);
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
        List<TribeData> tribes = gameLogicData.GetTribes(TribeData.CategoryEnum.Human).ToArray()
            .Concat(gameLogicData.GetTribes(TribeData.CategoryEnum.Special).ToArray()).ToList();

        foreach (TribeData tribeData in tribes)
        {
            TribeType tribeType = tribeData.type;
            if(excludedTribes.Contains(tribeType))
                continue;

            string tribeName = Localization.Get(tribeData.displayName);

            base.CreateChoiceButton(selectViewmodePopup, tribeName,
                (int)tribeType, ref num, OnClick, ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeData.type, SkinType.Default)), 1f), SetHeadIcon);

            foreach (SkinType skinType in tribeData.skins)
            {
                if(excludedSkins.Contains(skinType))
                    continue;
                string skinHeader = string.Format(Localization.Get(SkinTypeExtensions.GetSkinNameKey(), new Il2CppSystem.Object[] { }), Localization.Get(skinType.GetLocalizationKey(), new Il2CppSystem.Object[] { }));

                base.CreateChoiceButton(selectViewmodePopup, skinHeader,
                    -(int)skinType, ref num, OnClick, ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeData.type, skinType)), 1f), SetHeadIcon);
            }
        }
    }
}