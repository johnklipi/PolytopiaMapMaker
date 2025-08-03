using HarmonyLib;
using PolyMod.Managers;
using PolytopiaBackendBase.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using static PopupBase;

namespace PolytopiaMapManager
{
	internal static class CustomPopup
	{
		internal static bool active = false;
		private static int inputValue = 0;
		private const string header = "POLYMOD";

		// [HarmonyPostfix]
		// [HarmonyPatch(typeof(PopupButtonContainer), nameof(PopupButtonContainer.SetButtonData))]
		private static void PopupButtonContainer_SetButtonData(PopupButtonContainer __instance)
		{
			int num = __instance.buttons.Length;
			for (int i = 0; i < num; i++)
			{
				UITextButton uitextButton = __instance.buttons[i];
				Vector2 vector = new((num == 1) ? 0.5f : (i / (num - 1.0f)), 0.5f);
				uitextButton.rectTransform.anchorMin = vector;
				uitextButton.rectTransform.anchorMax = vector;
				uitextButton.rectTransform.pivot = vector;
			}
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SearchFriendCodePopup), nameof(SearchFriendCodePopup.OnInputChanged))]
		private static bool SearchFriendCodePopup_OnInputChanged(SearchFriendCodePopup __instance)
		{
			if (active)
			{
				OnInputChanged(__instance);
			}
			return !active;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(SearchFriendCodePopup), nameof(SearchFriendCodePopup.OnInputDone))]
		private static bool SearchFriendCodePopup_OnInputDone()
		{
			return !active;
		}

		public static void Show()
		{
			active = true;

			SearchFriendCodePopup polymodPopup = PopupManager.GetSearchFriendCodePopup();
			polymodPopup.ShowSetWidth(600);
			polymodPopup.Header = header;
			polymodPopup.Description = "";

			polymodPopup.buttonData = new PopupButtonData[0];
			polymodPopup.Show(new Vector2(NativeHelpers.Screen().x * 0.5f, NativeHelpers.Screen().y * 0.5f));

			UINavigationManager.Select(polymodPopup.inputfield);
			polymodPopup.CurrentSelectable = polymodPopup.inputfield;
		}

		public static void OnInputChanged(SearchFriendCodePopup polymodPopup)
		{
			if (int.TryParse(polymodPopup.inputfield.text, out int _))
			{
				polymodPopup.Buttons[1].ButtonEnabled = (!string.IsNullOrEmpty(polymodPopup.inputfield.text) && polymodPopup.inputfield.text.Length <= 10);
				inputValue = int.Parse(polymodPopup.inputfield.text);
			}
			else
			{
				polymodPopup.Buttons[1].ButtonEnabled = false;
			}
		}

		public static PopupButtonData[] CreatePopupButtonData()
		{
			List<PopupButtonData> popupButtons = new()
			{
				new(Localization.Get("buttons.back"), PopupButtonData.States.None, (UIButtonBase.ButtonAction)OnBackButtonClicked, -1, true, null)
			};

			return popupButtons.ToArray();

			void OnBackButtonClicked(int buttonId, BaseEventData eventData)
			{
				active = false;
			}
		}
	}
}