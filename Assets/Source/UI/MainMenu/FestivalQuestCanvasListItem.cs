using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FestivalQuestCanvasListItem : MonoBehaviour
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

	FestivalCollectTableData _festivalCollectTableData;
	public void Initialize(FestivalCollectTableData festivalCollectTableData)
	{
		_festivalCollectTableData = festivalCollectTableData;

		// 리워드
		sumPointRewardText.text = festivalCollectTableData.festivalPoint.ToString("N0");
		//rewardIcon.RefreshReward(festivalCollectTableData.rewardType, festivalCollectTableData.rewardValue, festivalCollectTableData.rewardCount);
		nameText.SetLocalizedText(UIString.instance.GetString(festivalCollectTableData.descriptionId, festivalCollectTableData.needCount));

		// 버튼
		int currentCount = FestivalData.instance.GetProceedingCount(festivalCollectTableData.typeId);
		proceedingCountText.text = string.Format("{0:N0} / {1:N0}", Mathf.Min(currentCount, festivalCollectTableData.needCount), festivalCollectTableData.needCount);
		proceedingCountSlider.value = (float)currentCount / festivalCollectTableData.needCount;
		bool received = FestivalData.instance.IsGetFestivalCollect(festivalCollectTableData.num);
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

			bool getable = (festivalCollectTableData.needCount <= currentCount);
			notAchievedObject.SetActive(!getable);
			getTextObject.SetActive(getable);
			if (getable)
				AlarmObject.Show(alarmRootTransform);
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
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("SevenDaysUI_NotEnoughCondition"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestGetFestivalCollect(_festivalCollectTableData, () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
			{
				Initialize(_festivalCollectTableData);
				FestivalTabCanvas.instance.currencySmallInfo.RefreshInfo();
				//FestivalQuestCanvas.instance.RefreshSumReward();
				//FestivalQuestCanvas.instance.RefreshDayAlarmObject();
				FestivalTabCanvas.instance.RefreshAlarmObject();
				MainCanvas.instance.RefreshFestivalAlarmObject();
				//CommonRewardCanvas.instance.RefreshReward(_sevenDaysRewardTableData.rewardType, _sevenDaysRewardTableData.rewardValue, _sevenDaysRewardTableData.rewardCount);
			});
		});
	}
}