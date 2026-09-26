using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;

[System.Serializable]
public class EstimateResponseWrapper
{
	public bool success;
	public ExchangeOnlyData data;
}

[System.Serializable]
public class ExchangeOnlyData
{
	public double baseExchangeRate;
	public double additionalRate;
	public int rateBasisUnit;
	public double finalDisplayRate;
	public bool exchangeRateFetchFailed;
}

/// <summary>
/// 메인 화면 (UI Toolkit)
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainController : MonoBehaviour
{
	/// <summary> 홈페이지 서버의 환율/견적 API URL </summary>
	private string apiUrl = "https://www.mikushop.co.kr/api/estimate";
	//private string apiUrl = "http://localhost:3000/api/estimate";

	/// <summary> 현재 환율 (1엔 기준) </summary>
	private double exchangeRate = 0;
	/// <summary> 환율을 한 번이라도 정상적으로 받았는지 여부 (받기 전에는 계산/복사 차단) </summary>
	private bool exchangeRateLoaded = false;
	/// <summary> 환율 요청 진행 중 여부 (중복 요청 방지) </summary>
	private bool exchangeRateRequesting = false;
	/// <summary> 환율을 아직 못 받았을 때 재시도 간격(초) </summary>
	private const float exchangeRateRetryDelay = 10f;
	/// <summary> 환율 표시 기준 단위 (예: 100엔) </summary>
	private int rateBasisUnit = 100;
	/// <summary> 마지막 환율 갱신 시각 </summary>
	private DateTime lastRateUpdated;

	/// <summary> 상품 갯수 </summary>
	private int quantityCount = 1;
	/// <summary> 대행 수수료 </summary>
	private int texCount = 300;
	/// <summary> 합계 값 </summary>
	private double resultCount;

	// 계산 과정 표시용 (마지막 계산 값)
	private double lastAddRate;
	private double lastSalePrice;
	private double lastPayment;
	private double lastDailyTax;
	private double lastRawResult;

	[SerializeField] private UIDocument uiDocument;
	/// <summary> 시간 관리 메니져 </summary>
	[SerializeField] private TimeController timeController;
	[SerializeField] private GetImageController getImageController;

	private VisualElement root;

	private Label exchangeRateLabel;
	private VisualElement rateBreakdown;
	private Label rateBaseLabel;
	private Label rateAddLabel;
	private Label rateUpdatedLabel;
	private Label quantityLabel;
	private Label texCountLabel;
	private Button calcBtn;
	private CalcPopover calcPopover;
	private Label resultCountLabel;
	private Label resultHintLabel;
	private VisualElement statusDot;

	/// <summary> 추가 증가액 </summary>
	private TextField addRateInput;
	/// <summary> 상품가격 </summary>
	private TextField salePriceInput;
	/// <summary> 결제수수료 </summary>
	private TextField paymentInput;
	/// <summary> 일내배송료 </summary>
	private TextField dailyTaxInput;
	/// <summary> 추가문구 1 </summary>
	private TextField addStringInput_0;
	/// <summary> 추가문구 2 </summary>
	private TextField addStringInput_1;

	private MacroPopup macroPopup;

	// 저장 키 (추가 증가액 / 추가문구)
	private const string addRateKey = "addRateValue";
	private const string addString0Key = "addString0";
	private const string addString1Key = "addString1";

	/// <summary> 안전 영역(노치) 여백을 적용할 요소 </summary>
	private readonly List<VisualElement> safeAreaTargets = new List<VisualElement>();
	private Rect lastSafeArea;
	private Vector2Int lastScreenSize;

	// 가로(맥북) / 세로(아이폰) 레이아웃
	/// <summary> 가로 화면 기준 해상도 </summary>
	private static readonly Vector2Int landscapeReference = new Vector2Int(1920, 1080);
	/// <summary> 세로 화면 기준 해상도 (폭을 좁게 잡아 폰에서 글자/버튼이 충분히 크게 보이도록) </summary>
	private static readonly Vector2Int portraitReference = new Vector2Int(900, 1600);
	private bool? isPortraitLayout;
	private Vector2Int originalReference;

	private readonly List<string> replaceStrList = new List<string>()
	{
		"{상품가격}",
		"{결제수수료}",
		"{합계}",
		"{추가문구1}",
		"{추가문구2}",
	};

	public IReadOnlyList<string> ReplaceStrList => replaceStrList;

	void Awake()
	{
		if (uiDocument == null)
			uiDocument = GetComponent<UIDocument>();

#if UNITY_STANDALONE && !UNITY_EDITOR
		// 맥 앱: 이전 버전은 전체 화면이었고 Unity 가 그 설정을 기억하므로,
		// 이번 업데이트 후 처음 한 번은 크기 조절 가능한 창 모드로 바꿔 줌 (이후엔 사용자가 정한 창 크기 유지)
		const string windowModeKey = "windowModeApplied";
		if (PlayerPrefs.GetInt(windowModeKey, 0) == 0)
		{
			PlayerPrefs.SetInt(windowModeKey, 1);
			PlayerPrefs.Save();
			Screen.SetResolution(1280, 800, FullScreenMode.Windowed);
		}
#endif
	}

	void Start()
	{
		root = uiDocument.rootVisualElement;

		if (uiDocument.panelSettings != null)
			originalReference = uiDocument.panelSettings.referenceResolution;
		UpdateOrientationLayout();

		BindUI();

		// 카드마다 부드러운 그림자, 합계 카드는 핑크 글로우
		root.Query<VisualElement>(className: "card").ForEach(card => UIUtils.AddShadow(card));
		UIUtils.AddShadow(root.Q<VisualElement>("result-card"), true);
		root.Query<VisualElement>(className: "back-btn").ForEach(btn => UIUtils.AddShadow(btn));

		timeController.timeOn += TimeOn;

		PopupManager.Instance.SetRoot(root.Q<VisualElement>("popup-layer"));

		getImageController.Init(root.Q<VisualElement>("bg"), root.Q<VisualElement>("app"), root);

		macroPopup = new MacroPopup(root, this);

		root.Query<VisualElement>(className: "safe-pad").ForEach(e => safeAreaTargets.Add(e));
		root.RegisterCallback<GeometryChangedEvent>(_ => ApplySafeArea());

		QuantityCountReset();
	}

	void Update()
	{
		UpdateOrientationLayout();

		if (Screen.safeArea != lastSafeArea || Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
		{
			ApplySafeArea();
		}
	}

	/// <summary>
	/// 앱이 백그라운드에서 돌아오면 환율을 바로 갱신
	/// </summary>
	void OnApplicationPause(bool pause)
	{
		if (pause)
		{
			PlayerPrefs.Save();
		}
		else
		{
			ResetExchangeRate();
		}
	}

	void OnDestroy()
	{
		// 에디터에서 플레이를 멈추면 PanelSettings 에셋의 기준 해상도를 원래대로 되돌림
		if (uiDocument != null && uiDocument.panelSettings != null && originalReference != Vector2Int.zero)
			uiDocument.panelSettings.referenceResolution = originalReference;
	}

	/// <summary>
	/// 화면이 세로로 길면(아이폰) 세로 레이아웃, 가로로 길면(맥북) 가로 레이아웃으로 바꿉니다.
	/// </summary>
	void UpdateOrientationLayout()
	{
		if (root == null)
			return;

		bool portrait = Screen.height > Screen.width;
		if (isPortraitLayout == portrait)
			return;

		isPortraitLayout = portrait;

		if (uiDocument.panelSettings != null)
			uiDocument.panelSettings.referenceResolution = portrait ? portraitReference : landscapeReference;

		VisualElement app = root.Q<VisualElement>("app");
		app?.EnableInClassList("portrait", portrait);
		app?.EnableInClassList("landscape", !portrait);

		// 세로 화면에서는 적용 환율 카드를 견적 입력보다 위로 (가로 화면에서는 오른쪽 열 맨 위)
		VisualElement rateCard = root.Q<VisualElement>("rate-card");
		VisualElement columns = root.Q<VisualElement>(className: "columns");
		VisualElement rightCol = root.Q<VisualElement>("right-col");
		if (rateCard != null && columns != null && rightCol != null)
		{
			if (portrait)
				columns.Insert(0, rateCard);
			else
				rightCol.Insert(0, rateCard);
		}

		// 레이아웃이 바뀌면 떠 있던 계산 과정 말풍선은 닫기
		calcPopover?.Hide();
	}

	#region UI 연결

	void BindUI()
	{
		exchangeRateLabel = root.Q<Label>("exchange-rate");
		rateBreakdown = root.Q<VisualElement>("rate-breakdown");
		rateBaseLabel = root.Q<Label>("rate-base");
		rateAddLabel = root.Q<Label>("rate-add");
		rateUpdatedLabel = root.Q<Label>("rate-updated");
		quantityLabel = root.Q<Label>("quantity");
		texCountLabel = root.Q<Label>("tex-count");
		resultCountLabel = root.Q<Label>("result-count");
		resultHintLabel = root.Q<Label>("result-hint");
		statusDot = root.Q<VisualElement>("status-dot");

		addRateInput = SetupNumberField("add-rate", CalculationBtn);
		salePriceInput = SetupNumberField("sale-price", PaymentValueReset);
		paymentInput = SetupNumberField("payment", CalculationBtn);
		dailyTaxInput = SetupNumberField("daily-tax", CalculationBtn);

		// 입력 줄 어디를 눌러도 그 줄의 입력칸에 바로 입력되도록 (모바일 터치 영역 확대)
		root.Query<VisualElement>(className: "form-row").ForEach(row =>
		{
			TextField rowField = row.Q<TextField>();
			if (rowField == null)
				return;

			row.RegisterCallback<ClickEvent>(evt =>
			{
				VisualElement target = evt.target as VisualElement;
				if (target == rowField || (target != null && rowField.Contains(target)))
					return;

				rowField.Focus();
			});
		});

		// 추가문구(매크로 화면)와 추가 증가액은 앱을 다시 켜도 남도록 저장
		addStringInput_0 = SetupSavedTextField("add-string-0", addString0Key);
		addStringInput_1 = SetupSavedTextField("add-string-1", addString1Key);
		addRateInput.SetValueWithoutNotify(PlayerPrefs.GetString(addRateKey, "0"));
		addRateInput.RegisterValueChangedCallback(evt => PlayerPrefs.SetString(addRateKey, addRateInput.value));
		addRateInput.RegisterCallback<FocusOutEvent>(_ => PlayerPrefs.Save());

		root.Q<Button>("clear-btn").clicked += ClearQuote;

		root.Q<Button>("qty-up").clicked += QuantityCountUp;
		root.Q<Button>("qty-down").clicked += QuantityCountDown;
		root.Q<Button>("macro-btn").clicked += () => macroPopup.Open();
		root.Q<Button>("bg-btn").clicked += () => PopupManager.Instance.BackgroundPopupCreate(getImageController);

		UIUtils.RegisterTap(root.Q<VisualElement>("rate-card"), ResetExchangeRate);
		UIUtils.RegisterTap(root.Q<VisualElement>("result-card"), CopyToResult);

		// 계산 과정 보기: 누르고 있는 동안만 말풍선 표시, 손을 떼면 사라짐
		calcBtn = root.Q<Button>("calc-btn");
		calcPopover = new CalcPopover(root.Q<VisualElement>("popup-layer"));
		calcBtn.RegisterCallback<PointerDownEvent>(_ => ShowCalcPopover(), TrickleDown.TrickleDown);
		calcBtn.RegisterCallback<PointerUpEvent>(_ => calcPopover.Hide(), TrickleDown.TrickleDown);
		calcBtn.RegisterCallback<PointerCancelEvent>(_ => calcPopover.Hide());
		calcBtn.RegisterCallback<PointerCaptureOutEvent>(_ => calcPopover.Hide());
		root.RegisterCallback<PointerUpEvent>(_ => calcPopover.Hide(), TrickleDown.TrickleDown);
	}

	/// <summary>
	/// 값이 저장되는 글자 입력칸 설정
	/// </summary>
	TextField SetupSavedTextField(string name, string key)
	{
		TextField field = root.Q<TextField>(name);
		field.SetValueWithoutNotify(PlayerPrefs.GetString(key, string.Empty));
		field.RegisterValueChangedCallback(evt => PlayerPrefs.SetString(key, evt.newValue ?? string.Empty));
		field.RegisterCallback<FocusOutEvent>(_ => PlayerPrefs.Save());
		return field;
	}

	/// <summary>
	/// 숫자 입력칸 설정 (숫자/소수점/쉼표만 허용)
	/// </summary>
	TextField SetupNumberField(string name, Action onChanged)
	{
		TextField field = root.Q<TextField>(name);
		field.keyboardType = TouchScreenKeyboardType.DecimalPad;
		field.RegisterValueChangedCallback(evt =>
		{
			string filtered = CommonUtils.FilterNumber(evt.newValue);
			if (filtered != evt.newValue)
			{
				field.SetValueWithoutNotify(filtered);
			}
			onChanged();
		});
		return field;
	}

	/// <summary>
	/// 노치/홈바 영역을 피하도록 여백 적용
	/// </summary>
	void ApplySafeArea()
	{
		if (root == null || root.panel == null)
			return;

		float panelWidth = root.resolvedStyle.width;
		if (float.IsNaN(panelWidth) || panelWidth <= 0 || Screen.width <= 0)
			return;

		Rect safeArea = Screen.safeArea;
		lastSafeArea = safeArea;
		lastScreenSize = new Vector2Int(Screen.width, Screen.height);

		float scale = panelWidth / Screen.width;
		float left = safeArea.xMin * scale;
		float right = (Screen.width - safeArea.xMax) * scale;
		float bottom = safeArea.yMin * scale;
		float top = (Screen.height - safeArea.yMax) * scale;

		// 휴대폰 가로 화면: 맨 위 가장자리는 iOS 제어 센터/알림 센터 제스처 영역이라
		// 오른쪽 위 버튼(배경 사진) 터치가 씹힐 수 있으므로 조금 아래로 내림
		if (Application.isMobilePlatform && Screen.width > Screen.height)
		{
			top = Mathf.Max(top, 36f);
		}

		foreach (VisualElement target in safeAreaTargets)
		{
			target.style.paddingLeft = left;
			target.style.paddingRight = right;
			target.style.paddingTop = top;
			target.style.paddingBottom = bottom;
		}
	}

	#endregion

	#region 환율

	/// <summary>
	/// 일정 시간마다 호출
	/// </summary>
	void TimeOn()
	{
		ResetExchangeRate();
	}

	/// <summary>
	/// 환율 리셋 (홈페이지 서버 API 호출)
	/// </summary>
	public void ResetExchangeRate()
	{
		if (exchangeRateRequesting)
			return;

		if (rateUpdatedLabel != null)
			rateUpdatedLabel.text = "환율 불러오는 중…";

		StartCoroutine(GetExchangeRateFromServer());
	}

	IEnumerator GetExchangeRateFromServer()
	{
		exchangeRateRequesting = true;
		bool success = false;

		using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
		{
			request.timeout = 10;
			yield return request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError($"서버 통신 에러: {request.error}");
			}
			else
			{
				try
				{
					EstimateResponseWrapper res = JsonUtility.FromJson<EstimateResponseWrapper>(request.downloadHandler.text);

					if (res == null || !res.success || res.data == null)
					{
						Debug.LogError("서버에서 환율 데이터를 정상적으로 반환하지 않았습니다.");
					}
					else if (res.data.exchangeRateFetchFailed || res.data.baseExchangeRate <= 0)
					{
						Debug.LogError("서버가 환율 조회 실패를 알렸습니다.");
					}
					else
					{
						// 1엔 기준 기본 환율 저장
						exchangeRate = res.data.baseExchangeRate;
						exchangeRateLoaded = true;
						success = true;
						lastRateUpdated = DateTime.Now;

						// 웹사이트와 같은 "기준 단위(예: 100엔)"로 표시 (화면 표시는 UpdateRateDisplay에서)
						rateBasisUnit = res.data.rateBasisUnit > 0 ? res.data.rateBasisUnit : 100;

						Debug.Log($"서버 환율 갱신 성공: 기본환율({exchangeRate}), 최종환율({res.data.finalDisplayRate})");
					}
				}
				catch (Exception e)
				{
					Debug.LogError($"JSON 파싱 에러: {e.Message}");
				}
			}
		}

		UpdateRateStatus(success);

		// 환율 갱신(또는 실패 표시) 후 견적 재계산
		CalculationBtn();

		if (!success && !exchangeRateLoaded)
		{
			// 한 번도 환율을 받지 못했다면 잠시 후 다시 시도합니다.
			// (이전에 받은 환율이 있으면 그 값을 유지하고 다음 정기 갱신을 기다립니다.)
			yield return new WaitForSeconds(exchangeRateRetryDelay);
			exchangeRateRequesting = false;
			ResetExchangeRate();
			yield break;
		}

		exchangeRateRequesting = false;
	}

	void UpdateRateStatus(bool success)
	{
		if (rateUpdatedLabel == null)
			return;

		// 상태 점 색상: 정상(초록) / 실패(빨강)
		statusDot?.EnableInClassList("status-dot--ok", success || exchangeRateLoaded);
		statusDot?.EnableInClassList("status-dot--error", !success);

		if (success)
		{
			rateUpdatedLabel.text = $"실시간 환율 · {lastRateUpdated:HH:mm} 갱신";
		}
		else if (exchangeRateLoaded)
		{
			rateUpdatedLabel.text = $"환율 갱신 {lastRateUpdated:HH:mm} · 최신화 실패";
		}
		else
		{
			exchangeRateLabel.text = "—";
			rateUpdatedLabel.text = "환율 연결 실패 · 잠시 후 다시 시도합니다";
		}
	}

	/// <summary>
	/// 환율을 아직 받지 못했으면 복사를 막고 안내합니다.
	/// </summary>
	bool CanCopyResult()
	{
		if (exchangeRateLoaded)
			return true;

		PopupManager.Instance.WarningPopupCreate("환율을 불러오지 못했습니다. 잠시 후 다시 시도해 주세요.");
		ResetExchangeRate();
		return false;
	}

	#endregion

	#region 계산

	/// <summary>
	/// 계산 과정을 간단히 정리해서 말풍선으로 보여줍니다.
	/// </summary>
	void ShowCalcPopover()
	{
		List<CalcPopover.Line> lines = new List<CalcPopover.Line>();

		if (!exchangeRateLoaded)
		{
			lines.Add(new CalcPopover.Line("환율", "불러오는 중이에요"));
		}
		else
		{
			double appliedRate = exchangeRate + lastAddRate;
			double yenTotal = lastSalePrice + lastPayment + lastDailyTax + texCount;

			lines.Add(new CalcPopover.Line("적용 환율 (1엔)", $"{exchangeRate:0.####} + {lastAddRate:0.####} = {appliedRate:0.####}원"));
			lines.Add(new CalcPopover.Line("엔화 합계", $"{lastSalePrice:n0} + {lastPayment:n0} + {lastDailyTax:n0} + {texCount:n0} = {yenTotal:n0}엔"));
			lines.Add(new CalcPopover.Line("환율 × 엔화", $"{appliedRate:0.####} × {yenTotal:n0} = {lastRawResult:n2}원"));
			lines.Add(new CalcPopover.Line("10원 단위 올림", $"{resultCount:n0}원", true));
		}

		calcPopover.Show(calcBtn, lines);
	}

	/// <summary>
	/// 새 견적: 상품가격/결제수수료/일내배송료/수량을 처음 상태로 (추가 증가액은 유지)
	/// </summary>
	public void ClearQuote()
	{
		salePriceInput.SetValueWithoutNotify(string.Empty);
		paymentInput.SetValueWithoutNotify(string.Empty);
		dailyTaxInput.SetValueWithoutNotify(string.Empty);
		quantityCount = 1;
		QuantityCountReset();
		salePriceInput.Focus();
	}

	public void PaymentValueReset()
	{
		double salePrice = salePriceInput.value.ToNumber();

		if (salePrice == 0)
		{
			paymentInput.SetValueWithoutNotify(string.Empty);
		}
		else
		{
			int payment = salePrice < 30000 ? 220 : 330;
			paymentInput.SetValueWithoutNotify(payment.ToString());
		}

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
		quantityLabel.text = quantityCount.ToString();
		TexCountReset();

		CalculationBtn();
	}

	/// <summary>
	/// 대행 수수료 리셋
	/// </summary>
	void TexCountReset()
	{
		texCount = quantityCount == 0 ? 0 :
					quantityCount < 4 ? 300 : quantityCount * 100;
		texCountLabel.text = texCount.ToString("n0");
	}

	/// <summary>
	/// 엔화 환율 카드: 큰 숫자는 적용 환율(기준 + 추가), 아래에 기준/추가를 작게 표시
	/// </summary>
	void UpdateRateDisplay(double addRatePerYen)
	{
		if (!exchangeRateLoaded || exchangeRateLabel == null)
			return;

		double baseRate = exchangeRate * rateBasisUnit;
		double addRate = addRatePerYen * rateBasisUnit;
		double appliedRate = baseRate + addRate;

		exchangeRateLabel.text = appliedRate.ToString("N2");

		bool hasAdd = Math.Abs(addRate) > 0.000001;
		UIUtils.SetVisible(rateBreakdown, hasAdd);

		if (hasAdd)
		{
			rateBaseLabel.text = $"기준 {baseRate:N2}";
			rateAddLabel.text = $"추가 {addRate:#,0.##}";
		}
	}

	/// <summary>
	/// 합계 계산
	/// </summary>
	public void CalculationBtn()
	{
		double addRate = addRateInput.value.ToNumber();
		addRate *= 0.01;

		UpdateRateDisplay(addRate);

		double salePrice = salePriceInput.value.ToNumber();
		double payment = paymentInput.value.ToNumber();
		double dailyTax = dailyTaxInput.value.ToNumber();

		lastAddRate = addRate;
		lastSalePrice = salePrice;
		lastPayment = payment;
		lastDailyTax = dailyTax;

		// 환율을 받기 전에는 0원이 계산되지 않도록 결과를 막습니다.
		if (!exchangeRateLoaded)
		{
			resultCount = 0;
			resultCountLabel.text = "—";
			resultHintLabel.text = "환율 확인 중";
			return;
		}

		//최종 계산 = ( 현재 환률 + 추가 금액 ) * ( 상품 가격 + 결제수수료 + 일내배송료 + 대행 수수료 )
		resultCount = (exchangeRate + addRate) * (salePrice + payment + dailyTax + texCount);
		lastRawResult = resultCount;

		// 10원 단위 올림 (계산 오차로 딱 떨어지는 값이 한 칸 올라가지 않도록 아주 작은 값을 빼고 올림)
		resultCount = Math.Ceiling(resultCount / 10.0 - 1e-6) * 10;

		resultCountLabel.text = resultCount.ToString("n0");
		resultHintLabel.text = "탭하여 복사";
	}

	#endregion

	#region 복사

	public void CopyToResult()
	{
		if (!CanCopyResult())
			return;

		string resultText = $"{resultCount:n0}원";
		GUIUtility.systemCopyBuffer = resultText;

		PopupManager.Instance.WarningPopupCreate($"{resultText} 복사 완료");
	}

	public void CopyToMacroTextOn(MacroData macroData)
	{
		string content = macroData.contens ?? string.Empty;

		// 합계가 들어가는 매크로는 환율을 받기 전에는 복사하지 않습니다.
		if (content.Contains("{합계}") && !CanCopyResult())
			return;

		foreach (var replaceStr in replaceStrList)
		{
			if (!content.Contains(replaceStr))
				continue;

			switch (replaceStr)
			{
				case "{상품가격}":
					replaceStr.ReplaceStr(salePriceInput, ref content);
					break;
				case "{결제수수료}":
					replaceStr.ReplaceStr(paymentInput, ref content);
					break;
				case "{합계}":
					replaceStr.ReplaceStr(resultCount, ref content);
					break;
				// 추가문구는 비어 있으면 빈 문자열로 치환합니다. (0이 들어가지 않도록)
				case "{추가문구1}":
					content = content.Replace(replaceStr, addStringInput_0.value ?? string.Empty);
					break;
				case "{추가문구2}":
					content = content.Replace(replaceStr, addStringInput_1.value ?? string.Empty);
					break;
			}
		}

		GUIUtility.systemCopyBuffer = content;

		string title = string.IsNullOrEmpty(macroData.title) ? "매크로" : macroData.title;
		PopupManager.Instance.WarningPopupCreate($"{title} 복사 완료");
	}

	#endregion
}
