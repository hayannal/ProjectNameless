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
	ObscuredInt _enhanceLevel;
	ObscuredInt _mainOption;
	public bool isLock { get { return _isLock; } }
	public int mainOption { get { return _mainOption; } }
	public int enhanceLevel { get { return _enhanceLevel; } }

	// 메인 공격력 스탯 및 랜덤옵 합산
	ObscuredInt _mainStatusValue = 0;
	public int mainStatusValue { get { return _mainStatusValue; } }
	EquipStatusList _equipStatusList = new EquipStatusList();
	public EquipStatusList equipStatusList { get { return _equipStatusList; } }

	// for AlarmObject
	public bool newEquip { get; set; }

	public static string KeyMainOp = "mainOp";
	public static string KeyLock = "lock";
	public static string KeyEnhan = "enhan";
	public static string KeyRandomOp = "randOp";
	public static string KeyTransmuteRemainCount = "trsmtReCnt";

	public void Initialize(Dictionary<string, string> customData)
	{
		bool lockState = false;
		int enhan = 0;
		float mainOp = 0.0f;
		int trsmtCount = 0;
		if (customData.ContainsKey(KeyEnhan))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyEnhan], out intValue))
				enhan = intValue;
		}
		if (customData.ContainsKey(KeyLock))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyLock], out intValue))
				lockState = (intValue == 1);
		}
		if (customData.ContainsKey(KeyMainOp))
		{
			float floatValue = 0;
			if (float.TryParse(customData[KeyMainOp], out floatValue))
				mainOp = floatValue;
		}
		bool invalidEquipOption = false;

		_isLock = lockState;
		_enhanceLevel = enhan;

		// 이후 Status 계산
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		// 자리별 보너스 같은건 사라졌지만 UI표기를 위해서라도 따로 가지고 있는게 편하다. 이 캐싱엔 강화 정보까지 포함되어있다.
		_mainStatusValue = _mainOption;
	}

	public void SetLock(bool lockState)
	{
		_isLock = lockState;
	}

	public void OnEnhance(int targetEnhanceLevel)
	{
		_enhanceLevel = targetEnhanceLevel;

		RefreshCachedStatus();
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
