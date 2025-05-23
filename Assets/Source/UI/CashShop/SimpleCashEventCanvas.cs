﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleCashEventCanvas : SimpleCashCanvas
{
	public string cashEventId;
	public Text remainTimeText;
	
	void OnEnable()
	{
		SetInfo();
	}

	protected void SetInfo()
	{
		if (string.IsNullOrEmpty(cashEventId))
			return;

		// show 상태가 아니면 안보이겠지만 혹시 모르니 안전하게 구해온다.
		bool isShow = CashShopData.instance.IsShowEvent(cashEventId);
		if (isShow)
			_eventExpireDateTime = CashShopData.instance.GetExpireDateTime(cashEventId);
	}

	void Update()
	{
		UpdateRemainTime();
	}

	DateTime _eventExpireDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateRemainTime()
	{
		if (ServerTime.UtcNow < _eventExpireDateTime)
		{
			if (remainTimeText != null)
			{
				TimeSpan remainTime = _eventExpireDateTime - ServerTime.UtcNow;
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
			// 이벤트 기간이 끝났으면 닫아버리는게 제일 편하다.
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_EventExpired"), 2.0f);
			gameObject.SetActive(false);
		}
	}

	public void OnClickShortcutButton()
	{
		gameObject.SetActive(false);

		// 다른 창들은 구매창이라서 바로가기가 필요없다. 바로가기 필요한 이벤트 위주로만 해둔다.
		switch (cashEventId)
		{
			case "ev7":
				MainCanvas.instance.OnClickGachaButton();
				break;
			case "ev8":
				MainCanvas.instance.OnClickCharacterButton();
				break;
			case "ev9":
				MainCanvas.instance.OnClickCashShopButton();
				break;
			case "ev10":
				MainCanvas.instance.OnClickCashShopButton();
				break;
		}
	}
}
