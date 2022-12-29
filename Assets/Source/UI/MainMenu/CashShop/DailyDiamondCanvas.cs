using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;

public class DailyDiamondCanvas : SimpleCashCanvas
{
	public static DailyDiamondCanvas instance;

	public Text dayCountText;
	public Text buyingDiaText;
	public Text dailyDiaText;
	public DOTweenAnimation dailyDiaTweenAnimation;
	public RectTransform priceTextTransform;

	public Text addText;
	public GameObject remainDayTextObject;
	public Text remainDayText;
	public GameObject plusButtonObject;

	public Text receiveText;
	public DOTweenAnimation receiveTextTweenAnimation;
	public Text completeText;
	public Text remainTimeText;

	public RectTransform alarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	bool _started = false;
	void Start()
	{
		_started = true;
	}

	void OnEnable()
	{
		RefreshInfo();
	}

	bool _reserveAnimation;
	void Update()
	{
		if (_reserveAnimation)
		{
			dailyDiaTweenAnimation.DORestart();
			receiveTextTweenAnimation.DORestart();
			_reserveAnimation = false;
		}

		UpdateRemainTime();
		UpdateRefresh();
	}

	ShopProductTableData _shopProductTableData;
	ObscuredInt _dailyDiamondAmount;
	public void RefreshInfo()
	{
		_shopProductTableData = TableDataManager.instance.FindShopProductTableData("dailygem");
		_dailyDiamondAmount = BattleInstanceManager.instance.GetCachedGlobalConstantInt("DailyGemAmount");

		// 첫번째 항목이 캐시아이템 카운트다.
		dayCountText.text = _shopProductTableData.rewardCount1.ToString();
		// 두번째 항목이 처음 구매할때 지급하는 다이아다.
		buyingDiaText.text = _shopProductTableData.rewardCount2.ToString("N0");
		// 매일 얻는 다이아는 글로벌 인트에 있다.
		dailyDiaText.text = _dailyDiamondAmount.ToString("N0");

		AlarmObject.Hide(alarmRootTransform);

		// 서버에서 상태값을 받아와서 비교해야한다.
		if (CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.DailyDiamond) > 0)
		{
			priceTextTransform.gameObject.SetActive(false);
			addText.gameObject.SetActive(false);
			remainDayTextObject.SetActive(true);
			remainDayText.text = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.DailyDiamond).ToString();
			plusButtonObject.SetActive(CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.DailyDiamond) <= 3);

			// 이미 오늘자 보상을 받았는지 판단해야한다.
			if (CashShopData.instance.dailyDiamondReceived)
			{
				dailyDiaTweenAnimation.DOPause();
				receiveTextTweenAnimation.DOPause();
				receiveText.gameObject.SetActive(false);
				completeText.gameObject.SetActive(true);
				remainTimeText.gameObject.SetActive(true);
				_nextResetDateTime = PlayerData.instance.dayRefreshTime;
				_needUpdate = true;
			}
			else
			{
				if (_started)
				{
					dailyDiaTweenAnimation.DORestart();
					receiveTextTweenAnimation.DORestart();
				}
				else
					_reserveAnimation = true;
				receiveText.gameObject.SetActive(true);
				completeText.gameObject.SetActive(false);
				remainTimeText.gameObject.SetActive(false);
				_needUpdate = false;
				AlarmObject.Show(alarmRootTransform);
			}
		}
		else
		{
			dailyDiaTweenAnimation.DOPause();
			receiveTextTweenAnimation.DOPause();
			priceTextTransform.gameObject.SetActive(true);

			RefreshPrice(_shopProductTableData.serverItemId, _shopProductTableData.kor, _shopProductTableData.eng);

			addText.gameObject.SetActive(true);
			remainDayTextObject.SetActive(false);

			receiveText.gameObject.SetActive(false);
			completeText.gameObject.SetActive(false);
			remainTimeText.gameObject.SetActive(false);
			_needUpdate = false;
		}
	}

	DateTime _nextResetDateTime;
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		// 오리진 박스 처리와 다른게 있는데..
		// 오리진 박스는 패킷을 보내서 갱신 여부를 확인하고 갱신했었다.
		// 그런데 일일 다이아 패키지나 일일 무료 아이템은 어차피 하루에 한번 받는게 확정이라 sharedDailyPackageOpened를 클라에서 스스로 해제했다.
		// 이러다보니 하필 같은 타이밍에 데이터쪽에서 sharedDailyPackageOpened플래그를 false로 리셋하면
		// UpdateRemainTime 호출이 안되서 _needUpdate플래그가 켜지질 않게 됐다.
		// 하필 호출 순서가 데이터쪽에서의 Update 후에 캔버스 Update라서 데이터가 리셋해버리면 캔버스는 감지하지 못한채 끝나버리는거였다.
		// (순서를 바꾸는 방법도 있으나 이건 문제가 생길 가능성을 남기는거니 안하기로 한다.)
		//
		// 그렇다고 PlayerData나 DailyShopData쪽에서 캔버스 열려있는지 확인하는건
		// 구조상 데이터가 뷰를 참조하는 셈이라 별로인거 같아서
		// 그냥 이렇게 UpdateRemainTime에선 플래그를 체크하지 않는 형태로 가기로 한다.
		//if (PlayerData.instance.sharedDailyPackageOpened == false)
		//	return;
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _nextResetDateTime)
		{
			TimeSpan remainTime = _nextResetDateTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				if (remainTime.Days > 0)
					remainTimeText.text = string.Format("{0}d {1:00}:{2:00}:{3:00}", remainTime.Days, remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				else
					remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			// 일퀘과 달리 패킷 없이 클라가 선처리 하기로 했으니 PlayerData쪽에서 sharedDailyPackageOpened 값을 false로 바꿔두길 기다렸다가 갱신한다.
			// 시간을 변조했다면 받을 수 있는거처럼 보이게 될거다.
			_needUpdate = false;
			remainTimeText.text = "00:00:00";
			_needRefresh = true;
		}
	}

	bool _needRefresh = false;
	int _lastCurrent;
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (CashShopData.instance.dailyDiamondReceived == false)
		{
			RefreshInfo();
			_needRefresh = false;
		}
	}

	ShopProductTableData GetShopProductTableData()
	{
		return _shopProductTableData;
	}


	public void OnClickCustomButton()
	{
		// 버튼을 누를땐 세가지 경우가 있다.
		if (completeText.gameObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyDailyDiamond"), 2.0f);
			return;
		}

		// 받을 수 있는 상태
		if (receiveText.gameObject.activeSelf)
		{
			PlayFabApiManager.instance.RequestReceiveDailyDiamond(_dailyDiamondAmount, () =>
			{
				RefreshInfo();
				CashShopCanvas.instance.currencySmallInfo.RefreshInfo();
				MainCanvas.instance.RefreshCashShopAlarmObject();

				UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
				{
					CommonRewardCanvas.instance.RefreshReward(0, _dailyDiamondAmount, 0);
				});
			});
			return;
		}

		// 실제 구매
		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
	}

	public void OnClickPlusButton()
	{
		// 여기선 강제로 추가만 하면 된다.
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
	}



	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product, this);
	}

	public static void ExternalRequestServerPacket(Product product, DailyDiamondCanvas instance)
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
				shopProductTableData = instance.GetShopProductTableData();
			else
				shopProductTableData = TableDataManager.instance.FindShopProductTableDataByServerItemId(product.definition.id);
			if (shopProductTableData != null)
			{
				// 1번 리워드가 아이템일거라고 가정해둔거라 이렇게 한다.
				CashShopData.instance.PurchaseCount(CashShopData.eCashItemCountType.DailyDiamond, shopProductTableData.rewardCount1);

				//CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType1, shopProductTableData.rewardValue1, shopProductTableData.rewardCount1);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType2, shopProductTableData.rewardValue2, shopProductTableData.rewardCount2);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType3, shopProductTableData.rewardValue3, shopProductTableData.rewardCount3);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType4, shopProductTableData.rewardValue4, shopProductTableData.rewardCount4);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType5, shopProductTableData.rewardValue5, shopProductTableData.rewardCount5);
			}

			// 일일 다이아 패키지에는 특이한게 하나 있는데
			// 구매 즉시 지급되는 다이아 큰 덩이와 매일 받을 수 있는 다이아가 동시에 들어있다는 점 때문에 첫 구매날에는 두번 상자를 열게된다.
			// 유저한테 느끼는 체감이 좋지 않아서
			// 첫 구매시 첫날 얻을 수 있는 다이아를 한번에 주려다보니 이런 특이한 로직이 필요하게 되었다.
			// 
			// 대신 이건 구매 후 처리하지 못하고 튕겼을땐 복구로직 같은건 따로 없으며, 바로 지급되는 보석만 인벤에 이미 들어있는 상태일거고
			// 첫째날 보상은 유저가 받기 버튼을 직접 눌러서 받게 될거다.

			// 그런데 하나 예외상황이 있는게 이미 구매해서 마지막 날짜꺼까지 받은 상태에서
			// 재구매하면 이미 디비에는 그날 받은거로 되어있어서
			// 첫째날 보상을 지급할 수 없게 되버린다.
			// 그러니 항상 보내는게 아니라 오늘 데일리 보상을 받을 수 있는지를 확인하고 보내야한다.

			bool receivableFirstDay = false;
			if (CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.DailyDiamond) > 0 && CashShopData.instance.dailyDiamondReceived == false)
				receivableFirstDay = true;

			if (receivableFirstDay)
			{
				PlayFabApiManager.instance.RequestReceiveDailyDiamond(BattleInstanceManager.instance.GetCachedGlobalConstantInt("DailyGemAmount"), () =>
				{
					OnRecvBuyDailyDiamond(true, shopProductTableData.rewardCount2, instance);
				});
			}
			else
			{
				// 첫째날 처리가 필요없다면 그냥 패키지내에 큰 덩이 하나만 받는 연출 보여주면 된다.
				OnRecvBuyDailyDiamond(false, shopProductTableData.rewardCount2, instance);
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


	bool _receivableFirstDay = false;
	public static void OnRecvBuyDailyDiamond(bool receivableFirstDay, int baseRewardCount, DailyDiamondCanvas instance)
	{
		if (instance != null)
			instance.RefreshInfo();
		CashShopCanvas.instance.currencySmallInfo.RefreshInfo();
		MainCanvas.instance.RefreshCashShopAlarmObject();

		UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
		{
			if (receivableFirstDay)
				baseRewardCount += BattleInstanceManager.instance.GetCachedGlobalConstantInt("DailyGemAmount");
			CommonRewardCanvas.instance.RefreshReward(0, baseRewardCount, 0);
		});
	}
}