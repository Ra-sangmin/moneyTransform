using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MacroChagnePopup : MonoBehaviour
{
    [SerializeField] InputField titleInputField;
    [SerializeField] InputField contensInputField;

    private MacroItem macroItem;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetData(MacroItem macroItem) 
    {
        this.macroItem = macroItem;
        titleInputField.text = macroItem.macroData.title;
        contensInputField.text = macroItem.macroData.contens;
    }

    public void TitleInputChangeOn()
    {
        if (macroItem == null)
            return;

        macroItem.TitleStrChangeOn(titleInputField.text);
    }

    public void ContensInputChangeOn()
    {
        if (macroItem == null)
            return;

        macroItem.ContensStrChangeOn(contensInputField.text);
    }
}