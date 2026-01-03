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

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Tile), nameof(Tile.OnHoverStart))]
    private static void Tile_OnHoverStart(Tile __instance)
    {
        if(!Main.isActive || !Input.GetKey(KeyCode.Mouse1) || !GameManager.Instance.isLevelLoaded)
            return;

        GameState gameState = GameManager.GameState;

        HandleTerrain(__instance.data, (Polytopia.Data.TerrainData.Type)Editor.terrainPicker.chosenValue);
        HandleResource(gameState, __instance.data, (ResourceData.Type)Editor.resourcePicker.chosenValue);
        HandleTileEffect(__instance.data, (TileData.EffectType)Editor.tileEffectPicker.chosenValue);
        HandleImprovement(gameState, __instance.data, (ImprovementData.Type)Editor.improvementPicker.chosenValue);
        HandleBiome(__instance.data, Editor.biomePicker.chosenValue, Editor.biomePicker.chosenSkinType);

        __instance.Render();
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

        if(!gameState.GameLogicData.TryGetData(resource, out ResourceData data) && Time.time >= nextAllowedTimeForPopup)
        {
            NotificationManager.Notify(Localization.Get("mapmaker.failed.retrieve", new Il2CppSystem.Object[] { tileData.resource}));
            nextAllowedTimeForPopup = Time.time + 1f;
            return;
        }

        if(!data.resourceTerrainRequirements.Contains(tileData.terrain) && Time.time >= nextAllowedTimeForPopup)
        {
            NotificationManager.Notify(
                Localization.Get(
                    "mapmaker.failed.creation",
                    new Il2CppSystem.Object[]
                    {
                        Localization.Get(data.displayName, new Il2CppSystem.Object[] {} ),
                        Localization.Get(tileData.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )
                    }
                )
            );
            nextAllowedTimeForPopup = Time.time + 1f;
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
            else if(Time.time >= nextAllowedTimeForPopup)
            {
                NotificationManager.Notify(
                    Localization.Get(
                        "mapmaker.failed.creation",
                        new Il2CppSystem.Object[]
                        {
                            Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(effectType)}"),
                            Localization.Get(tileData.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )
                        }
                    )
                );
                nextAllowedTimeForPopup = Time.time + 1f;
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
            else if(Time.time >= nextAllowedTimeForPopup)
            {
                NotificationManager.Notify(
                    Localization.Get(
                        "mapmaker.failed.creation",
                        new Il2CppSystem.Object[]
                        {
                            Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(effectType)}"),
                            Localization.Get(tileData.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )
                        }
                    )
                );
                nextAllowedTimeForPopup = Time.time + 1f;
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
            return;

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

    internal static void HandleBiome(TileData tileData, int climate, SkinType skinType)
    {
        if(Editor.biomePicker.chosenValue == 0)
            return;

        tileData.climate = climate;
        tileData.Skin = skinType;
    }
}