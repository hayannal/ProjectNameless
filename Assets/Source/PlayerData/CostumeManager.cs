using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

public class CostumeManager : MonoBehaviour
{
	public static CostumeManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("CostumeManager")).AddComponent<CostumeManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static CostumeManager _instance = null;

	public ObscuredInt cachedValue { get; set; }

	List<ObscuredString> _listCostumeId = new List<ObscuredString>();
	public void OnRecvCostumeInventory(List<ItemInstance> userInventory, List<StatisticValue> playerStatistics)
	{
		ClearInventory();

		// list
		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId.StartsWith("Costume_") == false)
				continue;

			CostumeTableData costumeTableData = TableDataManager.instance.FindCostumeTableData(userInventory[i].ItemId);
			if (costumeTableData == null)
				continue;
			_listCostumeId.Add(userInventory[i].ItemId);
		}

		// status
		RefreshCachedStatus();
	}

	public void ClearInventory()
	{
		_listCostumeId.Clear();

		// status
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		cachedValue = 0;

		// spell level status
		for (int i = 0; i < _listCostumeId.Count; ++i)
		{
			CostumeTableData costumeTableData = TableDataManager.instance.FindCostumeTableData(_listCostumeId[i]);
			if (costumeTableData == null)
				continue;
			cachedValue += costumeTableData.atk;
		}
	}

	public void OnChangedStatus()
	{
		RefreshCachedStatus();
		PlayerData.instance.OnChangedStatus();
	}


	public bool Contains(string costumeId)
	{
		if (_listCostumeId.Contains(costumeId))
			return true;
		return false;
	}

	public void OnRecvPurchase(string costumeId)
	{
		if (_listCostumeId.Contains(costumeId) == false)
			_listCostumeId.Add(costumeId);
		OnChangedStatus();
	}
}