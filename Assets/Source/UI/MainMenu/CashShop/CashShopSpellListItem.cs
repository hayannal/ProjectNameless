using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class CashShopSpellListItem : MonoBehaviour
{
	public int index = 0;

	const float moreRate = 1.5f;

	public GameObject moreTextObject;
	public Text countText;
	public Text prevCountText;
	public RectTransform lineImageRectTransform;
	public RectTransform rightTopRectTransform;

	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	ObscuredInt _resultCount;
	ShopSpellTableData _shopSpellTableData;
	void OnEnable()
	{
		// hardcode ev10
		bool eventApplied = CashShopData.instance.IsShowEvent("ev10");
		//eventApplied = true;

		ShopSpellTableData shopSpellTableData = TableDataManager.instance.FindShopSpellTableDataByIndex(index);
		if (shopSpellTableData == null)
			return;
		
		if (eventApplied)
		{
			moreTextObject.SetActive(true);
			prevCountText.text = string.Format("X {0:N0}", shopSpellTableData.count);
			prevCountText.gameObject.SetActive(false);
			prevCountText.gameObject.SetActive(true);
			_resultCount = (int)(shopSpellTableData.count * moreRate);
			countText.text = string.Format("X {0:N0}", _resultCount);
			priceText.text = shopSpellTableData.price.ToString("N0");
			RefreshLineImage();
			_updateRefreshLineImageCount = 3;
			_shopSpellTableData = shopSpellTableData;
		}
		else
		{
			moreTextObject.SetActive(false);
			prevCountText.gameObject.SetActive(false);
			_resultCount = shopSpellTableData.count;
			countText.text = string.Format("X {0:N0}", _resultCount);
			priceText.text = shopSpellTableData.price.ToString("N0");
			_shopSpellTableData = shopSpellTableData;
		}
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

	public void OnClickInfoButton()
	{
		if (SpellSpriteContainer.instance == null)
		{
			DelayedLoadingCanvas.Show(true);
			AddressableAssetLoadManager.GetAddressableGameObject("SpellSpriteContainer", "", (prefab) =>
			{
				BattleInstanceManager.instance.GetCachedObject(prefab, null);
				DelayedLoadingCanvas.Show(false);
				UIInstanceManager.instance.ShowCanvasAsync("GachaSpellInfoCanvas", null);
			});
		}
		else
			UIInstanceManager.instance.ShowCanvasAsync("GachaSpellInfoCanvas", null);
	}

	int _count;
	public void OnClickButton()
	{
		if (CurrencyData.instance.dia < _shopSpellTableData.price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		// 혹시나 로드 되어있지 않다면 로드 걸어둔다. 연출 다 되기 전엔 로딩 될거다.
		if (SpellSpriteContainer.instance == null)
		{
			AddressableAssetLoadManager.GetAddressableGameObject("SpellSpriteContainer", "", (prefab) =>
			{
				BattleInstanceManager.instance.GetCachedObject(prefab, null);
			});
		}

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_ConfirmPurchase"), () => {

			// 연출 및 보상 처리. 100개씩 뽑으면 느릴 수 있으니 패킷 대기 없이 바로 시작한다.
			UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
			{
				// 연출창 시작과 동시에 패킷을 보내고
				List<ObscuredString> listSpellId = SpellManager.instance.GetRandomIdList(_resultCount);
				_count = listSpellId.Count;
				PlayFabApiManager.instance.RequestOpenSpellBox(listSpellId, _shopSpellTableData.count, _shopSpellTableData.price, moreTextObject.activeSelf, OnRecvResult);
			});
		});
	}

	void OnRecvResult(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = SpellManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.SpellGacha, _count);

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvResult(RandomBoxScreenCanvas.eBoxType.Spell, listItemInstance);
	}
}