using HarmonyLib;
using Polytopia.Data;
using PolytopiaBackendBase.Common;
using PolytopiaMapManager.UI;
using UnityEngine;

namespace PolytopiaMapManager;

public static class Brush
{
    internal static int brushSize = 2;
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
                foreach (var tileData in GameManager.GameState.Map.GetArea(__instance.Coordinates, brushSize, true))
                {
                    var tile = tileData.GetInstance();

                    if(chosenTerrain != Polytopia.Data.TerrainData.Type.None)
                        tile.data.terrain = chosenTerrain;
                    if(chosenResource == ResourceData.Type.None)
                    {
                        tile.data.resource = null;
                        ActionUtils.CheckSurroundingArea(GameManager.GameState, GameManager.LocalPlayer.Id, tile.data);
                    }
                    else if(GameManager.GameState.GameLogicData.TryGetData(chosenResource, out ResourceData data))
                    {
                        if(data.resourceTerrainRequirements.Contains(tile.data.terrain))
                        {
                            tile.data.resource = new ResourceState
                            {
                                type = chosenResource
                            };
                            ActionUtils.CheckSurroundingArea(GameManager.GameState, GameManager.LocalPlayer.Id, tile.data);
                        }
                        else if(Time.time >= nextAllowedTimeForPopup)
                        {
                            NotificationManager.Notify(Localization.Get("mapmaker.failed.creation", new Il2CppSystem.Object[] { Localization.Get(data.displayName, new Il2CppSystem.Object[] {} ),Localization.Get(tile.data.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )}));
                            nextAllowedTimeForPopup = Time.time + 1f;
                        }
                    }
                    else if(Time.time >= nextAllowedTimeForPopup)
                    {
                        NotificationManager.Notify(Localization.Get("mapmaker.failed.retrieve", new Il2CppSystem.Object[] { tile.data.resource}));
                        nextAllowedTimeForPopup = Time.time + 1f;
                    }
                    // if(!tile.data.HasEffect(chosenTileEffect))
                    // {
                    //     if(chosenTileEffect != TileData.EffectType.None)
                    //         tile.data.AddEffect(chosenTileEffect);
                    // }
                    // else
                    // {
                    //     tile.data.RemoveEffect(chosenTileEffect);
                    // }
                    if(chosenTileEffect != TileData.EffectType.None)
                    {
                        if(!tile.data.HasEffect(chosenTileEffect))
                        {
                            if(chosenTileEffect == TileData.EffectType.Flooded)
                            {
                                if(tile.data.isFloodable())
                                {
                                    tile.data.AddEffect(chosenTileEffect);
                                    if(chosenSkinType == SkinType.Swamp)
                                        tile.data.AddEffect(TileData.EffectType.Swamped);
                                }
                                else if(Time.time >= nextAllowedTimeForPopup)
                                {
                                    NotificationManager.Notify(Localization.Get("mapmaker.failed.creation", new Il2CppSystem.Object[] { Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(chosenTileEffect)}"), Localization.Get(tile.data.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )}));
                                    nextAllowedTimeForPopup = Time.time + 1f;
                                }
                            }
                            else if(chosenTileEffect == TileData.EffectType.Algae)
                            {
                                if(tile.data.IsWater)
                                {
                                    tile.data.AddEffect(chosenTileEffect);
                                    if(chosenSkinType == SkinType.Cute)
                                        tile.data.AddEffect(TileData.EffectType.Foam);
                                }
                                else if(Time.time >= nextAllowedTimeForPopup)
                                {
                                    NotificationManager.Notify(Localization.Get("mapmaker.failed.creation", new Il2CppSystem.Object[] { Localization.Get($"tile.effect.{EnumCache<TileData.EffectType>.GetName(chosenTileEffect)}"), Localization.Get(tile.data.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )}));
                                    nextAllowedTimeForPopup = Time.time + 1f;
                                }
                            }
                        }
                    }
                    else
                    {
                        TileData.EffectType lastEffect = tile.data.effects.ToArray().ToList().LastOrDefault(TileData.EffectType.None);
                        tile.data.RemoveEffect(lastEffect);
                    }
                    if(!tile.data.HasImprovement(ImprovementData.Type.LightHouse))
                    {
                        if(chosenBuilding == ImprovementData.Type.None)
                        {
                            GameManager.Client.ActionManager.ExecuteCommand(new DestroyCommand(GameManager.LocalPlayer.Id, tile.data.coordinates), out string error);
                        }
                        else
                        {
                            bool succeded = GameManager.Client.ActionManager.ExecuteCommand(new BuildCommand(GameManager.LocalPlayer.Id, chosenBuilding, tile.data.coordinates), out string error);
                            // if(!succeded && Time.time >= nextAllowedTimeForPopup)
                            // {
                            //     NotificationManager.Notify(Localization.Get("mapmaker.failed.creation", new Il2CppSystem.Object[] { Localization.Get(chosenBuilding.GetDisplayName()), Localization.Get(tile.data.terrain.GetDisplayName(), new Il2CppSystem.Object[] {} )}));
                            // }
                        }
                    }
                    if(chosenClimate != 0)
                    {
                        tile.data.climate = chosenClimate;
                        tile.data.Skin = chosenSkinType;
                    }
                    tile.Render();
                }
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