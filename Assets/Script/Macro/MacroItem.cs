using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class MacroItem : MonoBehaviour
{
    [SerializeField] Text titleText;
    [SerializeField] Text contensText;

    ////public int index = 0;
    //public string titleStr;
    //public string contensStr;

    public MacroData macroData;

    public UnityAction<MacroItem> CopyEventOn = data  => { };
    public UnityAction<MacroItem> ChangeEventOn = data => { };
    public UnityAction<MacroItem> DeleteEventOn = data => { };

    // Start is called before the first frame update
    void Start()
    {
        InputDataChangeOn();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetData(MacroData macroData)
    {
        this.macroData = macroData;
    }

    public void CopyBtnClick()
    {
        CopyEventOn(this);
    }
    public void ChangeBtnClick()
    {
        ChangeEventOn(this);
    }

    public void DeleteBtnClick()
    {
        DeleteEventOn(this);
    }

    public void TitleStrChangeOn(string str)
    {
        macroData.title = str;

        MacroManager.Instance.ResetMacroData(macroData);
        InputDataChangeOn();
    }

    public void ContensStrChangeOn(string str)
    {
        macroData.contens = str;
        MacroManager.Instance.ResetMacroData(macroData);
        InputDataChangeOn();
    }


    public void InputDataChangeOn()
    {
        if (macroData == null || string.IsNullOrEmpty(macroData.title))
        {
            titleText.text = "제목";
        }
        else
        {
            titleText.text = macroData.title;
        }

        if (macroData == null || string.IsNullOrEmpty(macroData.contens))
        {
            contensText.text = "내용";
        }
        else
        {
            contensText.text = macroData.contens;
        }
    }
}
