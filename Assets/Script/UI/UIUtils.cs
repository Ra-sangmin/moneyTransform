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
			if (element is Button)
				return true;

			element = element.parent;
		}
		return false;
	}
}
