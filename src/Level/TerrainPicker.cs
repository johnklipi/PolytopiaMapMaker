using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;
using DG.Tweening;

namespace PolytopiaMapManager.Level
{
    internal static class TerrainPicker
    {
        internal static List<Polytopia.Data.TerrainData.Type> excludedTerrains = new()
        {
            Polytopia.Data.TerrainData.Type.Wetland,
            Polytopia.Data.TerrainData.Type.Mangrove
        };
        internal static UIRoundButton? terrainButton = null;
        protected override int transformPositionOffsetX = 180;
        protected override string popupHeaderLocalizationKey = "terrain";

        protected override void CreateButtons(SelectViewmodePopup selectViewmodePopup, GameState gameState, ref float num)
        {
            foreach (Polytopia.Data.TerrainData terrainData in gameState.GameLogicData.AllTerrainData.Values)
            {
                Polytopia.Data.TerrainData.Type terrainType = terrainData.type;
                if(excludedTerrains.Contains(terrainType))
                    continue;
                string terrainName = Localization.Get(terrainType.GetDisplayName());
                CreateChoiceButton(selectViewmodePopup, terrainName, SpriteData.TerrainToString(terrainType), (int)terrainType, ref num);
            }
        }

        protected override void UpdateButton(UIRoundButton button)
        {
            if (MapMaker.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                button.icon.sprite = PickersHelper.GetSprite((int)MapMaker.chosenTerrain, SpriteData.TerrainToString(MapMaker.chosenTerrain), gameLogicData);
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
                terrainButton = button;
            }
        }

        protected override void OnButtonClicked(int id, BaseEventData eventData)
        {
            int type = id;
            Main.modLogger!.LogInfo("Clicked i guess");
            Main.modLogger!.LogInfo(id);
            MapMaker.chosenTerrain = (Polytopia.Data.TerrainData.Type)type;
            UpdateButton(terrainButton!);
        }
    }
}