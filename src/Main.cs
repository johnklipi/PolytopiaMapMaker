using BepInEx.Logging;
using HarmonyLib;
using PolytopiaBackendBase.Game;
using UnityEngine.EventSystems;

namespace PolytopiaMapManager;
public static class Main
{
    internal static ManualLogSource? modLogger;
    public static void Load(ManualLogSource logger)
    {
        modLogger = logger;
        Harmony.CreateAndPatchAll(typeof(Core));
        Harmony.CreateAndPatchAll(typeof(Loader));
        Harmony.CreateAndPatchAll(typeof(Brush));
        Harmony.CreateAndPatchAll(typeof(UI.Editor));
        Harmony.CreateAndPatchAll(typeof(UI.Menu.Start));
        Harmony.CreateAndPatchAll(typeof(UI.Menu.GameSetup));
        Harmony.CreateAndPatchAll(typeof(UI.Picker.Manager));
        Harmony.CreateAndPatchAll(typeof(Popup.CustomInput));
        PolyMod.Loader.AddGameMode("mapmaker", (UIButtonBase.ButtonAction)OnMapMaker, false);
        PolyMod.Loader.AddPatchDataType("mapPreset", typeof(MapPreset));
        PolyMod.Loader.AddPatchDataType("mapSize", typeof(MapSize));
        Directory.CreateDirectory(IO.MAPS_PATH);

        static void OnMapMaker(int id, BaseEventData eventData)
        {
            Core.Init();
        }
    }
}