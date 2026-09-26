using System.Collections.Generic;
using UnityEngine;
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

	/// <summary> 모두 펼치기/접기 버튼 </summary>
	private readonly Button expandAllBtn;
	/// <summary> 모두 펼쳐 보기 상태 (앱을 다시 켜도 유지) </summary>
	private bool expandAll;
	private const string expandAllKey = "macroExpandAll";

	// 추가 문구 패널 (기본 닫힘)
	private readonly VisualElement msgCard;
	private readonly VisualElement msgBody;
	private readonly Label msgSub;
	private readonly Label msgToggleLabel;
	private readonly TextField addString0;
	private readonly TextField addString1;
	private bool msgOpen;
	private const string msgSubDefault = "{추가문구1}, {추가문구2} 자리에 들어가요";

	public MacroPopup(VisualElement root, MainController mainController)
	{
		this.mainController = mainController;

		screen = root.Q<VisualElement>("macro-screen");
		macroList = root.Q<ScrollView>("macro-list");
		// 터치는 물론 마우스로 끌어도 스크롤되도록
		// 휴대폰 터치: 끝에서 늘어났다가 손을 떼면 돌아옴 (마우스는 UIUtils.EnableDragScroll 이 같은 동작을 함)
		macroList.touchScrollBehavior = ScrollView.TouchScrollBehavior.Elastic;
		macroList.mouseWheelScrollSize = 60f;
		UIUtils.EnableDragScroll(macroList);
		emptyCard = root.Q<VisualElement>("macro-empty");
		countLabel = root.Q<Label>("macro-count");

		macroChagnePopup = new MacroChagnePopup(root, mainController.ReplaceStrList, ChangeEndOn);

		root.Q<Button>("macro-back").clicked += Close;
		root.Q<Button>("macro-add").clicked += AddDataClickOn;

		msgCard = root.Q<VisualElement>("msg-card");
		msgBody = root.Q<VisualElement>("msg-body");
		msgSub = root.Q<Label>("msg-sub");
		msgToggleLabel = root.Q<Label>("msg-toggle-label");
		addString0 = root.Q<TextField>("add-string-0");
		addString1 = root.Q<TextField>("add-string-1");
		UIUtils.RegisterTap(root.Q<VisualElement>("msg-head"), () => SetMsgOpen(!msgOpen));
		addString0.RegisterValueChangedCallback(_ => RefreshMsgSub());
		addString1.RegisterValueChangedCallback(_ => RefreshMsgSub());
		SetMsgOpen(false);

		expandAll = PlayerPrefs.GetInt(expandAllKey, 0) == 1;
		expandAllBtn = root.Q<Button>("macro-expand");
		expandAllBtn.clicked += () => SetExpandAll(!expandAll);
		RefreshExpandAllButton();
	}

	public void Open()
	{
		SetMacroItem();
		// 추가 문구 패널은 열 때마다 닫힌 상태로
		SetMsgOpen(false);
		// 열 때마다 목록 맨 위부터 보이도록
		UIUtils.ResetScroll(macroList);
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
			macroItem.SetExpanded(expandAll);
			macroItem.ExpandChangeEventOn += KeepItemInView;

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

	/// <summary>
	/// 추가 문구 패널을 열거나 닫습니다.
	/// </summary>
	void SetMsgOpen(bool open)
	{
		msgOpen = open;
		UIUtils.SetVisible(msgBody, open);
		msgCard.EnableInClassList("msg-card--open", open);
		msgToggleLabel.text = open ? "닫기" : "열기";

		// 닫을 때는 입력 중이던 칸의 포커스(키보드)도 내림
		if (!open)
		{
			addString0.Blur();
			addString1.Blur();
		}

		RefreshMsgSub();
	}

	/// <summary>
	/// 닫혀 있을 때는 입력해 둔 문구를 설명 자리에 간단히 보여 줌
	/// </summary>
	void RefreshMsgSub()
	{
		string a = addString0.value ?? string.Empty;
		string b = addString1.value ?? string.Empty;
		bool hasValue = a.Length > 0 || b.Length > 0;

		if (msgOpen || !hasValue)
		{
			msgSub.text = msgSubDefault;
			msgSub.RemoveFromClassList("msg-sub--value");
			return;
		}

		string Short(string t) => t.Replace("\n", " ").Length > 14 ? t.Replace("\n", " ").Substring(0, 14) + "…" : t.Replace("\n", " ");
		msgSub.text = $"문구 1: {(a.Length > 0 ? Short(a) : "-")}  ·  문구 2: {(b.Length > 0 ? Short(b) : "-")}";
		msgSub.AddToClassList("msg-sub--value");
	}

	/// <summary>
	/// 모든 매크로 내용을 펼치거나 접습니다.
	/// </summary>
	void SetExpandAll(bool expand)
	{
		expandAll = expand;
		PlayerPrefs.SetInt(expandAllKey, expand ? 1 : 0);
		PlayerPrefs.Save();

		foreach (MacroItem item in macroItemList)
		{
			item.SetExpanded(expand);
		}

		// 추가 문구 패널도 함께 펼치기/접기
		SetMsgOpen(expand);

		RefreshExpandAllButton();
	}

	void RefreshExpandAllButton()
	{
		if (expandAllBtn == null)
			return;

		expandAllBtn.Q<Label>(className: "expand-label").text = expandAll ? "모두 접기" : "모두 펼치기";
		expandAllBtn.Q<Label>(className: "expand-label-short").text = expandAll ? "접기" : "펼치기";
		expandAllBtn.EnableInClassList("expand-btn--on", expandAll);
	}

	/// <summary>
	/// 항목을 펼치거나 접을 때 목록이 튀지 않도록:
	/// 그 항목의 위쪽 위치를 화면에서 그대로 두고, 펼쳐서 아래가 잘리면 보이는 만큼만 살짝 내려 줍니다.
	/// (항목을 목록 맨 위로 끌어올리지 않음)
	/// </summary>
	void KeepItemInView(MacroItem macroItem)
	{
		VisualElement item = macroItem.Root;
		VisualElement viewport = macroList.contentViewport;

		// 누르기 전, 보이는 영역 안에서 항목 위쪽의 위치
		float topInView = item.layout.y - macroList.scrollOffset.y;

		EventCallback<GeometryChangedEvent> onLayout = null;
		onLayout = evt =>
		{
			item.UnregisterCallback(onLayout);

			const float gap = 20f;
			float viewHeight = viewport.layout.height;
			float itemTop = item.layout.y;
			float itemBottom = item.layout.yMax;

			// 1) 항목 위쪽을 원래 보이던 자리에 고정 (위로 잘려 있었으면 살짝 보이게)
			float offset = itemTop - Mathf.Max(topInView, gap);

			// 2) 펼쳐서 아래쪽이 화면 밖으로 나가면, 위쪽이 가려지지 않는 선에서 보이도록 내림
			float overflow = (itemBottom + gap) - (offset + viewHeight);
			if (overflow > 0f)
			{
				offset = Mathf.Min(offset + overflow, itemTop - gap);
			}

			offset = Mathf.Clamp(offset, 0f, UIUtils.GetMaxScrollY(macroList));
			macroList.scrollOffset = new Vector2(macroList.scrollOffset.x, offset);
		};
		item.RegisterCallback(onLayout);
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
