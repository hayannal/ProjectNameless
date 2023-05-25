using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class CashShopEnergyListItem : SimpleCashCanvas
{
	public string productId = "cashshopenergy";
	public int num = 1;

	public GameObject moreTextObject;
	public Text countText;
	public Text prevCountText;
	public RectTransform lineImageRectTransform;
	public RectTransform rightTopRectTransform;

	public IAPButton iapButton;

	ShopProductTableData _shopProductTableData;
	void OnEnable()
	{
		// hardcode ev9
		bool eventApplied = CashShopData.instance.IsShowEvent("ev9");
		//eventApplied = true;

		string id = string.Format("{0}_{1}", productId, num);
		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(id);
		if (shopProductTableData == null)
			return;

		string moreId = string.Format("{0}_more", id);
		ShopProductTableData moreShopProductTableData = TableDataManager.instance.FindShopProductTableData(moreId);
		if (moreShopProductTableData == null)
			return;

		if (eventApplied)
		{
			moreTextObject.SetActive(true);
			prevCountText.text = shopProductTableData.rewardCount1.ToString("N0");
			prevCountText.gameObject.SetActive(false);
			prevCountText.gameObject.SetActive(true);
			countText.text = moreShopProductTableData.rewardCount1.ToString("N0");
			RefreshPrice(moreShopProductTableData.serverItemId, moreShopProductTableData.kor, moreShopProductTableData.eng);
			RefreshLineImage();
			_updateRefreshLineImageCount = 3;
			_shopProductTableData = moreShopProductTableData;
		}
		else
		{
			moreTextObject.SetActive(false);
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
		if (CashShopTabCanvas.instance != null)
			lineImageRectTransform.sizeDelta = new Vector2(lineImageRectTransform.sizeDelta.x, diff.magnitude * CashShopTabCanvas.instance.lineLengthRatio);
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


	public void OnClickCustomButton()
	{
		if (productId.Contains("gold") && CurrencyData.instance.CheckMaxGold())
			return;

		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
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

			if (CashShopTabCanvas.instance != null && CashShopTabCanvas.instance.gameObject.activeSelf)
				CashShopTabCanvas.instance.currencySmallInfo.RefreshInfo();

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