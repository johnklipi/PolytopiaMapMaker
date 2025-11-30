using HarmonyLib;
using UnityEngine.EventSystems;
using Polytopia.Data;
using UnityEngine;
using PolytopiaBackendBase.Common;
using DG.Tweening;

namespace PolytopiaMapManager.Level
{
    internal static class ImprovementPicker : BasePicker
    {
        internal static List<Polytopia.Data.ImprovementData.Type> allowedImprovements = new()
        {
            Polytopia.Data.ImprovementData.Type.None,
            Polytopia.Data.ImprovementData.Type.City,
            Polytopia.Data.ImprovementData.Type.Ruin
        };
        internal static UIRoundButton? improvementButton = null;
        protected override int transformPositionOffsetX = 360;
        protected override string popupHeaderLocalizationKey = "improvement";


        protected override void CreateButtons(SelectViewmodePopup selectViewmodePopup, GameState gameState, ref float num)
        {
            foreach (Polytopia.Data.ImprovementData improvementData in gameState.GameLogicData.AllImprovementData.Values)
            {
                Polytopia.Data.ImprovementData.Type improvementType = improvementData.type;
                if(!allowedImprovements.Contains(improvementType))
                    continue;
                string improvementName = Localization.Get(improvementData.displayName);
                CreateImprovementChoiceButton(selectViewmodePopup, gameState.GameLogicData, improvementName, SpriteData.ImprovementToString(improvementType), (int)improvementType, ref num);
            }
        }

        protected override void UpdateButton(UIRoundButton button)
        {
            if (MapMaker.inMapMaker)
            {
                button.rectTransform.sizeDelta = new Vector2(75f, 75f);
                GameLogicData gameLogicData = GameManager.GameState.GameLogicData;
                button.icon.sprite = PickersHelper.GetSprite((int)MapMaker.chosenBuilding, SpriteData.ImprovementToString(MapMaker.chosenBuilding), gameLogicData);
                button.Outline.gameObject.SetActive(false);
                button.BG.color = ColorUtil.SetAlphaOnColor(Color.white, 0.5f);
                improvementButton = button;
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
                MapMaker.chosenBuilding = (Polytopia.Data.ImprovementData.Type)type;
                UpdateButton(improvementButton!);
            }
            playerButton.icon.sprite = PickersHelper.GetSprite(type, spriteName, gameLogicData);
            if (playerButton.Label.PreferedValues.y > num)
            {
                num = playerButton.Label.PreferedValues.y;
            }
            viewmodePopup.buttons.Add(playerButton);
        }
    }
}