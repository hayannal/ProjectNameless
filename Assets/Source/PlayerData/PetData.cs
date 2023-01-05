using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

public class PetData
{
	public ObscuredString uniqueId;
	public ObscuredString petId;

	ObscuredInt _count;
	ObscuredInt _step;
	ObscuredInt _heart;
	public int count { get { return _count; } }
	public int step { get { return _step; } }
	public int heart { get { return _heart; } }

	// 메인 공격력 스탯 및 랜덤옵 합산
	ObscuredInt _mainStatusValue = 0;
	public int mainStatusValue { get { return _mainStatusValue; } }

	public static string KeyStep = "stp";

	public static string GetAddressByPetId(string petId)
	{
		PetTableData petTableData = TableDataManager.instance.FindPetTableData(petId);
		if (petTableData == null)
			return "";
		return petTableData.prefabAddress;
	}

	public static string GetNameByPetId(string petId)
	{
		PetTableData petTableData = TableDataManager.instance.FindPetTableData(petId);
		if (petTableData == null)
			return "";
		return UIString.instance.GetString(petTableData.nameId);
	}

	public void Initialize(int count, int heart, Dictionary<string, string> customData)
	{
		int step = 0;
		if (customData != null && customData.ContainsKey(KeyStep))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyStep], out intValue))
				step = intValue;
		}
		
		_count = count;
		_step = step;
		_heart = heart;

		// 이후 Status 계산
		RefreshCachedStatus();
	}

	public void SetCount(int count)
	{
		_count = count;

		// 이후 Status 계산
		RefreshCachedStatus();
	}

	public void SetHeart(int heart)
	{
		_heart = heart;
	}

	void RefreshCachedStatus()
	{
		_mainStatusValue = 0;

		// base
		_mainStatusValue = cachedPetTableData.accumulatedAtk;

		int applyCount = _count;
		PetCountTableData petCountTableData = TableDataManager.instance.FindPetCountTableData(cachedPetTableData.star, step);
		if (petCountTableData != null)
		{
			if (applyCount > petCountTableData.max)
				applyCount = petCountTableData.max;
		}

		// multiple
		_mainStatusValue *= applyCount;
	}

	public void OnMaxLevelUp(int targetStep)
	{
		_step = targetStep;
		RefreshCachedStatus();
		PetManager.instance.OnChangedStatus();
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