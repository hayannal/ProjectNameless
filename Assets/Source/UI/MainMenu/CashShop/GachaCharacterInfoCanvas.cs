using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaCharacterInfoCanvas : MonoBehaviour
{
	public static GachaCharacterInfoCanvas instance;
	
	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<GachaCharacterInfoCanvasListItem>
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

	List<GachaCharacterInfoCanvasListItem> _listGachaCharacterInfoCanvasListItem = new List<GachaCharacterInfoCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listGachaCharacterInfoCanvasListItem.Count; ++i)
			_listGachaCharacterInfoCanvasListItem[i].gameObject.SetActive(false);
		_listGachaCharacterInfoCanvasListItem.Clear();

		for (int i = 0; i < TableDataManager.instance.gachaActorTable.dataArray.Length; ++i)
		{
			GachaCharacterInfoCanvasListItem gachaCharacterInfoCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			gachaCharacterInfoCanvasListItem.Initialize(TableDataManager.instance.gachaActorTable.dataArray[i]);
			_listGachaCharacterInfoCanvasListItem.Add(gachaCharacterInfoCanvasListItem);
		}
	}
}