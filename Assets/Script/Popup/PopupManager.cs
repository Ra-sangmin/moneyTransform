using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class PopupManager : MonoSingleton<PopupManager>
{
	public RectTransform parantTranform = null;
	//public RectTransform parantRectTranform = null;

    public bool otherPopupOn = false;

    //public bool PopupOnCheck()
    //{
	   // return (parantTranform != null && parantTranform.childCount != 0 )  || 
	   //        (parantRectTranform != null && parantRectTranform.childCount != 0) || 
	   //        otherPopupOn;
    //}
    
    protected override void Init() 
	{
		//ParantSet(transform);
	}

	public void SetCanvasParant(Transform target)
	{
		Canvas canvas = target.GetComponentInParent<Canvas>();
		ParantSet(canvas);
	}

	public void ParantSet(Canvas canvas)
	{
		GameObject newParantObj = new GameObject("PopupParant");
		newParantObj.transform.SetParent(canvas.transform);
        RectTransform rectTransform = newParantObj.AddComponent<RectTransform>();

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = Vector2.one * 0.5f;

        parantTranform = rectTransform;
        rectTransform.anchoredPosition3D = new Vector3(0, 0, 0);
		rectTransform.sizeDelta = (canvas.transform as RectTransform).sizeDelta;
        rectTransform.transform.localScale = Vector3.one;
	}

	public OkPopup OkPopupCreate(string contensStr, UnityAction okBtnClickEvent, UnityAction cancelBtnClickEvent = null , bool maskClickDestoryOn = false, bool okBtnOnly = false, string okBtnStr = "예", string cancelBtnStr = "아니오")
	{
        if (parantTranform == null)
			return null;
		
		parantTranform.SetAsLastSibling();

		OkPopup okPopup = Instantiate(Resources.Load<OkPopup>("Popup/OkPopup"), parantTranform);
#if UNITY_WEBGL || UNITY_STANDALONE
#else
		okPopup.transform.localScale = Vector3.one * 1.5f;
#endif
		okPopup.DataSet(contensStr, okBtnClickEvent , cancelBtnClickEvent , okBtnOnly,maskClickDestoryOn,  okBtnStr, cancelBtnStr);

		return okPopup;
	}

	public WarningPopup WarningPopupCreate(string contensStr)
	{
		if (parantTranform == null)
			return null;

		parantTranform.SetAsLastSibling();
		WarningPopup warningPopup = Instantiate(Resources.Load<WarningPopup>("Popup/WarningPopup"), parantTranform);
		warningPopup.DataSet(contensStr);

		Vector3 scale = Vector3.one;

		switch (SceneManager.GetActiveScene().name)
		{
			case "Search":
			case "Intro": scale *= 2f; break;
			case "Login": scale *= 1.5f; break;
		}

		warningPopup.transform.localScale = scale;

		return warningPopup;
	}
}
