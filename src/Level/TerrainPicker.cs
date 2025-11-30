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
                CreateTerrainChoiceButton(selectViewmodePopup, gameState, terrainName, SpriteData.TerrainToString(terrainType), (int)terrainType, ref num);
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
                MapMaker.chosenTerrain = (Polytopia.Data.TerrainData.Type)type;
                UpdateButton(terrainButton!);
            }

            GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
            playerButton.icon.sprite = PickersHelper.GetSprite(type, spriteName, gameLogicData);

            if (playerButton.Label.PreferedValues.y > num)
            {
                num = playerButton.Label.PreferedValues.y;
            }
            viewmodePopup.buttons.Add(playerButton);
        }
    }
}