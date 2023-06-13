using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class ContinuousShopProductInfo : SimpleCashCanvas
{
	public string idExcludeEventId = "_conti";
	public int num = 1;

	public IAPButton iapButton;
	public GameObject cashButtonObject;
	public GameObject freeButtonObject;
	public RectTransform alarmRootTransform;

	public Image blurImage;
	public Image backgroundImge;

	ContinuousShopProductCanvas _simpleCashEventCanvas;
	ShopProductTableData _shopProductTableData;

	void Start()
	{
		string cashEventId = "ev1";
		ContinuousShopProductCanvas simpleCashEventCanvas = transform.GetComponentInParent<ContinuousShopProductCanvas>();
		if (simpleCashEventCanvas != null)
		{
			cashEventId = simpleCashEventCanvas.cashEventId;
			_simpleCashEventCanvas = simpleCashEventCanvas;
		}

		string id = string.Format("{0}{1}_{2}", cashEventId, idExcludeEventId, num);
		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(id);
		if (shopProductTableData == null)
			return;

		if (shopProductTableData.free)
		{
			cashButtonObject.SetActive(false);
			freeButtonObject.SetActive(true);
		}
		else
		{
			cashButtonObject.SetActive(true);
			freeButtonObject.SetActive(false);
			iapButton.productId = shopProductTableData.serverItemId;
			RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
		}

		_shopProductTableData = shopProductTableData;

		blurImage.color = (shopProductTableData.free == false) ? new Color(0.65f, 0.65f, 0.65f, 0.2f) : new Color(0.44f, 0.44f, 0.44f, 0.5f);
		backgroundImge.color = (shopProductTableData.free == false) ? new Color(1.0f, 1.0f, 1.0f, 0.82f) : new Color(1.0f, 1.0f, 1.0f, 0.68f);
		backgroundImge.sprite = _simpleCashEventCanvas.backgroundSpriteList[(shopProductTableData.free == false) ? 0 : 1];

		RefreshActive();

		RewardIcon[] listRewardIcon = GetComponentsInChildren<RewardIcon>(true);
		for (int i = 0; i < listRewardIcon.Length; ++i)
		{
			bool applyOnlyIcon = false;
			EventRewardTableData eventRewardTableData = TableDataManager.instance.FindEventRewardTableData(listRewardIcon[i].eventRewardId, listRewardIcon[i].num);
			if (eventRewardTableData != null)
			{
				if (eventRewardTableData.rewardType == "cu" || (eventRewardTableData.rewardValue.StartsWith("Cash_s") && eventRewardTableData.rewardValue.Contains("EquipTypeGacha") == false))
					applyOnlyIcon = true;
			}
			if (applyOnlyIcon == false)
				continue;

			listRewardIcon[i].ShowOnlyIcon(true);
			listRewardIcon[i].ActivePunchAnimation(true);
			listRewardIcon[i].iconRootTransform.GetComponent<Button>().enabled = false;
		}
	}

	public void RefreshActive()
	{
		if (_simpleCashEventCanvas == null)
			return;

		int currentCompleteStep = CashShopData.instance.GetContinuousProductStep(_simpleCashEventCanvas.cashEventId);
		if (num <= currentCompleteStep)
		{
			gameObject.SetActive(false);
			return;
		}

		// alarm
		if (_shopProductTableData != null)
		{
			if (_shopProductTableData.free)
			{
				if (num == (currentCompleteStep + 1))
					AlarmObject.Show(alarmRootTransform);
				else
					AlarmObject.Hide(alarmRootTransform);
			}
			else
			{
				AlarmObject.Hide(alarmRootTransform);
			}
		}

		gameObject.SetActive(true);
	}




	// SimpleCashCanvas 과 다르게 구현하는거만 처리
	public void OnClickCustomButton()
	{
		int currentCompleteStep = CashShopData.instance.GetContinuousProductStep(_simpleCashEventCanvas.cashEventId);
		if (num > (currentCompleteStep + 1))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ContiUI_PurchaseFirst"), 2.0f);
			return;
		}
		if (num <= currentCompleteStep)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ContiUI_AlreadyPurchased"), 2.0f);
			return;
		}

		if (freeButtonObject.activeSelf)
		{
			PlayFabApiManager.instance.RequestGetContinuousProduct(_simpleCashEventCanvas.cashEventId, _shopProductTableData, currentCompleteStep, () =>
			{
				ExternalOnPurchased(null, this, _simpleCashEventCanvas, _shopProductTableData);
			});
		}
		else
		{
			// 실제 구매
			// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
			PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
		}
	}

	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product, this, _simpleCashEventCanvas, _shopProductTableData);
	}

	public static void ExternalRequestServerPacket(Product product, ContinuousShopProductInfo infoInstance, SimpleCashEventCanvas instance, ShopProductTableData shopProductTableData)
	{
#if UNITY_ANDROID
		GooglePurchaseData data = new GooglePurchaseData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (uint)product.metadata.localizedPrice * 100, data.inAppPurchaseData, data.inAppDataSignature, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload, () =>
#endif
		{
			ExternalOnPurchased(product, infoInstance, instance, shopProductTableData);

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

	public static void ExternalOnPurchased(Product product, ContinuousShopProductInfo infoInstance, SimpleCashEventCanvas instance, ShopProductTableData shopProductTableData)
	{
		if (shopProductTableData == null)
		{
			if (product != null)
				shopProductTableData = TableDataManager.instance.FindShopProductTableDataByServerItemId(product.definition.id);
		}
		if (shopProductTableData != null)
		{
			CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType1, shopProductTableData.rewardValue1, shopProductTableData.rewardCount1);
			CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType2, shopProductTableData.rewardValue2, shopProductTableData.rewardCount2);
			CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType3, shopProductTableData.rewardValue3, shopProductTableData.rewardCount3);
			CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType4, shopProductTableData.rewardValue4, shopProductTableData.rewardCount4);
			CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType5, shopProductTableData.rewardValue5, shopProductTableData.rewardCount5);
		}

		WaitingNetworkCanvas.Show(false);
		if (shopProductTableData != null)
		{
			UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
			{
				CommonRewardCanvas.instance.RefreshReward(shopProductTableData);
			});
		}

		if (infoInstance != null)
			infoInstance.gameObject.SetActive(false);
		if (instance != null)
		{
			ContinuousShopProductCanvas continuousShopProductCanvas = instance.GetComponent<ContinuousShopProductCanvas>();
			if (continuousShopProductCanvas != null)
				continuousShopProductCanvas.RefreshActiveList();
		}

		string cashEventId = "";
		if (instance != null) cashEventId = instance.cashEventId;
		else if (product != null)
		{
			string[] split = product.definition.id.Split('_');
			if (split.Length == 3 && split[0].Contains("ev"))
				cashEventId = split[0];
		}
		if (cashEventId == "ev4")
		{
			if (MainCanvas.instance != null)
				MainCanvas.instance.RefreshContinuousProduct1AlarmObject();
			if (product != null)
			{
				CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.Ev4ContiNext);
				PlayFabApiManager.instance.RequestConsumeContinuousNext(cashEventId, true, null);
			}
		}
		bool closeCanvas = false;
		EventTypeTableData eventTypeTableData = TableDataManager.instance.FindEventTypeTableData(cashEventId);
		if (eventTypeTableData != null)
		{
			if (CashShopData.instance.GetContinuousProductStep(cashEventId) >= eventTypeTableData.productCount)
				closeCanvas = true;
		}
		if (closeCanvas)
		{
			if (CashShopData.instance.IsShowEvent(cashEventId))
			{
				PlayFabApiManager.instance.RequestCloseCashEvent(cashEventId, () =>
				{
					if (MainCanvas.instance != null && MainCanvas.instance.gameObject.activeSelf)
						MainCanvas.instance.CloseCashEventButton(cashEventId);
				});
			}

			if (instance.gameObject != null)
				instance.gameObject.SetActive(false);
		}
	}
}