using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class EnergyPaybackCanvas : SimpleCashEventCanvas
{
	public static EnergyPaybackCanvas instance;

	public Text usedCountText;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<EnergyPaybackCanvasListItem>
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
		
		// refresh
		RefreshGrid();

		/*
		if (CashShopData.instance.levelPassAlarmStateForNoPass)
		{
			CashShopData.instance.levelPassAlarmStateForNoPass = false;
			MainCanvas.instance.RefreshLevelPassAlarmObject();
		}
		*/
	}

	void OnEnable()
	{
		usedCountText.text = CashShopData.instance.energyUseForPayback.ToString("N0");

		SetInfo();
		MainCanvas.instance.OnEnterCharacterMenu(true);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	List<EnergyPaybackCanvasListItem> _listEnergyPaybackCanvasListItem = new List<EnergyPaybackCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listEnergyPaybackCanvasListItem.Count; ++i)
			_listEnergyPaybackCanvasListItem[i].gameObject.SetActive(false);
		_listEnergyPaybackCanvasListItem.Clear();

		for (int i = 0; i < TableDataManager.instance.energyUsePaybackTable.dataArray.Length; ++i)
		{
			EnergyPaybackCanvasListItem energyPaybackCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			energyPaybackCanvasListItem.Initialize(TableDataManager.instance.energyUsePaybackTable.dataArray[i].use, TableDataManager.instance.energyUsePaybackTable.dataArray[i].payback);
			_listEnergyPaybackCanvasListItem.Add(energyPaybackCanvasListItem);
		}
	}
}