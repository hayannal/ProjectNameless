using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageClearGroupInfo : MonoBehaviour
{
	public static StageClearGroupInfo instance;

	public ScrollSnap scrollSnap;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<StageClearPackageBox>
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
	List<StageClearPackageBox> _listStageClearPackageBoxListItem = new List<StageClearPackageBox>();
	void OnEnable()
	{
		for (int i = 0; i < _listStageClearPackageBoxListItem.Count; ++i)
			_listStageClearPackageBoxListItem[i].gameObject.SetActive(false);
		_listStageClearPackageBoxListItem.Clear();
		_listShowIndex.Clear();

		for (int i = 0; i < TableDataManager.instance.stageClearTable.dataArray.Length; ++i)
		{
			int stage = TableDataManager.instance.stageClearTable.dataArray[i].stagecleared;
			if (PlayerData.instance.highestClearStage < stage)
				continue;
			if (CashShopData.instance.IsPurchasedStageClearPackage(stage))
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
			StageClearPackageBox stageClearPackageBox = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			stageClearPackageBox.RefreshInfo(TableDataManager.instance.stageClearTable.dataArray[_listShowIndex[i]]);
			_listStageClearPackageBoxListItem.Add(stageClearPackageBox);
		}

		// 다 구성하고나서 이렇게 Setup 호출해주면 된다.
		scrollSnap.Setup();
		scrollSnap.GoToLastPanel();
		gameObject.SetActive(true);
	}
}