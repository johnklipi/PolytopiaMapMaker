using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Common;
using UnityEngine;

namespace PolytopiaMapManager;

public static class Brush
{
    internal static int chosenClimate = 0;
    internal static SkinType chosenSkinType = SkinType.Default;
    internal static Polytopia.Data.TerrainData.Type chosenTerrain = Polytopia.Data.TerrainData.Type.None;
    internal static Polytopia.Data.ResourceData.Type chosenResource = Polytopia.Data.ResourceData.Type.None;
    internal static TileData.EffectType chosenTileEffect = TileData.EffectType.None;
    internal static ImprovementData.Type chosenBuilding = ImprovementData.Type.None;
    private static float nextAllowedTimeForPopup = 0f;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Tile), nameof(Tile.OnHoverStart))]
    private static void Tile_OnHoverStart(Tile __instance)
    {
        if(!Main.isActive || !Input.GetKey(KeyCode.Mouse1) || !GameManager.Instance.isLevelLoaded)
            return;

        GameState gameState = GameManager.GameState;
        byte localPlayer = GameManager.LocalPlayer.Id;

        if(chosenTerrain != Polytopia.Data.TerrainData.Type.None)
            __instance.data.terrain = chosenTerrain;

        if(chosenResource != ResourceData.Type.None)
        {
            if(chosenResource == (ResourceData.Type)1000)
            {
                __instance.data.resource = null;
                ActionUtils.CheckSurroundingArea(gameState, localPlayer, __instance.data);
            }
            else if(gameState.GameLogicData.TryGetData(chosenResource, out ResourceData data))
            {
                if(data.resourceTerrainRequirements.Contains(__instance.data.terrain))
                {
                    __instance.data.resource = new ResourceState
                    {
                        type = chosenResource
                    };
                    ActionUtils.CheckSurroundingArea(gameState, localPlayer, __instance.data);
                }
                else if(Time.time >= nextAllowedTimeForPopup)
                {
                    NotificationManager.Notify(Localization.Get("mapmaker.failed.creation", new Il2CppSystem.Object[] { Localization.Get(data.displayName, new Il2CppSystem.Object[] {} ),Localization.Get(__instance.data.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )}));
                    nextAllowedTimeForPopup = Time.time + 1f;
                }
            }
            else if(Time.time >= nextAllowedTimeForPopup)
            {
                NotificationManager.Notify(Localization.Get("mapmaker.failed.retrieve", new Il2CppSystem.Object[] { __instance.data.resource}));
                nextAllowedTimeForPopup = Time.time + 1f;
            }
        }
        if(chosenTileEffect != TileData.EffectType.None)
        {
            __instance.data.effects.Clear();
            if(!__instance.data.HasEffect(chosenTileEffect))
            {
                if(chosenTileEffect == TileData.EffectType.Flooded)
                {
                    if(__instance.data.isFloodable())
                    {
                        __instance.data.AddEffect(chosenTileEffect);
                        if(chosenSkinType == SkinType.Swamp)
                            __instance.data.AddEffect(TileData.EffectType.Swamped);
                    }
                    else if(Time.time >= nextAllowedTimeForPopup)
                    {
                        NotificationManager.Notify(Localization.Get("mapmaker.failed.creation", new Il2CppSystem.Object[] { Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(chosenTileEffect)}"), Localization.Get(__instance.data.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )}));
                        nextAllowedTimeForPopup = Time.time + 1f;
                    }
                }
                else if(chosenTileEffect == TileData.EffectType.Algae)
                {
                    if(__instance.data.IsWater)
                    {
                        __instance.data.AddEffect(chosenTileEffect);
                        if(chosenSkinType == SkinType.Cute)
                            __instance.data.AddEffect(TileData.EffectType.Foam);
                    }
                    else if(Time.time >= nextAllowedTimeForPopup)
                    {
                        NotificationManager.Notify(Localization.Get("mapmaker.failed.creation", new Il2CppSystem.Object[] { Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(chosenTileEffect)}"), Localization.Get(__instance.data.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )}));
                        nextAllowedTimeForPopup = Time.time + 1f;
                    }
                }
            }
        }
        if(chosenBuilding != ImprovementData.Type.None && !__instance.data.HasImprovement(ImprovementData.Type.LightHouse))
        {
            if(chosenBuilding == (ImprovementData.Type)1000)
            {
                __instance.data.improvement = null;
                Data.Capital? capital = Loader.GetCapital(__instance.data.coordinates, Main.currCapitals);

                if(capital != null)
                    Main.currCapitals.Remove(capital);  
            }
            else if (gameState.GameLogicData.TryGetData(chosenBuilding, out ImprovementData improvementData))
            {
                ImprovementState improvementState = new ImprovementState
                {
                    type = chosenBuilding,
                    borderSize = (ushort)improvementData.borderSize,
                    level = 0,
                    xp = 0,
                    production = 1,
                    founded = 0,
                    baseScore = 0,
                    founder = localPlayer
                };
                __instance.data.improvement = improvementState;
            }
        }
        if(chosenClimate != 0)
        {
            __instance.data.climate = chosenClimate;
            __instance.data.Skin = chosenSkinType;
        }
        __instance.Render();
    }
}