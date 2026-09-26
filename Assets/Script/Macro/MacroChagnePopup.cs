using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

/// <summary>
/// 매크로 편집 화면 (UI Toolkit) - 입력하는 즉시 저장
/// </summary>
public class MacroChagnePopup
{
	private readonly VisualElement screen;
	private readonly TextField titleInputField;
	private readonly TextField contensInputField;

	private readonly Action<MacroData> onClose;

	private MacroData macroData;

	/// <summary> 내용 입력칸의 마지막 커서 위치 (자동 입력 문구를 끼워 넣을 자리) </summary>
	private int lastCursorIndex = -1;

	public MacroChagnePopup(VisualElement root, IReadOnlyList<string> tokens, Action<MacroData> onClose)
	{
		this.onClose = onClose;

		screen = root.Q<VisualElement>("edit-screen");
		titleInputField = root.Q<TextField>("edit-title");
		contensInputField = root.Q<TextField>("edit-content");

		titleInputField.RegisterValueChangedCallback(evt => TitleInputChangeOn(evt.newValue));
		contensInputField.RegisterValueChangedCallback(evt => ContensInputChangeOn(evt.newValue));
		contensInputField.RegisterCallback<FocusOutEvent>(_ => lastCursorIndex = contensInputField.cursorIndex);

		root.Q<Button>("edit-done").clicked += Close;

		VisualElement tokenRow = root.Q<VisualElement>("token-row");
		foreach (string token in tokens)
		{
			string insertToken = token;
			Button tokenBtn = new Button(() => InsertToken(insertToken)) { text = token.Trim('{', '}') };
			tokenBtn.AddToClassList("token");
			tokenRow.Add(tokenBtn);
		}
	}

	public void Open(MacroData macroData)
	{
		this.macroData = macroData;
		lastCursorIndex = -1;

		titleInputField.SetValueWithoutNotify(macroData.title ?? string.Empty);
		contensInputField.SetValueWithoutNotify(macroData.contens ?? string.Empty);

		UIUtils.Show(screen);
	}

	void Close()
	{
		UIUtils.Hide(screen);

		MacroData closedData = macroData;
		macroData = null;

		onClose?.Invoke(closedData);
	}

	void TitleInputChangeOn(string str)
	{
		if (macroData == null)
			return;

		macroData.title = str;
		MacroManager.Instance.ResetMacroData(macroData);
	}

	void ContensInputChangeOn(string str)
	{
		if (macroData == null)
			return;

		macroData.contens = str;
		MacroManager.Instance.ResetMacroData(macroData);
	}

	/// <summary>
	/// {상품가격} 같은 자동 입력 문구를 커서 위치(없으면 끝)에 넣습니다.
	/// </summary>
	void InsertToken(string token)
	{
		string value = contensInputField.value ?? string.Empty;
		int index = (lastCursorIndex >= 0 && lastCursorIndex <= value.Length) ? lastCursorIndex : value.Length;

		contensInputField.value = value.Insert(index, token);
		lastCursorIndex = index + token.Length;
	}
}
