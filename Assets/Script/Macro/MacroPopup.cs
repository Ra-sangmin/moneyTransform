using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// 매크로 목록 화면 (UI Toolkit)
/// </summary>
public class MacroPopup
{
	private readonly MainController mainController;

	private readonly VisualElement screen;
	private readonly ScrollView macroList;
	private readonly VisualElement emptyCard;
	private readonly Label countLabel;

	private readonly MacroChagnePopup macroChagnePopup;

	private readonly List<MacroItem> macroItemList = new List<MacroItem>();

	public MacroPopup(VisualElement root, MainController mainController)
	{
		this.mainController = mainController;

		screen = root.Q<VisualElement>("macro-screen");
		macroList = root.Q<ScrollView>("macro-list");
		emptyCard = root.Q<VisualElement>("macro-empty");
		countLabel = root.Q<Label>("macro-count");

		macroChagnePopup = new MacroChagnePopup(root, mainController.ReplaceStrList, ChangeEndOn);

		root.Q<Button>("macro-back").clicked += Close;
		root.Q<Button>("macro-add").clicked += AddDataClickOn;
	}

	public void Open()
	{
		SetMacroItem();
		UIUtils.Show(screen);
	}

	public void Close()
	{
		UIUtils.Hide(screen);
	}

	/// <summary>
	/// 저장된 매크로로 목록을 다시 그립니다.
	/// </summary>
	void SetMacroItem()
	{
		macroList.Clear();
		macroItemList.Clear();

		List<MacroData> macroDataList = MacroManager.Instance.macroAllData.macroDataList;

		for (int i = 0; i < macroDataList.Count; i++)
		{
			MacroData macroData = macroDataList[i];
			MacroItem macroItem = new MacroItem(macroData, i);
			macroItem.CopyEventOn += CopyEventOn;
			macroItem.ChangeEventOn += ChangeEventOn;
			macroItem.DeleteEventOn += DeleteEventOn;

			macroList.Add(macroItem.Root);
			macroItemList.Add(macroItem);
		}

		bool isEmpty = macroDataList.Count == 0;
		UIUtils.SetVisible(emptyCard, isEmpty);
		UIUtils.SetVisible(macroList, !isEmpty);

		if (countLabel != null)
		{
			countLabel.text = $"{macroDataList.Count}개";
		}
	}

	public void CopyEventOn(MacroItem macroItem)
	{
		mainController.CopyToMacroTextOn(macroItem.macroData);
	}

	public void ChangeEventOn(MacroItem macroItem)
	{
		macroChagnePopup.Open(macroItem.macroData);
	}

	public void DeleteEventOn(MacroItem macroItem)
	{
		MacroData macroData = macroItem.macroData;
		string title = string.IsNullOrEmpty(macroData.title) ? "제목 없음" : macroData.title;

		PopupManager.Instance.OkPopupCreate($"'{title}' 매크로를 삭제할까요?", () =>
		{
			MacroManager.Instance.DeleteMacroData(macroData);
			SetMacroItem();
		}, null, false, false, "삭제", "취소", true);
	}

	public void AddDataClickOn()
	{
		MacroData macroData = MacroManager.Instance.AddMacroData();
		SetMacroItem();

		macroChagnePopup.Open(macroData);
	}

	/// <summary>
	/// 편집 화면을 닫았을 때 호출 (제목/내용이 모두 비어 있으면 삭제)
	/// </summary>
	void ChangeEndOn(MacroData macroData)
	{
		if (macroData != null && string.IsNullOrEmpty(macroData.title) && string.IsNullOrEmpty(macroData.contens))
		{
			MacroManager.Instance.DeleteMacroData(macroData);
		}

		SetMacroItem();
	}
}
