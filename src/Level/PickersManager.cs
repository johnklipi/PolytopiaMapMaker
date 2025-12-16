using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;

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

    internal static void CreateClimateChoiceButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, int color, ref float num)
    {
        UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
        playerButton.id = (int)type;
        playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
        playerButton.Outline.gameObject.SetActive(false);
        playerButton.BG.color = ColorUtil.SetAlphaOnColor(ColorUtil.ColorFromInt(color), 1f);
        playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
        playerButton.SetIconColor(Color.white);
        playerButton.ButtonEnabled = true;
        playerButton.OnClicked = (UIButtonBase.ButtonAction)OnClimateButtonClicked;
        void OnClimateButtonClicked(int id, BaseEventData eventData)
        {
            int type = id;
            Main.modLogger!.LogInfo("Clicked i guess");
            Main.modLogger!.LogInfo(id);
            if (type >= SKINS_NUM)
            {
                type -= SKINS_NUM;
                SkinType skinType = (SkinType)type;
                Brush.chosenClimate = MapLoader.GetTribeClimateFromSkin(skinType, gameState.GameLogicData);
                Brush.chosenSkinType = skinType;
            }
            else
            {
                if((TribeType)type != TribeType.None)
                {
                    Brush.chosenClimate = MapLoader.GetTribeClimateFromType((TribeType)type, gameState.GameLogicData);
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
            // viewmodePopup.Hide();
        }
        playerButton.iconSpriteHandle.SetCompletion((SpriteHandleCallback)TribeSpriteHandle);
        void TribeSpriteHandle(SpriteHandle spriteHandleCallback)
        {
            playerButton.SetFaceIcon(spriteHandleCallback.sprite);
        }
        if((TribeType)type != TribeType.None)
        {
            playerButton.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(spriteName));
        }
        else
        {
            playerButton.iconSpriteHandle.Request(SpriteData.GetHeadSpriteAddress(SpriteData.SpecialFaceIcon.neutral));
        }
        if (playerButton.Label.PreferedValues.y > num)
        {
            num = playerButton.Label.PreferedValues.y;
        }
        viewmodePopup.buttons.Add(playerButton);
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

    internal static void CreateImprovementChoiceButton(SelectViewmodePopup viewmodePopup, GameLogicData gameLogicData, string header, string spriteName, int type, ref float num)
    {
        UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
        playerButton.id = type;
        playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
        playerButton.Outline.gameObject.SetActive(false);
        playerButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
        playerButton.SetIconColor(Color.white);
        playerButton.ButtonEnabled = true;
        playerButton.OnClicked = (UIButtonBase.ButtonAction)OnImprovementButtonClicked;
        void OnImprovementButtonClicked(int id, BaseEventData eventData)
        {
            int type = id;
            Main.modLogger!.LogInfo("Clicked i guess");
            Main.modLogger!.LogInfo(id);
            Brush.chosenBuilding = (Polytopia.Data.ImprovementData.Type)type;
            UpdateImprovementButton(improvementButton!);
        }
        SetIcon(playerButton, GetSprite(type, spriteName, gameLogicData));
        if (playerButton.Label.PreferedValues.y > num)
        {
            num = playerButton.Label.PreferedValues.y;
        }
        viewmodePopup.buttons.Add(playerButton);
    }

    #endregion
    #region MAP PICKER

    internal static UIRoundButton? mapChoiceButton = null;
    internal static List<string> visualMaps = new();

    internal static void UpdatemapChoiceButton(UIRoundButton button)
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

    internal static void CreateMapChoiceButton(SelectViewmodePopup viewmodePopup, GameLogicData gameLogicData, string header, int idx, ref float num)
    {
        UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
        playerButton.id = idx;
        playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
        playerButton.Outline.gameObject.SetActive(false);
        playerButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
        playerButton.SetIconColor(Color.white);
        playerButton.ButtonEnabled = true;
        playerButton.OnClicked = (UIButtonBase.ButtonAction)OnmapChoiceButtonClicked;
        void OnmapChoiceButtonClicked(int id, BaseEventData eventData)
        {
            int type = id;
            Main.modLogger!.LogInfo("Clicked i guess");
            Main.modLogger!.LogInfo(id);
            GameState gameState = GameManager.GameState;
            MapMaker.MapName = visualMaps[id];
            MapLoader.chosenMap = MapLoader.LoadMapFile(MapMaker.MapName);
            MapLoader.LoadMapInState(ref gameState);
            GameManager.Client.UpdateGameState(gameState, PolytopiaBackendBase.Game.StateUpdateReason.Unknown);
            MapLoader.RevealMap(GameManager.LocalPlayer.Id);
            // Brush.chosenBuilding = (Polytopia.Data.MapData.Type)type;
            UpdatemapChoiceButton(mapChoiceButton!);
        }
        // PickersHelper.SetIcon(playerButton, PickersHelper.GetSprite(idx, "", gameLogicData));
        if (playerButton.Label.PreferedValues.y > num)
        {
            num = playerButton.Label.PreferedValues.y;
        }
        viewmodePopup.buttons.Add(playerButton);
    }

    #endregion
    #region RESOURCE PICKER¨


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

    internal static void CreateResourceChoiceButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, ref float num)
    {
        UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
        playerButton.id = (int)type;
        playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
        playerButton.Outline.gameObject.SetActive(false);
        playerButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
        playerButton.SetIconColor(Color.white);
        playerButton.ButtonEnabled = true;
        playerButton.OnClicked = (UIButtonBase.ButtonAction)OnResourceButtonClicked;
        void OnResourceButtonClicked(int id, BaseEventData eventData)
        {
            int type = id;
            Main.modLogger!.LogInfo("Clicked i guess");
            Main.modLogger!.LogInfo(id);
            Brush.chosenResource = (Polytopia.Data.ResourceData.Type)type;
            UpdateResourceButton(resourceButton!);
            // viewmodePopup.Hide();
        }

        GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
        SetIcon(playerButton, GetSprite(type, spriteName, gameLogicData));
        if (playerButton.Label.PreferedValues.y > num)
        {
            num = playerButton.Label.PreferedValues.y;
        }
        viewmodePopup.buttons.Add(playerButton);
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

    internal static void CreateTerrainChoiceButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, ref float num)
    {
        UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
        playerButton.id = (int)type;
        playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
        playerButton.Outline.gameObject.SetActive(false);
        playerButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
        playerButton.SetIconColor(Color.white);
        playerButton.ButtonEnabled = true;
        playerButton.OnClicked = (UIButtonBase.ButtonAction)OnTerrainButtonClicked;
        void OnTerrainButtonClicked(int id, BaseEventData eventData)
        {
            int type = id;
            Main.modLogger!.LogInfo("Clicked i guess");
            Main.modLogger!.LogInfo(id);
            Brush.chosenTerrain = (Polytopia.Data.TerrainData.Type)type;
            UpdateTerrainButton(terrainButton!);
            // viewmodePopup.Hide();
        }

        GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
        SetIcon(playerButton, GetSprite(type, spriteName, gameLogicData), 0.6f);

        if (playerButton.Label.PreferedValues.y > num)
        {
            num = playerButton.Label.PreferedValues.y;
        }
        viewmodePopup.buttons.Add(playerButton);
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

    internal static void CreateTileEffectChoiceButton(SelectViewmodePopup viewmodePopup, GameState gameState, string header, string spriteName, int type, ref float num)
    {
        UIRoundButton playerButton = GameObject.Instantiate<UIRoundButton>(viewmodePopup.buttonPrefab, viewmodePopup.gridLayout.transform);
        playerButton.id = (int)type;
        playerButton.rectTransform.sizeDelta = new Vector2(56f, 56f);
        playerButton.Outline.gameObject.SetActive(false);
        playerButton.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
        playerButton.text = header[0].ToString().ToUpper() + header.Substring(1);
        playerButton.SetIconColor(Color.white);
        playerButton.ButtonEnabled = true;
        playerButton.OnClicked = (UIButtonBase.ButtonAction)OnTileEffectButtonClicked;
        void OnTileEffectButtonClicked(int id, BaseEventData eventData)
        {
            int type = id;
            Main.modLogger!.LogInfo("Clicked i guess");
            Main.modLogger!.LogInfo(id);
            Brush.chosenTileEffect = (TileData.EffectType)type;
            UpdateTileEffectButton(tileEffectButton!);
            // viewmodePopup.Hide();
        }

        GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
        SetIcon(playerButton, GetSprite(type, spriteName, gameLogicData), 0.6f);
        if (playerButton.Label.PreferedValues.y > num)
        {
            num = playerButton.Label.PreferedValues.y;
        }
        viewmodePopup.buttons.Add(playerButton);
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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HudScreen), nameof(HudScreen.OnMatchStart))]
    private static void HudScreen_OnMatchStart(HudScreen __instance)
    {
        if (MapLoader.inMapMaker)
        {
            // CLIMATE PICKER
            climateButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
            climateButton.gameObject.SetActive(true);
            climateButton.OnClicked = (UIButtonBase.ButtonAction)ShowClimatePopup;
            climateButton.text = string.Empty;
            UpdateClimateButton(climateButton);

            void ShowClimatePopup(int id, BaseEventData eventData)
            {
                SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                // __instance.selectViewmodePopup.Header = Localization.Get("replay.viewmode.header", new Il2CppSystem.Object[] { });
                selectViewmodePopup.Header = Localization.Get("mapmaker.choose.climate", new Il2CppSystem.Object[] { });
                GameState gameState = GameManager.GameState;

                // Set Data
                selectViewmodePopup.ClearButtons();
                selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                float num = 0f;
                CreateClimateChoiceButton(selectViewmodePopup, gameState, "none", EnumCache<TribeType>.GetName(TribeType.None), (int)TribeType.None, 16777215, ref num);
                GameLogicData gameLogicData = gameState.GameLogicData;
                List<TribeData> tribes = gameLogicData.GetTribes(TribeData.CategoryEnum.Human).ToArray().ToList().Concat(gameLogicData.GetTribes(TribeData.CategoryEnum.Special).ToArray().ToList()).ToList();
                foreach (TribeData tribeData in tribes)
                {
                    TribeType tribeType = tribeData.type;
                    string tribeName = Localization.Get(tribeData.displayName);
                    CreateClimateChoiceButton(selectViewmodePopup, gameState, tribeName, EnumCache<TribeType>.GetName(tribeType), (int)tribeType, gameLogicData.GetTribeColor(tribeData.type, SkinType.Default), ref num);
                    foreach (SkinType skinType in tribeData.skins)
                    {
                        // gameLogicData.TryGetData(skinType, out SkinData data);
                        string skinHeader = string.Format(Localization.Get(SkinTypeExtensions.GetSkinNameKey(), new Il2CppSystem.Object[] { }), Localization.Get(skinType.GetLocalizationKey(), new Il2CppSystem.Object[] { }));
                        CreateClimateChoiceButton(selectViewmodePopup, gameState, skinHeader, EnumCache<SkinType>.GetName(skinType), (int)skinType + 1000, gameLogicData.GetTribeColor(tribeData.type, skinType), ref num);
                    }
                }
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
                selectViewmodePopup.Show(climateButton!.rectTransform.position);
            }
            // CLIMATE PICKER END

            // IMPROVEMENT PICKER
            improvementButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
            improvementButton.transform.position = improvementButton.transform.position + new Vector3(360, 0, 0);
            improvementButton.gameObject.SetActive(true);
            improvementButton.OnClicked = (UIButtonBase.ButtonAction)ShowImprovementPopup;
            improvementButton.text = string.Empty;
            UpdateImprovementButton(improvementButton);

            void ShowImprovementPopup(int id, BaseEventData eventData)
            {
                SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                selectViewmodePopup.Header = Localization.Get("mapmaker.choose.improvement", new Il2CppSystem.Object[] { });
                GameState gameState = GameManager.GameState;
                // Set Data
                selectViewmodePopup.ClearButtons();
                selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                float num = 0f;
                foreach (Polytopia.Data.ImprovementData improvementData in gameState.GameLogicData.AllImprovementData.Values)
                {
                    Polytopia.Data.ImprovementData.Type improvementType = improvementData.type;
                    if(!allowedImprovements.Contains(improvementType))
                        continue;
                    string improvementName = Localization.Get(improvementData.displayName);
                    CreateImprovementChoiceButton(selectViewmodePopup, gameState.GameLogicData, improvementName, SpriteData.ImprovementToString(improvementType), (int)improvementType, ref num);
                }
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
                selectViewmodePopup.Show(improvementButton!.rectTransform.position);
                // IMPROVEMENT PICKER END

                // MAP PICKER
                mapChoiceButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
                mapChoiceButton.transform.position = mapChoiceButton.transform.position - new Vector3(0, 90, 0);
                mapChoiceButton.gameObject.SetActive(true);
                mapChoiceButton.OnClicked = (UIButtonBase.ButtonAction)ShowMapPopup;
                mapChoiceButton.text = string.Empty;

                UpdatemapChoiceButton(mapChoiceButton);

                void ShowMapPopup(int id, BaseEventData eventData)
                {
                    SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                    selectViewmodePopup.Header = Localization.Get("mapmaker.choose.map", new Il2CppSystem.Object[] { });
                    GameState gameState = GameManager.GameState;
                    // Set Data
                    selectViewmodePopup.ClearButtons();
                    selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                    float num = 0f;
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
                        CreateMapChoiceButton(selectViewmodePopup, gameState.GameLogicData, name, index, ref num);
                    }
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
                    selectViewmodePopup.Show(mapChoiceButton!.rectTransform.position);
                }
                // MAP PICKER END

                // RESOURCE PICKER
                resourceButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
                resourceButton.transform.position = resourceButton.transform.position + new Vector3(90, 0, 0);
                resourceButton.gameObject.SetActive(true);
                resourceButton.OnClicked = (UIButtonBase.ButtonAction)ShowResourcePopup;
                resourceButton.text = string.Empty;
                UpdateResourceButton(resourceButton);

                void ShowResourcePopup(int id, BaseEventData eventData)
                {
                    SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                    // __instance.selectViewmodePopup.Header = Localization.Get("replay.viewmode.header", new Il2CppSystem.Object[] { });
                    selectViewmodePopup.Header = Localization.Get("mapmaker.choose.resource", new Il2CppSystem.Object[] { });
                    GameState gameState = GameManager.GameState;
                    // Set Data
                    selectViewmodePopup.ClearButtons();
                    selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                    float num = 0f;
                    foreach (Polytopia.Data.ResourceData resourceData in gameState.GameLogicData.AllResourceData.Values)
                    {
                        Polytopia.Data.ResourceData.Type resourceType = resourceData.type;
                        if(excludedResources.Contains(resourceType))
                            continue;
                        string resourceName = Localization.Get(resourceData.displayName);
                        CreateResourceChoiceButton(selectViewmodePopup, gameState, resourceName, SpriteData.ResourceToString(resourceType), (int)resourceType, ref num);
                    }
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
                    selectViewmodePopup.Show(resourceButton!.rectTransform.position);
                }
                // RESOURCE PICKER END

                // TERRAIN PICKER
                terrainButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
                terrainButton.transform.position = terrainButton.transform.position + new Vector3(180, 0, 0);
                terrainButton.gameObject.SetActive(true);
                terrainButton.OnClicked = (UIButtonBase.ButtonAction)ShowTerrainPopup;
                terrainButton.text = string.Empty;
                UpdateTerrainButton(terrainButton);

                void ShowTerrainPopup(int id, BaseEventData eventData)
                {
                    SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                    // __instance.selectViewmodePopup.Header = Localization.Get("replay.viewmode.header", new Il2CppSystem.Object[] { });
                    selectViewmodePopup.Header = Localization.Get("mapmaker.choose.terrain", new Il2CppSystem.Object[] { });
                    GameState gameState = GameManager.GameState;
                    // Set Data
                    selectViewmodePopup.ClearButtons();
                    selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                    float num = 0f;
                    foreach (Polytopia.Data.TerrainData terrainData in gameState.GameLogicData.AllTerrainData.Values)
                    {
                        Polytopia.Data.TerrainData.Type terrainType = terrainData.type;
                        if(excludedTerrains.Contains(terrainType))
                            continue;
                        string terrainName = Localization.Get(terrainType.GetDisplayName());
                        CreateTerrainChoiceButton(selectViewmodePopup, gameState, terrainName, SpriteData.TerrainToString(terrainType), (int)terrainType, ref num);
                    }
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
                    selectViewmodePopup.Show(terrainButton!.rectTransform.position);
                }
                // TERRAIN PICKER END

                // TILE EFFECT PICKER
                tileEffectButton = GameObject.Instantiate<UIRoundButton>(__instance.replayInterface.viewmodeSelectButton, __instance.transform);
                tileEffectButton.transform.position = tileEffectButton.transform.position + new Vector3(270, 0, 0);
                tileEffectButton.gameObject.SetActive(true);
                tileEffectButton.OnClicked = (UIButtonBase.ButtonAction)ShowTileEffectPopup;
                tileEffectButton.text = string.Empty;
                UpdateTileEffectButton(tileEffectButton);

                void ShowTileEffectPopup(int id, BaseEventData eventData)
                {
                    SelectViewmodePopup selectViewmodePopup = PopupManager.GetSelectViewmodePopup();
                    // __instance.selectViewmodePopup.Header = Localization.Get("replay.viewmode.header", new Il2CppSystem.Object[] { });
                    selectViewmodePopup.Header = Localization.Get("mapmaker.choose.tileeffect", new Il2CppSystem.Object[] { });
                    GameState gameState = GameManager.GameState;
                    // Set Data
                    selectViewmodePopup.ClearButtons();
                    selectViewmodePopup.buttons = new Il2CppSystem.Collections.Generic.List<UIRoundButton>();
                    float num = 0f;
                    foreach (TileData.EffectType tileEffect in Enum.GetValues(typeof(TileData.EffectType)))
                    {
                        if(excludedTileEffects.Contains(tileEffect))
                            continue;
                        EnumCache<TileData.EffectType>.GetName(tileEffect);
                        string tileEffectName = Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(tileEffect)}");
                        CreateTileEffectChoiceButton(selectViewmodePopup, gameState, tileEffectName, TileEffectToString(tileEffect), (int)tileEffect, ref num);
                    }
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
                    selectViewmodePopup.Show(tileEffectButton!.rectTransform.position);
                }
                // TILE EFFECT PICKER END
            }
        }
    }

    #endregion
}