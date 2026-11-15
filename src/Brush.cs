using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Common;
using PolytopiaMapManager.UI;
using PolytopiaMapManager.UI.Picker;
using UnityEngine;

namespace PolytopiaMapManager;

public static class Brush
{
    private static float nextAllowedTimeForPopup = 0f;
    internal static bool PaintMode = false;

    private static bool IsTouchPainting()
    {
        return Main.isActive && PaintMode && InputManager.GetTouchCount() == 1;
    }

    private static bool IsPainting()
    {
        return Input.GetKey(KeyCode.Mouse1) || (IsTouchPainting() && InputManager.HasActiveTouch());
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Tile), nameof(Tile.OnHoverStart))]
    private static void Tile_OnHoverStart(Tile __instance)
    {
        if(!Main.isActive || !GameManager.Instance.isLevelLoaded || !IsPainting())
            return;

        GameState gameState = GameManager.GameState;
        TileData tileData = __instance.data;
        string before = GetTileSignature(tileData);

        HandleTerrain(tileData, (Polytopia.Data.TerrainData.Type)Editor.terrainPicker.chosenValue);
        HandleResource(gameState, tileData, (ResourceData.Type)Editor.resourcePicker.chosenValue);
        HandleTileEffect(tileData, (TileData.EffectType)Editor.tileEffectPicker.chosenValue);
        HandleImprovement(gameState, tileData, (ImprovementData.Type)Editor.improvementPicker.chosenValue);
        HandleBiome(tileData, (TribeType)Editor.biomePicker.chosenValue, Editor.biomePicker.chosenSkinType);

        if(GetTileSignature(tileData) == before)
            return;

        strokeTiles.Add((tileData.coordinates.x, tileData.coordinates.y));
        Main.UnsavedChanges = true;
        __instance.Render();
    }

    private static readonly HashSet<(int x, int y)> strokeTiles = new();

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.Update))]
    private static void GameManager_Update()
    {
        if(strokeTiles.Count > 0 && !IsPainting())
            CommitStroke();
    }

    internal static void CommitStroke()
    {
        if(strokeTiles.Count == 0)
            return;

        if(Main.isActive && GameManager.GameState != null)
            History.RecordTiles(GameManager.GameState, strokeTiles.Select(tile => new WorldCoordinates(tile.x, tile.y)).ToList());
        strokeTiles.Clear();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(CameraController), nameof(CameraController.UpdateInput))]
    private static bool CameraController_UpdateInput(CameraController __instance)
    {
        if(!IsTouchPainting())
            return true;

        __instance.isDragging = false;
        __instance.touchCount = 1;
        __instance.NullVelocity();
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ClientInteraction), nameof(ClientInteraction.OnRelease))]
    private static bool ClientInteraction_OnRelease()
    {
        return !(Main.isActive && PaintMode);
    }

    private static string GetTileSignature(TileData tileData)
    {
        System.Text.StringBuilder sb = new();
        sb.Append((int)tileData.terrain).Append('|')
            .Append(tileData.resource == null ? -1 : (int)tileData.resource.type).Append('|')
            .Append(tileData.improvement == null ? -1 : (int)tileData.improvement.type).Append('|')
            .Append((int)tileData.climate).Append('|')
            .Append((int)tileData.Skin);
        foreach (TileData.EffectType effect in tileData.effects)
            sb.Append('|').Append((int)effect);
        return sb.ToString();
    }

    internal static void HandleTerrain(TileData tileData, Polytopia.Data.TerrainData.Type terrain)
    {
        if(terrain != Polytopia.Data.TerrainData.Type.None)
            tileData.terrain = terrain;
    }

    internal static void HandleResource(GameState gameState, TileData tileData, ResourceData.Type resource)
    {
        if(resource == ResourceData.Type.None)
            return;

        if(resource == (ResourceData.Type)PickerBase.DESTROY_OPTION_ID)
        {
            tileData.resource = null;
            return;
        }

        if(!gameState.GameLogicData.TryGetData(resource, out ResourceData data))
        {
            ShowNotification(Localization.Get("mapmaker.failed.retrieve", new Il2CppSystem.Object[] { tileData.resource.type.ToString() }));
            return;
        }

        if(!data.resourceTerrainRequirements.Contains(tileData.terrain))
        {
            ShowNotification(
                Localization.Get(
                    "mapmaker.failed.creation",
                    new Il2CppSystem.Object[]
                    {
                        Localization.Get(data.displayName, new Il2CppSystem.Object[] {} ),
                        Localization.Get(tileData.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )
                    }
                )
            );
            return;
        }

        tileData.resource = new ResourceState
        {
            type = resource
        };
    }

    internal static void HandleTileEffect(TileData tileData, TileData.EffectType effectType)
    {
        if(effectType == TileData.EffectType.None)
            return;

        tileData.effects.Clear();

        if(effectType == TileData.EffectType.Flooded)
        {
            if(tileData.isFloodable())
            {
                tileData.AddEffect(effectType);
                if(Editor.biomePicker.chosenSkinType == SkinType.Swamp)
                    tileData.AddEffect(TileData.EffectType.Swamped);
            }
            else
            {
                ShowNotification(
                    Localization.Get(
                        "mapmaker.failed.creation",
                        new Il2CppSystem.Object[]
                        {
                            Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(effectType)}"),
                            Localization.Get(tileData.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )
                        }
                    )
                );
            }
        }
        else if(effectType == TileData.EffectType.Algae)
        {
            if(tileData.IsWater)
            {
                tileData.AddEffect(effectType);
                if(Editor.biomePicker.chosenSkinType == SkinType.Cute)
                    tileData.AddEffect(TileData.EffectType.Foam);
            }
            else
            {
                ShowNotification(
                    Localization.Get(
                        "mapmaker.failed.creation",
                        new Il2CppSystem.Object[]
                        {
                            Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(effectType)}"),
                            Localization.Get(tileData.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )
                        }
                    )
                );
            }
        }
    }

    internal static void HandleImprovement(GameState gameState, TileData tileData, ImprovementData.Type improvement)
    {
        if(improvement == ImprovementData.Type.None || tileData.HasImprovement(ImprovementData.Type.LightHouse))
            return;

        if(improvement == (ImprovementData.Type)PickerBase.DESTROY_OPTION_ID)
        {
            tileData.improvement = null;
            Data.Capital? capital = Loader.GetCapital(tileData.coordinates, Main.currCapitals);

            if(capital != null)
                Main.currCapitals.Remove(capital);  
            return;
        }

        if (!gameState.GameLogicData.TryGetData(improvement, out ImprovementData improvementData))
        {
            ShowNotification(Localization.Get("mapmaker.failed.retrieve", new Il2CppSystem.Object[] { tileData.improvement.type.ToString()}));
            return;
        }

        if (!gameState.TryGetPlayer(gameState.CurrentPlayer, out PlayerState playerState))
        {
            ShowNotification(Localization.Get("mapmaker.failed.retrieve", new Il2CppSystem.Object[] { gameState.CurrentPlayer.ToString()}));
            return;
        }

        if(!gameState.GameLogicData.MeetsRequirement(tileData, improvementData, playerState, gameState))
        {
            ShowNotification(
                Localization.Get(
                    "mapmaker.failed.creation",
                    new Il2CppSystem.Object[]
                    {
                        Localization.Get(improvementData.displayName, new Il2CppSystem.Object[] {} ),
                        Localization.Get(tileData.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )
                    }
                )
            );
            return;
        }

        ImprovementState improvementState = new ImprovementState
        {
            type = improvement,
            borderSize = (ushort)improvementData.borderSize,
            level = 0,
            xp = 0,
            production = 1,
            founded = 0,
            baseScore = 0,
            founder = gameState.CurrentPlayer
        };
        tileData.improvement = improvementState;
    }

    internal static void HandleBiome(TileData tileData, TribeType climate, SkinType skinType)
    {
        if(Editor.biomePicker.chosenValue == 0)
            return;

        tileData.climate = climate;
        tileData.Skin = skinType;
    }

    private static void ShowNotification(string message, string? title = null)
    {
        if(Time.time < nextAllowedTimeForPopup)
            return;

        NotificationManager.Notify(message, title);
        nextAllowedTimeForPopup = Time.time + 1f;
    }
}