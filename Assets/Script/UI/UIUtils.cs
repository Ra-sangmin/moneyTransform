using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI Toolkit 공통 도우미
/// </summary>
public static class UIUtils
{
	/// <summary> 누른 뒤 이만큼 이상 움직이면(스크롤 등) 탭으로 보지 않음 </summary>
	private const float tapMoveLimit = 30f;

	public static void Show(VisualElement element)
	{
		if (element != null) element.style.display = DisplayStyle.Flex;
	}

	public static void Hide(VisualElement element)
	{
		if (element != null) element.style.display = DisplayStyle.None;
	}

	public static void SetVisible(VisualElement element, bool visible)
	{
		if (visible) Show(element);
		else Hide(element);
	}

	/// <summary>
	/// 스크롤 가능한 최대 위치 (내용 높이 - 보이는 영역 높이)
	/// </summary>
	public static float GetMaxScrollY(ScrollView scrollView)
	{
		float content = scrollView.contentContainer.layout.height;
		float viewport = scrollView.contentViewport.layout.height;
		if (float.IsNaN(content) || float.IsNaN(viewport))
			return 0f;
		return Mathf.Max(0f, content - viewport);
	}

	/// <summary>
	/// 스크롤 위치가 범위를 벗어나 있으면 안쪽으로 되돌립니다.
	/// </summary>
	public static void ClampScroll(ScrollView scrollView)
	{
		float y = Mathf.Clamp(scrollView.scrollOffset.y, 0f, GetMaxScrollY(scrollView));
		if (!Mathf.Approximately(y, scrollView.scrollOffset.y))
		{
			scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, y);
		}
	}

	/// <summary>
	/// 마우스로도 끌어서 스크롤할 수 있게 합니다. (터치는 ScrollView가 원래 지원)
	/// - 끝을 넘어 당기면 고무줄처럼 조금 따라오고, 손을 떼면 부드럽게 제자리로 돌아옵니다.
	/// - 빠르게 끌고 놓으면 그 속도로 조금 미끄러지다 멈춥니다.
	/// - 실제 스크롤 위치는 항상 범위 안에 두고, 넘친 만큼은 내용을 살짝 옮겨서(translate) 보여 줍니다.
	/// </summary>
	public static void EnableDragScroll(ScrollView scrollView)
	{
		if (scrollView == null)
			return;

		const float dragThreshold = 12f;
		const float deceleration = 0.9f;   // 프레임(16ms)마다 속도 감소 비율
		const float minVelocity = 0.05f;   // px/ms 이하면 멈춤
		const float maxVelocity = 4f;      // px/ms
		const float resistance = 0.45f;    // 끝을 넘어 당길 때 따라오는 비율
		const float maxOverscroll = 220f;  // 최대로 늘어나는 거리
		const float springBack = 0.22f;    // 프레임마다 되돌아오는 비율

		VisualElement content = scrollView.contentContainer;

		bool pressed = false;
		bool dragging = false;
		int pointerId = -1;
		Vector2 startPosition = Vector2.zero;
		float startOffset = 0f;
		float lastY = 0f;
		float lastTime = 0f;
		float velocity = 0f;               // px/ms (스크롤 위치 기준)
		float overscroll = 0f;             // +: 위쪽 끝을 넘어 아래로 당김, -: 아래쪽 끝을 넘어 위로 당김
		IVisualElementScheduledItem animation = null;

		void SetOverscroll(float value)
		{
			overscroll = value;
			// ScrollView 가 스크롤에 contentContainer 의 translate 를 쓰므로, 넘친 거리는 각 항목에 적용
			StyleTranslate translate = Mathf.Abs(value) < 0.5f
				? new StyleTranslate(StyleKeyword.Null)
				: new StyleTranslate(new Translate(0, value));
			foreach (VisualElement child in content.Children())
			{
				child.style.translate = translate;
			}
		}

		// 원하는 스크롤 위치를 실제 위치(범위 안) + 넘친 거리로 나눠 적용
		void ApplyDesired(float desired)
		{
			float max = GetMaxScrollY(scrollView);
			float y = Mathf.Clamp(desired, 0f, max);
			scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, y);

			float over = desired < 0f ? -desired : desired > max ? max - desired : 0f;
			// 멀리 당길수록 덜 따라오도록 (고무줄 느낌)
			float sign = Mathf.Sign(over);
			float amount = Mathf.Abs(over) * resistance;
			amount = maxOverscroll * (1f - 1f / (amount / maxOverscroll + 1f));
			SetOverscroll(sign * amount);
		}

		void StopAnimation()
		{
			animation?.Pause();
			velocity = 0f;
		}

		// 손을 뗀 뒤: 미끄러짐 + 넘친 부분 되돌리기
		void Tick()
		{
			bool moving = false;

			if (Mathf.Abs(velocity) >= minVelocity && Mathf.Abs(overscroll) < 0.5f)
			{
				float before = scrollView.scrollOffset.y;
				float max = GetMaxScrollY(scrollView);
				float next = Mathf.Clamp(before + velocity * 16f, 0f, max);
				scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, next);
				velocity *= deceleration;
				// 끝에 닿으면 멈춤
				if (Mathf.Approximately(next, 0f) || Mathf.Approximately(next, max))
					velocity = 0f;
				moving = Mathf.Abs(velocity) >= minVelocity;
			}
			else
			{
				velocity = 0f;
			}

			if (Mathf.Abs(overscroll) >= 0.5f)
			{
				SetOverscroll(overscroll * (1f - springBack));
				moving = true;
			}
			else if (overscroll != 0f)
			{
				SetOverscroll(0f);
			}

			if (!moving)
				animation?.Pause();
		}

		void StartAnimation()
		{
			if (animation == null)
				animation = scrollView.schedule.Execute(Tick).Every(16);
			else
				animation.Resume();
		}

		void EndDrag()
		{
			if (dragging && scrollView.HasPointerCapture(pointerId))
			{
				scrollView.ReleasePointer(pointerId);
			}

			bool wasDragging = dragging;
			pressed = false;
			dragging = false;

			if (!wasDragging)
				return;

			// 마지막 움직임이 오래전이거나 끝을 넘어 당긴 상태면 미끄러지지 않음
			if (Time.realtimeSinceStartup * 1000f - lastTime > 80f || Mathf.Abs(overscroll) >= 0.5f)
				velocity = 0f;

			StartAnimation();
		}

		scrollView.RegisterCallback<PointerDownEvent>(evt =>
		{
			if (evt.pointerType != UnityEngine.UIElements.PointerType.mouse)
				return;

			animation?.Pause();
			velocity = 0f;

			pressed = true;
			dragging = false;
			pointerId = evt.pointerId;
			startPosition = evt.position;
			// 되돌아오는 중에 다시 잡으면 그 모습 그대로 이어서 끌기
			float shown = Mathf.Min(Mathf.Abs(overscroll), maxOverscroll * 0.99f);
			float pulled = maxOverscroll * (1f / (1f - shown / maxOverscroll) - 1f) / resistance;
			startOffset = scrollView.scrollOffset.y - Mathf.Sign(overscroll) * pulled;
			lastY = evt.position.y;
			lastTime = Time.realtimeSinceStartup * 1000f;
		}, TrickleDown.TrickleDown);

		scrollView.RegisterCallback<PointerMoveEvent>(evt =>
		{
			if (!pressed || evt.pointerId != pointerId)
				return;

			// 버튼을 뗀 채로 움직이면 (창 밖에서 뗀 경우 등) 끌기 종료
			if ((evt.pressedButtons & 1) == 0)
			{
				EndDrag();
				return;
			}

			float deltaY = evt.position.y - startPosition.y;
			if (!dragging && Mathf.Abs(deltaY) > dragThreshold)
			{
				dragging = true;
				scrollView.CapturePointer(pointerId);
			}

			if (dragging)
			{
				float now = Time.realtimeSinceStartup * 1000f;
				float dt = Mathf.Max(1f, now - lastTime);
				// 마우스가 아래로 가면 목록도 아래로 → 스크롤 위치는 감소
				float instant = -(evt.position.y - lastY) / dt;
				velocity = Mathf.Clamp(Mathf.Lerp(velocity, instant, 0.6f), -maxVelocity, maxVelocity);
				lastY = evt.position.y;
				lastTime = now;

				ApplyDesired(startOffset - deltaY);
				evt.StopPropagation();
			}
		}, TrickleDown.TrickleDown);

		scrollView.RegisterCallback<PointerUpEvent>(evt =>
		{
			if (evt.pointerId != pointerId)
				return;

			bool wasDragging = dragging;
			EndDrag();

			// 끌었던 경우에는 항목 탭(복사)으로 처리되지 않도록
			if (wasDragging)
				evt.StopPropagation();
		}, TrickleDown.TrickleDown);

		scrollView.RegisterCallback<PointerCaptureOutEvent>(_ =>
		{
			if (dragging)
				EndDrag();
			pressed = false;
			dragging = false;
		});

		// 마우스 휠 / 맥북 트랙패드(두 손가락) 스크롤은 직접 처리:
		// - ScrollView 기본 처리는 끝에서 튕기려다(Elastic) 트랙패드의 연속 입력과 부딪혀 마구 떨리므로
		//   항상 범위 안으로만 움직이게 함
		// - 트랙패드는 작은 값이 아주 자주 들어오므로 1단위당 이동량을 작게 잡음
		const float wheelStep = 18f;
		scrollView.RegisterCallback<WheelEvent>(evt =>
		{
			velocity = 0f;
			if (Mathf.Abs(overscroll) >= 0.5f)
				StartAnimation();

			float max = GetMaxScrollY(scrollView);
			float y = Mathf.Clamp(scrollView.scrollOffset.y + evt.delta.y * wheelStep, 0f, max);
			if (!Mathf.Approximately(y, scrollView.scrollOffset.y))
				scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, y);

			// ScrollView 기본 휠 처리(범위 밖으로 튕기는 동작)는 막음
			evt.StopImmediatePropagation();
		}, TrickleDown.TrickleDown);

		// 펼치기/접기 등으로 내용 높이가 바뀌면 범위 안으로 되돌림
		content.RegisterCallback<GeometryChangedEvent>(_ => ClampScroll(scrollView));
		scrollView.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => ClampScroll(scrollView));
	}

	/// <summary>
	/// 화면을 다시 열 때 등, 넘친 상태/움직임 없이 맨 위로 되돌립니다.
	/// </summary>
	public static void ResetScroll(ScrollView scrollView)
	{
		if (scrollView == null)
			return;
		foreach (VisualElement child in scrollView.contentContainer.Children())
		{
			child.style.translate = new StyleTranslate(StyleKeyword.Null);
		}
		scrollView.scrollOffset = Vector2.zero;
	}

	/// <summary>
	/// 카드 뒤에 부드러운 그림자를 붙입니다. (모서리 32px 카드 기준 9-slice 이미지)
	/// </summary>
	public static VisualElement AddShadow(VisualElement target, bool glow = false)
	{
		if (target == null)
			return null;

		VisualElement shadow = new VisualElement();
		shadow.AddToClassList("shadow");
		if (glow) shadow.AddToClassList("shadow--glow");
		shadow.pickingMode = PickingMode.Ignore;
		// 그림자 이미지는 2배 해상도로 만들어져 있음
		shadow.style.unitySliceScale = 0.5f;

		target.Insert(0, shadow);
		return shadow;
	}

	/// <summary>
	/// 카드 등 일반 요소를 버튼처럼 탭할 수 있게 만듭니다.
	/// (안쪽 버튼을 누른 경우와 스크롤로 끌고 간 경우는 무시)
	/// </summary>
	public static void RegisterTap(VisualElement target, Action onTap)
	{
		if (target == null || onTap == null)
			return;

		Vector2 downPosition = Vector2.zero;

		target.AddToClassList("tappable");

		target.RegisterCallback<PointerDownEvent>(evt =>
		{
			downPosition = evt.position;
			if (!IsInsideButton(evt.target as VisualElement, target))
			{
				target.AddToClassList("is-pressed");
			}
		}, TrickleDown.TrickleDown);

		target.RegisterCallback<PointerUpEvent>(_ => target.RemoveFromClassList("is-pressed"), TrickleDown.TrickleDown);
		target.RegisterCallback<PointerLeaveEvent>(_ => target.RemoveFromClassList("is-pressed"));
		target.RegisterCallback<PointerCancelEvent>(_ => target.RemoveFromClassList("is-pressed"));

		target.RegisterCallback<ClickEvent>(evt =>
		{
			if (IsInsideButton(evt.target as VisualElement, target))
				return;

			if (((Vector2)evt.position - downPosition).sqrMagnitude > tapMoveLimit * tapMoveLimit)
				return;

			onTap();
		});
	}

	private static bool IsInsideButton(VisualElement element, VisualElement stopAt)
	{
		while (element != null && element != stopAt)
		{
			// 버튼, 입력칸, no-tap 영역을 누른 경우는 카드 탭으로 보지 않음
			if (element is Button || element is TextField || element.ClassListContains("no-tap"))
				return true;

			element = element.parent;
		}
		return false;
	}
}
