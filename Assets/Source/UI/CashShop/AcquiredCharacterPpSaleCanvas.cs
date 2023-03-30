using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using MEC;

// hardcode ev17
public class AcquiredCharacterPpSaleCanvas : SimpleCashEventCanvas
{
	public static AcquiredCharacterPpSaleCanvas instance;

	public IAPButton iapButton;

	public CharacterCanvasListItem characterCanvasListItem;
	public Text shopCountText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		SetInfo();
		RefreshInfo();
	}

	//void Update()
	//{
	//	UpdateRemainTime();

	//	if (Input.GetKeyDown(KeyCode.A))
	//	{
	//		CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.AcquiredCompanionPp);
	//		ConsumeProduct();
	//	}
	//}

	ActorTableData _actorTableData;
	PickOneCharacterTableData _pickOneCharacterTableData;
	void RefreshInfo()
	{
		// acquiredCharacterSelectedId
		string selectedActorId = CashShopData.instance.acquiredCharacterPpSelectedId;
		if (string.IsNullOrEmpty(selectedActorId))
			return;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(selectedActorId);
		if (actorTableData == null)
			return;
		int level = 0;
		int transcend = 0;
		CharacterData characterData = CharacterManager.instance.GetCharacterData(actorTableData.actorId);
		if (characterData != null)
		{
			level = characterData.level;
			transcend = characterData.transcend;
		}
		characterCanvasListItem.Initialize(actorTableData.actorId, level, transcend, false, 0, null, null, null);
		_actorTableData = actorTableData;

		_pickOneCharacterTableData = TableDataManager.instance.FindPickOneCharacterTableData((int)AcquiredCharacterSaleCanvas.eAcquiredType.AcquiredCharacterPp, selectedActorId);
		if (_pickOneCharacterTableData == null)
			return;
		shopCountText.text = string.Format("+ {0:N0}", _pickOneCharacterTableData.count);

		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(_pickOneCharacterTableData.shopProductId);
		if (shopProductTableData == null)
			return;
		iapButton.productId = shopProductTableData.serverItemId;
		RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
	}

	public void OnClickDetailButton()
	{
		Timing.RunCoroutine(ShowCharacterInfoCanvasProcess());
	}

	IEnumerator<float> ShowCharacterInfoCanvasProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		gameObject.SetActive(false);

		while (gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		// Character
		MainCanvas.instance.OnClickTeamButton();

		while ((CharacterListCanvas.instance != null && CharacterListCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		CharacterListCanvas.instance.OnClickListItem(CashShopData.instance.acquiredCharacterPpSelectedId);

		while ((CharacterInfoCanvas.instance != null && CharacterInfoCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.4f);
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

			CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.AcquiredCompanionPp);
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
		string selectedId = CashShopData.instance.acquiredCharacterPpSelectedId;
		if (string.IsNullOrEmpty(selectedId))
			return;
		PickOneCharacterTableData pickOneCharacterTableData = TableDataManager.instance.FindPickOneCharacterTableData((int)AcquiredCharacterSaleCanvas.eAcquiredType.AcquiredCharacterPp, selectedId);
		if (pickOneCharacterTableData == null)
			return;

		PlayFabApiManager.instance.RequestConsumeAcquiredCharacterPp(selectedId, pickOneCharacterTableData.count, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CompletePurchase"), 2.0f);
		});
	}
}