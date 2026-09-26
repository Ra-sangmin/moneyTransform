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

		textBox.Add(titleText);
		textBox.Add(contensText);

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
	}
}
