using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;
using Il2CppSystem.Linq.Expressions.Interpreter;
using Il2CppSystem;
using AsmResolver;

namespace PolytopiaMapManager.Level;
internal static class PickersManager
{
    #region CLIMATE PICKER

    internal const int SKINS_NUM = 1000;
    internal static UIRoundButton? climateButton = null;

    internal static void UpdateClimateButton(UIRoundButton button)
    {
        if (MapLoader.inMapMaker)
        {
            button.rectTransform.sizeDelta = new Vector2(75f, 75f);
            button.iconSpriteHandle.SetCompletion((SpriteHandleCallback)TribeSpriteHandle);
            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
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
            button.Outline.gameObject.SetActive(false);
        }
    }

    #endregion
    #region IMPROVEMENT PICKER

    internal static List<Polytopia.Data.ImprovementData.Type> allowedImprovements = new()
    {
        Polytopia.Data.ImprovementData.Type.None,
        Polytopia.Data.ImprovementData.Type.City,
        Polytopia.Data.ImprovementData.Type.Ruin
    };
    internal static UIRoundButton? improvementButton = null;

    internal static void UpdateImprovementButton(UIRoundButton button)
    {
        if (MapLoader.inMapMaker)
        {
            button.rectTransform.sizeDelta = new Vector2(75f, 75f);
            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
            SetIcon(button, GetSprite((int)Brush.chosenBuilding, SpriteData.ImprovementToString(Brush.chosenBuilding), gameLogicData));
            button.Outline.gameObject.SetActive(false);
            button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        }
    }

    #endregion
    #region MAP PICKER

    internal static UIRoundButton? mapChoiceButton = null;
    internal static List<string> visualMaps = new();

    internal static void UpdateMapChoiceButton(UIRoundButton button)
    {
        if (MapLoader.inMapMaker)
        {
            button.rectTransform.sizeDelta = new Vector2(75f, 75f);
            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
            // PickersHelper.SetIcon(button, PickersHelper.GetSprite((int)Brush.chosenBuilding, SpriteData.MapToString(Brush.chosenBuilding), gameLogicData));
            button.Outline.gameObject.SetActive(false);
            button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        }
    }

    #endregion
    #region RESOURCE PICKER

    internal static List<Polytopia.Data.ResourceData.Type> excludedResources = new()
    {
        Polytopia.Data.ResourceData.Type.Whale,
    };
    internal static UIRoundButton? resourceButton = null;

    internal static void UpdateResourceButton(UIRoundButton button)
    {
        if (MapLoader.inMapMaker)
        {
            button.rectTransform.sizeDelta = new Vector2(75f, 75f);
            button.Outline.gameObject.SetActive(false);
            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
            SetIcon(button, GetSprite((int)Brush.chosenResource, SpriteData.ResourceToString(Brush.chosenResource), gameLogicData));
            button.Outline.gameObject.SetActive(false);
            button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        }
    }

    #endregion
    #region TERRAIN PICKER

    internal static List<Polytopia.Data.TerrainData.Type> excludedTerrains = new()
    {
        Polytopia.Data.TerrainData.Type.Wetland,
        Polytopia.Data.TerrainData.Type.Mangrove
    };
    internal static UIRoundButton? terrainButton = null;

    internal static void UpdateTerrainButton(UIRoundButton button)
    {
        if (MapLoader.inMapMaker)
        {
            button.rectTransform.sizeDelta = new Vector2(75f, 75f);
            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
            SetIcon(button, GetSprite((int)Brush.chosenTerrain, SpriteData.TerrainToString(Brush.chosenTerrain), gameLogicData), 0.6f);
            button.Outline.gameObject.SetActive(false);
            button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        }
    }

    #endregion
    #region TILE EFFECT PICKER

    internal static List<TileData.EffectType> excludedTileEffects = new()
    {
        TileData.EffectType.Swamped,
        TileData.EffectType.Tentacle,
        TileData.EffectType.Foam,
    };
    internal static UIRoundButton? tileEffectButton = null;

    internal static void UpdateTileEffectButton(UIRoundButton button)
    {
        if (MapLoader.inMapMaker)
        {
            button.rectTransform.sizeDelta = new Vector2(75f, 75f);
            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
            SetIcon(button, GetSprite((int)Brush.chosenTileEffect, TileEffectToString(Brush.chosenTileEffect), gameLogicData), 0.6f);
            button.Outline.gameObject.SetActive(false);
            button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
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

    #endregion

    #region GENERAL
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

    private delegate UIRoundButton PickerShowAction(UIRoundButton? picker, ref float num,
                                            SelectViewmodePopup selectViewmodePopup, GameState gameState);

    private static UIRoundButton CreatePicker(UIRoundButton? picker, UIRoundButton referenceButton, Transform parent, PickerShowAction showAction, Vector3? indent = null, string headerKey = "")
    {
        picker = GameObject.Instantiate<UIRoundButton>(referenceButton, parent);
        if(indent != null)
        {
            picker.transform.position = picker.transform.position + (Vector3)indent;
        }
        picker.gameObject.SetActive(true);
        picker.OnClicked = (UIButtonBase.ButtonAction)ShowPopup;
        picker.text = string.Empty;

        void ShowPopup(int id, BaseEventData eventData)
        {
            SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
            selectViewmodePopup.Header = Localization.Get(headerKey, new Il2CppSystem.Object[] { });
            GameState gameState = GameManager.GameState;

            selectViewmodePopup.ClearButtons();
            selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
            float num = 0f;

            picker = showAction(picker, ref num, selectViewmodePopup, gameState);

            selectViewmodePopup.gridLayout.spacing = new Vector2(selectViewmodePopup.gridLayout.spacing.x, num + 10f);
            selectViewmodePopup.gridLayout.padding.bottom = Mathf.RoundToInt(num + 10f);
            selectViewmodePopup.gridBottomSpacer.minHeight = num + 10f;


            selectViewmodePopup.buttonData = new PopupBase.PopupButtonData[]
            {
                new PopupBase.PopupButtonData("buttons.ok", PopupBase.PopupButtonData.States.None, (UIButtonBase.ButtonAction)Exit, -1, true, null)
            };

            void Exit(int id, BaseEventData eventData)
            {
                selectViewmodePopup.Hide();
            }
            selectViewmodePopup.Show(picker!.rectTransform.position);
        }
        return picker;
    }

    internal static void CreateChoiceButton(
        SelectViewmodePopup viewmodePopup,
        string header,
        int id,
        ref float maxLabelHeight,
        System.Action<int> onClick,
        Color? backgroundColor = null,
        System.Action<UIRoundButton, int>? setIcon = null
    )
    {
        if(backgroundColor == null)
            ColorUtil.SetAlphaOnColor(Color.white, 0.5f);

        UIRoundButton button = GameObject.Instantiate(
            viewmodePopup.buttonPrefab,
            viewmodePopup.gridLayout.transform
        );

        button.id = id;
        button.rectTransform.sizeDelta = new Vector2(56f, 56f);
        button.Outline.gameObject.SetActive(false);
        button.BG.color = (Color)backgroundColor!;

        button.text = char.ToUpper(header[0]) + header.Substring(1);
        button.SetIconColor(Color.white);
        button.ButtonEnabled = true;

        button.OnClicked = (UIButtonBase.ButtonAction)((btnId, eventData) =>
        {
            onClick(btnId);
        });

        if(setIcon != null)
            setIcon(button, id);

        if (button.Label.PreferedValues.y > maxLabelHeight)
        {
            maxLabelHeight = button.Label.PreferedValues.y;
        }

        viewmodePopup.buttons.Add(button);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
    private static void HudScreen_OnMatchStart(HudScreen __instance)
    {
        if (MapLoader.inMapMaker)
        {
            UIRoundButton referenceButton = __instance.replayInterface.viewmodeSelectButton;
            // CLIMATE PICKER
            climateButton = CreatePicker(climateButton, referenceButton, __instance.transform, CreateClimateButtons, headerKey: "mapmaker.choose.climate");
            UIRoundButton? CreateClimateButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
            {
                if(picker != null)
                {
                    CreateChoiceButton(selectViewmodePopup, "none",
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
                        UpdateClimateButton(climateButton!);
                        UpdateImprovementButton(improvementButton!);
                        UpdateResourceButton(resourceButton!);
                        UpdateTerrainButton(terrainButton!);
                        UpdateTileEffectButton(tileEffectButton!);
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

                        CreateChoiceButton(selectViewmodePopup, tribeName,
                            (int)tribeType, ref num, OnClick, ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeData.type, SkinType.Default)), 1f), SetHeadIcon);

                        foreach (SkinType skinType in tribeData.skins)
                        {
                            string skinHeader = string.Format(Localization.Get(SkinTypeExtensions.GetSkinNameKey(), new Il2CppSystem.Object[] { }), Localization.Get(skinType.GetLocalizationKey(), new Il2CppSystem.Object[] { }));

                            CreateChoiceButton(selectViewmodePopup, skinHeader,
                                (int)skinType + SKINS_NUM, ref num, OnClick, ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(gameLogicData.GetTribeColor(tribeData.type, skinType)), 1f), SetHeadIcon);
                        }
                    }
                    return picker;
                }
                return null;
            }
            // CLIMATE PICKER END

            // MAP PICKER
            mapChoiceButton = CreatePicker(mapChoiceButton, referenceButton, __instance.transform, CreateMapButtons, new Vector3(0, -90, 0), "mapmaker.choose.map");
            UIRoundButton? CreateMapButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
            {
                if(picker != null)
                {
                    string[] maps = Directory.GetFiles(MapLoader.MAPS_PATH, "*.json");
                    visualMaps = new();
                    if (maps.Length > 0)
                    {
                        visualMaps = maps.Select(map => Path.GetFileNameWithoutExtension(map)).ToList();
                        num++;
                    }
                    for (int index = 0; index < visualMaps.Count(); index++)
                    {
                        string name = visualMaps[index];
                        CreateChoiceButton(selectViewmodePopup, name,
                                index, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f));

                        void OnClick(int id)
                        {
                            MapMaker.MapName = visualMaps[id];
                            MapLoader.chosenMap = MapLoader.LoadMapFile(MapMaker.MapName);
                            MapLoader.LoadMapInState(ref gameState);
                            GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
                            MapLoader.RevealMap(GameManager.LocalPlayer.Id);
                            UpdateMapChoiceButton(mapChoiceButton!);
                        }
                        // CreateMapChoiceButton(selectViewmodePopup, gameState.GameLogicData, name, index, ref num);
                    }
                    return picker;
                }
                return null;
            }
            // MAP PICKER END

            // RESOURCE PICKER
            resourceButton = CreatePicker(resourceButton, referenceButton, __instance.transform, CreateResourceButtons, new Vector3(90, 0, 0), "mapmaker.choose.resource");
            UIRoundButton? CreateResourceButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
            {
                if(picker != null)
                {
                    foreach (Polytopia.Data.ResourceData resourceData in gameState.GameLogicData.AllResourceData.Values)
                    {
                        Polytopia.Data.ResourceData.Type resourceType = resourceData.type;
                        if(excludedResources.Contains(resourceType))
                            continue;
                        string resourceName = Localization.Get(resourceData.displayName);
                        CreateChoiceButton(selectViewmodePopup, resourceName,
                                (int)resourceType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetResourceIcon);

                        void OnClick(int id)
                        {
                            Brush.chosenResource = (Polytopia.Data.ResourceData.Type)id;
                            UpdateResourceButton(resourceButton!);
                        }

                        void SetResourceIcon(UIRoundButton button, int type)
                        {
                            SetIcon(button, GetSprite(type, SpriteData.ResourceToString((ResourceData.Type)type), gameState.GameLogicData), 0.6f);
                        }
                    }
                    return picker;
                }
                return null;
            }
            // RESOURCE PICKER END

            // TERRAIN PICKER
            terrainButton = CreatePicker(terrainButton, referenceButton, __instance.transform, CreateTerrainButtons, new Vector3(180, 0, 0), "mapmaker.choose.terrain");
            UIRoundButton? CreateTerrainButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
            {
                if(picker != null)
                {
                    foreach (Polytopia.Data.TerrainData terrainData in gameState.GameLogicData.AllTerrainData.Values)
                    {
                        Polytopia.Data.TerrainData.Type terrainType = terrainData.type;
                        if(excludedTerrains.Contains(terrainType))
                            continue;
                        string terrainName = Localization.Get(terrainType.GetDisplayName());
                        CreateChoiceButton(selectViewmodePopup, terrainName,
                                (int)terrainType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetTerrainIcon);

                        void OnClick(int id)
                        {
                            Brush.chosenTerrain = (Polytopia.Data.TerrainData.Type)id;
                            UpdateTerrainButton(terrainButton!);
                        }

                        void SetTerrainIcon(UIRoundButton button, int type)
                        {
                            SetIcon(button, GetSprite(type, SpriteData.TerrainToString((Polytopia.Data.TerrainData.Type)type), gameState.GameLogicData), 0.6f);
                        }
                    }
                    return picker;
                }
                return null;
            }
            // TERRAIN PICKER END

            // TILE EFFECT PICKER
            tileEffectButton = CreatePicker(tileEffectButton, referenceButton, __instance.transform, CreateTileEffectButtons, new Vector3(270, 0, 0), "mapmaker.choose.tileeffect");
            UIRoundButton? CreateTileEffectButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
            {
                if(picker != null)
                {
                    foreach (TileData.EffectType tileEffect in System.Enum.GetValues(typeof(TileData.EffectType)))
                    {
                        if(excludedTileEffects.Contains(tileEffect))
                            continue;
                        EnumCache<TileData.EffectType>.GetName(tileEffect);
                        string tileEffectName = Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(tileEffect)}");

                        CreateChoiceButton(selectViewmodePopup, tileEffectName,
                                (int)tileEffect, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetTileEffectIcon);

                        void OnClick(int id)
                        {
                            Brush.chosenTileEffect = (TileData.EffectType)id;
                            UpdateTileEffectButton(tileEffectButton!);
                        }

                        void SetTileEffectIcon(UIRoundButton button, int type)
                        {
                            SetIcon(button, GetSprite(type, TileEffectToString((TileData.EffectType)type), gameState.GameLogicData), 0.6f);
                        }
                    }
                    return picker;
                }
                return null;
            }
            // TILE EFFECT PICKER END

            // IMPROVEMENT PICKER
            improvementButton = CreatePicker(improvementButton, referenceButton, __instance.transform, CreateImprovementButtons, new Vector3(360, 0, 0), "mapmaker.choose.improvement");
            UIRoundButton? CreateImprovementButtons(UIRoundButton? picker, ref float num, SelectViewmodePopup selectViewmodePopup, GameState gameState)
            {
                if(picker != null)
                {
                    foreach (Polytopia.Data.ImprovementData improvementData in gameState.GameLogicData.AllImprovementData.Values)
                    {
                        Polytopia.Data.ImprovementData.Type improvementType = improvementData.type;
                        if(!allowedImprovements.Contains(improvementType))
                            continue;
                        string improvementName = Localization.Get(improvementData.displayName);
                        CreateChoiceButton(selectViewmodePopup, improvementName,
                                (int)improvementType, ref num, OnClick, ColorUtil.SetAlphaOnColor(Color.white, 0.6f), SetImprovementIcon);

                        void OnClick(int id)
                        {
                            Brush.chosenBuilding = (ImprovementData.Type)id;
                            UpdateImprovementButton(improvementButton!);
                        }

                        void SetImprovementIcon(UIRoundButton button, int type)
                        {
                            SetIcon(button, GetSprite(type, SpriteData.ImprovementToString(improvementType), gameState.GameLogicData), 0.6f);
                        }
                    }
                    return picker;
                }
                return null;
            }
            // IMPROVEMENT PICKER END

            UpdateClimateButton(climateButton!);
            UpdateMapChoiceButton(tileEffectButton!);
            UpdateResourceButton(resourceButton!);
            UpdateTerrainButton(terrainButton!);
            UpdateTileEffectButton(tileEffectButton!);
            UpdateImprovementButton(improvementButton!);
        }
    }

    #endregion
}