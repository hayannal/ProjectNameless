using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class BigBoostEventCanvas : SimpleCashEventCanvas
{
	public static BigBoostEventCanvas instance;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData("bigboost");
		if (shopProductTableData == null)
			return;
		RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
	}

	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product);
	}

	public static void ExternalRequestServerPacket(Product product)
	{
#if UNITY_ANDROID
		//Debug.LogFormat("PurchaseComplete. isoCurrencyCode = {0} / localizedPrice = {1}", product.metadata.isoCurrencyCode, product.metadata.localizedPrice);
		GooglePurchaseData data = new GooglePurchaseData(product.receipt);

		// 플레이팹은 센트 단위로 시작하기 때문에 100을 곱해서 넘기는게 맞는데 한국돈 결제일때도 * 100 해서 보내야하는지 궁금해서 테스트 해봤다.
		// 0을 보냈더니 플레이어 현금 구매 Stream이 뜨지 않는다.(player_realmoney_purchase 이벤트)
		// 함수 설명에는 필수 인자가 아니었는데 0보내면 인식을 안하게 내부적으로 되어있는건가 싶다.
		// 그래서 곱하기 100을 안하고 그냥 보내봤더니 1200원 KRW를 샀는데 12원 KRW를 산거처럼 처리된다. 즉 USD로 사든 KRW로 사든 * 100은 무조건 해서 보내야한다.
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (uint)product.metadata.localizedPrice * 100, data.inAppPurchaseData, data.inAppDataSignature, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload, () =>
#endif
		{
			// ValidatePurchase함수를 하나만 만들어서 쓰기로 하면서
			// 재화에 대한 처리를 외부에서 하기로 한다.
			ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData("bigboost");
			if (shopProductTableData != null)
			{
				// 원래라면 5개 다 해야겠지만 BigBoost는 3개 넘어서는 사용하지 않을거 같으니 이렇게만 해본다.
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType1, shopProductTableData.rewardValue1, shopProductTableData.rewardCount1);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType2, shopProductTableData.rewardValue2, shopProductTableData.rewardCount2);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType3, shopProductTableData.rewardValue3, shopProductTableData.rewardCount3);
			}

			// 결과화면도 보여줘야한다.
			// 연출일수도 있고
			// DropDiaBox();
			//
			// 간단한 결과창 하나로 처리할 수도 있을거다.
			WaitingNetworkCanvas.Show(false);
			if (shopProductTableData != null)
			{
				UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
				{
					CommonRewardCanvas.instance.RefreshReward(shopProductTableData);
				});
			}

			if (instance != null)
			{
				if (CashShopData.instance.IsShowEvent(instance.cashEventId))
				{
					PlayFabApiManager.instance.RequestCloseCashEvent(instance.cashEventId, () =>
					{
						if (MainCanvas.instance != null && MainCanvas.instance.gameObject.activeSelf)
							MainCanvas.instance.CloseCashEventButton(instance.cashEventId);
					});
				}
				if (instance.gameObject != null)
					instance.gameObject.SetActive(false);
			}

			CodelessIAPStoreListener.Instance.StoreController.ConfirmPendingPurchase(product);
			IAPListenerWrapper.instance.CheckConfirmPendingPurchase(product);

		}, (error) =>
		{
			// 거의 그럴일은 없겠지만 서버에서 영수증 처리 후 오는 패킷을 받지 못해서 ConfirmPendingPurchase하지 못했다면
			// 다음번 재시작 후 클라이언트는 아직 미완료된줄 알고 재구매 처리를 할텐데
			// 서버에 보내보니 이미 영수증을 사용했다고 뜬다면 이쪽으로 오게 된다.
			// 이럴땐 이미 디비에는 구매했던 템들이 다 들어있는 상황일테니 ConfirmPendingPurchase 처리를 해주면 된다.
			//
			// 오히려 여기 더 자주 들어올만한 상황은 영수증 패킷 조작해서 보내는 악성 유저들일거다.
			// 그러니 안내메세지 처리같은거 없이 그냥 Confirm처리 하고 시작화면으로 보내도록 한다.
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
}