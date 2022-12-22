using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using DG.Tweening;
using MEC;
using CodeStage.AntiCheat.ObscuredTypes;

public class PetSaleCanvas : SimpleCashCanvas
{
	public static PetSaleCanvas instance;

	public IAPButton iapButton;

	public Text nameText;
	public GameObject starGridRootObject;
	public GameObject[] starObjectList;
	public GameObject fiveStarObject;

	public Text countText;
	public Text atkText;

	public GameObject resultRootObject;
	
	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		RefreshInfo();
	}

	void OnDisable()
	{
		resultRootObject.SetActive(false);
	}

	//private void Update()
	//{
	//	if (Input.GetKeyDown(KeyCode.A))
	//	{
	//		CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.PetSale);
	//		ConsumeProduct();
	//	}
	//}

	PetSaleTableData _petSaleTableData;
	void RefreshInfo()
	{
		PetTableData petTableData = TableDataManager.instance.FindPetTableData(PetManager.instance.petSaleId);
		PetSaleTableData petSaleTableData = TableDataManager.instance.FindPetSaleTableData(petTableData.star);
		atkText.text = petTableData.accumulatedAtk.ToString("N0");
		countText.text = string.Format("+{0}", petSaleTableData.count);

		nameText.SetLocalizedText(UIString.instance.GetString(petTableData.nameId));

		starGridRootObject.SetActive(petTableData.star <= 4);
		fiveStarObject.SetActive(petTableData.star == 5);
		for (int i = 0; i < starObjectList.Length; ++i)
			starObjectList[i].SetActive(i < petTableData.star);

		_petSaleTableData = petSaleTableData;


		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(petSaleTableData.shopProductId);
		if (shopProductTableData == null)
			return;
		iapButton.productId = shopProductTableData.serverItemId;
		RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
	}

	public void OnClickBackButton()
	{
		Timing.RunCoroutine(ShowSaleProcess());
	}

	IEnumerator<float> ShowSaleProcess()
	{
		gameObject.SetActive(false);
		PetInfoGround.instance.petBattleInfo.gameObject.SetActive(true);

		CustomFollowCamera.instance.cachedTransform.DORotate(new Vector3(CustomFollowCamera.instance.cachedTransform.eulerAngles.x, 0.0f, 0.0f), 0.4f);
		yield return Timing.WaitForSeconds(0.4f);

		PetInfoCanvas.instance.rootObject.SetActive(true);
	}


	public void OnClickPurchaseExitButton()
	{
		Timing.RunCoroutine(PurchaseExitProcess());
	}

	IEnumerator<float> PurchaseExitProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		gameObject.SetActive(false);
		PetInfoGround.instance.petBattleInfo.gameObject.SetActive(true);

		CustomFollowCamera.instance.cachedTransform.DORotate(new Vector3(CustomFollowCamera.instance.cachedTransform.eulerAngles.x, 0.0f, 0.0f), 0.1f);
		yield return Timing.WaitForSeconds(0.1f);

		PetInfoCanvas.instance.rootObject.SetActive(true);
		PetInfoCanvas.instance.RefreshInfo();
		PetInfoCanvas.instance.RefreshHeart();
		PetListCanvas.instance.RefreshGrid();

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.5f);
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
			// 엄밀히는 PetSale을 등급별로 5개 만들어야겠지만 과금쪽이라 엄격하게 체크하지 않아도 될거 같아서 한개로 통합해서 써본다.
			CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.PetSale);
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
		if (string.IsNullOrEmpty(PetManager.instance.petSaleId))
			return;

		PetTableData petTableData = TableDataManager.instance.FindPetTableData(PetManager.instance.petSaleId);
		PetSaleTableData petSaleTableData = TableDataManager.instance.FindPetSaleTableData(petTableData.star);

		List<ObscuredString> listGainId = new List<ObscuredString>();
		for (int i = 0; i < petSaleTableData.count; ++i)
			listGainId.Add(PetManager.instance.petSaleId);

		PlayFabApiManager.instance.RequestConsumePetSale(listGainId, (itemGrantString) =>
		{
			if (itemGrantString == "")
				return;

			PetManager.instance.OnRecvItemGrantResult(itemGrantString);

			if (instance != null && instance.gameObject.activeSelf)
				instance.resultRootObject.SetActive(true);
			else
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("PetUI_ResultSuccess"), 2.0f);
		});
	}
}