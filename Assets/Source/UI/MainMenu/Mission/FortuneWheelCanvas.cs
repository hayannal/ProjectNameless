using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;
using DG.Tweening;

public class FortuneWheelCanvas : SimpleCashCanvas
{
	public static FortuneWheelCanvas instance;

	public Button backButton;
	public Button spinButton;

	public CurrencySmallInfo currencySmallInfo;
	public Text stageText;
	public GameObject goldWheelBonusTextObject;
	public RectTransform priceTextRectTransform;
	public GameObject goldIconObject;

	public Text[] rewardTextList;
	public RectTransform circleRectTransform;
	public RectTransform pointerRectTransform;
	public Animator pointerAnimator;
	public Image lightImage;

	public GameObject fortuneWheelRootObject;
	public GameObject wheelImageObject;
	public GameObject goldWheelImageObject;
	public GameObject centerImageObject;
	public GameObject goldCenterImageObject;
	public Image arrowImage;
	public GameObject goldArrowImageObject;

	public RectTransform alarmRootTransform;

	public const int sectorsCount = 7;


	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (restore)
			return;

		RefreshInfo();
	}

	void OnDisable()
	{
		if (StackCanvas.Pop(gameObject))
			return;

		// 인포창 같은거에 stacked 되어서 disable 상태중에 Home키를 누르면 InfoCamera 모드를 복구해야한다.
		// 이걸 위해 OnPop Action으로 감싸고 Push할때 넣어둔다.
		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

	}

	public void RefreshInfo()
	{
		stageText.text = string.Format("STAGE {0:N0}", PlayerData.instance.currentRewardStage);
		lightImage.gameObject.SetActive(false);
		RefreshReward();
		RefreshCost();
	}

	ObscuredInt _currentIndex = 0;
	List<ObscuredInt> _listReward = new List<ObscuredInt>();
	void RefreshReward()
	{
		bool useGoldWheel = (SubMissionData.instance.fortuneWheelDailyCount > 0);
		GoldWheelOn(useGoldWheel);

		StageBetTableData stageBetTableData = TableDataManager.instance.FindStageBetTableData(PlayerData.instance.currentRewardStage);
		if (stageBetTableData == null)
			return;

		int rate = 1;
		if (useGoldWheel) rate = BattleInstanceManager.instance.GetCachedGlobalConstantInt("FortuneWheelGolden");

		_listReward.Clear();
		_listReward.Add(stageBetTableData.roulette_1 * rate);
		_listReward.Add(stageBetTableData.roulette_2 * rate);
		_listReward.Add(stageBetTableData.roulette_3 * rate);
		_listReward.Add(stageBetTableData.roulette_4 * rate);
		_listReward.Add(stageBetTableData.roulette_5 * rate);
		_listReward.Add(stageBetTableData.roulette_6 * rate);
		_listReward.Add(stageBetTableData.roulette_7 * rate);

		for (int i = 0; i < _listReward.Count; ++i)
			rewardTextList[i].text = _listReward[i].ToString("N0");

		AlarmObject.Hide(alarmRootTransform);
		if (!useGoldWheel)
			AlarmObject.Show(alarmRootTransform);
	}

	void RefreshCost()
	{
		if (SubMissionData.instance.fortuneWheelDailyCount == 0)
		{
			int cost = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyRoulette");
			priceText.text = cost.ToString("N0");
			goldIconObject.SetActive(true);
			priceTextRectTransform.anchoredPosition = new Vector2(16.0f, priceTextRectTransform.anchoredPosition.y);
		}
		else
		{
			goldIconObject.SetActive(false);
			priceTextRectTransform.anchoredPosition = new Vector2(0.0f, priceTextRectTransform.anchoredPosition.y);

			//iapButton.productId = costumeTableData.serverItemId;
			ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData("roulette");
			if (shopProductTableData != null)
				RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
		}
	}

	int _randomResult;
	public void OnClickSpinButton()
	{
		if (CurrencyData.instance.CheckMaxGold())
			return;

		if (SubMissionData.instance.fortuneWheelDailyCount == 0)
		{
			Spin();
		}
		else if (SubMissionData.instance.fortuneWheelDailyCount == 1)
		{
			// 실제 구매
			// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
			PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
		}
		else
		{
			// 오늘은 더이상 굴릴 수 없다.
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TodayCountComplete"), 2.0f);
		}
	}

	void Spin()
	{
		_randomResult = Random.Range(0, sectorsCount);

		_currentIndex += _randomResult;
		if (_currentIndex >= sectorsCount)
			_currentIndex -= sectorsCount;

		//Debug.LogFormat("reward = {0:N0}", _listReward[_currentIndex]);
		AlarmObject.Hide(alarmRootTransform);

		int useEnergy = 0;
		bool consume = false;
		if (SubMissionData.instance.fortuneWheelDailyCount == 0)
			useEnergy = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyRoulette");
		else if (SubMissionData.instance.fortuneWheelDailyCount == 1)
			consume = true;

		PlayFabApiManager.instance.RequestFortuneWheel(_listReward[_currentIndex], useEnergy, consume, () =>
		{
			if (useEnergy > 0)
			{
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.FreeFortuneWheel);
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.UseEnergy, useEnergy);
			}
			MainCanvas.instance.RefreshMissionAlarmObject();

			Timing.RunCoroutine(SpinProcess());
		});
	}

	bool _processed = false;
	const int BaseRotationCount = 8;
	const float Duration = 3.0f;
	IEnumerator<float> SpinProcess()
	{
		if (_processed)
			yield break;

		_processed = true;
		backButton.interactable = false;
		spinButton.interactable = false;
		lightImage.gameObject.SetActive(false);

		circleRectTransform.DOLocalRotate(new Vector3(0.0f, 0.0f, circleRectTransform.eulerAngles.z - 10.0f), 0.5f);
		yield return Timing.WaitForSeconds(0.5f);

		float targetZ = circleRectTransform.eulerAngles.z + 10.0f + 360.0f * BaseRotationCount;
		float sectorAngleDeg = 360f / sectorsCount;
		targetZ += sectorAngleDeg * _randomResult;
		float resultTargetZ = targetZ;
		targetZ += 10.0f;
		circleRectTransform.DOLocalRotate(new Vector3(0.0f, 0.0f, targetZ), Duration, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad);
		Timing.RunCoroutine(PointerAnimatorProcess(Duration));
		yield return Timing.WaitForSeconds(Duration + 0.05f);

		circleRectTransform.DOLocalRotate(new Vector3(0.0f, 0.0f, circleRectTransform.eulerAngles.z - 10.0f), 1.2f);
		yield return Timing.WaitForSeconds(1.2f);

		// 혹시나 트윈이 틀어졌을때를 대비해서 최종 결과를 한번 더 적용한다.
		circleRectTransform.eulerAngles = new Vector3(0.0f, 0.0f, resultTargetZ);

		lightImage.gameObject.SetActive(true);
		backButton.interactable = true;
		spinButton.interactable = true;
		_processed = false;

		currencySmallInfo.RefreshInfo();

		UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
		{
			CommonRewardCanvas.instance.RefreshReward(_listReward[_currentIndex], 0, 0, () =>
			{
				RefreshInfo();
				fortuneWheelRootObject.SetActive(false);
				fortuneWheelRootObject.SetActive(true);
			});
		});
	}

	IEnumerator<float> PointerAnimatorProcess(float duration)
	{
		yield return Timing.WaitForSeconds(0.15f);
		pointerAnimator.speed = 1.0f;
		pointerAnimator.enabled = true;

		yield return Timing.WaitForSeconds(duration - 0.15f - 0.15f);
		pointerAnimator.speed = 0.5f;
		yield return Timing.WaitForSeconds(0.3f);

		pointerAnimator.enabled = false;
		pointerRectTransform.localRotation = Quaternion.identity;
	}

	public void GoldWheelOn(bool on)
	{
		wheelImageObject.SetActive(!on);
		goldWheelImageObject.SetActive(on);
		centerImageObject.SetActive(!on);
		goldCenterImageObject.SetActive(on);
		arrowImage.enabled = !on;
		goldArrowImageObject.SetActive(on);
		goldWheelBonusTextObject.SetActive(on);
	}





	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product);
	}

	public static void ExternalRequestServerPacket(Product product)
	{
#if UNITY_ANDROID
		GooglePurchaseData data = new GooglePurchaseData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (uint)product.metadata.localizedPrice * 100, data.inAppPurchaseData, data.inAppDataSignature, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload, () =>
#endif
		{

			// 정상적인 구매에서는 하던대로 스핀 다 돌려서 처리하면 된다.
			if (instance != null && instance.gameObject.activeSelf)
			{
				CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.FortuneWheel);
				instance.Spin();
			}

			CodelessIAPStoreListener.Instance.StoreController.ConfirmPendingPurchase(product);
			IAPListenerWrapper.instance.CheckConfirmPendingPurchase(product);

		}, (error) =>
		{
			if (error.Error == PlayFab.PlayFabErrorCode.ReceiptAlreadyUsed)
			{
				CodelessIAPStoreListener.Instance.StoreController.ConfirmPendingPurchase(product);
				IAPListenerWrapper.instance.CheckConfirmPendingPurchase(product);
			}
		});
	}

	// 해당 캔버스를 열지 않고 복구 로직을 진행하려면 
	public static void ExternalRetryPurchase(Product product)
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress", product.metadata.localizedTitle), () =>
		{
			WaitingNetworkCanvas.Show(true);
			ExternalRequestServerPacket(product);
		}, () =>
		{
		}, true);
	}

	public static void ConsumeProduct()
	{
		// PetSaleCanvas와 달리 창에서 랜덤을 돌려서 구하는 구조라서
		// 창이 켜있을때의 구매는 원래 로직대로 돌린다.
		// 그러나 재구동 후의 컨슘에서는 패킷만 보내서 복구하는 식으로 처리한다.
		StageBetTableData stageBetTableData = TableDataManager.instance.FindStageBetTableData(PlayerData.instance.currentRewardStage);
		if (stageBetTableData == null)
			return;

		int rate = BattleInstanceManager.instance.GetCachedGlobalConstantInt("FortuneWheelGolden");

		List<int> listReward = new List<int>();
		listReward.Add(stageBetTableData.roulette_1 * rate);
		listReward.Add(stageBetTableData.roulette_2 * rate);
		listReward.Add(stageBetTableData.roulette_3 * rate);
		listReward.Add(stageBetTableData.roulette_4 * rate);
		listReward.Add(stageBetTableData.roulette_5 * rate);
		listReward.Add(stageBetTableData.roulette_6 * rate);
		listReward.Add(stageBetTableData.roulette_7 * rate);

		int randomIndex = Random.Range(0, listReward.Count);
		PlayFabApiManager.instance.RequestConsumeFortuneWheel(listReward[randomIndex], () =>
		{
			MainCanvas.instance.RefreshMissionAlarmObject();

			UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
			{
				CommonRewardCanvas.instance.RefreshReward(listReward[randomIndex], 0, 0);
			});
		});
	}
}