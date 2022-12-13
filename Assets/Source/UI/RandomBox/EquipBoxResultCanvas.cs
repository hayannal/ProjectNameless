using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using MEC;

public class EquipBoxResultCanvas : MonoBehaviour
{
	public static EquipBoxResultCanvas instance;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<EquipBoxResultCanvasListItem>
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

	void OnDisable()
	{
		// 부모인 RandomBoxScreenCanvas가 닫힐때 함께 닫히도록 한다.
		gameObject.SetActive(false);
	}

	public void ShowResult(List<ItemInstance> listItemInstance)
	{
		Timing.RunCoroutine(ShowGridProcess(listItemInstance));
	}

	List<EquipBoxResultCanvasListItem> _listEquipBoxResultCanvasListItem = new List<EquipBoxResultCanvasListItem>();
	IEnumerator<float> ShowGridProcess(List<ItemInstance> listItemInstance)
	{
		for (int i = 0; i < _listEquipBoxResultCanvasListItem.Count; ++i)
			_listEquipBoxResultCanvasListItem[i].gameObject.SetActive(false);
		_listEquipBoxResultCanvasListItem.Clear();

		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listItemInstance[i].ItemId);
			if (equipTableData == null)
				continue;

			EquipBoxResultCanvasListItem equipBoxResultCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipBoxResultCanvasListItem.Initialize(listItemInstance[i], equipTableData);
			_listEquipBoxResultCanvasListItem.Add(equipBoxResultCanvasListItem);

			yield return Timing.WaitForSeconds(0.1f);
		}

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.exitObject.SetActive(true);
	}
}