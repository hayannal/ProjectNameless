using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class RelayPackageBox : SimpleCashCanvas
{
	public RewardIcon[] rewardIconList;
	public Transform iconImageRootTransform;
	public Text valueXText;
	public RectTransform priceTextTransform;
	public Text prevPriceText;
	public RectTransform lineImageRectTransform;
	public RectTransform rightTopRectTransform;
	public Text nameText;

	public Text itemAtkValueText;
	public GameObject priceObject;
	public GameObject completeObject;
	public GameObject blackObject;

	public IAPButton iapButton;

	RelayPackTableData _relayPackTableData;
	ShopProductTableData _shopProductTableData;
	public void RefreshInfo(RelayPackTableData relayPackTableData)
	{
		_relayPackTableData = relayPackTableData;
		_shopProductTableData = TableDataManager.instance.FindShopProductTableData(relayPackTableData.shopProductId);

		nameText.SetLocalizedText(string.Format("{0} {1}", UIString.instance.GetString("ShopUI_RelayPackage"), GetRomanNumberString(_relayPackTableData.num)));

		RefreshPrice(_shopProductTableData.serverItemId, _shopProductTableData.kor, _shopProductTableData.eng);

		valueXText.gameObject.SetActive(false);
		prevPriceText.gameObject.SetActive(false);
		if (_shopProductTableData.times > 0)
		{
			valueXText.text = string.Format("{0}x", _shopProductTableData.times);
			valueXText.gameObject.SetActive(true);

			decimal localizedPrice = 0;
			Product product = CodelessIAPStoreListener.Instance.GetProduct(_shopProductTableData.serverItemId);
			if (product != null && product.metadata != null && product.metadata.localizedPrice > 0)
				localizedPrice = product.metadata.localizedPrice;
			else
			{
				if (Application.systemLanguage == SystemLanguage.Korean)
					localizedPrice = _shopProductTableData.kor;
				else
					localizedPrice = (decimal)_shopProductTableData.eng;
			}
			prevPriceText.text = (localizedPrice * _shopProductTableData.times).ToString("N0");
			prevPriceText.gameObject.SetActive(true);
			RefreshLineImage();
			_updateRefreshLineImage = true;
		}

		// for medium large icon
		for (int i = 0; i < rewardIconList.Length; ++i)
			rewardIconList[i].eventRewardId = "_none";

		// reward icon list
		for (int i = 0; i < rewardIconList.Length; ++i)
		{
			switch (i)
			{
				case 0: rewardIconList[i].RefreshReward(_shopProductTableData.rewardType1, _shopProductTableData.rewardValue1, _shopProductTableData.rewardCount1); break;
				case 1: rewardIconList[i].RefreshReward(_shopProductTableData.rewardType2, _shopProductTableData.rewardValue2, _shopProductTableData.rewardCount2); break;
				case 2: rewardIconList[i].RefreshReward(_shopProductTableData.rewardType3, _shopProductTableData.rewardValue3, _shopProductTableData.rewardCount3); break;
				case 3: rewardIconList[i].RefreshReward(_shopProductTableData.rewardType4, _shopProductTableData.rewardValue4, _shopProductTableData.rewardCount4); break;
				case 4: rewardIconList[i].RefreshReward(_shopProductTableData.rewardType5, _shopProductTableData.rewardValue5, _shopProductTableData.rewardCount5); break;
			}
			switch (i)
			{
				case 0: if (_shopProductTableData.rewardType1 == "cu" || (_shopProductTableData.rewardValue1.StartsWith("Cash_s") && _shopProductTableData.rewardValue1.Contains("EquipTypeGacha") == false)) rewardIconList[i].ShowOnlyIcon(true, 1.2f); break;
				case 1: if (_shopProductTableData.rewardType2 == "cu" || (_shopProductTableData.rewardValue2.StartsWith("Cash_s") && _shopProductTableData.rewardValue2.Contains("EquipTypeGacha") == false)) rewardIconList[i].ShowOnlyIcon(true, 1.2f); break;
				case 2: if (_shopProductTableData.rewardType3 == "cu" || (_shopProductTableData.rewardValue3.StartsWith("Cash_s") && _shopProductTableData.rewardValue3.Contains("EquipTypeGacha") == false)) rewardIconList[i].ShowOnlyIcon(true, 1.2f); break;
				case 3: if (_shopProductTableData.rewardType4 == "cu" || (_shopProductTableData.rewardValue4.StartsWith("Cash_s") && _shopProductTableData.rewardValue4.Contains("EquipTypeGacha") == false)) rewardIconList[i].ShowOnlyIcon(true, 1.2f); break;
				case 4: if (_shopProductTableData.rewardType5 == "cu" || (_shopProductTableData.rewardValue5.StartsWith("Cash_s") && _shopProductTableData.rewardValue5.Contains("EquipTypeGacha") == false)) rewardIconList[i].ShowOnlyIcon(true, 1.2f); break;
			}
		}

		string attackItemId = PassManager.ShopProductId2ItemId(_relayPackTableData.shopProductId);
		itemAtkValueText.text = PassManager.instance.GetItemAttackValue(attackItemId).ToString("N0");

		bool purchased = (_relayPackTableData.num <= CashShopData.instance.relayPackagePurchasedNum);
		priceObject.SetActive(!purchased);
		completeObject.SetActive(purchased);
		blackObject.SetActive(purchased);

		// 다른 캐시상품들과 달리 프리팹 하나에서 정보를 바꿔가며 내용을 구성하기 때문에 productId에는 최초꺼로 설정되어있다.
		// 이걸 현재에 맞는 상품으로 바꿔주는 절차가 필요하다.
		iapButton.productId = _shopProductTableData.serverItemId;
		gameObject.SetActive(true);
	}

	public static string GetRomanNumberString(int number)
	{
		return UIString.instance.GetString(string.Format("GameUI_RomanNumber{0}", number));
	}

	void RefreshLineImage()
	{
		Vector3 diff = rightTopRectTransform.position - lineImageRectTransform.position;
		lineImageRectTransform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(-diff.x, diff.y) * Mathf.Rad2Deg);
		lineImageRectTransform.sizeDelta = new Vector2(lineImageRectTransform.sizeDelta.x, diff.magnitude * CashShopTabCanvas.instance.lineLengthRatio);
	}

	bool _updateRefreshLineImage;
	void Update()
	{
		if (_updateRefreshLineImage)
		{
			RefreshLineImage();
			_updateRefreshLineImage = false;
		}
	}

	public void OnClickAtkInfoButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ItemAtkInfoCanvas", () =>
		{
			ItemAtkInfoCanvas.instance.RefreshInfo(PassManager.ShopProductId2ItemId(_relayPackTableData.shopProductId));
		});
	}

	public void OnClickRewardButton(int index)
	{
		bool processed = false;
		string rewardType = "";
		string rewardValue = "";
		switch (index)
		{
			// 0번 index는 Atk로 고정이고 rewardIcon은 하이드 된 상태일거다.
			//case 0: rewardType = _shopProductTableData.rewardType1; rewardValue = _shopProductTableData.rewardValue1; break;
			case 1: rewardType = _shopProductTableData.rewardType2; rewardValue = _shopProductTableData.rewardValue2; break;
			case 2: rewardType = _shopProductTableData.rewardType3; rewardValue = _shopProductTableData.rewardValue3; break;
			case 3: rewardType = _shopProductTableData.rewardType4; rewardValue = _shopProductTableData.rewardValue4; break;
			case 4: rewardType = _shopProductTableData.rewardType5; rewardValue = _shopProductTableData.rewardValue5; break;
		}

		if (rewardType == "it")
		{
			if (rewardValue.StartsWith("Cash_s"))
			{
				ConsumeItemTableData consumeItemTableData = TableDataManager.instance.FindConsumeItemTableData(rewardValue);
				if (consumeItemTableData != null)
					TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString(consumeItemTableData.name), 120, rewardIconList[index].iconRootTransform, new Vector2(0.0f, -45.0f));
			}
			else
				RewardIcon.ShowDetailInfo(rewardType, rewardValue);
			processed = true;
		}
		if (processed == false)
			OnClickCustomButton();
	}

	public void OnClickCustomButton()
	{
		if (blackObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyThatItem"), 2.0f);
			return;
		}

		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		if (CurrencyData.instance.CheckMaxGold())
			return;

		// 구매할 수 있는 인덱스인지 확인해야한다.
		int firstIndex = -1;
		for (int i = 0; i < TableDataManager.instance.relayPackTable.dataArray.Length; ++i)
		{
			int num = TableDataManager.instance.relayPackTable.dataArray[i].num;
			if (num <= CashShopData.instance.relayPackagePurchasedNum)
				continue;

			firstIndex = i;
			break;
		}
		if (TableDataManager.instance.relayPackTable.dataArray[firstIndex].num != _relayPackTableData.num)
		{
			RelayPackageGroupInfo.instance.scrollSnap.GoToPanel(firstIndex);
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_CannotBuyFirstProduct"), 2.0f);
			return;
		}

		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
	}




	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product, _relayPackTableData, _shopProductTableData);
	}

	public static void ExternalRequestServerPacket(Product product, RelayPackTableData relayPackTableData, ShopProductTableData shopProductTableData)
	{
#if UNITY_ANDROID
		GooglePurchaseData data = new GooglePurchaseData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (uint)product.metadata.localizedPrice * 100, data.inAppPurchaseData, data.inAppDataSignature, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload, () =>
#endif
		{
			float prevPowerValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

			if (shopProductTableData == null)
				shopProductTableData = TableDataManager.instance.FindShopProductTableData(product.definition.id);
			if (shopProductTableData != null)
			{
				// 여긴 캐시상품중에 유일하게 cu도 들어있고 it 중에서도 가차 컨슘도 들어있을 수 있고 심지어 고정 펫에 장비까지 다 들어있을 수 있기 때문에
				CurrencyData.instance.OnRecvProductRewardExtendGachaAndItem(shopProductTableData);
			}

			// 릴레이의 첫번째 상품은 공격력 보유아이템이라서 이렇게 별도로 호출해주기로 한다.
			string attackItemId = PassManager.ShopProductId2ItemId(shopProductTableData.productId);
			PassManager.instance.OnRecvItemGrantResult(attackItemId);

			int num = 0;
			if (relayPackTableData != null)
				num = relayPackTableData.num;
			else
			{
				if (product != null)
				{
					string[] split = product.definition.id.Split('_');
					if (split.Length == 2 && split[0].Contains("relay"))
						int.TryParse(split[1], out num);
				}
			}
			if (num != 0)
			{
				CashShopData.instance.PurchaseFlag(CashShopData.eCashConsumeFlagType.RelayPackage);
				PlayFabApiManager.instance.RequestConsumeRelayPackage(() =>
				{
					if (RelayPackageGroupInfo.instance != null && RelayPackageGroupInfo.instance.gameObject.activeSelf)
					{
						RelayPackageGroupInfo.instance.gameObject.SetActive(false);
						RelayPackageGroupInfo.instance.gameObject.SetActive(true);
					}
				});
			}

			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			if (nextValue > prevPowerValue)
			{
				UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
				{
					ChangePowerCanvas.instance.ShowInfo(prevPowerValue, nextValue);
				});
			}

			WaitingNetworkCanvas.Show(false);
			if (shopProductTableData != null)
			{
				// 공격력 항목이 있으니 CommonRewardCanvas 대신 토스트로 처리
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CompletePurchase"), 2.0f);

				// 컨슘처리
				if (ConsumeProductProcessor.ContainsConsumeGacha(shopProductTableData))
					ConsumeProductProcessor.instance.ConsumeGacha(shopProductTableData);

				// 다른 곳과 달리 장비 지정을 팔거라서 지정 장비를 가지고 있다면 인벤토리 리프레쉬를 시도한다.
				// 실패한다면 로비로 돌아갈거고 재접하면 제대로 인벤 리스트를 받게 될거다.
				if (EquipManager.ContainsEquip(shopProductTableData))
					PlayFabApiManager.instance.RequestEquipListByPurchase(null);

				if (CashShopTabCanvas.instance != null && CashShopTabCanvas.instance.gameObject.activeSelf)
					CashShopTabCanvas.instance.currencySmallInfo.RefreshInfo();
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