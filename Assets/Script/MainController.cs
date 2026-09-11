using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Threading.Tasks;

[System.Serializable]
public class EstimateResponseWrapper
{
	public bool success;
	public ExchangeOnlyData data;
}

[System.Serializable]
public class ExchangeOnlyData
{
	public float baseExchangeRate;
	public float additionalRate;
	public int rateBasisUnit;
	public float finalDisplayRate;
	public bool exchangeRateFetchFailed;
}

public class MainController : MonoBehaviour
{

	/// <summary> 홈페이지 서버의 환율/견적 API URL </summary>
	private string apiUrl = "http://www.mikushop.co.kr/api/estimate";

	/// <summary> 현재 환율 (1엔 기준 또는 계산에 쓰이는 최종 기준 환율) </summary>
	private float exchangeRate = 0;
	/// <summary> 현재 환률 Text </summary>
	[SerializeField] private Text exchangeRateText;

	/// <summary> 추가 증가액 </summary>
	[SerializeField] private InputField addRateInput;
	/// <summary> 상품가격 </summary>
	[SerializeField] private InputField salePriceInput;
	/// <summary> 결제수수료 Input </summary>
	[SerializeField] private InputField paymentInput;
	/// <summary> 일내배송료 Input </summary>
	[SerializeField] private InputField dailyTaxInput;
	/// <summary> 추가문구 1 Input </summary>
	[SerializeField] private InputField addStringInput_0;
	/// <summary> 추가문구 2 Input </summary>
	[SerializeField] private InputField addStringInput_1;

	/// <summary> 상품 갯수 </summary>
	private int quantityCount = 1;
	[SerializeField] private Text quantityCountText;
	/// <summary> 대행 수수료 </summary>
	private int texCount = 300;
	/// <summary> 대행 수수료 Text </summary>
	[SerializeField] private Text texCountText;

	/// <summary> 합계 계산 공식 알림 Text </summary>
	[SerializeField] private Text resultExDoingText;
	/// <summary> 합계 계산 공식 Text </summary>
	[SerializeField] private Text resultDoingText;
	/// <summary> 합계 Text </summary>
	[SerializeField] private Text resultCountText;
	/// <summary> 합계 값 </summary>
	private float resultCount;

	/// <summary> 시간 관리 메니져 </summary>
	[SerializeField] private TimeController timeController;

	[SerializeField] GetImageController getImageController;

	List<string> replaceStrList = new List<string>();

	// Use this for initialization
	void Start()
	{
		timeController.timeOn += TimeOn;

		PopupManager.Instance.SetCanvasParant(transform);

		replaceStrList = new List<string>()
		{
			"{상품가격}",
			"{결제수수료}",
			"{합계}",
			"{추가문구1}",
			"{추가문구2}",
		};
	}

	/// <summary>
	/// 일정 시간마다 호출
	/// </summary>
	void TimeOn()
	{
		ResetExchangeRate();
	}

	/// <summary>
	/// 환율 리셋 (홈페이지 서버 API 호출 방식으로 변경)
	/// </summary>
	public void ResetExchangeRate()
	{
		StartCoroutine(GetExchangeRateFromServer());
	}

	IEnumerator GetExchangeRateFromServer()
	{
		using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
		{
			yield return request.SendWebRequest();

			if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
			{
				Debug.LogError($"서버 통신 에러: {request.error}");
			}
			else
			{
				string jsonResponse = request.downloadHandler.text;
				try
				{
					EstimateResponseWrapper res = JsonUtility.FromJson<EstimateResponseWrapper>(jsonResponse);
					if (res != null && res.success)
					{
						// 서버에서 계산해둔 baseExchangeRate (또는 필요에 따라 finalDisplayRate 활용 가능)
						// 기존 로직이 (현재 환율 + 추가금액) 형태로 계산하므로 baseExchangeRate를 대입합니다.
						exchangeRate = res.data.baseExchangeRate;

						// 서버에 설정된 추가 증가액이 있다면 input에도 동기화 (원하는 경우 주석 해제)
						// if (addRateInput != null && string.IsNullOrEmpty(addRateInput.text))
						// {
						//     addRateInput.text = (res.data.additionalRate * res.data.rateBasisUnit).ToString();
						// }

						if (exchangeRateText != null)
						{
							// 화면에 표시할 때는 기준 단위(예: 100엔 기준 최종 환율)를 보여줄 수도 있습니다.
							exchangeRateText.text = res.data.finalDisplayRate.ToString();
						}

						Debug.Log($"서버 환율 갱신 성공: 기본환율({exchangeRate}), 최종표시환율({res.data.finalDisplayRate})");

						// 환율이 갱신된 후 자동으로 견적 재계산 수행
						CalculationBtn();
					}
					else
					{
						Debug.LogError("서버에서 환율 데이터를 정상적으로 반환하지 않았습니다.");
					}
				}
				catch (Exception e)
				{
					Debug.LogError($"JSON 파싱 에러: {e.Message}");
				}
			}
		}
	}

	public void PaymentValueReset()
	{
		if (string.IsNullOrEmpty(salePriceInput.text))
		{
			paymentInput.text = null;
			return;
		}

		int salePrice = int.Parse(salePriceInput.text);

		if (salePrice == 0)
		{
			paymentInput.text = null;
			return;
		}

		int payment = salePrice == 0 ? 0 :
						salePrice < 30000 ? 220 : 330;
		paymentInput.text = payment.ToString();

		CalculationBtn();
	}

	/// <summary>
	/// 상품 개수 증가
	/// </summary>
	public void QuantityCountUp()
	{
		quantityCount++;
		QuantityCountReset();
	}

	/// <summary>
	/// 상품 개수 감소
	/// </summary>
	public void QuantityCountDown()
	{
		quantityCount--;

		if (quantityCount < 0)
		{
			quantityCount = 0;
		}

		QuantityCountReset();
	}

	/// <summary>
	/// 상품 개수 리셋
	/// </summary>
	void QuantityCountReset()
	{
		quantityCountText.text = quantityCount.ToString();
		TexCountReset();

		CalculationBtn();
	}

	/// <summary>
	/// 대행 수수료 리셋
	/// </summary>
	void TexCountReset()
	{
		texCount = quantityCount == 0 ? 0 :
					quantityCount < 4 ? texCount = 300 : quantityCount * 100;
		texCountText.text = texCount.ToString();
	}

	/// <summary>
	/// 계산시작 버튼 클릭시 호출
	/// </summary>
	public void CalculationBtn()
	{
		float addRate = addRateInput.text != string.Empty ? float.Parse(addRateInput.text) : 0;
		addRate *= 0.01f;

		float salePrice = salePriceInput.text != string.Empty ? float.Parse(salePriceInput.text) : 0;

		float payment = paymentInput.text != string.Empty ? float.Parse(paymentInput.text) : 0;
		float dailyTax = dailyTaxInput.text != string.Empty ? float.Parse(dailyTaxInput.text) : 0;

		string resultDoingStr = string.Format("( {0} + {1} ) X ({2:n0} + {3} + {4} + {5})", exchangeRate, addRate, salePrice, payment, dailyTax, texCount);
		if (resultDoingText != null) resultDoingText.text = resultDoingStr;

		//최종 계산 = ( 현재 환률 + 추가 금액 ) * ( 상품 가격 + 결제수수료 + 일내배송료 + 대행 수수료 )
		resultCount = ((exchangeRate + addRate) * (salePrice + payment + dailyTax + texCount));

		resultCount = Mathf.CeilToInt(resultCount * 0.1f) * 10;

		if (resultCountText != null) resultCountText.text = string.Format("{0:n0}원", resultCount);
	}

	// Update is called once per frame
	void Update()
	{
	}

	public void CopyToResult()
	{
		GUIUtility.systemCopyBuffer = resultCountText.text;

		string warningMessage = $"{resultCountText.text} 복사 완료";
		PopupManager.Instance.WarningPopupCreate(warningMessage);
	}

	public void CopyToText(int index)
	{
		Debug.LogWarning(index);

		GUIUtility.systemCopyBuffer = resultCount.ToString("n0");
	}

	public void CopyToClipboard(string str)
	{
		GUIUtility.systemCopyBuffer = str;
	}

	public void CopyToTextPopupOn()
	{

	}

	public void CopyToMacroTextOn(MacroItem macroItem)
	{
		string content = macroItem.macroData.contens;

		foreach (var replaceStr in replaceStrList)
		{
			if (!content.Contains(replaceStr))
				continue;

			InputField inputField = null;
			float setValue = 0;
			string setString = string.Empty;

			switch (replaceStr)
			{
				case "{상품가격}":
					inputField = salePriceInput;
					break;
				case "{결제수수료}":
					inputField = paymentInput;
					break;
				case "{합계}":
					setValue = resultCount;
					break;
				case "{추가문구1}":
					setString = addStringInput_0.text;
					break;
				case "{추가문구2}":
					setString = addStringInput_1.text;
					break;
			}

			if (inputField != null)
			{
				replaceStr.ReplaceStr(inputField, ref content);
			}
			else if (!string.IsNullOrEmpty(setString))
			{
				content = content.Replace(replaceStr, setString);
			}
			else
			{
				replaceStr.ReplaceStr(setValue, ref content);
			}
		}

		GUIUtility.systemCopyBuffer = content;

		string warningMessage = $"{macroItem.macroData.title} 복사 완료";
		PopupManager.Instance.WarningPopupCreate(warningMessage);
	}
}