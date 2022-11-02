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
		if (CashShopCanvas.instance != null)
			lineImageRectTransform.sizeDelta = new Vector2(lineImageRectTransform.sizeDelta.x, diff.magnitude * CashShopCanvas.instance.lineLengthRatio);
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
		UIInstanceManager.instance.ShowCanvasAsync("GachaSpellInfoCanvas", null);
	}

	public void OnClickButton()
	{
		if (CurrencyData.instance.gold < _shopSpellTableData.price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		List<ObscuredString> listSpellId = SpellManager.instance.GetRandomIdList(_resultCount);
		PlayFabApiManager.instance.RequestOpenSpellBox(listSpellId, _shopSpellTableData.count, _shopSpellTableData.price, moreTextObject.activeSelf, (itemGrantString) =>
		{
			if (itemGrantString != "")
			{
				SpellManager.instance.OnRecvItemGrantResult(itemGrantString, listSpellId.Count);
			}
		});
	}
}