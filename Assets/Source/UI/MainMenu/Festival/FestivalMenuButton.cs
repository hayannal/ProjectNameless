using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class FestivalMenuButton : MonoBehaviour
{
	public Text remainTimeText;
	public GameObject buttonRootObject;

	bool _useExpire = false;
	void OnEnable()
	{
		_useExpire = false;
		if (FestivalData.instance.festivalExpireTime < FestivalData.instance.festivalExpire2Time)
		{
			_festivalExpireDateTime = FestivalData.instance.festivalExpireTime;
			_useExpire = true;
		}
		else
		{
			_festivalExpireDateTime = FestivalData.instance.festivalExpire2Time;
		}
		ShowButton(ServerTime.UtcNow < _festivalExpireDateTime);
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

	DateTime _festivalExpireDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateRemainTime()
	{
		if (ServerTime.UtcNow < _festivalExpireDateTime)
		{
			if (remainTimeText != null)
			{
				TimeSpan remainTime = _festivalExpireDateTime - ServerTime.UtcNow;
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
			if (_useExpire)
			{
				// 교환쪽 시간으로 교체해주고 계속 보이는채로 두면 된다.
				_useExpire = false;
				_festivalExpireDateTime = FestivalData.instance.festivalExpire2Time;
			}
			else
			{
				// 교환 시간마저 끝나면 원래대로 숨기면 된다.
				ShowButton(false);
			}
		}
	}

	public void OnClickButton()
	{
		Timing.RunCoroutine(ShowCanvasProcess());
	}

	IEnumerator<float> ShowCanvasProcess()
	{
		// 
		if (SpellSpriteContainer.instance == null)
		{
			AddressableAssetLoadManager.GetAddressableGameObject("SpellSpriteContainer", "", (prefab) =>
			{
				BattleInstanceManager.instance.GetCachedObject(prefab, null);
			});
		}
		while (SpellSpriteContainer.instance == null)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		if (PetSpriteContainer.instance == null)
		{
			AddressableAssetLoadManager.GetAddressableGameObject("PetSpriteContainer", "", (prefab) =>
			{
				BattleInstanceManager.instance.GetCachedObject(prefab, null);
			});
		}
		while (PetSpriteContainer.instance == null)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		UIInstanceManager.instance.ShowCanvasAsync("FestivalTabCanvas", () =>
		{
			if (_useExpire)
				FestivalTabCanvas.instance.defaulMenuButtonIndex = 0;
			else
				FestivalTabCanvas.instance.defaulMenuButtonIndex = 1;
		});
	}
}