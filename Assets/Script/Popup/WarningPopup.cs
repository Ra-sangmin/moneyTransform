using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// 잠깐 떴다 사라지는 알림 (UI Toolkit 토스트)
/// </summary>
public class WarningPopup
{
	public VisualElement Root { get; private set; }

	private readonly Label contensText;

	public WarningPopup(VisualElement layer)
	{
		// 이전 알림이 남아 있으면 바로 지웁니다.
		List<VisualElement> oldToasts = layer.Query<VisualElement>(className: "toast-wrap").ToList();
		foreach (VisualElement oldToast in oldToasts)
		{
			oldToast.RemoveFromHierarchy();
		}

		Root = new VisualElement();
		Root.AddToClassList("toast-wrap");
		Root.pickingMode = PickingMode.Ignore;

		contensText = new Label();
		contensText.AddToClassList("toast");
		contensText.pickingMode = PickingMode.Ignore;

		Root.Add(contensText);
		layer.Add(Root);
	}

	public void DataSet(string contensStr)
	{
		contensText.text = contensStr;

		// 나타남 → 1.6초 후 위로 사라짐 → 제거
		Root.schedule.Execute(() => Root.AddToClassList("toast-wrap--show")).StartingIn(16);
		Root.schedule.Execute(() => Root.AddToClassList("toast-wrap--hide")).StartingIn(1600);
		Root.schedule.Execute(() => Root.RemoveFromHierarchy()).StartingIn(2300);
	}
}
