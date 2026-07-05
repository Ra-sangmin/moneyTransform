using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class GetImageController : MonoBehaviour
{
    [SerializeField] private List<RawImage> imageList = new List<RawImage>();
    [SerializeField] private RectTransform parant;
	//[SerializeField] private RawImage image;

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

    public void LoadImageAtPath(string path , bool forceOn = false)
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

					// 2. 핵심! 크롭된 이미지를 스크립트가 읽을 수 있도록 잠금을 해제합니다.
					cropperSettings.markTextureNonReadable = false;

					ImageCropper.Instance.Show(texture, (bool result, Texture originalImage, Texture2D croppedImage) =>
					{
						// 1. Texture2D 메모리 데이터를 PNG 파일 형식의 바이트(Byte) 배열로 변환
						byte[] bytes = croppedImage.EncodeToPNG();

						// 2. 기기 내부에 저장할 파일 이름과 절대 경로 생성 (파일명이 겹치지 않도록 현재 시간 추가)
						string fileName = "cropped_image_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

						// Application.persistentDataPath는 앱이 삭제되지 않는 한 유지되는 안전한 로컬 저장 폴더입니다.
						string savedPath = Path.Combine(Application.persistentDataPath, fileName);

						// 3. 실제 기기 저장소에 파일 쓰기 (저장)
						File.WriteAllBytes(savedPath, bytes);

						// 4. 이제 실제로 파일이 존재하므로 savedPath를 사용하여 원하는 로직을 처리하시면 됩니다.
						//Debug.Log("크롭된 이미지 저장 위치: " + savedPath);

						DataSet(croppedImage, savedPath);

						Destroy(texture);
					},cropperSettings);
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

            PlayerPrefs.SetString(imagePathKey,result.fullPath);
            PlayerPrefs.Save();

            float maxWidth = result.texture.width * maxHeight / result.texture.height;

            foreach(var image in imageList)
            {
               //image.rectTransform.sizeDelta = new Vector2(maxWidth,maxHeight);
                image.texture = result.texture;
            }
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
