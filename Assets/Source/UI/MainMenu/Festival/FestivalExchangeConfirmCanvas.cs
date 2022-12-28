using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class FestivalExchangeConfirmCanvas : MonoBehaviour
{
	public static FestivalExchangeConfirmCanvas instance;

	public RewardIcon rewardIcon;
	public Text countText;
	public Image minusButtonImage;
	public Image plusButtonImage;
	public Image sumPointImage;
	public Text needCountText;

	void Awake()
	{
		instance = this;
	}

	FestivalExchangeTableData _festivalExchangeTableData;
	int _baseCount = 1;
	int _maxCount = 0;
	public void RefreshInfo(FestivalExchangeTableData festivalExchangeTableData)
	{
		_festivalExchangeTableData = festivalExchangeTableData;
		rewardIcon.RefreshReward(festivalExchangeTableData.rewardType, festivalExchangeTableData.rewardValue, festivalExchangeTableData.rewardCount);
		_baseCount = 1;
		countText.text = _baseCount.ToString("N0");
		needCountText.text = (_baseCount * festivalExchangeTableData.neededCount).ToString("N0");

		FestivalTypeTableData festivalTypeTableData = TableDataManager.instance.FindFestivalTypeTableData(festivalExchangeTableData.groupId);
		AddressableAssetLoadManager.GetAddressableSprite(festivalTypeTableData.iconAddress, "Icon", (sprite) =>
		{
			sumPointImage.sprite = null;
			sumPointImage.sprite = sprite;
		});

		int tempA = FestivalData.instance.festivalSumPoint / festivalExchangeTableData.neededCount;
		int tempB = festivalExchangeTableData.exchangeTimes - FestivalData.instance.GetExchangeTime(festivalExchangeTableData.num);
		_maxCount = Mathf.Min(tempA, tempB);

		minusButtonImage.color = (_baseCount == 1) ? Color.gray : Color.white;
		plusButtonImage.color = (_baseCount == _maxCount) ? Color.gray : Color.white;
	}

	public void OnClickMinusButton()
	{
		if (_baseCount > 1)
		{
			_baseCount -= 1;
			RefreshCount();
		}
	}

	public void OnClickPlusButton()
	{
		if (_baseCount < _maxCount)
		{
			_baseCount += 1;
			RefreshCount();
		}
	}

	public void OnClickMaxButton()
	{
		if (_baseCount != _maxCount)
		{
			_baseCount = _maxCount;
			RefreshCount();
		}
	}

	void RefreshCount()
	{
		countText.text = _baseCount.ToString("N0");
		needCountText.text = (_baseCount * _festivalExchangeTableData.neededCount).ToString("N0");
		minusButtonImage.color = (_baseCount == 1) ? Color.gray : Color.white;
		plusButtonImage.color = (_baseCount == _maxCount) ? Color.gray : Color.white;

		// rewardIcon 개수도 갱신
		rewardIcon.countText.text = (_baseCount * _festivalExchangeTableData.rewardCount).ToString("N0");
	}

	public void OnClickExchangeButton()
	{
		PlayFabApiManager.instance.RequestFestivalExchange(_festivalExchangeTableData, _baseCount, OnRecvResult);
	}

	void OnRecvResult(string itemGrantString)
	{
		// 직접 수령이 있는 곳이라서 별도로 처리한다.
		if (_festivalExchangeTableData.rewardType == "it" && string.IsNullOrEmpty(itemGrantString) == false)
		{
			GetItReward(_festivalExchangeTableData.rewardValue, itemGrantString, _baseCount * _festivalExchangeTableData.rewardCount);
		}

		ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);
		FestivalTabCanvas.instance.currencySmallInfo.RefreshInfo();
		FestivalRewardCanvas.instance.RefreshCount();
		FestivalRewardCanvas.instance.RefreshGrid();
		MainCanvas.instance.RefreshMenuButton();
		gameObject.SetActive(false);
	}

	public static void GetItReward(string rewardValue, string itemGrantString, int expectCount)
	{
		if (rewardValue.StartsWith("Spell_"))
		{
			List<ItemInstance> listItemInstance = SpellManager.instance.OnRecvItemGrantResult(itemGrantString, expectCount);
			if (listItemInstance == null)
				return;
		}
		else if (rewardValue.StartsWith("Actor"))
		{

		}
		else if (rewardValue.StartsWith("Pet_"))
		{
			List<ItemInstance> listItemInstance = PetManager.instance.OnRecvItemGrantResult(itemGrantString, expectCount);
			if (listItemInstance == null)
				return;
		}
		else if (rewardValue.StartsWith("Equip"))
		{
			List<ItemInstance> listItemInstance = EquipManager.instance.OnRecvItemGrantResult(itemGrantString, expectCount);
			if (listItemInstance == null)
				return;
		}
	}
}