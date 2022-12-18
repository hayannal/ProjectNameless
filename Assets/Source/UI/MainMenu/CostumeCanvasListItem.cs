using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class CostumeCanvasListItem : SimpleCashCanvas
{
	public IAPButton iapButton;

	public Image costumeImage;
	public Text atkText;
	public GameObject previewObject;

	public RectTransform priceTextRectTransform;
	public GameObject goldIconObject;
	public GameObject priceObject;
	public GameObject completeObject;
	public GameObject eventGainObject;
	public GameObject baseCostumeObject;

	public GameObject equippedObject;

	bool _baseCostume;
	bool _contains;
	CostumeTableData _costumeTableData;
	public void Initialize(bool contains, CostumeTableData costumeTableData)
	{
		_baseCostume = (costumeTableData.costumeId == CostumeManager.s_DefaultCostumeId);

		costumeImage.sprite = CostumeListCanvas.instance.GetSprite(costumeTableData.spriteName);
		atkText.text = costumeTableData.atk.ToString("N0");
		atkText.gameObject.SetActive(!_baseCostume);

		if (contains == false)
		{
			if (costumeTableData.condition)
			{
				eventGainObject.SetActive(true);
				priceObject.SetActive(false);
			}
			else if (costumeTableData.goldCost > 0)
			{
				priceText.text = costumeTableData.goldCost.ToString("N0");
				eventGainObject.SetActive(false);
				priceObject.SetActive(true);
				goldIconObject.SetActive(true);
				priceTextRectTransform.anchoredPosition = new Vector2(16.0f, 0.0f);
			}
			else
			{
				eventGainObject.SetActive(false);
				priceObject.SetActive(true);
				goldIconObject.SetActive(false);
				priceTextRectTransform.anchoredPosition = Vector2.zero;

				iapButton.productId = costumeTableData.serverItemId;
				RefreshPrice(costumeTableData.serverItemId, costumeTableData.kor, costumeTableData.eng);
			}
			equippedObject.SetActive(false);
			completeObject.SetActive(false);
			baseCostumeObject.SetActive(false);
		}
		else
		{
			eventGainObject.SetActive(false);
			priceObject.SetActive(false);
			completeObject.SetActive(!_baseCostume);
			baseCostumeObject.SetActive(_baseCostume);

			equippedObject.SetActive(false);
			if (_baseCostume && CostumeManager.instance.selectedCostumeId == "")
			{
				equippedObject.SetActive(true);
				contains = true;
			}
			else if (!_baseCostume && CostumeManager.instance.selectedCostumeId == costumeTableData.costumeId)
				equippedObject.SetActive(true);
		}
		previewObject.SetActive(!contains);

		_contains = contains;
		_costumeTableData = costumeTableData;
	}


	public void OnClickPreviewButton()
	{
		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		if (CharacterCanvas.instance.previewId == _costumeTableData.costumeId)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("CostumeUI_AlreadyCostumeToast"), 2.0f);
			return;
		}

		CharacterCanvas.instance.ShowCanvasPlayerActorWithCostume(_costumeTableData.costumeId, () =>
		{
			CostumeListCanvas.instance.gameObject.SetActive(false);
			CharacterCanvas.instance.ShowPreviewObject(true, _costumeTableData.costumeId);
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("CostumeUI_ChangeCostumeToast"), 2.0f);
		});
	}

	// SimpleCashCanvas 과 다르게 구현하는거만 처리
	public void OnClickCustomButton()
	{
		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		if (_contains)
		{
			if (equippedObject.activeSelf)
			{
				if (string.IsNullOrEmpty(CharacterCanvas.instance.previewId) == false)
				{
					CharacterCanvas.instance.ShowCanvasPlayerActorWithCostume("", () =>
					{
						CostumeListCanvas.instance.gameObject.SetActive(false);
						CharacterCanvas.instance.ShowPreviewObject(false, "");
						ToastCanvas.instance.ShowToast(UIString.instance.GetString("CostumeUI_ChangeCostumeToast"), 2.0f);
					});
					return;
				}

				ToastCanvas.instance.ShowToast(UIString.instance.GetString("CostumeUI_AlreadyCostumeToast"), 2.0f);
				return;
			}

			// 보유하고 있다면 바로 입어보면 되는거다.
			string costumeId = _costumeTableData.costumeId;
			if (_baseCostume) costumeId = "";
			PlayFabApiManager.instance.RequestSelectCostume(costumeId, () =>
			{
				CharacterCanvas.instance.ShowCanvasPlayerActorWithCostume("", () =>
				{
					CostumeListCanvas.instance.gameObject.SetActive(false);
					CharacterCanvas.instance.ShowPreviewObject(false, "");
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("CostumeUI_ChangeCostumeToast"), 2.0f);

					// 이 타이밍에 맞춰서 캐릭터 변경도 해야한다.
					CostumeManager.instance.ChangeCostume();
				});
			});
			return;
		}

		// 보유중이 아니라면 상황에 따라 처리하면 된다.
		if (_costumeTableData.condition)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("CostumeUI_EventGainToast"), 2.0f);
			return;
		}
		if (_costumeTableData.goldCost > 0)
		{
			if (CurrencyData.instance.gold < _costumeTableData.goldCost)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
				return;
			}

			YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("CostumeUI_BuyConfirm"), () =>
			{
				PlayFabApiManager.instance.RequestPurchaseCostumeByGold(_costumeTableData.costumeId, _costumeTableData.goldCost, () =>
				{
					CostumeListCanvas.instance.currencySmallInfo.RefreshInfo();
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("CostumeUI_PurchaseComplete"), 2.0f);
					if (CostumeListCanvas.instance != null)
						CostumeListCanvas.instance.RefreshGrid();
				});
			});
			return;
		}

		// 실제 구매
		// 이건 다른 캐시상품도 마찬가지인데 클릭 즉시 간단한 패킷을 보내서 통신가능한 상태인지부터 확인한다.
		PlayFabApiManager.instance.RequestNetworkOnce(OnResponse, null, true);
	}

	protected override void RequestServerPacket(Product product)
	{
		ExternalRequestServerPacket(product, _costumeTableData);
	}

	public static void ExternalRequestServerPacket(Product product, CostumeTableData costumeTableData)
	{
#if UNITY_ANDROID
		GooglePurchaseData data = new GooglePurchaseData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (uint)product.metadata.localizedPrice * 100, data.inAppPurchaseData, data.inAppDataSignature, () =>
#elif UNITY_IOS
		iOSReceiptData data = new iOSReceiptData(product.receipt);
		PlayFabApiManager.instance.RequestValidatePurchase(product.metadata.isoCurrencyCode, (int)(product.metadata.localizedPrice * 100), data.Payload, () =>
#endif
		{
			if (costumeTableData == null)
			{
				if (product != null)
					costumeTableData = TableDataManager.instance.FindCostumeTableData(product.definition.id);
			}
			CostumeManager.instance.OnRecvPurchase(costumeTableData.costumeId);
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("CostumeUI_PurchaseComplete"), 2.0f);
			if (CostumeListCanvas.instance != null)
				CostumeListCanvas.instance.RefreshGrid();

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