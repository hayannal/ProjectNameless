using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

// hardcode ev14
public class UnacquiredSpellSaleCanvas : SimpleCashEventCanvas
{
	public static UnacquiredSpellSaleCanvas instance;

	public IAPButton iapButton;

	public SkillIcon skillIcon;
	public Text nameText;
	public Text atkText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		SetInfo();
		RefreshInfo();
	}

	//private void Update()
	//{
	//	UpdateRemainTime();

	//	if (Input.GetKeyDown(KeyCode.A))
	//	{
	//		CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.UnacquiredSpell);
	//		ConsumeProduct();
	//	}
	//}

	SkillTableData _skillTableData;
	PickOneSpellTableData _pickOneSpellTableData;
	void RefreshInfo()
	{
		// unacquiredSpellSelectedId
		string selectedSpellId = CashShopData.instance.unacquiredSpellSelectedId;
		if (string.IsNullOrEmpty(selectedSpellId))
			return;
		SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(selectedSpellId);
		if (skillTableData == null)
			return;
		skillIcon.SetInfo(skillTableData, false);
		_skillTableData = skillTableData;

		SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(selectedSpellId, 1);
		if (skillLevelTableData == null)
			return;
		SpellGradeLevelTableData spellGradeLevelTableData = TableDataManager.instance.FindSpellGradeLevelTableData(skillTableData.grade, skillTableData.star, 1);
		if (spellGradeLevelTableData == null)
			return;
		nameText.SetLocalizedText(UIString.instance.GetString(skillTableData.useNameIdOverriding ? skillLevelTableData.nameId : skillTableData.nameId));
		_descString = UIString.instance.GetString(skillTableData.useDescriptionIdOverriding ? skillLevelTableData.descriptionId : skillTableData.descriptionId, skillLevelTableData.parameter);
		atkText.text = spellGradeLevelTableData.accumulatedAtk.ToString("N0");

		_pickOneSpellTableData = TableDataManager.instance.FindPickOneSpellTableData(false, selectedSpellId);
		if (_pickOneSpellTableData == null)
			return;

		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(_pickOneSpellTableData.shopProductId);
		if (shopProductTableData == null)
			return;
		iapButton.productId = shopProductTableData.serverItemId;
		RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
	}

	string _descString;
	public void OnClickDetailButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("SpellInfoCanvas", () =>
		{
			SpellInfoCanvas.instance.SetInfo(_skillTableData, "", nameText.text, _descString);
		});
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

			CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.UnacquiredSpell);
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
		string selectedId = CashShopData.instance.unacquiredSpellSelectedId;
		if (string.IsNullOrEmpty(selectedId))
			return;
		PickOneSpellTableData pickOneSpellTableData = TableDataManager.instance.FindPickOneSpellTableData(false, selectedId);
		if (pickOneSpellTableData == null)
			return;

		PlayFabApiManager.instance.RequestConsumeUnacquiredSpell(selectedId, pickOneSpellTableData.count, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CompletePurchase"), 2.0f);
		});
	}
}