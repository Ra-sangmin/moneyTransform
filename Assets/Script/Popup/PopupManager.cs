using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// 확인 팝업 / 토스트 알림 관리 (UI Toolkit)
/// </summary>
public class PopupManager : MonoSingleton<PopupManager>
{
	/// <summary> 팝업이 올라갈 레이어 </summary>
	private VisualElement popupLayer = null;

	public void SetRoot(VisualElement layer)
	{
		popupLayer = layer;
	}

	public OkPopup OkPopupCreate(string contensStr, UnityAction okBtnClickEvent, UnityAction cancelBtnClickEvent = null, bool maskClickDestoryOn = false, bool okBtnOnly = false, string okBtnStr = "예", string cancelBtnStr = "아니오", bool danger = false)
	{
		if (popupLayer == null)
			return null;

		OkPopup okPopup = new OkPopup(popupLayer);
		okPopup.DataSet(contensStr, okBtnClickEvent, cancelBtnClickEvent, okBtnOnly, maskClickDestoryOn, okBtnStr, cancelBtnStr, danger);

		return okPopup;
	}

	public BackgroundPopup BackgroundPopupCreate(GetImageController getImageController)
	{
		if (popupLayer == null)
			return null;

		return new BackgroundPopup(popupLayer, getImageController);
	}

	public WarningPopup WarningPopupCreate(string contensStr)
	{
		if (popupLayer == null)
			return null;

		WarningPopup warningPopup = new WarningPopup(popupLayer);
		warningPopup.DataSet(contensStr);

		return warningPopup;
	}
}
