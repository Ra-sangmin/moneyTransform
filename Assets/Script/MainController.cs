using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
//using waqashaxhmi.AndroidNativePlugin;

public class MainController : MonoBehaviour {

	/// <summary> 환률 정보 URL </summary>
	private	string url = "https://finance.naver.com/marketindex/exchangeDetail.nhn?marketindexCd=FX_JPYKRW";
	/// <summary> 현재 환률 </summary>
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
    /// <summary> 관세률 Input </summary>
    [SerializeField] private InputField taxInput;
    /// <summary> 관세 결과 Input </summary>
    [SerializeField] private InputField taxResultInput;
    /// <summary> 관세 결과 출력 Toggle </summary>
    [SerializeField] private Toggle taxResultToggle;
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

	// Use this for initialization
	void Start () {

		timeController.timeOn += TimeOn;

		PopupManager.Instance.SetCanvasParant(transform);

//#if (UNITY_ANDROID && !UNITY_EDITOR)
//		AndroidNativeController.OnFileSelectSuccessEvent = OnSuccess;
//#endif
    }

	/// <summary>
	/// 일정 시간마다 호출
	/// </summary>
	void TimeOn()
	{
		ResetExchangeRate ();
    }

	/// <summary>
	/// 환률 리셋
	/// </summary>
	public void ResetExchangeRate()
	{
		StartCoroutine (GetExchangeRate ());
	}

	/// <summary>
	/// 환률 리셋
	/// </summary>
	IEnumerator GetExchangeRate()
	{
		WWW www = new WWW (url);

		yield return www;

		int seachIndex = www.text.LastIndexOf ("JPY<");
		int startIndex = 0;
		for (int i = seachIndex; i >= 0; i--) {

			if(www.text[i] == '<')
			{
				startIndex = i;
				break;
			}
		}

		int lastIndex = www.text.IndexOf('>',startIndex);

		string subStrText = www.text.Substring (startIndex, (lastIndex +1 - startIndex));

		int exchangeRateStartIndex = subStrText.IndexOf ('"');
		int exchangeRateLastIndex = subStrText.IndexOf ('"',exchangeRateStartIndex+1);

		string exchangeRateStr = subStrText.Substring (exchangeRateStartIndex+1, (exchangeRateLastIndex - (exchangeRateStartIndex+1)));
		exchangeRate = float.Parse (exchangeRateStr);

		SetExchangeRateText ();
	}

	/// <summary>
	/// 환률 Text 변경
	/// </summary>
	void SetExchangeRateText()
	{
		exchangeRateText.text = (exchangeRate*100).ToString();
	}

	public void PaymentValueReset()
	{
		if (string.IsNullOrEmpty(salePriceInput.text)) 
		{
            paymentInput.text = null;
            taxResultInput.text = null;
            return;
        }
	
		int salePrice = int.Parse(salePriceInput.text);

		if (salePrice == 0)
		{
            paymentInput.text = null;
            taxResultInput.text = null;
            return;
		}

		int payment = salePrice == 0 ? 0 :
						salePrice < 30000 ? 220 : 330;
		paymentInput.text = payment.ToString();
		
		float tax = int.Parse(taxInput.text);
		float resultValue = salePrice * (tax * 0.01f)* exchangeRate;
		taxResultInput.text = resultValue.ToString();
    }

	/// <summary>
	/// 상품 개수 증가
	/// </summary>
	public void QuantityCountUp()
	{
		quantityCount++;
		QuantityCountReset ();
	}

	/// <summary>
	/// 상품 개수 감소
	/// </summary>
	public void QuantityCountDown()
	{
		quantityCount--;

		if(quantityCount < 0)
		{
			quantityCount = 0;
		}

		QuantityCountReset ();
	}

	/// <summary>
	/// 상품 개수 리셋
	/// </summary>
	void QuantityCountReset()
	{
		quantityCountText.text = quantityCount.ToString();
		TexCountReset ();
	}

	/// <summary>
	/// 대행 수수료 리셋
	/// </summary>
	void TexCountReset()
	{
		texCount = 	quantityCount == 0 ? 0 : 
					quantityCount < 4 ? texCount = 300 : quantityCount * 100;
		texCountText.text = texCount.ToString();
	}

    /// <summary>
    /// 관세 활성화 리셋
    /// </summary>
    public void TexPanelReset()
    {
		taxInput.transform.parent.gameObject.SetActive(taxResultToggle.isOn);

		string setText = string.Empty;

		if (taxResultToggle.isOn) 
		{
            setText = "((1엔기준 환율 +추가금액) X (상품 가격  + 결제Tex + 일내배송료 + 대행Tex)) + 관세";
        }
		else
		{
            setText = "(1엔기준 환율 +추가금액) X (상품 가격  + 결제Tex + 일내배송료 + 대행Tex)";
        }

        resultExDoingText.text = setText;
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

		if (taxResultToggle.isOn)
		{
            float tax = taxResultInput.text != string.Empty ? float.Parse(taxResultInput.text) : 0;

            string resultDoingStr = string.Format("(( {0} + {1} ) X ({2:n0} + {3} + {4} + {5})) + {6}", exchangeRate, addRate, salePrice, payment, dailyTax, texCount, tax);
            resultDoingText.text = resultDoingStr;
            //최종 계산 = ( 현재 환률 + 추가 금액 ) * ( 상품 가격 + 결제수수료 + 일내배송료 + 대행 수수료 )
            resultCount = ((exchangeRate + addRate) * (salePrice + payment + dailyTax + texCount))+ tax;
            resultCountText.text = string.Format("= {0:n0} 원", resultCount);
        }
		else 
		{
            string resultDoingStr = string.Format("( {0} + {1} ) X ({2:n0} + {3} + {4} + {5})", exchangeRate, addRate, salePrice, payment, dailyTax, texCount);
            resultDoingText.text = resultDoingStr;
            //최종 계산 = ( 현재 환률 + 추가 금액 ) * ( 상품 가격 + 결제수수료 + 일내배송료 + 대행 수수료 )
            resultCount = ((exchangeRate + addRate) * (salePrice + payment + dailyTax + texCount));

			resultCount = Mathf.CeilToInt(resultCount * 0.1f) * 10;

            resultCountText.text = string.Format("{0:n0}원", resultCount);
        }
    }

#if (UNITY_ANDROID && !UNITY_EDITOR)

	//void OnTest()
	//{
	//	AndroidNativePluginLibrary.Instance.OpenGallary();
	//}

	//private void OnSuccess(string path)
	//{
	//	AndroidNativePluginLibrary.Instance.ShowToast("File Selected:" + path);
	//	getImageController.SetTexture(path);
	//}

#endif

	
	// Update is called once per frame
	void Update () {
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

}