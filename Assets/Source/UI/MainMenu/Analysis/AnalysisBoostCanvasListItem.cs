using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class AnalysisBoostCanvasListItem : SimpleCashCanvas
{
	public int index;
	public Text timeText;
	public RewardIcon[] rewardIconList;

	void Start()
	{
		for (int i = 0; i < rewardIconList.Length; ++i)
			rewardIconList[i].ShowOnlyIcon(true, 1.0f);
	}

	AnalysisBoostTableData _analysisBoostTableData;
	ShopProductTableData _shopProductTableData;
	void OnEnable()
	{
		AnalysisBoostTableData analysisBoostTableData = TableDataManager.instance.FindAnalysisBoostTableDataByIndex(index);
		if (analysisBoostTableData == null)
			return;
		_analysisBoostTableData = analysisBoostTableData;
		timeText.text = AnalysisResultCanvas.GetTimeString(analysisBoostTableData.count, true);

		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(analysisBoostTableData.shopProductId);
		if (shopProductTableData != null)
		{
			_shopProductTableData = shopProductTableData;
			RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
		}
	}



	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product, _analysisBoostTableData, _shopProductTableData);
	}

	public static void ExternalRequestServerPacket(Product product, AnalysisBoostTableData analysisBoostTableData, ShopProductTableData shopProductTableData)
	{
#if UNITY_ANDROID
		GooglePurchaseData data = new GooglePurchaseData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (uint)product.metadata.localizedPrice * 100, data.inAppPurchaseData, data.inAppDataSignature, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload, () =>
#endif
		{
			if (shopProductTableData == null)
			{
				if (product != null)
					shopProductTableData = TableDataManager.instance.FindShopProductTableDataByServerItemId(product.definition.id);
			}
			if (analysisBoostTableData == null)
			{
				if (shopProductTableData != null)
					analysisBoostTableData = TableDataManager.instance.FindAnalysisBoostTableDataByShopProductId(shopProductTableData.productId);
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
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CompletePurchase"), 2.0f);

			// 창 열려있었다면 닫으면서 자동으로 분석창이 다시 열리면서 리프레쉬 될거다.
			if (AnalysisBoostCanvas.instance != null && AnalysisBoostCanvas.instance.gameObject.activeSelf)
				AnalysisBoostCanvas.instance.gameObject.SetActive(false);

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
			ExternalRequestServerPacket(product, null, null);
		}, () =>
		{
		}, true);
	}
}