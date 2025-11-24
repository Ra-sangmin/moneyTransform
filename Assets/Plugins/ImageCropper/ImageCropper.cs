using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using Crystal;
using System.IO;

public class ImageCropper : MonoBehaviour
{
    public RawImage maskImage;
    public RectTransform maskRectSetObj;

    public RectTransform selection;
    Texture originTex;
    public RectTransform originImage;
    public RectTransform imageMask_windows;
    public RectTransform startRectSetObj;
    public RectTransform imageStartPosCheckSetObj;
	public RectTransform imageEndPosCheckSetObj;
	public RectTransform btnPanel;

	public UnityAction<CropData> resultOn = null;

    Vector2 dragStartPos = Vector2.one;
    Vector2 dragStartImagePos = Vector2.one;
    float startDistance = 0;
    Vector2 startScale = Vector2.one;
    Vector2 startsize = Vector2.one;

    float fixSize = 1080;
    float imageMinScale = 0.5f;
    float imageMinSize = 0;

    bool zoomOn = false;

    bool heightLong = false;

    Tween scaleTween;
    Tween moveTween;

    bool cropOn;

    bool testOn = false;
    Vector2 testImageSize;
    Rect testRect;
    public int testMaskAddValue;

    int bottemValue;

    private void Awake()
    {
        
    }

    public void BGMaskSet()
    {
        imageMinSize = selection.sizeDelta.x;

        maskRectSetObj.transform.parent = selection;
        maskRectSetObj.anchoredPosition3D = Vector2.zero;
        maskRectSetObj.transform.parent = transform;

        Canvas canvas = transform.GetComponentInParent<Canvas>();
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        Vector2 canvasSize = canvasRect.sizeDelta;
        SafeArea safeArea = transform.GetComponentInParent<SafeArea>();//.GetComponent<RectTransform>();

        Debug.LogWarning(canvasSize);

        transform.position = canvas.transform.position;

        Texture2D bgTex = new Texture2D((int)canvasSize.x, (int)canvasSize.y, TextureFormat.ARGB32, false);

        Vector2 startRect = maskRectSetObj.anchoredPosition3D;
        Vector2 selectionSize = selection.sizeDelta;

        bottemValue = 0;

        if (testOn)
        {
            bottemValue = testMaskAddValue;
        }
        else
        {
            if (safeArea.LastSafeArea.y != 0)
            {
                bottemValue = (int)(safeArea.LastSafeArea.y) + (int)((safeArea.LastSafeArea.y)*0.12f);
            }
        }
         
        for (int x = 0; x < (int)canvasSize.x; x++)
		{
			for (int y = 0; y < (int)canvasSize.y; y++)
			{
                Color32 cor = new Color32(0, 0, 0, 191);

                if (x >= startRect.x && x <= (startRect.x + selectionSize.x) &&
                    y >= startRect.y + bottemValue && y <= (startRect.y + selectionSize.y + bottemValue) ) 
                {
                    cor.a = 0;
                }

                bgTex.SetPixel(x, y, cor);
            }
		}

        bgTex.Apply();

        maskImage.rectTransform.sizeDelta = canvasSize;
        maskImage.texture = bgTex;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void OriginTexSet(Texture originTex)
    {
        this.originTex = originTex;

        //BGMaskSet();

        RawImage rawImage = originImage.GetComponent<RawImage>();
        rawImage.texture = originTex;

        float originWidth = rawImage.texture.width;
        float originHeight = rawImage.texture.height;

        float newWidth = originWidth;
        float newHeight = originHeight;

        heightLong = originWidth < originHeight;

        if (heightLong)
        {
            newWidth = fixSize;
            newHeight = originHeight * newWidth / originWidth;
        }
        else
        {
            newHeight = fixSize;
            newWidth = originWidth * newHeight / originHeight;
        }

        originImage.sizeDelta = new Vector2(newWidth, newHeight);

		Debug.LogWarning(originImage.sizeDelta);

		originImage.anchoredPosition3D = new Vector2(0, 0);

        PosSet(originImage.anchoredPosition3D);
    }

    public void TestSet(Vector2 testImageSize , Rect testRect)
    {
        this.testImageSize = testImageSize;
        this.testRect = testRect;
        testOn = true;
    }


    private void Update()
    {
		if (cropOn)
            return;

        InputCheck();
        
    }

    void InputCheck()
    {
#if UNITY_EDITOR

        if (Input.GetKey(KeyCode.Mouse0))
        {
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                dragStartPos = Input.mousePosition;
                dragStartImagePos = originImage.anchoredPosition3D;
                startDistance = Vector2.Distance(Vector2.zero, dragStartPos);
                startsize = originImage.sizeDelta;
            }
            else
            {
                Vector2 newDragPos = Input.mousePosition;

                if (zoomOn)
                {
                    ZoomCheck(Vector2.zero, newDragPos);
                }
                else
                {
                    MoveObj(newDragPos);
                }

            }
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            zoomOn = true;
        }
        else if (Input.GetKeyUp(KeyCode.Q))
        {
            zoomOn = false;
        }
#else
        if (Input.touchCount < 1)
            return;

        if (Input.touchCount == 1)
        {
            Touch t = Input.touches[0];

            if (t.phase == TouchPhase.Began)
            {
                //dragStartPos = Camera.main.ScreenToWorldPoint(t.position);
                //dragStartImagePos = originImage.transform.position;

                dragStartPos = t.position;
                dragStartImagePos = originImage.anchoredPosition3D;
            }
            else if (t.phase == TouchPhase.Moved)
            {
                //Vector2 newDragPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Vector2 newDragPos = t.position;
                MoveObj(newDragPos);
            }
        }
        else if (Input.touchCount == 2)
        {
            //Vector2 touches_0_pos = Camera.main.ScreenToWorldPoint(Input.touches[0].position);
            //Vector2 touches_1_pos = Camera.main.ScreenToWorldPoint(Input.touches[1].position);

            Vector2 touches_0_pos = Input.touches[0].position;
            Vector2 touches_1_pos = Input.touches[1].position;

            if (Input.touches[0].phase == TouchPhase.Began || Input.touches[1].phase == TouchPhase.Began)
            {
                startDistance = Vector2.Distance(touches_0_pos, touches_1_pos);
                //startScale = originImage.transform.localScale;
                startsize = originImage.sizeDelta;
            }
            else if (Input.touches[0].phase == TouchPhase.Moved && Input.touches[1].phase == TouchPhase.Moved)
            {
                ZoomCheck(touches_0_pos, touches_1_pos);
            }
            else if (Input.touches[0].phase == TouchPhase.Ended)
            {
                Touch t = Input.touches[1];
                //dragStartPos = Camera.main.ScreenToWorldPoint(t.position);
                //dragStartImagePos = originImage.transform.position;

                dragStartPos = t.position;
                dragStartImagePos = originImage.anchoredPosition3D;
            }
            else if (Input.touches[1].phase == TouchPhase.Ended)
            {
                Touch t = Input.touches[0];
                //dragStartPos = Camera.main.ScreenToWorldPoint(t.position);
                //dragStartImagePos = originImage.transform.position;

                dragStartPos = t.position;
                dragStartImagePos = originImage.anchoredPosition3D;
            }

        }

#endif
    }

    void ZoomCheck(Vector2 newDragPos_0 , Vector2 newDragPos_1)
    {
        float newDistance = Vector2.Distance(newDragPos_0, newDragPos_1);

        float addValue = newDistance - startDistance;

        Vector2 newSize = GetNewSize(addValue);

        float minCheckValue = heightLong ? newSize.x : newSize.y;

        if (minCheckValue < imageMinSize)
        {
            if (heightLong)
            {
                newSize.x = imageMinSize;

                newSize.y = (newSize.x * startsize.y) / startsize.x;
            }
            else
            {
                newSize.y = imageMinSize;

                newSize.x = (newSize.y * startsize.x) / startsize.y;
            }
        }

        if (scaleTween != null && scaleTween.IsPlaying())
            scaleTween.Kill();

        scaleTween = originImage.DOSizeDelta(newSize, 0.05f)
                        .OnComplete(()=> PosSet(originImage.anchoredPosition3D));
    }

    Vector2 GetNewSize(float addValue)
    {
        float newXsize = startsize.x + (startsize.x * addValue / 300);
        float newYsize = startsize.y + (startsize.y * addValue / 300);

        return new Vector2(newXsize, newYsize);
    }

    void MoveObj(Vector2 newDragPos)
    {
        float xValue = newDragPos.x - dragStartPos.x;
        float yValue = newDragPos.y - dragStartPos.y;

        Vector2 targetPos = new Vector2(dragStartImagePos.x + xValue, dragStartImagePos.y + yValue);

        PosSet(targetPos);
    }

    void PosSet(Vector2 targetPos)
    {
        float width = originImage.sizeDelta.x;
        float selectionWidth = (int)selection.sizeDelta.x;
        float minX = (width / 2) - (selectionWidth / 2);
        float maxX = minX * -1;
        targetPos.x = Mathf.Clamp(targetPos.x, maxX, minX);

        float heigth = originImage.sizeDelta.y;
        float selectionHeigth = (int)selection.sizeDelta.y;
        float minY = (heigth / 2) - (selectionHeigth / 2);
        float maxY = minY * -1;
        targetPos.y = Mathf.Clamp(targetPos.y, maxY, minY);

        if (moveTween != null && moveTween.IsPlaying())
            moveTween.Kill();
        
        moveTween = originImage.DOLocalMove(targetPos, 0.05f);
    }

    public void CropBtn()
    {
        ResultCrop();
    }

    void ResultCrop()
    {
        if (cropOn)
            return;

        cropOn = true;

		btnPanel.gameObject.SetActive(false);

		StartCoroutine(CaptureAndCrop());
	}

	public IEnumerator CaptureAndCrop()
	{
		// 1) 프레임 끝까지 기다린 뒤 화면 픽셀 캡처
		yield return new WaitForEndOfFrame();

		int width = Screen.width;
		int height = Screen.height;

		Texture2D screenshot = new Texture2D(width, height, TextureFormat.ARGB32, false);
		screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
		screenshot.Apply();

		yield return new WaitForEndOfFrame();

		// 2) Crop 수행
		Texture2D cropped = CropTexture(screenshot);

		yield return new WaitForEndOfFrame();

        // 3) PNG 저장
        string path = Path.Combine(Application.persistentDataPath, "crop.png");
        File.WriteAllBytes(path, cropped.EncodeToPNG());
        Debug.Log($"Saved cropped screenshot → {path}");

        CropData cropData = new CropData()
        {
            texture2D = cropped,
            path = path
		};


		if (resultOn != null)
		{
			resultOn(cropData);
		}
	}

	Texture2D CropTexture(Texture2D src)
	{
		RectInt rect = new RectInt()
		{
			x = 0,
			y = 0,
			width = Screen.width,
			height = Screen.height,
		};

		Color[] pixels = src.GetPixels(rect.x, rect.y, rect.width, rect.height);
		Texture2D dst = new Texture2D(rect.width, rect.height, TextureFormat.ARGB32, false);
		dst.SetPixels(pixels);
		dst.Apply();
		return dst;
	}

	public void CloseBtn()
    {
        Destroy(gameObject);
    }
}

public class CropData
{
    public Texture2D texture2D;
    public string path;
}
