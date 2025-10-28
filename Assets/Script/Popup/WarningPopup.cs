using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class WarningPopup : MonoBehaviour
{
    [SerializeField] Text contensText;

    public void DataSet(string contensStr)
    {
        contensText.text = contensStr;

        //위치 이동
        RectTransform rectObj = contensText.GetComponent<RectTransform>();
        float targetYPos = rectObj.anchoredPosition3D.y;
        rectObj
            .DOAnchorPosY(targetYPos + 50, 1)
            .SetDelay(1);

        //알파 조정
        contensText
            .DOFade(0, 1f)
            .SetDelay(1)
            .OnComplete(()=>Destroy(gameObject));
    }
}
