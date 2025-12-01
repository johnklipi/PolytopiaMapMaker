using BepInEx.Logging;
using HarmonyLib;
using PolytopiaBackendBase.Game;
using UnityEngine.EventSystems;

namespace PolytopiaMapManager
{
    public static class Main
    {
        internal static ManualLogSource? modLogger;
        public static void Load(ManualLogSource logger)
        {
            modLogger = logger;
            Harmony.CreateAndPatchAll(typeof(MapMaker));
            Harmony.CreateAndPatchAll(typeof(MapLoader));
            Harmony.CreateAndPatchAll(typeof(Brush));
            Harmony.CreateAndPatchAll(typeof(Menu.GameSetup));
            Harmony.CreateAndPatchAll(typeof(Level.ClimatePicker));
            Harmony.CreateAndPatchAll(typeof(Level.TerrainPicker));
            Harmony.CreateAndPatchAll(typeof(Level.ResourcePicker));
            Harmony.CreateAndPatchAll(typeof(Level.ImprovementPicker));
            Harmony.CreateAndPatchAll(typeof(Level.TileEffectPicker));
            PolyMod.Loader.AddGameMode("mapmaker", (UIButtonBase.ButtonAction)OnMapMaker);
            PolyMod.Loader.AddPatchDataType("mapPreset", typeof(MapPreset));
            PolyMod.Loader.AddPatchDataType("mapSize", typeof(MapSize));
            Directory.CreateDirectory(MapLoader.MAPS_PATH);

            void OnMapMaker(int id, BaseEventData eventData)
            {
                MapLoader.Init();
            }
        }

    }
}