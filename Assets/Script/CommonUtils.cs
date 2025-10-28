using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
//using Watermelon;

public static class CommonUtils
{
    /// <summary>
    /// 리스트 랜덤으로 섞기
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    public static void Shuffle<T>(this List<T> list)
    {
        var temp = list.OrderBy(item => Guid.NewGuid()).ToList();
        list.Clear();
        list.AddRange(temp);
    }

	///// <summary>
	///// 동작중인 Tween을 제거
	///// </summary>
	///// <param name="tween"></param>
	//public static void TweenKill(this Tween tween)
	//{
	//    if (tween != null && tween.isActiveAndEnabled)
	//    {
	//        tween.TweenKill();
	//    }
	//}

	public static void ReplaceStr(this string checkValue, InputField inputField , ref string result)
	{
		float setValue = inputField.text != string.Empty ? float.Parse(inputField.text) : 0;

		ReplaceStr(checkValue, setValue , ref result);
	}

	public static void ReplaceStr(this string checkValue, float setValue , ref string result)
	{
		result = result.Replace(checkValue, setValue.ToString("n0"));
	}

	public static string SetTimeStr(this int timeValue)
	{
		int min = (int)(timeValue / 60.0f);
		int sec = (int)(timeValue % 60.0f);

		string resultValue = string.Empty;

		if (min != 0)
		{
			resultValue += $"{min} m ";
		}

		resultValue += $"{sec} s";

        return resultValue;
	}

	/// <summary>
	/// Stretch 설정으로 변경
	/// </summary>
	/// <param name="tween"></param>
	public static void SetStretch(this RectTransform rect)
	{
		rect.anchoredPosition3D = new Vector3(0, 0, 0);
		rect.anchorMin = new Vector2(0, 0);   // 왼쪽 아래
		rect.anchorMax = new Vector2(1, 1);   // 오른쪽 위
		rect.offsetMin = Vector2.zero;        // 왼쪽/아래 여백 0
		rect.offsetMax = Vector2.zero;
	}
}