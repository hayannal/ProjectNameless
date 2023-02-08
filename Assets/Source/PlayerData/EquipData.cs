using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using ActorStatusDefine;

public class EquipData
{
	public ObscuredString uniqueId;
	public ObscuredString equipId;

	ObscuredBool _isLock;
	public bool isLock { get { return _isLock; } }

	// 메인 공격력 스탯 및 랜덤옵 합산
	public int mainStatusValue { get { return cachedEquipTableData.atk; } }
	EquipStatusList _equipStatusList = new EquipStatusList();
	public EquipStatusList equipStatusList { get { return _equipStatusList; } }

	// for AlarmObject
	public bool newEquip { get; set; }

	public static string KeyLock = "lock";
	public static int EquipOptionCountMax = 4;

	public void Initialize(Dictionary<string, string> customData)
	{
		bool lockState = false;
		if (customData.ContainsKey(KeyLock))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyLock], out intValue))
				lockState = (intValue == 1);
		}
		_isLock = lockState;

		// 이후 Status 계산
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		// 서브 옵션들을 돌면서 equipStatusList에 모아야한다. 같은 옵은 같은 옵션끼리.
		for (int i = 0; i < _equipStatusList.valueList.Length; ++i)
			_equipStatusList.valueList[i] = 0.0f;

		for (int i = 0; i < EquipOptionCountMax; ++i)
		{
			eActorStatus statusType = GetOption(i);
			float value = GetOptionValue(i);
			_equipStatusList.valueList[(int)statusType] += value;
		}
	}

	public void SetLock(bool lockState)
	{
		_isLock = lockState;
	}

	public string GetUsableEquipSkillId()
	{
		if (string.IsNullOrEmpty(cachedEquipTableData.skillId))
			return "";

		if (cachedEquipTableData.grade >= cachedEquipTableData.skillActive)
			return cachedEquipTableData.skillId;
		return "";
	}

	public bool IsGetAvailable(int index)
	{
		switch (index)
		{
			case 0: if (cachedEquipTableData.grade >= 3) return true; break;
			case 1: if (cachedEquipTableData.grade >= 4) return true; break;
			case 2: if (cachedEquipTableData.grade >= 5) return true; break;
			case 3: if (cachedEquipTableData.grade >= 6) return true; break;
		}
		return false;
	}

	public eActorStatus GetOption(int index)
	{
		switch (index)
		{
			case 0: return (eActorStatus)cachedEquipTableData.option3;
			case 1: return (eActorStatus)cachedEquipTableData.option4;
			case 2: return (eActorStatus)cachedEquipTableData.option5;
			case 3: return (eActorStatus)cachedEquipTableData.option6;
		}
		return eActorStatus.MaxHp;
	}

	public float GetOptionValue(int index)
	{
		switch (index)
		{
			case 0: return cachedEquipTableData.option3Value;
			case 1: return cachedEquipTableData.option4Value;
			case 2: return cachedEquipTableData.option5Value;
			case 3: return cachedEquipTableData.option6Value;
		}
		return 0.0f;
	}
	


	EquipTableData _cachedEquipTableData = null;
	public EquipTableData cachedEquipTableData
	{
		get
		{
			if (_cachedEquipTableData == null)
				_cachedEquipTableData = TableDataManager.instance.FindEquipTableData(equipId);
			return _cachedEquipTableData;
		}
	}
}
