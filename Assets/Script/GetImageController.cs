using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using UGUI = UnityEngine.UI;

/// <summary>
/// 배경 사진 선택/크롭/저장 (UI Toolkit 배경 요소에 표시)
/// </summary>
public class GetImageController : MonoBehaviour
{
	/// <summary> 배경 이미지 파일 이름 저장 키 </summary>
	private const string imagePathKey = "bgImagePath";
	/// <summary> 크롭 이미지 파일 이름 접두사 </summary>
	private const string croppedFilePrefix = "cropped_image_";

	/// <summary> 사진을 표시할 배경 요소 </summary>
	private VisualElement bgElement;
	/// <summary> 사진이 있을 때 클래스를 붙일 앱 루트 </summary>
	private VisualElement appElement;
	/// <summary> 크롭 화면을 띄울 때 숨길 UI 전체 </summary>
	private VisualElement uiRoot;

	private Texture2D currentTexture;

	[Header("크롭 화면 꾸미기")]
	/// <summary> 크롭 화면 버튼 글꼴 </summary>
	[SerializeField] private Font cropperFont;
	/// <summary> 크롭 화면 버튼 배경 (둥근 알약 모양) </summary>
	[SerializeField] private Sprite pillSprite;

	/// <summary> 크롭 화면 제목 글꼴 (잘난체) </summary>
	[SerializeField] private Font cropperTitleFont;
	/// <summary> 크롭 화면 설명 글꼴 </summary>
	[SerializeField] private Font cropperBodyFont;
	/// <summary> 크롭 화면 배경 (합계 카드와 같은 그라데이션) </summary>
	[SerializeField] private Texture2D cropperBackground;
	/// <summary> 크롭 화면 상단 취소/적용 버튼 이미지 </summary>
	[SerializeField] private Sprite cropButtonCancel;
	[SerializeField] private Sprite cropButtonApply;
	[SerializeField] private Sprite cropButtonGlow;
	[SerializeField] private Sprite cropIconClose;
	[SerializeField] private Sprite cropIconCheck;
	/// <summary> 크롭 화면 하단 툴바 아이콘 </summary>
	[SerializeField] private Sprite iconFlipHorizontal;
	[SerializeField] private Sprite iconFlipVertical;
	[SerializeField] private Sprite iconRotate;

	/// <summary> 이미 꾸민 크롭 화면 (한 번만 적용) </summary>
	private ImageCropper skinnedCropper;

	/// <summary> 사용자가 고른 배경 사진이 있는지 </summary>
	public bool HasPhoto => currentTexture != null;

	/// <summary> 지금 배경 사진 </summary>
	public Texture2D CurrentTexture => currentTexture;

	public void Init(VisualElement bg, VisualElement app, VisualElement root)
	{
		bgElement = bg;
		appElement = app;
		uiRoot = root;

		string savedFileName = GetSavedFileName();

		if (!string.IsNullOrEmpty(savedFileName))
		{
			// 현재 기기의 저장 폴더 경로와 파일 이름을 합쳐 실제 경로를 만듭니다.
			string currentFullPath = Path.Combine(Application.persistentDataPath, savedFileName);

			if (File.Exists(currentFullPath))
			{
				LoadImageAtPath(currentFullPath, true);
			}
		}
	}

	/// <summary>
	/// 저장된 배경 사진 파일 이름을 찾습니다.
	/// 이전 버전은 저장 키가 비어 있는 채로(null) 저장했기 때문에, 업데이트 후에도
	/// 기존 배경이 유지되도록 예전 방식으로 저장된 값도 찾아서 새 키로 옮깁니다.
	/// </summary>
	private string GetSavedFileName()
	{
		string fileName = PlayerPrefs.GetString(imagePathKey, string.Empty);
		if (!string.IsNullOrEmpty(fileName))
			return fileName;

		// 1) 예전 버전 방식 (키 없이 저장된 값)
		foreach (string legacyKey in new string[] { null, string.Empty })
		{
			try
			{
				string legacy = PlayerPrefs.GetString(legacyKey, string.Empty);
				if (!string.IsNullOrEmpty(legacy) && File.Exists(Path.Combine(Application.persistentDataPath, legacy)))
				{
					fileName = legacy;
					break;
				}
			}
			catch (System.Exception)
			{
				// 예전 키로 읽을 수 없는 환경이면 다음 방법으로
			}
		}

		// 2) 그래도 없으면 저장 폴더에 남아 있는 가장 최근 크롭 이미지를 사용
		if (string.IsNullOrEmpty(fileName))
		{
			try
			{
				string newestFile = null;
				System.DateTime newestTime = System.DateTime.MinValue;

				foreach (string file in Directory.GetFiles(Application.persistentDataPath, croppedFilePrefix + "*.png"))
				{
					System.DateTime time = File.GetLastWriteTime(file);
					if (time > newestTime)
					{
						newestTime = time;
						newestFile = file;
					}
				}

				if (newestFile != null)
				{
					fileName = Path.GetFileName(newestFile);
				}
			}
			catch (System.Exception e)
			{
				Debug.LogWarning($"예전 배경 사진 찾기 실패: {e.Message}");
			}
		}

		// 찾았으면 새 키로 옮겨 저장
		if (!string.IsNullOrEmpty(fileName))
		{
			PlayerPrefs.SetString(imagePathKey, fileName);
			PlayerPrefs.Save();
		}

		return fileName;
	}

	public void PickImage()
	{
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
		// 맥 앱: NativeGallery 는 맥을 지원하지 않으므로 macOS 기본 파일 선택 창을 사용
		PickImageMac();
#else
		// 이미 사진 선택 창이 열려 있는 중이면 (iOS) 안내만
		if (NativeGallery.IsMediaPickerBusy())
		{
			PopupManager.Instance.WarningPopupCreate("사진 선택 창이 아직 열려 있어요. 잠시 후 다시 눌러 주세요");
			return;
		}

		NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
		{
			LoadImageAtPath(path);
		}, "배경 사진 선택", "image/*");

		// 사진 접근 권한이 거부되어 있으면 아무 반응이 없는 것처럼 보이므로 안내 + 설정 열기
		if (permission == NativeGallery.Permission.Denied)
		{
			PopupManager.Instance.OkPopupCreate("사진 보관함 접근이 꺼져 있어요.\n설정에서 사진 접근을 허용해 주세요.", () =>
			{
				if (NativeGallery.CanOpenSettings())
					NativeGallery.OpenSettings();
			}, null, false, false, "설정 열기", "닫기");
		}
#endif
	}

#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
	private bool macPickerOpen;

	/// <summary>
	/// macOS 기본 "파일 열기" 창으로 사진을 고릅니다. (osascript 사용, 창이 닫힐 때까지 앱은 멈추지 않음)
	/// </summary>
	private async void PickImageMac()
	{
		if (macPickerOpen)
			return;

		macPickerOpen = true;
		string path = null;

		try
		{
			path = await System.Threading.Tasks.Task.Run(() =>
			{
				var startInfo = new System.Diagnostics.ProcessStartInfo
				{
					FileName = "/usr/bin/osascript",
					Arguments = "-e \"POSIX path of (choose file of type {\\\"public.png\\\", \\\"public.jpeg\\\"} with prompt \\\"배경 사진 선택\\\")\"",
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				};

				using (var process = System.Diagnostics.Process.Start(startInfo))
				{
					string output = process.StandardOutput.ReadToEnd();
					process.WaitForExit();
					// 취소하면 종료 코드가 0 이 아님
					return process.ExitCode == 0 ? output.Trim() : null;
				}
			});
		}
		catch (System.Exception e)
		{
			Debug.LogError($"맥 사진 선택 실패: {e.Message}");
			PopupManager.Instance.WarningPopupCreate("사진 선택 창을 열지 못했어요");
		}
		finally
		{
			macPickerOpen = false;
		}

		if (!string.IsNullOrEmpty(path))
		{
			LoadImageAtPath(path);
		}
	}
#endif

	public void LoadImageAtPath(string path, bool forceOn = false)
	{
		if (path == null)
			return;

		// 고해상도 화면(태블릿·맥북 레티나)에서도 선명하도록 원본을 최대 3072px까지 불러옴
		Texture2D texture = NativeGallery.LoadImageAtPath(path, 3072);
		if (texture == null)
		{
			Debug.Log("Couldn't load texture from " + path);
			PopupManager.Instance.WarningPopupCreate(forceOn ? "저장된 배경 사진을 불러오지 못했어요" : "이 사진은 불러올 수 없어요. 다른 사진을 골라 주세요");
			return;
		}

		if (forceOn)
		{
			SetTexture(texture, Path.GetFileName(path));
			return;
		}

		// 1. 크로퍼 설정
		ImageCropper.Settings cropperSettings = new ImageCropper.Settings();

		// 2. 스크립트가 읽을 수 있도록 잠금을 해제합니다.
		cropperSettings.markTextureNonReadable = false;

		// 선택 영역을 픽셀 단위로 맞춰서 잘라낸 사진이 번지지(흐려지지) 않게
		cropperSettings.pixelPerfectSelection = true;

		// 3. 실제 배경 영역의 가로세로 비율로 크롭 박스를 고정합니다.
		if (bgElement != null)
		{
			float width = bgElement.resolvedStyle.width;
			float height = bgElement.resolvedStyle.height;

			if (width > 0 && height > 0)
			{
				float targetRatio = width / height;
				cropperSettings.selectionMinAspectRatio = targetRatio;
				cropperSettings.selectionMaxAspectRatio = targetRatio;
			}
		}

		// 크롭 화면(uGUI 플러그인)이 떠 있는 동안 UI Toolkit 화면을 숨깁니다.
		SetUIVisible(false);

		ApplyCropperSkin(ImageCropper.Instance);
		UpdateCropperLayout(ImageCropper.Instance);

		ImageCropper.Instance.Show(texture, (bool result, Texture originalImage, Texture2D croppedImage) =>
		{
			SetUIVisible(true);

			if (result && croppedImage != null)
			{
				byte[] bytes = croppedImage.EncodeToPNG();
				string fileName = croppedFilePrefix + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
				string savedPath = Path.Combine(Application.persistentDataPath, fileName);

				File.WriteAllBytes(savedPath, bytes);

				// 새 이미지를 저장했으니 이전 크롭 이미지 파일은 삭제 (저장공간 누적 방지)
				DeleteOldCroppedImages(fileName);

				SetTexture(croppedImage, fileName);

				PopupManager.Instance.WarningPopupCreate("배경 사진을 바꿨어요");
			}
			Destroy(texture);
		}, cropperSettings);
	}

	private void SetUIVisible(bool visible)
	{
		if (uiRoot != null)
		{
			uiRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
		}
	}

	/// <summary>
	/// 지금 사용하는 파일을 제외한 크롭 이미지 파일을 모두 삭제
	/// </summary>
	private void DeleteOldCroppedImages(string keepFileName)
	{
		try
		{
			foreach (string file in Directory.GetFiles(Application.persistentDataPath, croppedFilePrefix + "*.png"))
			{
				if (Path.GetFileName(file) != keepFileName)
				{
					File.Delete(file);
				}
			}
		}
		catch (System.Exception e)
		{
			Debug.LogWarning($"이전 이미지 삭제 실패: {e.Message}");
		}
	}

	private void SetTexture(Texture2D texture, string fileName)
	{
		if (texture == null || bgElement == null)
			return;

		// 파일 이름만 저장합니다. (기기 경로는 실행 때마다 바뀔 수 있음)
		PlayerPrefs.SetString(imagePathKey, fileName);
		PlayerPrefs.Save();

		if (currentTexture != null && currentTexture != texture)
		{
			Destroy(currentTexture);
		}
		currentTexture = texture;

		// 세로를 화면에 꽉 채우고, 가로는 원본 비율을 유지하며 반복(타일링)합니다.
		texture.wrapMode = TextureWrapMode.Repeat;
		bgElement.style.backgroundImage = new StyleBackground(texture);
		bgElement.style.backgroundSize = new BackgroundSize(Length.Auto(), Length.Percent(100));
		bgElement.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.NoRepeat);

		appElement?.AddToClassList("app--photo");
	}

	/// <summary>
	/// 사진을 지우고 기본 배경으로 되돌립니다.
	/// </summary>
	public void ResetToDefault()
	{
		PlayerPrefs.DeleteKey(imagePathKey);
		PlayerPrefs.Save();

		if (bgElement != null)
		{
			bgElement.style.backgroundImage = StyleKeyword.Null;
		}
		appElement?.RemoveFromClassList("app--photo");

		if (currentTexture != null)
		{
			Destroy(currentTexture);
			currentTexture = null;
		}

		// 저장해 둔 크롭 이미지 파일도 모두 삭제
		DeleteOldCroppedImages(string.Empty);

		PopupManager.Instance.WarningPopupCreate("기본 배경으로 바꿨어요");
	}

	#region 크롭 화면 꾸미기 (ImageCropper 플러그인은 그대로 두고 실행 중에 모양만 바꿈)

	private static readonly Color32 cropNavy = new Color32(0x1F, 0x24, 0x33, 0xFF);
	private static readonly Color32 cropCoral = new Color32(0xEC, 0x6B, 0x77, 0xFF);
	private static readonly Color32 cropGhost = new Color32(0xF2, 0xF3, 0xF7, 0xFF);
	private static readonly Color32 cropLine = new Color32(0xF6, 0xD3, 0xD8, 0xFF);
	private static readonly Color32 cropSub = new Color32(0x9A, 0xA0, 0xAE, 0xFF);
	private static readonly Color32 cropWell = new Color32(0xF5, 0xF6, 0xF9, 0xFF);

	private void ApplyCropperSkin(ImageCropper cropper)
	{
		if (cropper == null || cropper == skinnedCropper)
			return;

		skinnedCropper = cropper;

		try
		{
			Transform canvas = cropper.transform.Find("Canvas");
			if (canvas == null)
				return;

			// 1) 배경: 합계 카드와 같은 네이비-플럼 그라데이션
			SetGraphicColor(canvas, new Color32(0x1E, 0x1F, 0x2E, 0xFF));
			if (cropperBackground != null)
			{
				UGUI.RawImage background = CreateUI<UGUI.RawImage>("ThemeBackground", canvas);
				background.transform.SetSiblingIndex(0);
				Stretch(background.rectTransform);
				background.texture = cropperBackground;
				background.raycastTarget = false;
			}

			// 2) 상단 바: 흰색 + 가운데 제목 + 작은 알약 버튼
			Transform buttons = canvas.Find("Buttons");
			if (buttons != null)
			{
				UGUI.Image bar = buttons.GetComponent<UGUI.Image>();
				if (bar != null)
				{
					bar.sprite = null;
					bar.color = Color.white;
				}

				UGUI.Image bottomLine = CreateUI<UGUI.Image>("BottomLine", buttons);
				RectTransform lineRect = bottomLine.rectTransform;
				lineRect.anchorMin = new Vector2(0f, 0f);
				lineRect.anchorMax = new Vector2(1f, 0f);
				lineRect.pivot = new Vector2(0.5f, 1f);
				lineRect.sizeDelta = new Vector2(0f, 2f);
				lineRect.anchoredPosition = Vector2.zero;
				bottomLine.color = cropLine;
				bottomLine.raycastTarget = false;

				UGUI.Text title = CreateText("Title", buttons, "사진 자르기", cropperTitleFont, 24, cropNavy);
				SetAnchors(title.rectTransform, new Vector2(0.3f, 0.42f), new Vector2(0.7f, 0.95f));
				UGUI.Text subtitle = CreateText("Subtitle", buttons, "배경으로 보일 부분을 맞춰 주세요", cropperBodyFont, 14, cropSub);
				SetAnchors(subtitle.rectTransform, new Vector2(0.3f, 0.1f), new Vector2(0.7f, 0.44f));

				StyleCropButton(buttons.Find("CancelButton"), "취소", false);
				StyleCropButton(buttons.Find("CropButton"), "적용", true);

				// 3) 뒤집기/회전 버튼은 화면 아래 떠 있는 툴바로 옮김
				Transform orientation = buttons.Find("OrientationButtons");
				if (orientation != null)
				{
					BuildToolbar(canvas, orientation);
				}
			}

			SetGraphicColor(canvas.Find("NotchBackground"), Color.white);

			// 4) 선택 영역: 바깥은 네이비 톤으로 어둡게, 모서리는 코랄
			Transform selection = canvas.Find("Viewport/SelectionGraphics");
			if (selection != null)
			{
				Color fade = new Color(0.09f, 0.08f, 0.15f, 0.62f);
				SetGraphicColor(selection.Find("FadeOverlay"), fade);
				SetGraphicColor(selection.Find("OvalFadeOverlay"), fade);
				SetGraphicColor(selection.Find("Borders"), new Color(1f, 1f, 1f, 0.95f));

				Transform corners = selection.Find("Corners");
				if (corners != null)
				{
					foreach (Transform corner in corners)
					{
						SetGraphicColor(corner, cropCoral);
					}
				}

				Transform guidelines = selection.Find("Guidelines");
				if (guidelines != null)
				{
					foreach (Transform guide in guidelines)
					{
						SetGraphicColor(guide, new Color(1f, 1f, 1f, 0.45f));
					}
				}
			}
		}
		catch (System.Exception e)
		{
			Debug.LogWarning($"크롭 화면 꾸미기 실패: {e.Message}");
		}
	}

	/// <summary>
	/// 화면 방향에 맞춰 크롭 화면 크기를 조정합니다.
	/// - 가로(맥북): 기존처럼 1024x768 기준, 높이에 맞춤
	/// - 세로(아이폰): 폭 520 기준으로 맞춰 상단 버튼과 제목이 겹치지 않게 하고,
	///   아래 툴바는 홈 바(안전 영역) 위로 올림
	/// </summary>
	private void UpdateCropperLayout(ImageCropper cropper)
	{
		if (cropper == null)
			return;

		try
		{
			Transform canvas = cropper.transform.Find("Canvas");
			if (canvas == null)
				return;

			bool portrait = Screen.height > Screen.width;

			UGUI.CanvasScaler scaler = canvas.GetComponent<UGUI.CanvasScaler>();
			if (scaler != null)
			{
				scaler.uiScaleMode = UGUI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
				scaler.referenceResolution = portrait ? new Vector2(520f, 900f) : new Vector2(1024f, 768f);
				scaler.matchWidthOrHeight = portrait ? 0f : 1f;
			}

			// 캔버스 1px 당 화면 픽셀 (세로: 폭 기준, 가로: 높이 기준)
			float scale = portrait ? Screen.width / 520f : Screen.height / 768f;
			if (scale <= 0f)
				scale = 1f;

			Transform buttons = canvas.Find("Buttons");
			if (buttons != null)
			{
				UGUI.Text subtitle = buttons.Find("Subtitle")?.GetComponent<UGUI.Text>();
				if (subtitle != null)
					subtitle.fontSize = portrait ? 12 : 14;

				UGUI.Text title = buttons.Find("Title")?.GetComponent<UGUI.Text>();
				if (title != null)
					title.fontSize = portrait ? 22 : 24;
			}

			// 아래 툴바: 홈 바 영역만큼 위로
			RectTransform toolbar = canvas.Find("ToolBar") as RectTransform;
			if (toolbar != null)
			{
				float safeBottom = Screen.safeArea.yMin / scale;
				toolbar.anchoredPosition = new Vector2(0f, 22f + safeBottom);
			}
		}
		catch (System.Exception e)
		{
			Debug.LogWarning($"크롭 화면 크기 조정 실패: {e.Message}");
		}
	}

	/// <summary>
	/// 뒤집기/회전 버튼을 화면 아래 흰 알약 툴바로 옮기고 아이콘을 바꿉니다.
	/// </summary>
	private void BuildToolbar(Transform canvas, Transform orientation)
	{
		UGUI.Image toolbar = CreateUI<UGUI.Image>("ToolBar", canvas);
		Transform viewport = canvas.Find("Viewport");
		if (viewport != null)
		{
			toolbar.transform.SetSiblingIndex(viewport.GetSiblingIndex() + 1);
		}

		RectTransform rect = toolbar.rectTransform;
		rect.anchorMin = new Vector2(0.5f, 0f);
		rect.anchorMax = new Vector2(0.5f, 0f);
		rect.pivot = new Vector2(0.5f, 0f);
		rect.anchoredPosition = new Vector2(0f, 22f);

		toolbar.sprite = pillSprite;
		toolbar.type = pillSprite != null ? UGUI.Image.Type.Sliced : UGUI.Image.Type.Simple;
		toolbar.color = new Color(1f, 1f, 1f, 0.96f);

		UGUI.HorizontalLayoutGroup layout = toolbar.gameObject.AddComponent<UGUI.HorizontalLayoutGroup>();
		layout.padding = new RectOffset(10, 10, 8, 8);
		layout.spacing = 10f;
		layout.childAlignment = TextAnchor.MiddleCenter;
		layout.childControlWidth = true;
		layout.childControlHeight = true;
		layout.childForceExpandWidth = false;
		layout.childForceExpandHeight = false;

		UGUI.ContentSizeFitter fitter = toolbar.gameObject.AddComponent<UGUI.ContentSizeFitter>();
		fitter.horizontalFit = UGUI.ContentSizeFitter.FitMode.PreferredSize;
		fitter.verticalFit = UGUI.ContentSizeFitter.FitMode.PreferredSize;

		string[] buttonNames = { "FlipHorizontalButton", "FlipVerticalButton", "RotateButton" };
		Sprite[] icons = { iconFlipHorizontal, iconFlipVertical, iconRotate };

		for (int i = 0; i < buttonNames.Length; i++)
		{
			Transform button = orientation.Find(buttonNames[i]);
			if (button == null)
				continue;

			button.SetParent(toolbar.transform, false);

			UGUI.LayoutElement element = button.GetComponent<UGUI.LayoutElement>();
			if (element == null) element = button.gameObject.AddComponent<UGUI.LayoutElement>();
			element.minWidth = 56f;
			element.minHeight = 56f;
			element.preferredWidth = 56f;
			element.preferredHeight = 56f;

			UGUI.Image buttonImage = button.GetComponent<UGUI.Image>();
			if (buttonImage != null)
			{
				buttonImage.sprite = pillSprite;
				buttonImage.type = pillSprite != null ? UGUI.Image.Type.Sliced : UGUI.Image.Type.Simple;
				buttonImage.color = cropWell;
			}

			Transform iconTransform = button.Find("Image");
			if (iconTransform != null)
			{
				UGUI.Image icon = iconTransform.GetComponent<UGUI.Image>();
				if (icons[i] != null)
				{
					icon.sprite = icons[i];
					icon.color = Color.white;
				}
				else
				{
					icon.color = cropNavy;
				}
				icon.preserveAspect = true;
				SetAnchors((RectTransform)iconTransform, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
			}
		}
	}

	/// <summary>
	/// 상단 취소/적용 버튼: 고정 크기 알약 + 아이콘 + 글자 (적용은 코랄 그라데이션 + 글로우)
	/// </summary>
	private void StyleCropButton(Transform button, string label, bool primary)
	{
		if (button == null)
			return;

		// 화면 폭과 상관없이 모양이 유지되도록 고정 크기로 배치
		RectTransform rect = (RectTransform)button;
		Vector2 side = primary ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
		rect.anchorMin = side;
		rect.anchorMax = side;
		rect.pivot = side;
		rect.anchoredPosition = new Vector2(primary ? -22f : 22f, 0f);
		rect.sizeDelta = new Vector2(134f, 46f);

		// 원래 글자는 투명하게 두고 터치 영역으로만 사용
		UGUI.Text sourceText = button.GetComponent<UGUI.Text>();
		if (sourceText != null)
		{
			sourceText.text = label;
			sourceText.color = new Color(1f, 1f, 1f, 0f);
		}

		UGUI.Shadow shadow = button.GetComponent<UGUI.Shadow>();
		if (shadow != null)
		{
			shadow.enabled = false;
		}

		// 적용 버튼 뒤 코랄 글로우
		if (primary && cropButtonGlow != null)
		{
			UGUI.Image glow = CreateUI<UGUI.Image>("Glow", button);
			RectTransform glowRect = glow.rectTransform;
			glowRect.anchorMin = Vector2.zero;
			glowRect.anchorMax = Vector2.one;
			glowRect.pivot = new Vector2(0.5f, 0.5f);
			glowRect.sizeDelta = new Vector2(64f, 56f);
			glowRect.anchoredPosition = new Vector2(0f, -5f);
			glow.sprite = cropButtonGlow;
			glow.color = new Color(0.93f, 0.42f, 0.47f, 0.5f);
			glow.raycastTarget = false;
		}

		// 알약 배경
		UGUI.Image pill = CreateUI<UGUI.Image>("Pill", button);
		Stretch(pill.rectTransform);
		Sprite pillImage = primary ? cropButtonApply : cropButtonCancel;
		if (pillImage != null)
		{
			pill.sprite = pillImage;
			pill.color = Color.white;
		}
		else
		{
			pill.sprite = pillSprite;
			pill.type = UGUI.Image.Type.Sliced;
			pill.color = primary ? (Color)cropCoral : (Color)cropGhost;
		}
		pill.raycastTarget = false;

		Color contentColor = primary ? Color.white : (Color)cropNavy;

		// 아이콘 (취소: X, 적용: 체크)
		Sprite iconSprite = primary ? cropIconCheck : cropIconClose;
		if (iconSprite != null)
		{
			UGUI.Image icon = CreateUI<UGUI.Image>("Icon", button);
			RectTransform iconRect = icon.rectTransform;
			iconRect.anchorMin = new Vector2(0.5f, 0.5f);
			iconRect.anchorMax = new Vector2(0.5f, 0.5f);
			iconRect.pivot = new Vector2(0.5f, 0.5f);
			iconRect.sizeDelta = new Vector2(20f, 20f);
			iconRect.anchoredPosition = new Vector2(-23f, 0f);
			icon.sprite = iconSprite;
			icon.color = contentColor;
			icon.preserveAspect = true;
			icon.raycastTarget = false;
		}

		// 글자
		UGUI.Text text = CreateText("Label", button, label, cropperFont, 19, contentColor);
		RectTransform textRect = text.rectTransform;
		textRect.anchorMin = new Vector2(0.5f, 0.5f);
		textRect.anchorMax = new Vector2(0.5f, 0.5f);
		textRect.pivot = new Vector2(0.5f, 0.5f);
		textRect.sizeDelta = new Vector2(60f, 30f);
		textRect.anchoredPosition = new Vector2(iconSprite != null ? 11f : 0f, 1f);

		// 누르면 살짝 어두워지는 효과
		UGUI.Button uiButton = button.GetComponent<UGUI.Button>();
		if (uiButton != null)
		{
			uiButton.targetGraphic = pill;
			uiButton.transition = UGUI.Selectable.Transition.ColorTint;
			UGUI.ColorBlock colors = uiButton.colors;
			colors.normalColor = Color.white;
			colors.highlightedColor = new Color(0.97f, 0.97f, 0.98f, 1f);
			colors.pressedColor = new Color(0.85f, 0.85f, 0.88f, 1f);
			colors.selectedColor = Color.white;
			colors.fadeDuration = 0.08f;
			uiButton.colors = colors;
		}
	}

	private static T CreateUI<T>(string name, Transform parent) where T : Component
	{
		GameObject go = new GameObject(name, typeof(RectTransform), typeof(T));
		go.transform.SetParent(parent, false);
		return go.GetComponent<T>();
	}

	private UGUI.Text CreateText(string name, Transform parent, string value, Font font, int size, Color color)
	{
		UGUI.Text text = CreateUI<UGUI.Text>(name, parent);
		text.text = value;
		text.font = font != null ? font : cropperFont;
		text.fontSize = size;
		text.color = color;
		text.alignment = TextAnchor.MiddleCenter;
		text.horizontalOverflow = HorizontalWrapMode.Overflow;
		text.verticalOverflow = VerticalWrapMode.Overflow;
		text.raycastTarget = false;
		return text;
	}

	private static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max)
	{
		rect.anchorMin = min;
		rect.anchorMax = max;
		rect.pivot = new Vector2(0.5f, 0.5f);
		rect.anchoredPosition = Vector2.zero;
		rect.sizeDelta = Vector2.zero;
	}

	private static void Stretch(RectTransform rect)
	{
		SetAnchors(rect, Vector2.zero, Vector2.one);
	}

	private static void SetGraphicColor(Transform target, Color color)
	{
		if (target == null)
			return;

		UGUI.Graphic graphic = target.GetComponent<UGUI.Graphic>();
		if (graphic != null)
		{
			graphic.color = color;
		}
	}

	#endregion

	private void OnDestroy()
	{
		if (currentTexture != null)
		{
			Destroy(currentTexture);
		}
	}
}
