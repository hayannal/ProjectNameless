using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class TeamPassCanvas : SimpleCashCanvas
{
	public static TeamPassCanvas instance;

	public Text passAttackText;

	public GameObject priceButtonObject;
	public GameObject purchasedButtonObject;
	public Image purchasedButtonImage;
	public Text purchasedText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		passAttackText.text = PassManager.instance.GetPassAttackValue("teampass").ToString("N0");

		RefreshPriceButton();

		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData("teampass");
		if (shopProductTableData == null)
			return;
		RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
	}

	void RefreshPriceButton()
	{
		bool purchased = CharacterManager.instance.IsTeamPass();
		priceButtonObject.SetActive(!purchased);
		purchasedButtonObject.SetActive(purchased);
		if (purchased)
		{
			purchasedButtonImage.color = ColorUtil.halfGray;
			purchasedText.color = Color.gray;
		}
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
			CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.TeamPass);
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
		float prevPowerValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

		PlayFabApiManager.instance.RequestConsumeTeamPass(() =>
		{
			if (instance != null && instance.gameObject.activeSelf)
				instance.RefreshPriceButton();
			if (TeamPositionCanvas.instance != null && TeamPositionCanvas.instance.gameObject.activeSelf)
			{
				TeamPositionCanvas.instance.RefreshTeamPass();
				TeamPositionCanvas.instance.RefreshSlot();	
			}
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CompletePurchase"), 2.0f);

			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			if (nextValue > prevPowerValue)
			{
				UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
				{
					ChangePowerCanvas.instance.ShowInfo(prevPowerValue, nextValue);
				});
			}
		});
	}
}