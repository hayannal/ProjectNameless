using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class CashShopSpellSmallListItem : MonoBehaviour
{
	public int index = 0;

	const float moreRate = 1.5f;

	public Text moreText;
	public Text countText;
	public Text priceText;
	public GameObject blackObject;

	Color _moreTextDefaultColor;
	void Awake()
	{
		_moreTextDefaultColor = moreText.color;
	}

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
			moreText.gameObject.SetActive(true);
			_resultCount = (int)(shopSpellTableData.count * moreRate);
			countText.text = string.Format("X {0:N0}", _resultCount);
			priceText.text = shopSpellTableData.price.ToString("N0");
			_shopSpellTableData = shopSpellTableData;
		}
		else
		{
			moreText.gameObject.SetActive(false);
			_resultCount = shopSpellTableData.count;
			countText.text = string.Format("X {0:N0}", _resultCount);
			priceText.text = shopSpellTableData.price.ToString("N0");
			_shopSpellTableData = shopSpellTableData;
		}

		SetBlackObject(false);
	}

	void SetBlackObject(bool active)
	{
		blackObject.SetActive(active);
		moreText.color = active ? Color.white * 0.5f : _moreTextDefaultColor;
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

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.SetLastItem(this, _shopSpellTableData.price);

		// 이미 연출창이 열려있는 상태일테니 인풋을 막고 패킷 전송
		WaitingNetworkCanvas.Show(true);

		List<ObscuredString> listSpellId = SpellManager.instance.GetRandomIdList(_resultCount);
		_count = listSpellId.Count;
		PlayFabApiManager.instance.RequestOpenSpellBox(listSpellId, _shopSpellTableData.count, _shopSpellTableData.price, moreText.gameObject.activeSelf, OnRecvResult);
	}

	void OnRecvResult(string itemGrantString)
	{
		WaitingNetworkCanvas.Show(false);

		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = SpellManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.SpellGacha, _count);

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvRetryResult(RandomBoxScreenCanvas.eBoxType.Spell, listItemInstance);
	}
}