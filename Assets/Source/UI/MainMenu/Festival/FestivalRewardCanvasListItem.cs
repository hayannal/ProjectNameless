using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FestivalRewardCanvasListItem : MonoBehaviour
{
	public RewardIcon rewardIcon;
	public Image sumPointImage;
	public Text sumPointNeedCountText;
	public Text remainCountText;

	public GameObject notAchievedObject;
	public GameObject getTextObject;
	public GameObject completeTextObject;
	public GameObject blackObject;
	//public RectTransform alarmRootTransform;

	FestivalExchangeTableData _festivalExchangeTableData;
	public void Initialize(FestivalExchangeTableData festivalExchangeTableData)
	{
		_festivalExchangeTableData = festivalExchangeTableData;

		FestivalTypeTableData festivalTypeTableData = TableDataManager.instance.FindFestivalTypeTableData(festivalExchangeTableData.groupId);

		// 필요수량
		sumPointNeedCountText.text = festivalExchangeTableData.neededCount.ToString("N0");

		AddressableAssetLoadManager.GetAddressableSprite(festivalTypeTableData.iconAddress, "Icon", (sprite) =>
		{
			sumPointImage.sprite = null;
			sumPointImage.sprite = sprite;
		});

		// 교환 수량
		int exchangeCount = FestivalData.instance.GetExchangeTime(festivalExchangeTableData.num);
		remainCountText.text = string.Format("{0:N0} / {1:N0}", exchangeCount, festivalExchangeTableData.exchangeTimes);
		rewardIcon.RefreshReward(festivalExchangeTableData.rewardType, festivalExchangeTableData.rewardValue, festivalExchangeTableData.rewardCount);

		// 버튼
		int currentCount = FestivalData.instance.festivalSumPoint;
		bool complete = (exchangeCount >= festivalExchangeTableData.exchangeTimes);
		completeTextObject.SetActive(complete);
		blackObject.SetActive(complete);
		//AlarmObject.Hide(alarmRootTransform);
		if (complete)
		{
			notAchievedObject.SetActive(false);
			getTextObject.SetActive(false);
		}
		else
		{

			bool getable = (festivalExchangeTableData.neededCount <= currentCount);
			notAchievedObject.SetActive(!getable);
			getTextObject.SetActive(getable);
			//if (getable)
			//	AlarmObject.Show(alarmRootTransform);
		}
	}

	public void OnClickRewardButton()
	{
		// 여긴 토탈 점수 관여 없으니 it에 대해서만 자세히 보기만 처리해주면 된다.
		switch (_festivalExchangeTableData.rewardType)
		{
			case "it":
				RewardIcon.ShowDetailInfo(_festivalExchangeTableData.rewardType, _festivalExchangeTableData.rewardValue);
				break;
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
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("FestivalUI_NotEnoughPoint"), 2.0f);
			return;
		}

		if (_festivalExchangeTableData.rewardType == "cu" && _festivalExchangeTableData.rewardValue == "GO" && CurrencyData.instance.CheckMaxGold())
			return;

		UIInstanceManager.instance.ShowCanvasAsync("FestivalExchangeConfirmCanvas", () =>
		{
			FestivalExchangeConfirmCanvas.instance.RefreshInfo(_festivalExchangeTableData);
		});
	}
}