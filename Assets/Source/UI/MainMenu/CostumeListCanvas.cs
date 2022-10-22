using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CostumeListCanvas : MonoBehaviour
{
	public static CostumeListCanvas instance;

	public Sprite[] spriteList;
	public CurrencySmallInfo currencySmallInfo;
	public LayoutElement emptyRectLayoutElement; 
	public Transform separateLineTransform;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;
	public RectTransform noGainContentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<CostumeCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	public class NoGainCustomItemContainer : CachedItemHave<CostumeCanvasListItem>
	{
	}
	NoGainCustomItemContainer _noGainContainer = new NoGainCustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (restore)
			return;
		
		// refresh
		RefreshGrid();
	}

	void OnDisable()
	{
		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;


	}


	public Sprite GetSprite(string portraitAddress)
	{
		for (int i = 0; i < spriteList.Length; ++i)
		{
			if (spriteList[i].name == portraitAddress)
				return spriteList[i];
		}
		return null;
	}

	List<CostumeTableData> _listTempTableData = new List<CostumeTableData>();
	List<CostumeCanvasListItem> _listCostumeCanvasListItem = new List<CostumeCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listCostumeCanvasListItem.Count; ++i)
			_listCostumeCanvasListItem[i].gameObject.SetActive(false);
		_listCostumeCanvasListItem.Clear();

		_listTempTableData.Clear();
		separateLineTransform.gameObject.SetActive(false);
		int noGainCount = 0;
		for (int i = 0; i < TableDataManager.instance.costumeTable.dataArray.Length; ++i)
		{
			if (CostumeManager.instance.Contains(TableDataManager.instance.costumeTable.dataArray[i].costumeId) == false)
			{
				++noGainCount;
				continue;
			}
			_listTempTableData.Add(TableDataManager.instance.costumeTable.dataArray[i]);
		}

		if (_listTempTableData.Count > 0)
		{
			_listTempTableData.Sort(delegate (CostumeTableData x, CostumeTableData y)
			{
				if (x.orderIndex > y.orderIndex) return 1;
				else if (x.orderIndex < y.orderIndex) return -1;
				return 0;
			});

			for (int i = 0; i < _listTempTableData.Count; ++i)
			{
				CostumeCanvasListItem costumeCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
				costumeCanvasListItem.Initialize(true, _listTempTableData[i]);
				_listCostumeCanvasListItem.Add(costumeCanvasListItem);
			}
		}

		if (noGainCount == 0)
			return;

		separateLineTransform.gameObject.SetActive(true);
		emptyRectLayoutElement.preferredHeight = (_listTempTableData.Count == 0) ? 20.0f : 70.0f;

		_listTempTableData.Clear();
		for (int i = 0; i < TableDataManager.instance.costumeTable.dataArray.Length; ++i)
		{
			if (CostumeManager.instance.Contains(TableDataManager.instance.costumeTable.dataArray[i].costumeId))
				continue;
			_listTempTableData.Add(TableDataManager.instance.costumeTable.dataArray[i]);
		}

		_listTempTableData.Sort(delegate (CostumeTableData x, CostumeTableData y)
		{
			if (x.orderIndex > y.orderIndex) return 1;
			else if (x.orderIndex < y.orderIndex) return -1;
			return 0;
		});

		for (int i = 0; i < _listTempTableData.Count; ++i)
		{
			CostumeCanvasListItem costumeCanvasListItem = _noGainContainer.GetCachedItem(contentItemPrefab, noGainContentRootRectTransform);
			costumeCanvasListItem.Initialize(false, _listTempTableData[i]);
			_listCostumeCanvasListItem.Add(costumeCanvasListItem);
		}
	}
}