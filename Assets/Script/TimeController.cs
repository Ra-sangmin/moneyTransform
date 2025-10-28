using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class TimeController : MonoBehaviour {

	float timeValue = 60;
	int minuteValue = 5;
	public UnityAction timeOn;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		timeValue += Time.deltaTime; 

		if(timeValue > 60)
		{
			timeValue = 0;
			minuteValue ++;

			if(minuteValue >= 5)
			{
				minuteValue = 0;
				timeOn ();
			}
		}
	}
}
