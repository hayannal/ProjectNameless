using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class AlmostThereEventCanvas : SimpleCashEventCanvas
{
	public static AlmostThereEventCanvas instance;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		// hardcode ev2
		string tableId = cashEventId;
		string id = string.Format("{0}_almostthere", tableId);
		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(id);
		if (shopProductTableData == null)
			return;
		RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
	}

	void OnEnable()
	{
		SetInfo();
		MainCanvas.instance.OnEnterCharacterMenu(true);
	}

	void OnDisable()
	{
		MainCanvas.instance.OnEnterCharacterMenu(false);
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
			ShopProductTableData shopProductTableData = null;
			if (instance != null)
				shopProductTableData = TableDataManager.instance.FindShopProductTableData(string.Format("{0}_almostthere", instance.cashEventId));
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

			// 간단한 결과창 하나로 처리할 수도 있을거다.
			WaitingNetworkCanvas.Show(false);
			if (shopProductTableData != null)
			{
				UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
				{
					CommonRewardCanvas.instance.RefreshReward(shopProductTableData);
				});
			}

			#region Default Process
			string cashEventId = "";
			if (instance != null) cashEventId = instance.cashEventId;
			else
			{
				string[] split = product.definition.id.Split('_');
				if (split.Length == 2 && split[0].Contains("ev"))
					cashEventId = split[0];
			}
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
			#endregion

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
}