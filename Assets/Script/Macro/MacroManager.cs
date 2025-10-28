using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MacroManager : MonoSingleton<MacroManager>
{
    public MacroAllData macroAllData = new MacroAllData(); 

    private string macroAllDataKey = "macroAllDataKey";

    protected override void Init()
    {
        base.Init();

        GetMacroData();
    }

    void GetMacroData()
    {
        string macroAllDataJson = PlayerPrefs.GetString(macroAllDataKey, string.Empty);

        if (string.IsNullOrEmpty( macroAllDataJson) == false )
        {
            macroAllData = JsonUtility.FromJson<MacroAllData>(macroAllDataJson);
        }
    }

    public MacroData AddMacroData()
    {
        MacroData macroData = new MacroData();

        macroAllData.macroDataList.Add(macroData);

        MacroAllDataSave();

        return macroData;
    }

    public void ResetMacroData(MacroData macroData)
    {
        int index = macroAllData.macroDataList.IndexOf(macroData);

        if (index != -1)
        {
            macroAllData.macroDataList[index] = macroData;
            MacroAllDataSave();
        }
    }

    public void DeleteMacroData(MacroData macroData)
    {
        macroAllData.macroDataList.Remove(macroData);
        MacroAllDataSave();
    }

    private void MacroAllDataSave()
    {
        string macroAllDataJson = JsonUtility.ToJson(macroAllData);
        PlayerPrefs.SetString(macroAllDataKey, macroAllDataJson);
        PlayerPrefs.Save();
    }

}

[System.Serializable]
public class MacroAllData 
{
    public List<MacroData> macroDataList = new List<MacroData>();
}

[System.Serializable]
public class MacroData
{
    public string title = string.Empty;
    public string contens = string.Empty;

    public MacroData() { }

    public MacroData(string title, string contens) 
    {
        this.title = title;
        this.contens = contens;
    }
}
