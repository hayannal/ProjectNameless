using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SevenDaysMenuButton : MonoBehaviour
{
	public Text remainTimeText;
	public GameObject buttonRootObject;
	public bool totalMenuButton;

	void OnEnable()
	{
		_sevenDaysExpireDateTime = MissionData.instance.sevenDaysExpireTime;
		ShowButton(ServerTime.UtcNow < _sevenDaysExpireDateTime);
	}

	void Update()
	{
		UpdateRemainTime();
	}

	public void ShowButton(bool show)
	{
		if (buttonRootObject == null)
			buttonRootObject = gameObject;
		buttonRootObject.SetActive(show);
	}

	DateTime _sevenDaysExpireDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateRemainTime()
	{
		if (ServerTime.UtcNow < _sevenDaysExpireDateTime)
		{
			if (remainTimeText != null)
			{
				TimeSpan remainTime = _sevenDaysExpireDateTime - ServerTime.UtcNow;
				if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
				{
					if (remainTime.Days > 0)
						remainTimeText.text = string.Format("{0}d {1:00}:{2:00}:{3:00}", remainTime.Days, remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
					else
						remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
					_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
				}
			}
		}
		else
		{
			ShowButton(false);
		}
	}

	public void OnClickButton()
	{
		if (totalMenuButton == false)
			UIInstanceManager.instance.ShowCanvasAsync("SevenDaysTabCanvas", null);
		else
			UIInstanceManager.instance.ShowCanvasAsync("SevenTotalCanvas", null);
	}
}