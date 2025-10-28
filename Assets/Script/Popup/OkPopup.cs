using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class OkPopup : MonoBehaviour
{
    [SerializeField] Text contensText;
    [SerializeField] Text okBtnText;
    [SerializeField] Text cancelBtnText;

    [SerializeField] RectTransform okBtnRect;

    UnityAction okBtnClickEvent;
    UnityAction cancelBtnClickEvent;
    UnityAction destoryBtnClickEvent;

    bool clickOn = false;
    float clickDelay = 1;

    bool maskClickDestoryOn = false;

    enum BtnKind
    {
        okOnlyBtn,
        okBtn,
        cancelBtn
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (clickOn == false)
        {
            clickDelay -= Time.deltaTime;
            if (clickDelay < 0)
            {
                clickOn = true;
                clickDelay = 0.5f;
            }
        }
    }

    public void DataSet(string contensStr, UnityAction okBtnClickEvent , UnityAction cancelBtnClickEvent , bool okBtnOnly , bool maskClickDestoryOn, string okBtnStr , string cancelBtnStr )
    {
        this.okBtnClickEvent = okBtnClickEvent;
        this.cancelBtnClickEvent = cancelBtnClickEvent;

        this.maskClickDestoryOn = maskClickDestoryOn;

        contensText.text = contensStr;

        cancelBtnText.gameObject.SetActive(!okBtnOnly);

        okBtnText.text = okBtnStr;
        cancelBtnText.text = cancelBtnStr;

        //if (okBtnOnly)
        //{
        //    okBtnRect.offsetMin = new Vector2(0, okBtnRect.offsetMin.y);
            
        //}
        //else
        //{
        //    if (string.IsNullOrEmpty(okBtnStr))
        //    {
        //        okBtnStr = GetBtnStrKey(BtnKind.okBtn);
        //    }
            
        //    if (string.IsNullOrEmpty(cancelBtnStr))
        //    {
        //        cancelBtnStr = GetBtnStrKey(BtnKind.cancelBtn);
        //    }

        //    okBtnText.text = okBtnStr;
        //    cancelBtnText.text = cancelBtnStr;
        //}

        clickOn = false;
        clickDelay = 0.5f;
    }

    private string GetBtnStrKey(BtnKind btnKind)
    {
        string resultKey = string.Empty;

#if UNITY_WEBGL
        switch (btnKind)
        {
            case BtnKind.okOnlyBtn : resultKey = "key_42"; break;
            case BtnKind.okBtn :     resultKey = "key_33"; break;
            case BtnKind.cancelBtn : resultKey = "key_32"; break;
        }
#else
        switch (btnKind)
        {
            case BtnKind.okOnlyBtn : resultKey = "key_5"; break;
            case BtnKind.okBtn :     resultKey = "key_4"; break;
            case BtnKind.cancelBtn : resultKey = "key_3"; break;
        }
#endif

        return resultKey;
    }

    //void BtnTextSet(Text text , string str)
    //{
    //    string resultStr = LocalizeManager.Instance.GetStrData(str);

    //    if (resultStr == string.Empty)
    //    {
    //        resultStr = str;
    //    }

    //    text.text = resultStr;
    //}


    public void OkBtnClick()
    {
        if (clickOn == false)
            return;
        
        if (okBtnClickEvent != null)
        {
            okBtnClickEvent();
        }
        DestroyPopup();
    }
    public void CancelBtnClick()
    {
        if (clickOn == false)
            return;

        if (cancelBtnClickEvent != null)
        {
            cancelBtnClickEvent();
        }
        DestroyPopup();
    }

    public void MaskClick()
    {
        if (clickOn == false)
            return;

        if (maskClickDestoryOn == false)
        {
            if (cancelBtnClickEvent != null)
            {
                cancelBtnClickEvent();
            }
        }
        
        DestroyPopup();
    }

    void DestroyPopup()
    {
        Destroy(gameObject);
    }

}
