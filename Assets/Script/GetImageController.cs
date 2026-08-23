using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class GetImageController : MonoBehaviour
{
    //[SerializeField] private List<RawImage> imageList = new List<RawImage>();
    [SerializeField] private RectTransform parant;
	[SerializeField] private RawImage image;

	public float maxHeight;

    private string imagePath;
    private string imagePathKey;


    private void Awake()
    {
        SetMacHeight();
    }

    void SetMacHeight()
    {
        RectTransform canvasRect = transform.GetComponentInParent<Canvas>().transform as RectTransform;
        maxHeight = canvasRect.sizeDelta.y;
    }

    // Start is called before the first frame update
    void Start()
    {
        imagePath = PlayerPrefs.GetString(imagePathKey,string.Empty);

        if (imagePath != string.Empty)
        {
            LoadImageAtPath(imagePath , true);
        }
    }

    public void PickImage()
    {
        NativeGallery.Permission permission = NativeGallery.GetImageFromGallery((path) =>
        {
            LoadImageAtPath(path);
        }, "Select a PNG image", "image/png");
    }

	public void LoadImageAtPath(string path, bool forceOn = false)
	{
		if (path != null)
		{
			Texture2D texture = NativeGallery.LoadImageAtPath(path, 1080);
			if (texture == null)
			{
				Debug.Log("Couldn't load texture from " + path);
				return;
			}
			else
			{
				if (forceOn == false)
				{
					// 1. 크로퍼 설정을 새로 하나 만듭니다.
					ImageCropper.Settings cropperSettings = new ImageCropper.Settings();

					// 2. 스크립트가 읽을 수 있도록 잠금을 해제합니다.
					cropperSettings.markTextureNonReadable = false;

					// 🌟 [추가된 부분] 실제 표시될 RawImage의 가로세로 비율을 크로퍼에 적용합니다.
					if (image != null)
					{
						RectTransform imageRect = image.rectTransform;

						// 가로를 세로로 나누어 목표 비율(Aspect Ratio)을 구합니다.
						float targetRatio = imageRect.rect.width / imageRect.rect.height;

						// 최소 비율과 최대 비율을 동일하게 설정하여 크롭 박스의 비율을 강제로 고정합니다.
						cropperSettings.selectionMinAspectRatio = targetRatio;
						cropperSettings.selectionMaxAspectRatio = targetRatio;
					}

					ImageCropper.Instance.Show(texture, (bool result, Texture originalImage, Texture2D croppedImage) =>
					{
						if (result && croppedImage != null)
						{
							byte[] bytes = croppedImage.EncodeToPNG();
							string fileName = "cropped_image_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
							string savedPath = Path.Combine(Application.persistentDataPath, fileName);

							File.WriteAllBytes(savedPath, bytes);

							DataSet(croppedImage, savedPath);
						}
						Destroy(texture);
					}, cropperSettings);
				}
				else
				{
					DataSet(texture, path);
				}
			}
		}
	}

	private void DataSet(Texture2D texture, string path)
    {
        string fileName = Path.GetFileName(path);
        string fullPath = path;

        PickTextureData data = new PickTextureData(texture, fileName, fullPath);

        SetTexture(data);
    }

	public void SetTexture(PickTextureData result)
	{
		if (result != null && result.texture != null)
		{
			imagePath = result.fullPath;
			PlayerPrefs.SetString(imagePathKey, result.fullPath);
			PlayerPrefs.Save();

			// 1. 가로로 자연스럽게 이어지도록 반복(Repeat) 설정
			result.texture.wrapMode = TextureWrapMode.Repeat;
			image.texture = result.texture;

			RectTransform imageRect = image.rectTransform;

			// 🚨 사이즈를 강제로 바꾸는 코드(sizeDelta 수정)를 삭제했습니다! 
			// 대신 현재 UI가 에디터 설정에 맞춰 렌더링하고 있는 '실제 높이와 너비'만 가져옵니다.
			float currentHeight = imageRect.rect.height;
			float currentWidth = imageRect.rect.width;

			// 2. 현재 세로 길이를 기준으로, 원본 비율이 유지되는 '하나의 패턴 가로 길이'를 계산합니다.
			float singlePatternWidth = result.texture.width * (currentHeight / result.texture.height);

			// 3. 실제 RawImage의 가로 전체 길이를 패턴 1개의 가로 길이로 나누어 줍니다.
			// (가로 영역에 패턴이 몇 번 반복되어야 하는지 계산)
			float tileX = currentWidth / singlePatternWidth;

			// 4. 세로는 화면에 꽉 차게 1로 고정합니다.
			float tileY = 1f;

			// 5. 남는 가로 영역을 계산된 횟수만큼 패턴화(타일링)합니다.
			image.uvRect = new Rect(0, 0, tileX, tileY);
		}
	}

}



public class PickTextureData
{
    public Texture2D texture;
    public string fileName;
    public string fullPath;

    public PickTextureData(Texture2D texture,string fileName,string fullPath)
    {
        this.texture = texture;
        this.fileName = fileName;
        this.fullPath = fullPath;
    }
}
