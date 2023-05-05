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

		// Ȥ�ó� �ε� �Ǿ����� �ʴٸ� �ε� �ɾ�д�. ���� �� �Ǳ� ���� �ε� �ɰŴ�.
		if (SpellSpriteContainer.instance == null)
		{
			AddressableAssetLoadManager.GetAddressableGameObject("SpellSpriteContainer", "", (prefab) =>
			{
				BattleInstanceManager.instance.GetCachedObject(prefab, null);
			});
		}

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.SetLastItem(this, _shopSpellTableData.price);

		// �̹� ����â�� �����ִ� �������״� ��ǲ�� ���� ��Ŷ ����
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

		// �и��� â�� ����������Ŵ�. ������ ���� �ȵȴ�.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvRetryResult(RandomBoxScreenCanvas.eBoxType.Spell, listItemInstance);
	}
}