using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ImageCropTest : MonoBehaviour
{
	public RawImage croppedImage;
	public Texture testImage;

	//#region Test
	//public InputField testMaskAddValue;
	//public InputField imageSizeX;
	//public InputField imageSizeY;
	//public InputField testRectX;
	//public InputField testRectY;
	//public InputField testRectWidth;
	//public InputField testRectHeight;
	//public bool testOn = false;
	//#endregion

	public void CropReady()
	{
		
		//int maskAddValue = 0;
		//int.TryParse(testMaskAddValue.text, out maskAddValue);

		ImageCropper newImageCropper = Instantiate(Resources.Load<ImageCropper>("ImageCropper"), transform);

  //      if (testOn)
  //      {
		//	newImageCropper.testMaskAddValue = maskAddValue;
		//	newImageCropper.TestSet(GetImageSize(), GetRect());
		//}
        
		newImageCropper.OriginTexSet(testImage);

		newImageCropper.resultOn += resultTex =>
		{
			//ResultOn(resultTex);
			Destroy(newImageCropper.gameObject);
		};

	}

	Vector2 GetImageSize()
    {
		int imageSizeX_value = 0;
		int imageSizeY_value = 0;

		//int.TryParse(imageSizeX.text, out imageSizeX_value);
		//int.TryParse(imageSizeY.text, out imageSizeY_value);

		return new Vector2(imageSizeX_value, imageSizeY_value);
	}

	Rect GetRect()
	{
		int testRectX_value = 0;
		int testRectY_value = 0;
		int testRectWidth_value = 0;
		int testRectHeight_value = 0;

		//int.TryParse(testRectX.text, out testRectX_value);
		//int.TryParse(testRectY.text, out testRectY_value);
		//int.TryParse(testRectWidth.text, out testRectWidth_value);
		//int.TryParse(testRectHeight.text, out testRectHeight_value);

		return new Rect(testRectX_value, testRectY_value, testRectWidth_value, testRectHeight_value);
	}


	void ResultOn(Texture2D image)
	{
		float originWidth = image.width;
		float originHeight = image.height;

		croppedImage.texture = image;
	}

}
