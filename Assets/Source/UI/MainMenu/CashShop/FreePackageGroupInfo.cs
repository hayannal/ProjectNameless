using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreePackageGroupInfo : MonoBehaviour
{
	public enum eFreeType
	{
		Level = 1,
		Stage = 2
	}

	public int freeType;
	public GameObject emptyObject;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<FreePackageBox>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	List<int> _listShowIndex = new List<int>();
	List<FreePackageBox> _listFreePackageBoxListItem = new List<FreePackageBox>();
	void OnEnable()
	{
		for (int i = 0; i < _listFreePackageBoxListItem.Count; ++i)
			_listFreePackageBoxListItem[i].gameObject.SetActive(false);
		_listFreePackageBoxListItem.Clear();
		_listShowIndex.Clear();

		for (int i = 0; i < TableDataManager.instance.freePackageTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.freePackageTable.dataArray[i].type != freeType)
				continue;

			int conValue = TableDataManager.instance.freePackageTable.dataArray[i].conValue;
			if (freeType == (int)eFreeType.Level)
			{
				if (CashShopData.instance.IsRewardedFreeLevelPackage(conValue))
					continue;
			}
			else if (freeType == (int)eFreeType.Stage)
			{
				if (CashShopData.instance.IsRewardedFreeStagePackage(conValue))
					continue;
			}
			_listShowIndex.Add(i);
		}

		// 보여줄게 없다면 
		if (_listShowIndex.Count == 0)
		{
			emptyObject.SetActive(true);
			return;
		}

		emptyObject.SetActive(false);
		for (int i = 0; i < _listShowIndex.Count; ++i)
		{
			FreePackageBox freePackageBox = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			freePackageBox.RefreshInfo(TableDataManager.instance.freePackageTable.dataArray[_listShowIndex[i]]);
			_listFreePackageBoxListItem.Add(freePackageBox);
		}
	}
}