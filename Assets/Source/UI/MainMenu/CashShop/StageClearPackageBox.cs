using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;
using PlayFab;

public class StageClearPackageBox : SimpleCashCanvas
{
	public RewardIcon[] rewardIconList;
	public Transform iconImageRootTransform;
	public Text valueXText;
	public RectTransform priceTextTransform;
	public Text prevPriceText;
	public RectTransform lineImageRectTransform;
	public RectTransform rightTopRectTransform;
	public Text nameText;

	public IAPButton iapButton;

	StageClearTableData _stageClearTableData;
	ShopProductTableData _shopProductTableData;
	public void RefreshInfo(StageClearTableData stageClearTableData)
	{
		_stageClearTableData = stageClearTableData;
		_shopProductTableData = TableDataManager.instance.FindShopProductTableData(stageClearTableData.shopProductId);

		nameText.SetLocalizedText(UIString.instance.GetString("ShopUI_StageClearPackage", stageClearTableData.stagecleared));

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

		// 다른 캐시상품들과 달리 프리팹 하나에서 정보를 바꿔가며 내용을 구성하기 때문에 productId에는 최초꺼로 설정되어있다.
		// 이걸 현재에 맞는 상품으로 바꿔주는 절차가 필요하다.
		iapButton.productId = _shopProductTableData.serverItemId;
		gameObject.SetActive(true);
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

	public void OnClickCustomButton()
	{
		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		if (CurrencyData.instance.CheckMaxGold())
			return;

		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
	}




	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product, _stageClearTableData, _shopProductTableData);
	}

	public static void ExternalRequestServerPacket(Product product, StageClearTableData stageClearTableData, ShopProductTableData shopProductTableData)
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
				// 여긴 캐시상품중에 유일하게 cu도 들어있고 it 중에서도 가차 컨슘 들어있는 곳이라서 이렇게 호출한다.
				// Summon 이벤트 리워드랑은 다른 점은 디비에 알아서 들어갈거기 때문에
				// 클라에서는 컨슘처리만 제대로 하면 된다는거다.
				CurrencyData.instance.OnRecvProductRewardExtendGacha(shopProductTableData);
			}

			int stage = 0;
			if (stageClearTableData != null)
				stage = stageClearTableData.stagecleared;
			else
			{
				if (product != null)
				{
					string[] split = product.definition.id.Split('_');
					if (split.Length == 2 && split[0].Contains("stageclear"))
						int.TryParse(split[1], out stage);
				}
			}
			if (stage != 0)
			{
				List<int> listResult = CashShopData.instance.OnRecvStageClearPackage(stage);
				var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
				string jsonStageClearPackageList = serializer.SerializeObject(listResult);
				PlayFabApiManager.instance.RequestUpdateStageClearPackageList(jsonStageClearPackageList, null);
			}

			WaitingNetworkCanvas.Show(false);
			if (shopProductTableData != null)
			{
				UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
				{
					CommonRewardCanvas.instance.RefreshReward(shopProductTableData, () =>
					{
						// 컨슘처리
						if (ConsumeProductProcessor.ContainsConsumeGacha(shopProductTableData))
							ConsumeProductProcessor.instance.ConsumeGacha(shopProductTableData);
					});
				});

				if (StageClearGroupInfo.instance != null && StageClearGroupInfo.instance.gameObject.activeSelf)
				{
					StageClearGroupInfo.instance.gameObject.SetActive(false);
					StageClearGroupInfo.instance.gameObject.SetActive(true);
				}

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
		// 이미 구매했던 상품인지 확인해야하나?
		// 그렇다 하더라도 복구는 해야 
		//if (CashShopData.instance.IsPurchasedStageClearPackage(stage))
		//{
		//	OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingAccount", product.metadata.localizedTitle), null, -1, true);
		//	return;
		//}

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress", product.metadata.localizedTitle), () =>
		{
			WaitingNetworkCanvas.Show(true);
			ExternalRequestServerPacket(product, null, null);
		}, () =>
		{
		}, true);
	}
}