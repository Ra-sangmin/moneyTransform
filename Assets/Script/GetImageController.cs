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
					ImageCropper newImageCropper = Instantiate(Resources.Load<ImageCropper>("ImageCropper"), parant);
					newImageCropper.OriginTexSet(texture);

					newImageCropper.resultOn += cropData =>
					{
                        DataSet(cropData.texture2D, cropData.path);
						//ResultOn(resultTex);
						Destroy(newImageCropper.gameObject);
					};
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

        Debug.LogWarning(fullPath);

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
