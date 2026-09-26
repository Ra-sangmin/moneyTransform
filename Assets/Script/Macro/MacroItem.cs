using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// 매크로 목록의 한 줄 (UI Toolkit)
/// </summary>
public class MacroItem
{
	public VisualElement Root { get; private set; }

	public MacroData macroData;

	public UnityAction<MacroItem> CopyEventOn = data => { };
	public UnityAction<MacroItem> ChangeEventOn = data => { };
	public UnityAction<MacroItem> DeleteEventOn = data => { };

	private readonly Label titleText;
	private readonly Label contensText;
	private readonly Button moreBtn;

	/// <summary> 접힌 상태에서 보이는 내용 높이 (약 2줄) </summary>
	private const float collapsedHeight = 70f;

	/// <summary> 내용을 모두 펼쳐 보이는 중인지 </summary>
	public bool IsExpanded { get; private set; }
	/// <summary> 내용이 접힌 높이보다 길어서 펼칠 수 있는지 </summary>
	public bool CanExpand { get; private set; }

	/// <summary> 펼침/접힘을 사용자가 바꿨을 때 </summary>
	public UnityAction<MacroItem> ExpandChangeEventOn = data => { };

	/// <summary> 목록 순서대로 돌아가며 쓰는 색 (보라/파랑/핑크/주황) </summary>
	private static readonly string[] colorNames = { "purple", "blue", "pink", "orange" };

	public MacroItem(MacroData macroData, int index = 0)
	{
		string colorName = colorNames[((index % colorNames.Length) + colorNames.Length) % colorNames.Length];

		Root = new VisualElement();
		Root.AddToClassList("macro-item");
		Root.AddToClassList("macro-item--" + colorName);

		VisualElement textBox = new VisualElement();
		textBox.AddToClassList("macro-text");

		titleText = new Label();
		titleText.AddToClassList("macro-title");
		contensText = new Label();
		contensText.AddToClassList("macro-content");

		// 내용이 길면 "전체 보기 / 접기" 로 세로 크기를 늘리고 줄임
		moreBtn = new Button(() =>
		{
			SetExpanded(!IsExpanded);
			ExpandChangeEventOn(this);
		});
		moreBtn.AddToClassList("macro-more");
		// 버튼에 포커스가 가면 ScrollView 가 그 위치로 자동 스크롤하므로 포커스를 받지 않게
		moreBtn.focusable = false;
		moreBtn.style.display = DisplayStyle.None;

		textBox.Add(titleText);
		textBox.Add(contensText);
		textBox.Add(moreBtn);

		contensText.RegisterCallback<GeometryChangedEvent>(_ => RefreshExpandable());

		VisualElement actions = new VisualElement();
		actions.AddToClassList("macro-actions");

		Button changeBtn = new Button(ChangeBtnClick) { text = "편집" };
		changeBtn.AddToClassList("btn");
		changeBtn.AddToClassList("btn--soft");

		Button deleteBtn = new Button(DeleteBtnClick) { text = "삭제" };
		deleteBtn.AddToClassList("btn");
		deleteBtn.AddToClassList("btn--soft");
		deleteBtn.AddToClassList("btn--soft-danger");

		actions.Add(changeBtn);
		actions.Add(deleteBtn);

		VisualElement icon = new VisualElement();
		icon.AddToClassList("macro-icon");
		icon.AddToClassList("macro-icon--" + colorName);
		icon.pickingMode = PickingMode.Ignore;

		Root.Add(icon);
		Root.Add(textBox);
		Root.Add(actions);

		UIUtils.AddShadow(Root);

		// 항목을 탭하면 복사
		UIUtils.RegisterTap(Root, CopyBtnClick);

		SetData(macroData);
	}

	public void SetData(MacroData macroData)
	{
		this.macroData = macroData;
		InputDataChangeOn();
	}

	public void CopyBtnClick()
	{
		CopyEventOn(this);
	}

	public void ChangeBtnClick()
	{
		ChangeEventOn(this);
	}

	public void DeleteBtnClick()
	{
		DeleteEventOn(this);
	}

	public void InputDataChangeOn()
	{
		bool emptyTitle = macroData == null || string.IsNullOrEmpty(macroData.title);
		bool emptyContens = macroData == null || string.IsNullOrEmpty(macroData.contens);

		titleText.text = emptyTitle ? "제목 없음" : macroData.title;
		contensText.text = emptyContens ? "내용 없음" : macroData.contens;

		Root.EnableInClassList("macro-item--empty", emptyTitle && emptyContens);
		RefreshExpandable();
	}

	/// <summary>
	/// 내용을 모두 펼치거나 약 2줄로 접습니다.
	/// </summary>
	public void SetExpanded(bool expanded)
	{
		IsExpanded = expanded;
		Root.EnableInClassList("macro-item--expanded", expanded);
		moreBtn.text = expanded ? "접기" : "전체 보기";
	}

	/// <summary>
	/// 내용 길이를 재서 펼치기 버튼이 필요한지 판단합니다.
	/// </summary>
	void RefreshExpandable()
	{
		float width = contensText.resolvedStyle.width;
		if (float.IsNaN(width) || width <= 0)
			return;

		Vector2 size = contensText.MeasureTextSize(contensText.text, width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
		bool canExpand = size.y > collapsedHeight + 2f;

		if (canExpand == CanExpand && moreBtn.style.display.value == (canExpand ? DisplayStyle.Flex : DisplayStyle.None))
			return;

		CanExpand = canExpand;
		moreBtn.style.display = canExpand ? DisplayStyle.Flex : DisplayStyle.None;
		moreBtn.text = IsExpanded ? "접기" : "전체 보기";
	}
}
