using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaEquipInfoCanvas : MonoBehaviour
{
	public static GachaEquipInfoCanvas instance;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<GachaEquipInfoCanvasListItem>
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
		RefreshGrid();
	}

	List<GachaEquipInfoCanvasListItem> _listGachaEquipInfoCanvasListItem = new List<GachaEquipInfoCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listGachaEquipInfoCanvasListItem.Count; ++i)
			_listGachaEquipInfoCanvasListItem[i].gameObject.SetActive(false);
		_listGachaEquipInfoCanvasListItem.Clear();

		for (int i = 0; i < TableDataManager.instance.gachaEquipTable.dataArray.Length; ++i)
		{
			GachaEquipInfoCanvasListItem gachaEquipInfoCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			gachaEquipInfoCanvasListItem.Initialize(TableDataManager.instance.gachaEquipTable.dataArray[i]);
			_listGachaEquipInfoCanvasListItem.Add(gachaEquipInfoCanvasListItem);
		}
	}
}