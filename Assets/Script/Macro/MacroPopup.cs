using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MacroPopup : MonoBehaviour
{
    [SerializeField] MacroChagnePopup macroChagnePopup;

    [SerializeField] MacroItem macroItemPrefab;

    [SerializeField] RectTransform macroItemParant;

    private List<MacroItem> macroItemList = new List<MacroItem>();

    private void Awake()
    {
        SetEvent();
    }

    void SetEvent()
    {
        //foreach (MacroItem item in macroItemList) 
        //{
        //    AddMacroEventOn(item);
        //}
    }

    private void OnDestroy()
    {
        foreach (MacroItem item in macroItemList)
        {
            DeleteMacroEventOn(item);
        }
    }

    void AddMacroEventOn(MacroItem macroItem)
    {
        macroItem.CopyEventOn += CopyEventOn;
        macroItem.ChangeEventOn += ChangeEventOn;
        macroItem.DeleteEventOn += DeleteEventOn;
    }

    void DeleteMacroEventOn(MacroItem macroItem)
    {
        macroItem.CopyEventOn -= CopyEventOn;
        macroItem.ChangeEventOn -= ChangeEventOn;
        macroItem.DeleteEventOn -= DeleteEventOn;
    }

    public void CopyEventOn(MacroItem macroItem)
    {
        GUIUtility.systemCopyBuffer = macroItem.macroData.contens;

        string warningMessage = $"{macroItem.macroData.title} 복사 완료";
        PopupManager.Instance.WarningPopupCreate(warningMessage);
    }

    public void ChangeEventOn(MacroItem macroItem)
    {
        macroChagnePopup.gameObject.SetActive(true);
        macroChagnePopup.SetData(macroItem);
        SetMacroItem();
    }

    public void DeleteEventOn(MacroItem macroItem)
    {
        MacroManager.Instance.DeleteMacroData(macroItem.macroData);
        SetMacroItem();
    }

    // Start is called before the first frame update
    void Start()
    {
        SetMacroItem();
    }

    void SetMacroItem()
    {
        List<MacroData> macroDataList = MacroManager.Instance.macroAllData.macroDataList;

        foreach (MacroItem macroItem in macroItemList)
        {
            macroItem.gameObject.SetActive(false);
        }

        if (macroItemList.Count <= macroDataList.Count)
        {
            for (int i = macroItemList.Count; i < macroDataList.Count; i++)
            {
                MacroItem macroItem = Instantiate(macroItemPrefab, macroItemParant);

                AddMacroEventOn(macroItem);

                macroItemList.Add(macroItem);
            }
        }


        for (int i = 0; i < macroDataList.Count; i++)
        {
            MacroItem macroItem = macroItemList[i];

            macroItem.gameObject.SetActive(true);

            macroItem.SetData(macroDataList[i]);
        }

    }

    public void AddDataClickOn()
    {
        MacroData macroData = MacroManager.Instance.AddMacroData();
        SetMacroItem();

        MacroItem macroItem = macroItemList.FirstOrDefault(data => data.macroData == macroData);

        ChangeEventOn(macroItem);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}