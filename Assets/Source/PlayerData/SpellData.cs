using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

public class SpellData
{
	public ObscuredString uniqueId;
	public ObscuredString spellId;

	ObscuredInt _count;
	ObscuredInt _level;
	public int count { get { return _count; } }
	public int level { get { return _level; } }

	// 메인 공격력 스탯 및 랜덤옵 합산
	ObscuredInt _mainStatusValue = 0;
	public int mainStatusValue { get { return _mainStatusValue; } }

	public static string KeyLevel = "lv";

	public void Initialize(int count, Dictionary<string, string> customData)
	{
		int level = 1;
		if (customData.ContainsKey(KeyLevel))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyLevel], out intValue))
				level = intValue;
		}
		bool invalidEquipOption = false;
		int invalidEquipOptionParam2 = 0;

		// level table 검사

		if (invalidEquipOption)
		{
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidEquipOption, false, invalidEquipOptionParam2);
		}

		_count = count;
		_level = level;
		
		// 이후 Status 계산
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		_mainStatusValue = 0;
		if (_level > 0)
		{
			SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(spellId, _level);
			if (skillLevelTableData != null)
				_mainStatusValue = skillLevelTableData.accumulatedAtk;
		}
	}
}