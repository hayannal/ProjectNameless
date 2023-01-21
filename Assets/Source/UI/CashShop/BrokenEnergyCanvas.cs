using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class BrokenEnergyCanvas : SimpleCashCanvas
{
	public static BrokenEnergyCanvas instance;

	public IAPButton iapButton;

	public Text levelText;
	public Slider valueSlider;
	public Text valueText;
	public Text minText;
	public Text maxText;
	public RectTransform valueTextTransform;
	public GameObject maxObject;
	public Image underMinImage;

	void Awake()
	{
		instance = this;
	}

	int _minValue;
	void OnEnable()
	{
		BrokenEnergyTableData brokenEnergyTableData = TableDataManager.instance.FindBrokenEnergyTableData(CashShopData.instance.brokenEnergyLevel);
		if (brokenEnergyTableData == null)
			return;

		_minValue = brokenEnergyTableData.minEnergy;
		levelText.text = UIString.instance.GetString("GameUI_Lv", brokenEnergyTableData.level);
		minText.text = brokenEnergyTableData.minEnergy.ToString("N0");
		maxText.text = brokenEnergyTableData.maxEnergy.ToString("N0");
		valueText.text = CurrencyData.instance.brokenEnergy.ToString("N0");
		maxObject.SetActive(false);

		if (CurrencyData.instance.brokenEnergy < brokenEnergyTableData.minEnergy)
		{
			underMinImage.gameObject.SetActive(true);
			valueText.color = underMinImage.color;
			valueTextTransform.anchoredPosition = new Vector2(-30.0f, valueTextTransform.anchoredPosition.y);
			valueSlider.value = 0.0f;
		}
		else
		{
			underMinImage.gameObject.SetActive(false);
			valueText.color = Color.black;
			valueTextTransform.anchoredPosition = new Vector2(0.0f, valueTextTransform.anchoredPosition.y);

			maxObject.SetActive(CurrencyData.instance.brokenEnergy >= brokenEnergyTableData.maxEnergy);

			float ratio = (float)(CurrencyData.instance.brokenEnergy - brokenEnergyTableData.minEnergy) / (brokenEnergyTableData.maxEnergy - brokenEnergyTableData.minEnergy);
			valueSlider.value = ratio;
		}

		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(brokenEnergyTableData.shopProductId);
		if (shopProductTableData == null)
			return;
		iapButton.productId = shopProductTableData.serverItemId;
		RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);

		// 아마 다른 상품들에는 이런 체크를 하지 않을거 같은데
		// 중복해서 구매할때 상자같이 그냥 얻는게 아닌 이 에너지 구매에 대해서만 이런 체크를 해둔다.
		if (CashShopData.instance.IsPurchasedFlag(CashShopData.eCashConsumeFlagType.BrokenEnergy))
		{
			string itemName = "";
			string id = CashShopData.instance.GetConsumeId(CashShopData.eCashConsumeFlagType.BrokenEnergy);
			ConsumeItemTableData consumeItemTableData = TableDataManager.instance.FindConsumeItemTableData(id);
			if (consumeItemTableData != null)
				itemName = UIString.instance.GetString(consumeItemTableData.name);
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress", itemName), () =>
			{
				ConsumeProduct();
			}, -1, true);
		}
	}

	public void OnClickCustomButton()
	{
		if (underMinImage.gameObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_BrokenEnergyMinToast", _minValue), 2.0f);
			return;
		}

		// 가능할때만 구매
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
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
			// PetSale처럼 컨슘은 하나로 통일해서 쓰기로 한다.
			CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.BrokenEnergy);
			ConsumeProduct();

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
		int nextLevel = CashShopData.instance.brokenEnergyLevel + 1;
		if (nextLevel > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBrokenStep"))
			nextLevel = 1;
		int currentBrokenEnergy = CurrencyData.instance.brokenEnergy;
		PlayFabApiManager.instance.RequestConsumeBrokenEnergy(CashShopData.instance.brokenEnergyLevel, nextLevel, () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
			{
				CommonRewardCanvas.instance.RefreshReward(0, 0, currentBrokenEnergy, () =>
				{
					if (instance != null)
						instance.gameObject.SetActive(false);
				});
			});
		});
	}
}