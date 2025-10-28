using System.Collections.Generic;
using System.IO;

using UnityEngine;
using UnityEngine.UI;

namespace Crosstales.FB.Demo
{
	public class GetImageController : MonoBehaviour
	{
		[SerializeField] private List<RawImage> imageList = new List<RawImage>();

		private string imagePath;
		private string imagePathKey;

		public void Start()
		{
			imagePath = PlayerPrefs.GetString(imagePathKey, string.Empty);

			if (imagePath != string.Empty)
			{
				SetTexture(imagePath);
			}
		}

		public void GetImageOn()
		{
#if (UNITY_ANDROID && !UNITY_EDITOR)
			GameObject.Find("MainController").SendMessage("OnTest");
#else
			SetTexture(FileBrowser.OpenSingleFile("Open File", string.Empty, string.Empty));
#endif
		}

		public void SetTexture( string path)
		{
			if (path == null || path == string.Empty)
				return;
			
			imagePath = path;
			//로컬에 path정보 저장
			PlayerPrefs.SetString(imagePathKey, imagePath);
			PlayerPrefs.Save();

			Texture2D texture2D = LoadPNG(imagePath);

			foreach (var image in imageList)
			{
				image.texture = texture2D;
			}
		}

		public Texture2D LoadPNG(string filePath)
		{
			Texture2D tex = null;
			byte[] fileData;

			if (File.Exists(filePath))
			{
				fileData = File.ReadAllBytes(filePath);
				tex = new Texture2D(100, 100);
				tex.LoadImage(fileData);
			}
			return tex;
		}

	}
}
