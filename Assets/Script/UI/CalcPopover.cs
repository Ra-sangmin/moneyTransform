using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// 버튼 바로 위에 뜨는 "계산 과정" 말풍선 (누르고 있는 동안만 표시)
/// </summary>
public class CalcPopover
{
	public struct Line
	{
		public string label;
		public string value;
		public bool highlight;

		public Line(string label, string value, bool highlight = false)
		{
			this.label = label;
			this.value = value;
			this.highlight = highlight;
		}
	}

	private readonly VisualElement layer;
	private readonly VisualElement popover;
	private readonly VisualElement rows;
	private readonly VisualElement arrow;

	public bool IsShowing => popover.parent != null;

	public CalcPopover(VisualElement layer)
	{
		this.layer = layer;

		popover = new VisualElement();
		popover.AddToClassList("calc-pop");
		popover.pickingMode = PickingMode.Ignore;

		Label title = new Label("계산 과정");
		title.AddToClassList("calc-pop-title");
		title.pickingMode = PickingMode.Ignore;

		rows = new VisualElement();
		rows.pickingMode = PickingMode.Ignore;

		arrow = new VisualElement();
		arrow.AddToClassList("calc-arrow");
		arrow.pickingMode = PickingMode.Ignore;

		UIUtils.AddShadow(popover);
		popover.Add(title);
		popover.Add(rows);
		popover.Add(arrow);
	}

	/// <summary>
	/// anchor(버튼) 바로 위, 오른쪽 끝을 맞춰서 표시
	/// </summary>
	public void Show(VisualElement anchor, IList<Line> lines)
	{
		if (layer == null || anchor == null)
			return;

		rows.Clear();
		foreach (Line line in lines)
		{
			VisualElement row = new VisualElement();
			row.AddToClassList("calc-row");
			row.pickingMode = PickingMode.Ignore;

			Label label = new Label(line.label);
			label.AddToClassList("calc-label");
			label.pickingMode = PickingMode.Ignore;

			Label value = new Label(line.value);
			value.AddToClassList("calc-value");
			value.EnableInClassList("calc-value--hl", line.highlight);
			value.pickingMode = PickingMode.Ignore;

			row.Add(label);
			row.Add(value);
			rows.Add(row);
		}

		// 버튼 위치를 레이어 좌표로 변환
		Rect bounds = anchor.worldBound;
		Vector2 topRight = layer.WorldToLocal(new Vector2(bounds.xMax, bounds.yMin));
		float layerWidth = layer.resolvedStyle.width;
		float layerHeight = layer.resolvedStyle.height;

		popover.style.right = Mathf.Max(8f, layerWidth - topRight.x);
		popover.style.bottom = layerHeight - topRight.y + 16f;

		// 꼬리는 버튼 가운데를 가리키도록
		float anchorWidth = layer.WorldToLocal(new Vector2(bounds.xMax, 0)).x - layer.WorldToLocal(new Vector2(bounds.xMin, 0)).x;
		arrow.style.right = anchorWidth * 0.5f - 11f;

		popover.RemoveFromClassList("calc-pop--show");
		layer.Add(popover);
		popover.BringToFront();
		popover.schedule.Execute(() =>
		{
			if (IsShowing)
				popover.AddToClassList("calc-pop--show");
		}).StartingIn(16);
	}

	public void Hide()
	{
		popover.RemoveFromClassList("calc-pop--show");
		popover.RemoveFromHierarchy();
	}
}
