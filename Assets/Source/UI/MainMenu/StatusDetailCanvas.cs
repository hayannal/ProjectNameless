using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusDetailCanvas : MonoBehaviour
{
	public static StatusDetailCanvas instance;

	public GameObject rootObject;
	public GameObject smallRootObject;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;
	public RectTransform smallContentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<StatusDetailCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	public class SmallCustomItemContainer : CachedItemHave<StatusDetailCanvasListItem>
	{
	}
	SmallCustomItemContainer _smallContainer = new SmallCustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	bool _useSmall;
	public void Initialize(int count)
	{
		_useSmall = (count <= 4);
		rootObject.SetActive(!_useSmall);
		smallRootObject.SetActive(_useSmall);

		for (int i = 0; i < _listStatusDetailCanvasListItem.Count; ++i)
			_listStatusDetailCanvasListItem[i].gameObject.SetActive(false);
		_listStatusDetailCanvasListItem.Clear();
	}

	List<StatusDetailCanvasListItem> _listStatusDetailCanvasListItem = new List<StatusDetailCanvasListItem>();
	public void AddStatus(string stringId, int value)
	{
		AddStatus(stringId, value.ToString("N0"));
	}

	public void AddStatus(string stringId, string value)
	{
		if (_useSmall)
		{
			StatusDetailCanvasListItem statusDetailCanvasListItem = _smallContainer.GetCachedItem(contentItemPrefab, smallContentRootRectTransform);
			statusDetailCanvasListItem.Initialize(_listStatusDetailCanvasListItem.Count % 2 == 0, stringId, value);
			_listStatusDetailCanvasListItem.Add(statusDetailCanvasListItem);
		}
		else
		{
			StatusDetailCanvasListItem statusDetailCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			statusDetailCanvasListItem.Initialize(_listStatusDetailCanvasListItem.Count % 2 == 0, stringId, value);
			_listStatusDetailCanvasListItem.Add(statusDetailCanvasListItem);
		}
	}
}