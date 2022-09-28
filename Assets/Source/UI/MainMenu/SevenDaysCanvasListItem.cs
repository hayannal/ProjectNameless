using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SevenDaysCanvasListItem : MonoBehaviour
{
	public Text sumPointRewardText;
	public RewardIcon rewardIcon;
	public Text nameText;
	public Slider proceedingCountSlider;
	public Text proceedingCountText;
	public GameObject notAchievedObject;
	public GameObject getTextObject;
	public GameObject receivedTextObject;
	public GameObject blackObject;
	public RectTransform alarmRootTransform;

	bool _initialized;
	SevenDaysRewardTableData _sevenDaysRewardTableData;
	public void Initialize(SevenDaysRewardTableData sevenDaysRewardTableData)
	{
		_sevenDaysRewardTableData = sevenDaysRewardTableData;
		_initialized = true;

		// 리워드
		sumPointRewardText.text = sevenDaysRewardTableData.sumPoint.ToString("N0");
		rewardIcon.RefreshReward(sevenDaysRewardTableData.rewardType, sevenDaysRewardTableData.rewardValue, sevenDaysRewardTableData.rewardCount);
		nameText.SetLocalizedText(UIString.instance.GetString(sevenDaysRewardTableData.descriptionId, sevenDaysRewardTableData.needCount));

		// 버튼
		int currentCount = MissionData.instance.GetProceedingCount(sevenDaysRewardTableData.typeId);
		proceedingCountText.text = string.Format("{0:N0} / {1:N0}", Mathf.Min(currentCount, sevenDaysRewardTableData.needCount), sevenDaysRewardTableData.needCount);
		proceedingCountSlider.value = (float)currentCount / sevenDaysRewardTableData.needCount;
		//bool received = CashShopData.instance.IsGetEnergyPaybackReward(use);
		bool received = false;
		receivedTextObject.SetActive(received);
		blackObject.SetActive(received);
		AlarmObject.Hide(alarmRootTransform);
		if (received)
		{
			notAchievedObject.SetActive(false);
			getTextObject.SetActive(false);
		}
		else
		{
			
			bool getable = (MissionData.instance.IsOpenDay(sevenDaysRewardTableData.day) && sevenDaysRewardTableData.needCount <= currentCount);
			notAchievedObject.SetActive(!getable);
			getTextObject.SetActive(getable);
			if (getable)
				AlarmObject.Show(alarmRootTransform);
		}
	}

	public void OnClickButton()
	{
		if (MissionData.instance.IsOpenDay(_sevenDaysRewardTableData.day) == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("SevenDaysUI_NotOpened"), 2.0f);
			return;
		}

		if (blackObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LevelPassUI_AlreadyClaimed"), 2.0f);
			return;
		}

		if (getTextObject.activeSelf == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("SevenDaysUI_NotEnoughCondition"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestGetSevenDaysReward(_sevenDaysRewardTableData, () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
			{
				Initialize(_sevenDaysRewardTableData);
				//MainCanvas.instance.RefreshEnergyPaybackAlarmObject();
				CommonRewardCanvas.instance.RefreshReward(_sevenDaysRewardTableData.rewardType, _sevenDaysRewardTableData.rewardValue, _sevenDaysRewardTableData.rewardCount);
			});
		});
	}
}
