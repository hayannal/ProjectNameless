using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using MEC;

public class OneOfThreeCanvas : SimpleCashEventCanvas
{
	public Text price1Text;
	public Text price2Text;
	public Text price3Text;
	public IAPButton iap1Button;
	public IAPButton iap2Button;
	public IAPButton iap3Button;
	public Button iapBridge1Button;
	public Button iapBridge2Button;
	public Button iapBridge3Button;

	List<ShopProductTableData> _listShopProductTableData = new List<ShopProductTableData>();
	void Start()
	{
		string tableId = cashEventId;
		string id = string.Format("{0}_oneofthree_1", tableId);
		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(id);
		if (shopProductTableData != null)
		{
			_listShopProductTableData.Add(shopProductTableData);
			priceText = price1Text;
			RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
			iap1Button.productId = shopProductTableData.serverItemId;
		}

		id = string.Format("{0}_oneofthree_2", tableId);
		shopProductTableData = TableDataManager.instance.FindShopProductTableData(id);
		if (shopProductTableData != null)
		{
			_listShopProductTableData.Add(shopProductTableData);
			priceText = price2Text;
			RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
			iap2Button.productId = shopProductTableData.serverItemId;
		}

		id = string.Format("{0}_oneofthree_3", tableId);
		shopProductTableData = TableDataManager.instance.FindShopProductTableData(id);
		if (shopProductTableData != null)
		{
			_listShopProductTableData.Add(shopProductTableData);
			priceText = price3Text;
			RefreshPrice(shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
			iap3Button.productId = shopProductTableData.serverItemId;
		}

		RewardIcon[] listRewardIcon = GetComponentsInChildren<RewardIcon>(true);
		for (int i = 0; i < listRewardIcon.Length; ++i)
		{
			bool applyOnlyIcon = false;
			EventRewardTableData eventRewardTableData = TableDataManager.instance.FindEventRewardTableData(listRewardIcon[i].eventRewardId, listRewardIcon[i].num);
			if (eventRewardTableData != null)
			{
				if (eventRewardTableData.rewardType == "cu" || (eventRewardTableData.rewardValue.StartsWith("Cash_s") && eventRewardTableData.rewardValue.Contains("EquipTypeGacha") == false))
					applyOnlyIcon = true;
			}
			if (applyOnlyIcon == false)
				continue;

			listRewardIcon[i].ShowOnlyIcon(true);
			listRewardIcon[i].ActivePunchAnimation(true);
			listRewardIcon[i].iconRootTransform.GetComponent<Button>().enabled = false;
		}
	}




	public void OnClickRewardButton(RewardIcon rewardIcon)
	{
		if (rewardIcon == null)
			return;
		if (rewardIcon.eventRewardId == "")
			return;
		EventRewardTableData eventRewardTableData = TableDataManager.instance.FindEventRewardTableData(rewardIcon.eventRewardId, rewardIcon.num);
		if (eventRewardTableData == null)
			return;

		string rewardType = eventRewardTableData.rewardType;
		string rewardValue = eventRewardTableData.rewardValue;

		if (rewardType == "it")
		{
			if (rewardValue.StartsWith("Cash_s"))
			{
				ConsumeItemTableData consumeItemTableData = TableDataManager.instance.FindConsumeItemTableData(rewardValue);
				if (consumeItemTableData != null)
					TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString(consumeItemTableData.name), 120, rewardIcon.iconRootTransform, new Vector2(0.0f, -45.0f));
			}
			else
			{
				// 타입에 따라 상세정보창으로 가기로 한다.
				switch (rewardType)
				{
					case "it":
						if (rewardValue.StartsWith("Spell_"))
						{
							RewardIcon.ShowDetailInfo(rewardType, rewardValue);
						}
						else if (rewardValue.StartsWith("Actor"))
						{
							// 액터는 현재 패스
						}
						else if (rewardValue.StartsWith("Pet_"))
						{
							OnClickPetDetailButton(rewardValue);
						}
						else if (rewardValue.StartsWith("Equip"))
						{
							OnClickEquipDetailButton(rewardValue);
						}
						break;
				}
			}
		}
	}


	#region Detail Info
	public void OnClickPetDetailButton(string rewardValue)
	{
		Timing.RunCoroutine(ShowPetDetailCanvasProcess(rewardValue));
	}

	IEnumerator<float> ShowPetDetailCanvasProcess(string rewardValue)
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		gameObject.SetActive(false);

		while (gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		// Pet
		MainCanvas.instance.OnClickPetButton();

		while ((PetListCanvas.instance != null && PetListCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		// 아무래도 카운트는 없는게 맞아보인다
		int count = 0;
		PetData petData = PetManager.instance.GetPetData(rewardValue);
		if (petData != null) count = petData.count;
		PetListCanvas.instance.OnClickListItem(rewardValue, count);

		while ((PetInfoCanvas.instance != null && PetInfoCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.4f);
	}

	public void OnClickEquipDetailButton(string rewardValue)
	{
		Timing.RunCoroutine(ShowEquipDetailCanvasProcess(rewardValue));
	}

	IEnumerator<float> ShowEquipDetailCanvasProcess(string rewardValue)
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		gameObject.SetActive(false);

		while (gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		MissionListCanvas.ShowCanvasAsyncWithPrepareGround("PickUpEquipDetailCanvas", null);

		while ((PickUpEquipDetailCanvas.instance != null && PickUpEquipDetailCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;
		PickUpEquipDetailCanvas.instance.RefreshInfo(rewardValue);
		PickUpEquipDetailCanvas.instance.SetRestoreCanvas(cashEventId);

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.4f);
	}
	#endregion














	int _buttonIndex;
	public void OnClickButton(int index)
	{
		_buttonIndex = index;

		// 실제 구매
		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponseCustom, null, true);
	}

	public void OnResponseCustom()
	{
		WaitingNetworkCanvas.Show(true);
		switch (_buttonIndex)
		{
			case 0: iapBridge1Button.onClick.Invoke(); break;
			case 1: iapBridge2Button.onClick.Invoke(); break;
			case 2: iapBridge3Button.onClick.Invoke(); break;
		}
		//iapBridgeButton.onClick.Invoke();
	}

	ShopProductTableData GetShopProduct(string serverItemId)
	{
		for (int i = 0; i < _listShopProductTableData.Count; ++i)
		{
			if (_listShopProductTableData[i].serverItemId == serverItemId)
				return _listShopProductTableData[i];
		}
		return null;
	}

	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product, this);
	}

	public static void ExternalRequestServerPacket(Product product, OneOfThreeCanvas instance)
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
				shopProductTableData = instance.GetShopProduct(product.definition.id);
			else
				shopProductTableData = TableDataManager.instance.FindShopProductTableDataByServerItemId(product.definition.id);
			if (shopProductTableData != null)
			{
				CurrencyData.instance.OnRecvProductRewardExtendGachaAndItem(shopProductTableData);
			}

			WaitingNetworkCanvas.Show(false);
			if (shopProductTableData != null)
			{
				// 대부분 다 팝업으로 되어있는걸 또 다시 커먼 리워드 창으로 보여줄 필요는 없을거 같으니 구매 완료로 처리해본다.
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CompletePurchase"), 2.0f);

				// 컨슘처리
				if (ConsumeProductProcessor.ContainsConsumeGacha(shopProductTableData))
					ConsumeProductProcessor.instance.ConsumeGacha(shopProductTableData);

				// RelayPackageBox 했던거처럼 지정 장비를 가지고 있다면 인벤토리 리프레쉬를 시도한다.
				// 실패한다면 로비로 돌아갈거고 재접하면 제대로 인벤 리스트를 받게 될거다.
				if (EquipManager.ContainsEquip(shopProductTableData))
					PlayFabApiManager.instance.RequestEquipListByPurchase(null);
			}

			#region Default Process
			string cashEventId = "";
			if (instance != null) cashEventId = instance.cashEventId;
			else
			{
				string[] split = product.definition.id.Split('_');
				if (split.Length == 3 && split[0].Contains("ev"))
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
			// 거의 그럴일은 없겠지만 서버에서 영수증 처리 후 오는 패킷을 받지 못해서 ConfirmPendingPurchase하지 못했다면
			// 다음번 재시작 후 클라이언트는 아직 미완료된줄 알고 재구매 처리를 할텐데
			// 서버에 보내보니 이미 영수증을 사용했다고 뜬다면 이쪽으로 오게 된다.
			// 이럴땐 이미 디비에는 구매했던 템들이 다 들어있는 상황일테니 ConfirmPendingPurchase 처리를 해주면 된다.
			//
			// 오히려 여기 더 자주 들어올만한 상황은 영수증 패킷 조작해서 보내는 악성 유저들일거다.
			// 그러니 안내메세지 처리같은거 없이 그냥 Confirm처리 하고 시작화면으로 보내도록 한다.
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
			ExternalRequestServerPacket(product, null);
		}, () =>
		{
		}, true);
	}
}