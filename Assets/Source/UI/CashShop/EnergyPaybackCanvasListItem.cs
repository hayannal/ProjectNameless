using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EnergyPaybackCanvasListItem : MonoBehaviour
{
	public RewardIcon rewardIcon;
	public Text nameText;
	public Slider proceedingCountSlider;
	public Text proceedingCountText;
	public GameObject getTextObject;
	public GameObject receivedTextObject;
	public GameObject blackObject;
	public RectTransform alarmRootTransform;

	void OnEnable()
	{
		if (_initialized)
			Initialize(_use, _payback);
	}

	int _use;
	int _payback;
	bool _initialized;
	public void Initialize(int use, int payback)
	{
		_use = use;
		_payback = payback;
		_initialized = true;

		// 리워드
		rewardIcon.RefreshReward("cu", "EN", payback);
		nameText.SetLocalizedText(UIString.instance.GetString("EnergyPaybackUI_UseAchieved", use));

		// 버튼
		bool received = CashShopData.instance.IsGetEnergyPaybackReward(use);
		receivedTextObject.SetActive(received);
		blackObject.SetActive(received);
		AlarmObject.Hide(alarmRootTransform);
		if (received)
		{
			getTextObject.SetActive(false);
			proceedingCountText.gameObject.SetActive(false);
			proceedingCountSlider.gameObject.SetActive(false);
		}
		else
		{
			bool getable = (use <= CashShopData.instance.energyUseForPayback);
			getTextObject.SetActive(getable);
			if (getable)
				AlarmObject.Show(alarmRootTransform);
			proceedingCountText.gameObject.SetActive(!getable);
			proceedingCountSlider.gameObject.SetActive(!getable);
			if (!getable)
			{
				proceedingCountText.text = string.Format("{0:N0} / {1:N0}", CashShopData.instance.energyUseForPayback, use);
				proceedingCountSlider.value = (float)CashShopData.instance.energyUseForPayback / use;
			}
		}
	}

	public void OnClickButton()
	{
		if (blackObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LevelPassUI_AlreadyClaimed"), 2.0f);
			return;
		}

		if (getTextObject.activeSelf == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EnergyPaybackUI_NotEnoughUse"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestGetEnergyPaybackReward(_use, _payback, () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
			{
				Initialize(_use, _payback);
				MainCanvas.instance.RefreshEnergyPaybackAlarmObject();
				CommonRewardCanvas.instance.RefreshReward(0, 0, _payback);
			});
		});
	}
}
