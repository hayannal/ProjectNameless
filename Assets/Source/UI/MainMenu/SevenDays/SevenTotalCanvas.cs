using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class SevenTotalCanvas : SimpleCashCanvas
{
	public static SevenTotalCanvas instance;

	public Text remainTimeText;

	public Text[] priceTextList;
	public IAPButton[] iapButtonList;
	public Button[] iapBridgeButtonList;
	public GameObject[] priceObjectList;
	public GameObject[] completeObjectList;
	public GameObject[] blackObjectList;

	public RewardIcon[] bigRewardIconList;
	public RewardIcon[] smallRewardIconList;

	void Awake()
	{
		instance = this;
	}

	List<ShopProductTableData> _listShopProductTableData = new List<ShopProductTableData>();
	void Start()
	{
		for (int i = 0; i < 4; ++i)
		{
			string id = string.Format("seventotalgroup{0}_{1}", MissionData.instance.sevenDaysId, (i + 1));
			ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(id);
			if (shopProductTableData != null)
			{
				_listShopProductTableData.Add(shopProductTableData);
				priceText = priceTextList[i];
				RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
				iapButtonList[i].productId = shopProductTableData.serverItemId;
			}
		}

		for (int i = 0; i < bigRewardIconList.Length; ++i)
		{
			bigRewardIconList[i].ShowOnlyIcon(true);
			bigRewardIconList[i].ActivePunchAnimation(true);
		}
		for (int i = 0; i < smallRewardIconList.Length; ++i)
		{
			smallRewardIconList[i].ShowOnlyIcon(true, 1.1f);
			smallRewardIconList[i].ActivePunchAnimation(true);
		}
	}

	void OnEnable()
	{
		RefreshButtonState();
		SetRemainTimeInfo();
	}

	void Update()
	{
		UpdateRemainTime();
	}

	void SetRemainTimeInfo()
	{
		if (MissionData.instance.sevenDaysId == 0)
			return;

		// show 상태가 아니면 안보이겠지만 혹시 모르니 안전하게 구해온다.
		_sevenDaysExpireDateTime = MissionData.instance.sevenDaysExpireTime;
	}

	DateTime _sevenDaysExpireDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateRemainTime()
	{
		if (ServerTime.UtcNow < _sevenDaysExpireDateTime)
		{
			if (remainTimeText != null)
			{
				TimeSpan remainTime = _sevenDaysExpireDateTime - ServerTime.UtcNow;
				if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
				{
					if (remainTime.Days > 0)
						remainTimeText.text = string.Format("{0}d {1:00}:{2:00}:{3:00}", remainTime.Days, remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
					else
						remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
					_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
				}
			}
		}
		else
		{
			// 이벤트 기간이 끝났으면 닫아버리는게 제일 편하다.
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_EventExpired"), 2.0f);
			SevenDaysTabCanvas.instance.gameObject.SetActive(false);
		}
	}

	void RefreshButtonState()
	{
		for (int i = 0; i < 4; ++i)
		{
			bool rewarded = MissionData.instance.IsPurchasedCashSlot(i);
			priceObjectList[i].SetActive(!rewarded);
			completeObjectList[i].SetActive(rewarded);
			blackObjectList[i].SetActive(rewarded);
		}
	}


	public int buttonIndex { get { return _buttonIndex; } }
	int _buttonIndex;
	public void OnClickButton(int index)
	{
		if (blackObjectList[index].activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyThatItem"), 2.0f);
			return;
		}

		_buttonIndex = index;

		// 실제 구매
		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponseCustom, null, true);
	}

	public void OnResponseCustom()
	{
		WaitingNetworkCanvas.Show(true);
		iapBridgeButtonList[_buttonIndex].onClick.Invoke();
		//iapBridgeButton.onClick.Invoke();
	}

	ShopProductTableData GetShopProduct(string serverItemId)
	{
		for (int i = 0; i < _listShopProductTableData.Count; ++i)
		{
			if (_listShopProductTableData[i].serverItemId == serverItemId)
				return _listShopProductTableData[i];
		}
		return null;
	}

	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product, this);
	}

	public static void ExternalRequestServerPacket(Product product, SevenTotalCanvas instance)
	{
#if UNITY_ANDROID
		GooglePurchaseData data = new GooglePurchaseData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (uint)product.metadata.localizedPrice * 100, data.inAppPurchaseData, data.inAppDataSignature, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload, () =>
#endif
		{
			ShopProductTableData shopProductTableData = null;
			if (instance != null)
				shopProductTableData = instance.GetShopProduct(product.definition.id);
			else
				shopProductTableData = TableDataManager.instance.FindShopProductTableDataByServerItemId(product.definition.id);
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

			int buttonIndex = 0;
			if (instance != null) buttonIndex = instance.buttonIndex;
			else if (product != null)
			{
				string[] split = product.definition.id.Split('_');
				if (split.Length == 2 && split[0].Contains("seventotalgroup"))
				{
					int.TryParse(split[1], out buttonIndex);
				}
			}
			CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.SevenSlot0 + buttonIndex);
			PlayFabApiManager.instance.RequestConsumeSevenSlot(buttonIndex, () =>
			{
				if (instance != null)
					instance.RefreshButtonState();
			});

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
			ExternalRequestServerPacket(product, null);
		}, () =>
		{
		}, true);
	}
}