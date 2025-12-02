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
	[SerializeField] RectTransform moveItem;

	public MacroData macroData;

    public UnityAction<MacroItem> CopyEventOn = data  => { };
    public UnityAction<MacroItem> ChangeEventOn = data => { };
    public UnityAction<MacroItem> DeleteEventOn = data => { };

    private ScrollRect scrollRect;

    private bool dragOn = false;

	private Vector2 startPos = Vector2.zero;

	public enum DirectionEnum
	{
		None,
		/// <summary> 세로 </summary>
		Vertical,
		/// <summary> 가로 </summary>
		Horizontal, 
	}

	private DirectionEnum directionEnum = DirectionEnum.None;

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

		startPos = eventData.position;
	}
	private void OnDrag(PointerEventData eventData)
	{
		if (directionEnum == DirectionEnum.None) 
		{
			CheckDirection(eventData);
			return;
		}

		if (directionEnum == DirectionEnum.Vertical)
		{
			scrollRect.OnDrag(eventData);
		}
		else if (directionEnum == DirectionEnum.Horizontal)
		{
			MoveItemAddPos(eventData);
		}
	}

	private void CheckDirection(PointerEventData eventData)
	{
		Vector2 diff = eventData.position - startPos;

		float checkXValue = Mathf.Abs(diff.x);
		float checkYValue = Mathf.Abs(diff.y);

		if (checkXValue > checkYValue && checkXValue > 10 && checkYValue < 3)
		{
			directionEnum = DirectionEnum.Horizontal;
		}
		else if (checkXValue < checkYValue && checkYValue > 10)
		{
			directionEnum = DirectionEnum.Vertical;
		}
	}

	private void MoveItemAddPos(PointerEventData eventData)
	{
		Vector3 currentPos = moveItem.anchoredPosition3D;
		currentPos.x += eventData.delta.x;
		currentPos.x = Mathf.Clamp(currentPos.x, -550, 550);
		moveItem.anchoredPosition3D = currentPos;
	}

	private void OnEndDrag(PointerEventData eventData)
	{
		dragOn = false;

		DirectionEnumResultOn();

		scrollRect.OnEndDrag(eventData);
	}

	private void DirectionEnumResultOn()
	{
		if (directionEnum == DirectionEnum.Horizontal) 
		{
			float posX = moveItem.anchoredPosition3D.x;

			if (posX > 500)
			{
				ChangeBtnClick();
			}
			else if (posX < -500)
			{
				DeleteBtnClick();
			}

			moveItem.anchoredPosition3D = Vector3.zero;
		}


		directionEnum = DirectionEnum.None;
	}


	private void OnClick(PointerEventData eventData)
	{
        if (dragOn == false )
        {
			CopyBtnClick();
		}
	}
	private void OnPointerDown(PointerEventData eventData)
	{
		directionEnum = DirectionEnum.None;
		moveItem.anchoredPosition3D = Vector3.zero;
	}
	private void OnPointerUp(PointerEventData eventData)
	{
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
