using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RelayPackageGroupInfo : MonoBehaviour
{
	public static RelayPackageGroupInfo instance;

	public ScrollSnap scrollSnap;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<RelayPackageBox>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	List<int> _listShowIndex = new List<int>();
	List<RelayPackageBox> _listRelayPackageBoxListItem = new List<RelayPackageBox>();
	void OnEnable()
	{
		// 데이터를 받지 않은 상태라면 상품 아이콘 보여줄수가 없을거다. 그냥 하이드 시켜둔다.
		if (PlayerData.instance.downloadConfirmed == false)
		{
			gameObject.SetActive(false);
			return;
		}

		for (int i = 0; i < _listRelayPackageBoxListItem.Count; ++i)
			_listRelayPackageBoxListItem[i].gameObject.SetActive(false);
		_listRelayPackageBoxListItem.Clear();
		_listShowIndex.Clear();

		for (int i = 0; i < TableDataManager.instance.relayPackTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.relayPackTable.dataArray[i].num <= CashShopData.instance.relayPackagePurchasedNum)
				continue;

			_listShowIndex.Add(i);
		}

		// 보여줄게 없다면 통째로 꺼두면 된다.
		if (_listShowIndex.Count == 0)
		{
			gameObject.SetActive(false);
			return;
		}

		for (int i = 0; i < _listShowIndex.Count; ++i)
		{
			RelayPackageBox relayPackageBox = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			relayPackageBox.RefreshInfo(TableDataManager.instance.relayPackTable.dataArray[_listShowIndex[i]]);
			_listRelayPackageBoxListItem.Add(relayPackageBox);
		}

		// 다 구성하고나서 이렇게 Setup 호출해주면 된다.
		scrollSnap.Setup();
		scrollSnap.GoToLastPanel();
		gameObject.SetActive(true);
	}
}