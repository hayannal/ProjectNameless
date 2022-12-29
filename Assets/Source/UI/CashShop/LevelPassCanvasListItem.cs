using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelPassCanvasListItem : MonoBehaviour
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
			Initialize(_level, _energy);
	}

	int _level;
	int _energy;
	bool _initialized;
    public void Initialize(int level, int energy)
	{
		_level = level;
		_energy = energy;
		_initialized = true;

		// 리워드
		rewardIcon.RefreshReward("cu", "EN", energy);
		nameText.SetLocalizedText(UIString.instance.GetString("LevelPassUI_LevelAchieved", level));

		// 버튼
		bool received = CashShopData.instance.IsGetLevelPassReward(level);
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
			bool getable = (level <= PlayerData.instance.playerLevel);
			getTextObject.SetActive(getable);
			if (getable && CashShopData.instance.IsPurchasedFlag(CashShopData.eCashFlagType.LevelPass))
				AlarmObject.Show(alarmRootTransform);
			proceedingCountText.gameObject.SetActive(!getable);
			proceedingCountSlider.gameObject.SetActive(!getable);
			if (!getable)
			{
				proceedingCountText.text = string.Format("{0:N0} / {1:N0}", PlayerData.instance.playerLevel, level);
				proceedingCountSlider.value = (float)PlayerData.instance.playerLevel / level;
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

		if (CashShopData.instance.IsPurchasedFlag(CashShopData.eCashFlagType.LevelPass) == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LevelPassUI_PurchaseFirst"), 2.0f);
			return;
		}

		if (getTextObject.activeSelf == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LevelPassUI_NotEnoughLevel"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestGetLevelPassReward(_level, _energy, () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
			{
				Initialize(_level, _energy);
				MainCanvas.instance.RefreshLevelPassAlarmObject();
				CommonRewardCanvas.instance.RefreshReward(0, 0, _energy);
			});
		});
	}
}
