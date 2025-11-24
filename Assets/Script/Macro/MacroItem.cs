using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

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

    private ScrollRect scrollRect;

    private bool dragOn = false;
	private bool longClickCheckOn = false;
    private float longClickDelay = 0;
	private float longClickCheckDelay = 1.5f;
	private bool longClickOn = false;

	// Start is called before the first frame update
	void Start()
    {
        InputDataChangeOn();
    }

    public void Init(ScrollRect scrollRect , float setWidth)
    {
		this.scrollRect = scrollRect;

		RectTransform rect = transform as RectTransform;
		Vector2 sizeDelta = rect.sizeDelta;
		sizeDelta.x = setWidth;
		rect.sizeDelta = sizeDelta;

        SetEvent();
	}

	void SetEvent()
	{
		EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();

		AddEvent(eventTrigger, EventTriggerType.BeginDrag,      (data) => OnBeginDrag((PointerEventData)data));
		AddEvent(eventTrigger, EventTriggerType.Drag,           (data) => OnDrag((PointerEventData)data));
		AddEvent(eventTrigger, EventTriggerType.EndDrag,        (data) => OnEndDrag((PointerEventData)data));
		AddEvent(eventTrigger, EventTriggerType.PointerClick,   (data) => OnClick((PointerEventData)data));
		AddEvent(eventTrigger, EventTriggerType.PointerDown,    (data) => OnPointerDown((PointerEventData)data));
		AddEvent(eventTrigger, EventTriggerType.PointerUp,      (data) => OnPointerUp((PointerEventData)data));
	}

	private void AddEvent(EventTrigger trigger, EventTriggerType eventType, System.Action<BaseEventData> action)
	{
		EventTrigger.Entry entry = new EventTrigger.Entry();
		entry.eventID = eventType;
		entry.callback.AddListener(action.Invoke);
		trigger.triggers.Add(entry);
	}

	private void OnBeginDrag(PointerEventData eventData)
	{
		dragOn = true;

		scrollRect.OnBeginDrag(eventData);
	}
	private void OnDrag(PointerEventData eventData)
	{
		longClickDelay = longClickCheckDelay;
		scrollRect.OnDrag(eventData);
	}

	private void OnEndDrag(PointerEventData eventData)
	{
		dragOn = false;

		scrollRect.OnEndDrag(eventData);
	}
	private void OnClick(PointerEventData eventData)
	{
        if (dragOn == false && longClickOn == false)
        {
			CopyBtnClick();
		}
	}
	private void OnPointerDown(PointerEventData eventData)
	{
        longClickOn = false;
		longClickCheckOn = true;
        longClickDelay = longClickCheckDelay;
	}
	private void OnPointerUp(PointerEventData eventData)
	{
		longClickCheckOn = false;
        longClickOn = false;
	}


	// Update is called once per frame
	void Update()
    {
        if (longClickCheckOn)
        {
            longClickDelay -= Time.deltaTime;

            if (longClickDelay < 0)
            {
                longClickOn = true;
                ChangeBtnClick();
			}
		}
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
