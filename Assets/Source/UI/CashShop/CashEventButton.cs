﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CashEventButton : MonoBehaviour
{
	public string cashEventId;
	public Text remainTimeText;
	public GameObject buttonRootObject;

	void OnEnable()
	{
		if (string.IsNullOrEmpty(cashEventId))
			return;

		bool isShow = CashShopData.instance.IsShowEvent(cashEventId);
		if (isShow)
			_eventExpireDateTime = CashShopData.instance.GetExpireDateTime(cashEventId);

		ShowButton(isShow);
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

		if (MainCanvas.instance != null)
			MainCanvas.instance.CheckCashEventButtonCount();
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
						remainTimeText.text = string.Format("{0}d", remainTime.Days, remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
					else
						remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
					_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
				}
			}
		}
		else
		{
			// 이벤트 기간이 끝났으면 닫아버리는게 제일 편하다.
			// 토스트 출력은 열어둔 창에 대해서만 할테니 버튼은 그냥 닫으면 될거같다.
			//ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_EventExpired"), 2.0f);
			ShowButton(false);
		}
	}

	public void OnClickButton()
	{
		ShowEventCanvas(cashEventId);
	}

	public static void ShowEventCanvas(string id)
	{
		// hardcode ev13
		if (id == "ev13" || id == "ev14")
		{
			if (SpellSpriteContainer.instance == null)
			{
				DelayedLoadingCanvas.Show(true);
				AddressableAssetLoadManager.GetAddressableGameObject("SpellSpriteContainer", "", (prefab) =>
				{
					BattleInstanceManager.instance.GetCachedObject(prefab, null);
					DelayedLoadingCanvas.Show(false);
					UIInstanceManager.instance.ShowCanvasAsync(string.Format("{0}CashEventCanvas", id), null);
				});
			}
			else
				UIInstanceManager.instance.ShowCanvasAsync(string.Format("{0}CashEventCanvas", id), null);
			return;
		}

		// hardcode ev4
		if (id == "ev4")
		{
			if (PetSpriteContainer.instance == null)
			{
				DelayedLoadingCanvas.Show(true);
				AddressableAssetLoadManager.GetAddressableGameObject("PetSpriteContainer", "", (prefab) =>
				{
					BattleInstanceManager.instance.GetCachedObject(prefab, null);
					DelayedLoadingCanvas.Show(false);
					UIInstanceManager.instance.ShowCanvasAsync(string.Format("{0}CashEventCanvas", id), null);
				});
			}
			else
				UIInstanceManager.instance.ShowCanvasAsync(string.Format("{0}CashEventCanvas", id), null);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync(string.Format("{0}CashEventCanvas", id), null);
	}
}