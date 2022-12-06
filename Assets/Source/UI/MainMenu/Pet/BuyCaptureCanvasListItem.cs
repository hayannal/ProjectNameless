using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class BuyCaptureCanvasListItem : SimpleCashCanvas
{
	public int index;
	public Text unlimitedText;
	public Text countText;
	public Text nameText;
	public Text shopCountText;
	public Text freeText;

	PetCaptureTableData _petCaptureTableData;
	ShopProductTableData _shopProductTableData;
	void OnEnable()
	{
		unlimitedText.gameObject.SetActive(index == 0);
		countText.gameObject.SetActive(index != 0);

		PetCaptureTableData petCaptureTableData = TableDataManager.instance.FindPetCaptureTableDataByIndex(index);
		if (petCaptureTableData == null)
			return;
		_petCaptureTableData = petCaptureTableData;

		nameText.SetLocalizedText(UIString.instance.GetString(petCaptureTableData.nameId));

		freeText.gameObject.SetActive(index == 0);
		priceText.gameObject.SetActive(index != 0);
		shopCountText.gameObject.SetActive(index != 0);
		if (index != 0)
		{
			shopCountText.text = string.Format("X {0:N0}", petCaptureTableData.count);
			ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(petCaptureTableData.shopProductId);
			if (shopProductTableData != null)
			{
				_shopProductTableData = shopProductTableData;
				RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
			}
		}

		int count = 0;
		switch (index)
		{
			case 1: count = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.CaptureBetter); break;
			case 2: count = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.CaptureBest); break;
		}
		countText.text = count.ToString("N0");
	}

	public void OnClickUnlimitedButton()
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CannotBuyCaptureBasic"), 2.0f);
	}


	
	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product, _petCaptureTableData, _shopProductTableData);
	}

	public static void ExternalRequestServerPacket(Product product, PetCaptureTableData petCaptureTableData, ShopProductTableData shopProductTableData)
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
			if (petCaptureTableData == null)
			{
				if (shopProductTableData != null)
					petCaptureTableData = TableDataManager.instance.FindCaptureTableDataByShopProductId(shopProductTableData.productId);
			}
			if (shopProductTableData != null && petCaptureTableData != null)
			{
				if (shopProductTableData.productId.Contains("better"))
					CashShopData.instance.PurchaseCount(CashShopData.eCashItemCountType.CaptureBetter, petCaptureTableData.count);
				else if (shopProductTableData.productId.Contains("best"))
					CashShopData.instance.PurchaseCount(CashShopData.eCashItemCountType.CaptureBest, petCaptureTableData.count);
			}

			WaitingNetworkCanvas.Show(false);
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CompletePurchase"), 2.0f);

			if (SelectCaptureCanvas.instance != null && SelectCaptureCanvas.instance.gameObject.activeSelf)
			{
				SelectCaptureCanvas.instance.gameObject.SetActive(false);
				SelectCaptureCanvas.instance.gameObject.SetActive(true);
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
			ExternalRequestServerPacket(product, null, null);
		}, () =>
		{
		}, true);
	}
}