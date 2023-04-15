using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestEndCanvas : MonoBehaviour
{
	public static QuestEndCanvas instance;

	public Transform subTitleTransform;
	public Text remainTimeText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		_questResetTime = PlayerData.instance.dayRefreshTime;
		_needUpdate = true;
	}
	
	void Update()
	{
		UpdateRemainTime();
	}

	DateTime _questResetTime;
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _questResetTime)
		{
			TimeSpan remainTime = _questResetTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			_needUpdate = false;
			remainTimeText.text = "00:00:00";

			// 진행중이던게 아니니 그냥 토스트 없이 닫아본다.
			//_needRefresh = true;
			gameObject.SetActive(false);
		}
	}

	public void OnClickMoreButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("QuestUI_SubQuestMore"), 300, subTitleTransform, new Vector2(0.0f, -35.0f));
	}
}