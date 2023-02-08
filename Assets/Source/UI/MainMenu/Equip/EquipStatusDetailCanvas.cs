using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class EquipStatusDetailCanvas : MonoBehaviour
{
	public static EquipStatusDetailCanvas instance;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<EquipStatusDetailCanvasListItem>
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

	List<EquipStatusDetailCanvasListItem> _listEquipStatusDetailCanvasListItem = new List<EquipStatusDetailCanvasListItem>();
	void OnEnable()
	{
		for (int i = 0; i < _listEquipStatusDetailCanvasListItem.Count; ++i)
			_listEquipStatusDetailCanvasListItem[i].gameObject.SetActive(false);
		_listEquipStatusDetailCanvasListItem.Clear();

		for (int i = 0; i < EquipManager.instance.cachedEquipStatusList.valueList.Length; ++i)
		{
			if (EquipManager.instance.cachedEquipStatusList.valueList[i] <= 0.0f)
				continue;

			EquipStatusDetailCanvasListItem equipStatusDetailCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipStatusDetailCanvasListItem.Initialize((eActorStatus)i, EquipManager.instance.cachedEquipStatusList.valueList[i]);
			_listEquipStatusDetailCanvasListItem.Add(equipStatusDetailCanvasListItem);
		}
	}
}