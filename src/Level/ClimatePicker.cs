using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;
using SevenZip.Compression.LZMA;
using UnityEngine.Rendering.Universal;

namespace PolytopiaMapManager.Level;
internal class ClimatePicker : PickerBase
{
    internal const int SKINS_NUM = 1000;

    internal override void Create(UIRoundButton referenceButton, Transform parent)
    {
        button = Pickers.CreatePicker(button, referenceButton, parent, CreateClimateButtons, headerKey: "mapmaker.choose.climate");
        UIRoundButton? CreateClimateButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
        {
            if(picker != null)
            {
                Pickers.CreateChoiceButton(selectViewmodePopup, "none",
                        (int)TribeType.None, ref num, OnClick, ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(16777215), 1f), SetHeadIcon);

                void OnClick(int id)
                {
                    if (id >= SKINS_NUM)
                    {
                        id -= SKINS_NUM;
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
                    // ResourcePicker.Update(gameState.GameLogicData);
                    // TerrainPicker.Update(gameState.GameLogicData);
                    // TileEffectPicker.Update(gameState.GameLogicData);
                    // ImprovementPicker.Update(gameState.GameLogicData);
                }

                void SetHeadIcon(UIRoundButton button, int type)
                {
                    string spriteName = EnumCache<TribeType>.GetName((TribeType)type);
                    if(type >= SKINS_NUM)
                    {
                        type -= SKINS_NUM;
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

                    Pickers.CreateChoiceButton(selectViewmodePopup, tribeName,
                        (int)tribeType, ref num, OnClick, ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeData.type, SkinType.Default)), 1f), SetHeadIcon);

                    foreach (SkinType skinType in tribeData.skins)
                    {
                        string skinHeader = string.Format(Localization.Get(SkinTypeExtensions.GetSkinNameKey(), new Il2CppSystem.Object[] { }), Localization.Get(skinType.GetLocalizationKey(), new Il2CppSystem.Object[] { }));

                        Pickers.CreateChoiceButton(selectViewmodePopup, skinHeader,
                            (int)skinType + SKINS_NUM, ref num, OnClick, ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeData.type, skinType)), 1f), SetHeadIcon);
                    }
                }
                return picker;
            }
            return null;
        }
    }

    internal override void Update(GameLogicData gameLogicData)
    {
        base.Update(gameLogicData);
        void TribeSpriteHandle(SpriteHandle spriteHandleCallback)
        {
            button.SetFaceIcon(spriteHandleCallback.sprite);
        }
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
}