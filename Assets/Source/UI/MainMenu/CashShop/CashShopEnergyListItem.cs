using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class CashShopEnergyListItem : SimpleCashCanvas
{
	public string productId = "cashshopenergy";
	public int num = 1;

	public Text countText;
	public Text prevCountText;
	public RectTransform lineImageRectTransform;
	public RectTransform rightTopRectTransform;

	public IAPButton iapButton;

	ShopProductTableData _shopProductTableData;
	void OnEnable()
	{
		bool eventApplied = CashShopData.instance.IsShowEvent("ev9");
		//eventApplied = true;

		string id = string.Format("{0}_{1}", productId, num);
		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(id);
		if (shopProductTableData == null)
			return;

		string saleId = string.Format("{0}_sale", id);
		ShopProductTableData saleShopProductTableData = TableDataManager.instance.FindShopProductTableData(saleId);
		if (saleShopProductTableData == null)
			return;

		if (eventApplied)
		{
			prevCountText.text = shopProductTableData.rewardCount1.ToString("N0");
			prevCountText.gameObject.SetActive(false);
			prevCountText.gameObject.SetActive(true);
			countText.text = saleShopProductTableData.rewardCount1.ToString("N0");
			RefreshPrice(saleShopProductTableData.serverItemId, saleShopProductTableData.kor, saleShopProductTableData.eng);
			RefreshLineImage();
			_updateRefreshLineImageCount = 3;
			_shopProductTableData = saleShopProductTableData;
		}
		else
		{
			prevCountText.gameObject.SetActive(false);
			countText.text = shopProductTableData.rewardCount1.ToString("N0");
			RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
			_shopProductTableData = shopProductTableData;
		}
		iapButton.productId = _shopProductTableData.serverItemId;
	}

	void RefreshLineImage()
	{
		Vector3 diff = rightTopRectTransform.position - lineImageRectTransform.position;
		lineImageRectTransform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(-diff.x, diff.y) * Mathf.Rad2Deg);
		if (CashShopCanvas.instance != null)
			lineImageRectTransform.sizeDelta = new Vector2(lineImageRectTransform.sizeDelta.x, diff.magnitude * CashShopCanvas.instance.lineLengthRatio);
	}

	int _updateRefreshLineImageCount;
	void Update()
	{
		if (_updateRefreshLineImageCount > 0)
		{
			RefreshLineImage();
			--_updateRefreshLineImageCount;
		}
	}



	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product, _shopProductTableData);
	}

	public static void ExternalRequestServerPacket(Product product, ShopProductTableData shopProductTableData)
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
				shopProductTableData = TableDataManager.instance.FindShopProductTableData(product.definition.id);
			if (shopProductTableData != null)
			{
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType1, shopProductTableData.rewardValue1, shopProductTableData.rewardCount1);
			}

			WaitingNetworkCanvas.Show(false);
			if (shopProductTableData != null)
			{
				UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
				{
					CommonRewardCanvas.instance.RefreshReward(shopProductTableData);
				});
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
}