using UnityEngine.UIElements;

/// <summary>
/// 배경 사진 설정 창 (미리보기 / 사진 고르기 / 기본 배경으로)
/// </summary>
public class BackgroundPopup
{
	public VisualElement Root { get; private set; }

	private readonly GetImageController getImageController;

	/// <summary> 연타 방지 (뜬 직후 잠깐은 클릭 무시) </summary>
	private bool clickOn = false;

	public BackgroundPopup(VisualElement layer, GetImageController getImageController)
	{
		this.getImageController = getImageController;
		bool hasPhoto = getImageController != null && getImageController.HasPhoto;

		Root = new VisualElement();
		Root.AddToClassList("modal-scrim");

		VisualElement card = new VisualElement();
		card.AddToClassList("modal");
		card.AddToClassList("modal--wide");

		// 헤더: 아이콘 + 제목
		VisualElement head = new VisualElement();
		head.AddToClassList("card-head");

		VisualElement icon = new VisualElement();
		icon.AddToClassList("icon-tile");
		icon.AddToClassList("icon--image");
		icon.pickingMode = PickingMode.Ignore;

		VisualElement titles = new VisualElement();
		titles.AddToClassList("card-titles");
		Label title = new Label("배경 사진");
		title.AddToClassList("card-title");
		Label sub = new Label("계산기 뒤에 보일 사진을 골라 주세요");
		sub.AddToClassList("card-sub");
		titles.Add(title);
		titles.Add(sub);

		head.Add(icon);
		head.Add(titles);

		// 지금 배경 미리보기
		VisualElement preview = new VisualElement();
		preview.AddToClassList("bg-preview");
		if (hasPhoto)
		{
			preview.style.backgroundImage = new StyleBackground(getImageController.CurrentTexture);
		}
		else
		{
			preview.AddToClassList("bg-preview--default");
		}

		Label previewChip = new Label(hasPhoto ? "지금 배경" : "기본 배경");
		previewChip.AddToClassList("bg-preview-chip");
		preview.Add(previewChip);

		Label hint = new Label("고른 사진은 화면 비율에 맞게 잘라서 저장돼요");
		hint.AddToClassList("bg-hint");

		// 버튼
		VisualElement actions = new VisualElement();
		actions.AddToClassList("modal-actions");

		Button closeBtn = new Button(Close) { text = "닫기" };
		closeBtn.AddToClassList("btn");
		closeBtn.AddToClassList("btn--ghost");

		Button resetBtn = new Button(ResetClick) { text = "기본 배경으로" };
		resetBtn.AddToClassList("btn");
		resetBtn.AddToClassList("btn--outline");
		UIUtils.SetVisible(resetBtn, hasPhoto);

		Button pickBtn = new Button(PickClick) { text = "사진 고르기" };
		pickBtn.AddToClassList("btn");
		pickBtn.AddToClassList("btn--primary");

		actions.Add(closeBtn);
		actions.Add(resetBtn);
		actions.Add(pickBtn);

		UIUtils.AddShadow(card);
		card.Add(head);
		card.Add(preview);
		card.Add(hint);
		card.Add(actions);
		Root.Add(card);

		// 카드 바깥을 누르면 닫기
		Root.RegisterCallback<ClickEvent>(evt =>
		{
			if (evt.target == Root)
			{
				Close();
			}
		});

		layer.Add(Root);

		Root.schedule.Execute(() => Root.AddToClassList("modal-scrim--show")).StartingIn(16);
		Root.schedule.Execute(() => clickOn = true).StartingIn(300);
	}

	void PickClick()
	{
		if (!clickOn)
			return;

		Close();
		getImageController?.PickImage();
	}

	void ResetClick()
	{
		if (!clickOn)
			return;

		Close();
		PopupManager.Instance.OkPopupCreate("기본 배경으로 되돌릴까요?", () =>
		{
			getImageController?.ResetToDefault();
		}, null, false, false, "되돌리기", "취소");
	}

	public void Close()
	{
		clickOn = false;
		Root.RemoveFromHierarchy();
	}
}
