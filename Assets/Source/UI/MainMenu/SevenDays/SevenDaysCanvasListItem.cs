using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SevenDaysCanvasListItem : MonoBehaviour
{
	public RewardIcon sevenDaysRewardIcon;
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
		sevenDaysRewardIcon.RefreshReward("it", "Cash_sSevenTotal", sevenDaysRewardTableData.sumPoint);
		rewardIcon.RefreshReward(sevenDaysRewardTableData.rewardType, sevenDaysRewardTableData.rewardValue, sevenDaysRewardTableData.rewardCount);
		nameText.SetLocalizedText(UIString.instance.GetString(sevenDaysRewardTableData.descriptionId, sevenDaysRewardTableData.needCount));

		// 버튼
		int currentCount = MissionData.instance.GetProceedingCount(sevenDaysRewardTableData.typeId);
		proceedingCountText.text = string.Format("{0:N0} / {1:N0}", Mathf.Min(currentCount, sevenDaysRewardTableData.needCount), sevenDaysRewardTableData.needCount);
		proceedingCountSlider.value = (float)currentCount / sevenDaysRewardTableData.needCount;
		bool received = MissionData.instance.IsGetSevenDaysReward(sevenDaysRewardTableData.day, sevenDaysRewardTableData.num);
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

	public void OnClickSecondRewardButton()
	{
		// 여긴 토탈 점수 관여 없으니 it에 대해서만 자세히 보기만 처리해주면 된다.
		switch (_sevenDaysRewardTableData.rewardType)
		{
			case "it":
				RewardIcon.ShowDetailInfo(_sevenDaysRewardTableData.rewardType, _sevenDaysRewardTableData.rewardValue);
				break;
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

		PlayFabApiManager.instance.RequestGetSevenDaysReward(_sevenDaysRewardTableData, OnRecvResult);
	}

	void OnRecvResult(string itemGrantString)
	{
		// 직접 수령이 있는 곳이라서 별도로 처리한다.
		if (_sevenDaysRewardTableData.rewardType == "it" && string.IsNullOrEmpty(itemGrantString) == false)
		{
			FestivalExchangeConfirmCanvas.GetItReward(_sevenDaysRewardTableData.rewardValue, itemGrantString, _sevenDaysRewardTableData.rewardCount);
		}

		Initialize(_sevenDaysRewardTableData);
		SevenDaysTabCanvas.instance.currencySmallInfo.RefreshInfo();
		SevenDaysCanvas.instance.RefreshSumReward();
		SevenDaysCanvas.instance.RefreshDayAlarmObject();
		SevenDaysTabCanvas.instance.RefreshAlarmObject();
		MainCanvas.instance.RefreshSevenDaysAlarmObject();
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);
	}
}
