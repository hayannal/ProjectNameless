using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using MEC;

public class ContinuousShopProductCanvas : SimpleCashEventCanvas
{
	public static ContinuousShopProductCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public GameObject[] allPurchasedObjectList;
	public Sprite[] backgroundSpriteList;

	void Awake()
	{
		instance = this;
	}

	bool _started = false;
	ContinuousShopProductInfo[] _continuousShopProductInfoList;
	void Start()
	{
		_continuousShopProductInfoList = transform.GetComponentsInChildren<ContinuousShopProductInfo>(true);
		_started = true;
	}

	void OnEnable()
	{
		SetInfo();

		//MainCanvas.instance.OnEnterCharacterMenu(true);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		bool allPurchased = false;
		EventTypeTableData eventTypeTableData = TableDataManager.instance.FindEventTypeTableData(cashEventId);
		if (eventTypeTableData != null)
		{
			if (CashShopData.instance.GetContinuousProductStep(cashEventId) >= eventTypeTableData.productCount)
				allPurchased = true;
		}
		for (int i = 0; i < allPurchasedObjectList.Length; ++i)
			allPurchasedObjectList[i].SetActive(allPurchased);

		if (_started == false)
			return;
		if (_continuousShopProductInfoList == null)
			return;

		RefreshActiveList();
	}

	public void RefreshActiveList()
	{
		for (int i = 0; i < _continuousShopProductInfoList.Length; ++i)
			_continuousShopProductInfoList[i].RefreshActive();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		//MainCanvas.instance.OnEnterCharacterMenu(false);
	}








	public void OnClickRewardButton(RewardIcon rewardIcon)
	{
		if (rewardIcon == null)
			return;
		if (rewardIcon.eventRewardId == "")
			return;
		EventRewardTableData eventRewardTableData = TableDataManager.instance.FindEventRewardTableData(rewardIcon.eventRewardId, rewardIcon.num);
		if (eventRewardTableData == null)
			return;

		string rewardType = eventRewardTableData.rewardType;
		string rewardValue = eventRewardTableData.rewardValue;

		if (rewardType == "it")
		{
			if (rewardValue.StartsWith("Cash_s"))
			{
				ConsumeItemTableData consumeItemTableData = TableDataManager.instance.FindConsumeItemTableData(rewardValue);
				if (consumeItemTableData != null)
					TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString(consumeItemTableData.name), 120, rewardIcon.iconRootTransform, new Vector2(0.0f, -45.0f));
			}
			else
				RewardIcon.ShowDetailInfo(rewardType, rewardValue);
		}
	}


	#region Detail Info
	public void OnClickPetDetailButton(string rewardValue)
	{
		Timing.RunCoroutine(ShowPetDetailCanvasProcess(rewardValue));
	}

	IEnumerator<float> ShowPetDetailCanvasProcess(string rewardValue)
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		gameObject.SetActive(false);

		while (gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		// Pet
		MainCanvas.instance.OnClickPetButton();

		while ((PetListCanvas.instance != null && PetListCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		// 아무래도 카운트는 없는게 맞아보인다
		int count = 0;
		PetData petData = PetManager.instance.GetPetData(rewardValue);
		if (petData != null) count = petData.count;
		PetListCanvas.instance.OnClickListItem(rewardValue, count);

		while ((PetInfoCanvas.instance != null && PetInfoCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.4f);
	}

	public void OnClickEquipDetailButton(string rewardValue)
	{
		Timing.RunCoroutine(ShowEquipDetailCanvasProcess(rewardValue));
	}

	IEnumerator<float> ShowEquipDetailCanvasProcess(string rewardValue)
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		gameObject.SetActive(false);

		while (gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		MissionListCanvas.ShowCanvasAsyncWithPrepareGround("PickUpEquipDetailCanvas", null);

		while ((PickUpEquipDetailCanvas.instance != null && PickUpEquipDetailCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;
		PickUpEquipDetailCanvas.instance.RefreshInfo(rewardValue);
		PickUpEquipDetailCanvas.instance.SetRestoreCanvas(cashEventId);

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.4f);
	}
	#endregion






	// 해당 캔버스를 열지 않고 복구 로직을 진행하려면 
	public static void ExternalRetryPurchase(Product product)
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress", product.metadata.localizedTitle), () =>
		{
			WaitingNetworkCanvas.Show(true);
			ContinuousShopProductInfo.ExternalRequestServerPacket(product, null, null, null);
		}, () =>
		{
		}, true);
	}
}