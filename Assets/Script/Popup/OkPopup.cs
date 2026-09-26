using UnityEngine.Events;
using UnityEngine.UIElements;

/// <summary>
/// 예/아니오 확인 팝업 (UI Toolkit)
/// </summary>
public class OkPopup
{
	public VisualElement Root { get; private set; }

	private readonly Label contensText;
	private readonly VisualElement dangerIcon;
	private readonly Button okBtn;
	private readonly Button cancelBtn;

	private UnityAction okBtnClickEvent;
	private UnityAction cancelBtnClickEvent;

	/// <summary> 연타 방지 (뜬 직후 잠깐은 클릭 무시) </summary>
	private bool clickOn = false;

	private bool maskClickDestoryOn = false;

	public OkPopup(VisualElement layer)
	{
		Root = new VisualElement();
		Root.AddToClassList("modal-scrim");

		VisualElement card = new VisualElement();
		card.AddToClassList("modal");

		dangerIcon = new VisualElement();
		dangerIcon.AddToClassList("icon-tile");
		dangerIcon.AddToClassList("icon--trash");
		dangerIcon.AddToClassList("modal-icon");
		dangerIcon.pickingMode = PickingMode.Ignore;

		contensText = new Label();
		contensText.AddToClassList("modal-message");

		VisualElement actions = new VisualElement();
		actions.AddToClassList("modal-actions");

		cancelBtn = new Button(CancelBtnClick);
		cancelBtn.AddToClassList("btn");
		cancelBtn.AddToClassList("btn--ghost");

		okBtn = new Button(OkBtnClick);
		okBtn.AddToClassList("btn");

		actions.Add(cancelBtn);
		actions.Add(okBtn);

		UIUtils.AddShadow(card);
		card.Add(dangerIcon);
		card.Add(contensText);
		card.Add(actions);
		Root.Add(card);

		// 카드 바깥(어두운 배경)을 누른 경우
		Root.RegisterCallback<ClickEvent>(evt =>
		{
			if (evt.target == Root)
			{
				MaskClick();
			}
		});

		layer.Add(Root);

		// 등장 애니메이션 + 연타 방지
		Root.schedule.Execute(() => Root.AddToClassList("modal-scrim--show")).StartingIn(16);
		Root.schedule.Execute(() => clickOn = true).StartingIn(300);
	}

	public void DataSet(string contensStr, UnityAction okBtnClickEvent, UnityAction cancelBtnClickEvent, bool okBtnOnly, bool maskClickDestoryOn, string okBtnStr, string cancelBtnStr, bool danger = false)
	{
		this.okBtnClickEvent = okBtnClickEvent;
		this.cancelBtnClickEvent = cancelBtnClickEvent;
		this.maskClickDestoryOn = maskClickDestoryOn;

		contensText.text = contensStr;

		UIUtils.SetVisible(cancelBtn, !okBtnOnly);

		okBtn.text = okBtnStr;
		cancelBtn.text = cancelBtnStr;

		UIUtils.SetVisible(dangerIcon, danger);

		okBtn.EnableInClassList("btn--danger", danger);
		okBtn.EnableInClassList("btn--primary", !danger);
	}

	public void OkBtnClick()
	{
		if (clickOn == false)
			return;

		okBtnClickEvent?.Invoke();
		DestroyPopup();
	}

	public void CancelBtnClick()
	{
		if (clickOn == false)
			return;

		cancelBtnClickEvent?.Invoke();
		DestroyPopup();
	}

	public void MaskClick()
	{
		if (clickOn == false)
			return;

		if (maskClickDestoryOn == false)
		{
			cancelBtnClickEvent?.Invoke();
		}

		DestroyPopup();
	}

	void DestroyPopup()
	{
		clickOn = false;
		Root.RemoveFromHierarchy();
	}
}
