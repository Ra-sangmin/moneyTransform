using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
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

	public static void ReplaceStr(this string checkValue, TextField inputField , ref string result)
	{
		double setValue = inputField.value.ToNumber();

		ReplaceStr(checkValue, setValue , ref result);
	}

	public static void ReplaceStr(this string checkValue, double setValue , ref string result)
	{
		result = result.Replace(checkValue, setValue.ToString("n0"));
	}

	/// <summary>
	/// 문자열을 숫자로 변환 (쉼표 허용, 비어 있거나 잘못된 값이면 0)
	/// </summary>
	public static double ToNumber(this string value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return 0;

		double result;
		if (double.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out result))
			return result;

		return 0;
	}

	/// <summary>
	/// 숫자 입력칸용: 숫자, 쉼표, 소수점(1개)만 남깁니다.
	/// </summary>
	public static string FilterNumber(this string value)
	{
		if (string.IsNullOrEmpty(value))
			return string.Empty;

		System.Text.StringBuilder builder = new System.Text.StringBuilder(value.Length);
		bool hasDot = false;

		foreach (char c in value)
		{
			if ((c >= '0' && c <= '9') || c == ',')
			{
				builder.Append(c);
			}
			else if (c == '.' && !hasDot)
			{
				builder.Append(c);
				hasDot = true;
			}
		}

		return builder.ToString();
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