using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

public class PetData
{
	public ObscuredString uniqueId;
	public ObscuredString petId;

	ObscuredInt _count;
	public int count { get { return _count; } }

	// 메인 공격력 스탯 및 랜덤옵 합산
	ObscuredInt _mainStatusValue = 0;
	public int mainStatusValue { get { return _mainStatusValue; } }

	public static string GetAddressByPetId(string petId)
	{
		PetTableData petTableData = TableDataManager.instance.FindPetTableData(petId);
		if (petTableData == null)
			return "";
		return petTableData.prefabAddress;
	}

	public void SetCount(int count)
	{
		_count = count;

		// 이후 Status 계산
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		_mainStatusValue = 0;

		// base
		_mainStatusValue = cachedPetTableData.accumulatedAtk;

		// multiple
		_mainStatusValue *= _count;
	}





	PetTableData _cachedPetTableData = null;
	public PetTableData cachedPetTableData
	{
		get
		{
			if (_cachedPetTableData == null)
				_cachedPetTableData = TableDataManager.instance.FindPetTableData(petId);
			return _cachedPetTableData;
		}
	}
}