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
        if(Input.GetKey(KeyCode.Mouse1))
        {
            if (GameManager.Instance.isLevelLoaded)
            {
                if(chosenTerrain != Polytopia.Data.TerrainData.Type.None)
                    __instance.data.terrain = chosenTerrain;
                if(chosenResource == ResourceData.Type.None)
                {
                    __instance.data.resource = null;
                    ActionUtils.CheckSurroundingArea(GameManager.GameState, GameManager.LocalPlayer.Id, __instance.data);
                }
                else if(GameManager.GameState.GameLogicData.TryGetData(chosenResource, out ResourceData data))
                {
                    if(data.resourceTerrainRequirements.Contains(__instance.data.terrain))
                    {
                        __instance.data.resource = new ResourceState
                        {
                            type = chosenResource
                        };
                        ActionUtils.CheckSurroundingArea(GameManager.GameState, GameManager.LocalPlayer.Id, __instance.data);
                    }
                    else if(Time.time >= nextAllowedTimeForPopup)
                    {
                        NotificationManager.Notify(Localization.Get("mapmaker.creation.failed", new Il2CppSystem.Object[] { Localization.Get(data.displayName, new Il2CppSystem.Object[] {} ),Localization.Get(__instance.data.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )}));
                        nextAllowedTimeForPopup = Time.time + 1f;
                    }
                }
                else if(Time.time >= nextAllowedTimeForPopup)
                {
                    NotificationManager.Notify(Localization.Get("gamelogicdata.retrieve.failed", new Il2CppSystem.Object[] { __instance.data.resource}));
                    nextAllowedTimeForPopup = Time.time + 1f;
                }
                // if(!__instance.data.HasEffect(chosenTileEffect))
                // {
                //     if(chosenTileEffect != TileData.EffectType.None)
                //         __instance.data.AddEffect(chosenTileEffect);
                // }
                // else
                // {
                //     __instance.data.RemoveEffect(chosenTileEffect);
                // }
                if(chosenTileEffect != TileData.EffectType.None)
                {
                    if(!__instance.data.HasEffect(chosenTileEffect))
                    {
                        if(chosenTileEffect == TileData.EffectType.Flooded)
                        {
                            if(__instance.data.isFloodable())
                            {
                                __instance.data.AddEffect(chosenTileEffect);
                            }
                            else if(Time.time >= nextAllowedTimeForPopup)
                            {
                                NotificationManager.Notify(Localization.Get("mapmaker.creation.failed", new Il2CppSystem.Object[] { Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(chosenTileEffect)}"), Localization.Get(__instance.data.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )}));
                                nextAllowedTimeForPopup = Time.time + 1f;
                            }
                        }
                        else if(chosenTileEffect == TileData.EffectType.Algae)
                        {
                            if(__instance.data.IsWater)
                            {
                                __instance.data.AddEffect(chosenTileEffect);
                            }
                            else if(Time.time >= nextAllowedTimeForPopup)
                            {
                                NotificationManager.Notify(Localization.Get("mapmaker.creation.failed", new Il2CppSystem.Object[] { Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(chosenTileEffect)}"), Localization.Get(__instance.data.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )}));
                                nextAllowedTimeForPopup = Time.time + 1f;
                            }
                        }
                    }
                }
                else
                {
                    TileData.EffectType lastEffect = __instance.data.effects.ToArray().ToList().LastOrDefault(TileData.EffectType.None);
                    __instance.data.RemoveEffect(lastEffect);
                }
                if(chosenBuilding == ImprovementData.Type.None)
                {
                    GameManager.Client.ActionManager.ExecuteCommand(new DestroyCommand(GameManager.LocalPlayer.Id, __instance.data.coordinates), out string error);
                }
                else
                {
                    bool succeded = GameManager.Client.ActionManager.ExecuteCommand(new BuildCommand(GameManager.LocalPlayer.Id, chosenBuilding, __instance.data.coordinates), out string error);
                    // if(!succeded && Time.time >= nextAllowedTimeForPopup)
                    // {
                    //     NotificationManager.Notify(Localization.Get("mapmaker.creation.failed", new Il2CppSystem.Object[] { Localization.Get(chosenBuilding.GetDisplayName()), Localization.Get(__instance.data.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )}));
                    // }
                }
                if(chosenClimate != 0)
                {
                    __instance.data.climate = chosenClimate;
                    __instance.data.Skin = chosenSkinType;
                }
                __instance.Render();
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BuildAction), nameof(BuildAction.ExecuteDefault))]
    private static void BuildAction_ExecuteDefault(BuildAction __instance, GameState gameState)
    {
        TileData tile = gameState.Map.GetTile(__instance.Coordinates);
        ImprovementData improvementData;
        PlayerState playerState;
        if (tile != null && gameState.GameLogicData.TryGetData(__instance.Type, out improvementData) && gameState.TryGetPlayer(__instance.PlayerId, out playerState))
        {
            if (improvementData.HasAbility(EnumCache<ImprovementAbility.Type>.GetType("climatechanger")))
            {
                tile.climate = chosenClimate;
                tile.Skin = chosenSkinType;
            }
        }
    }
}