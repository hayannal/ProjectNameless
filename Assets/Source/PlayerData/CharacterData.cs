using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.DataModels;

public class CharacterData
{
	public static string s_PlayerActorId = "Actor0245";

	public ObscuredString uniqueId;
	public ObscuredString actorId;

	ObscuredInt _count;
	ObscuredInt _level;
	public int count { get { return _count; } }
	public int level { get { return _level; } }

	ObscuredInt _transcend;
	public int transcendPoint { get { return _count - 1; } }
	public int transcend { get { return _transcend; } }

	ObscuredInt _pp;
	public int pp { get { return _pp; } }

	// 메인 공격력 스탯 및 랜덤옵 합산
	ObscuredInt _mainStatusValue = 0;
	public int mainStatusValue { get { return _mainStatusValue; } }

	public static string KeyLevel = "lv";
	public static string KeyTranscend = "tr";

	public static string GetAddressByActorId(string actorId)
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return "";
		return actorTableData.prefabAddress;
	}

	public static string GetNameByActorId(string actorId)
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return "";
		return UIString.instance.GetString(actorTableData.nameId);
	}

	public int maxPp
	{
		get
		{
			int maxActorLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxActorLevel");
			ActorLevelTableData actorLevelTableData = TableDataManager.instance.FindActorLevelTableData(cachedActorTableData.grade, maxActorLevel);
			if (actorLevelTableData == null)
				return 0;
			return actorLevelTableData.requiredAccumulatedCount;
		}
	}

	public void Initialize(int characterCount, int ppCount, Dictionary<string, string> customData)
	{
		_count = characterCount;
		_pp = ppCount;

		_level = 1;
		if (customData.ContainsKey(KeyLevel))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyLevel], out intValue))
				_level = intValue;
		}

		_transcend = 0;
		if (customData.ContainsKey(KeyTranscend))
		{
			int intValue = 0;
			if (int.TryParse(customData[KeyTranscend], out intValue))
				_transcend = intValue;
		}

		// 검증
		bool invalid = false;
		if (level > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxActorLevel"))
		{
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidPowerLevel, false, level);
			invalid = true;
		}

		if (invalid == false)
		{
			if (transcend > transcendPoint)
			{
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidTranscendLevel, false, transcend);
				invalid = true;
			}
		}

		// 이후 Status 계산
		RefreshCachedStatus();



		/*
		int pow = 1;
		int pp = 0;
		int lb = 0;
		int tr = 0;
		int trp = 0;

		if (characterStatistics.ContainsKey("pow"))
			pow = characterStatistics["pow"];
		if (characterStatistics.ContainsKey("pp"))
			pp = characterStatistics["pp"];
		if (characterStatistics.ContainsKey("lb"))
			lb = characterStatistics["lb"];
		if (characterStatistics.ContainsKey("tr"))
			tr = characterStatistics["tr"];
		if (characterStatistics.ContainsKey("trp"))
			trp = characterStatistics["trp"];

		// 검증
		bool invalid = false;
		if (pow > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPowerLevel"))
		{
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidPowerLevel, false, pow);
			invalid = true;
		}

		PowerLevelTableData powerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(pow);
		if (invalid == false && pp < powerLevelTableData.requiredAccumulatedPowerPoint)
		{
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidPp, false, pp);
			invalid = true;
		}

		if (invalid == false)
		{
			// lbp보다 높거나 2단계 이상 차이나도 이상한거다. - lbp가 삭제되면서 필요없는 코드들 지운다.
			//if (lb > lbp)
			//	invalid = true;
			//if (lbp - lb >= 2)
			//	invalid = true;

			if (invalid == false)
			{
				// 원래는 자신의 limitBreak값과 같은게 기본이지만 10렙일땐 lb가 0일수도 1일수도 있다.
				if (lb != powerLevelTableData.requiredLimitBreak)
				{
					PowerLevelTableData nextPowerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(pow + 1);
					if (nextPowerLevelTableData == null)
						invalid = true;
					else
					{
						if (lb != nextPowerLevelTableData.requiredLimitBreak)
							invalid = true;
					}
				}
				//if (invalid == false)
				//{
				//	// 위 절차를 lbp에 대해서도 해준다.
				//	if (lbp != powerLevelTableData.requiredLimitBreak)
				//	{
				//		PowerLevelTableData nextPowerLevelTableData = TableDataManager.instance.FindPowerLevelTableData(pow + 1);
				//		if (nextPowerLevelTableData == null)
				//			invalid = true;
				//		else
				//		{
				//			if (lbp != nextPowerLevelTableData.requiredLimitBreak)
				//				invalid = true;
				//		}
				//	}
				//}
			}
			if (invalid)
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidLimitBreakLevel, false, lb);
		}

		if (invalid == false)
		{
			if (GetTranscendPoint(tr) > trp)
				invalid = true;
			if (trp > GetTranscendPoint(TranscendLevelMax))
				invalid = true;
			if (invalid)
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidTranscendLevel, false, tr);
		}

		// 추가 데이터. 잠재 염색 날개 등등.
		if (dataObject != null)
		{

		}

		_powerLevel = pow;
		_pp = pp;
		_limitBreakLevel = lb;
		_transcendLevel = tr;
		_transcendPoint = trp;

		*/

		/*

		// _powerLevel이 저장되고 나서야 파싱할 수 있다.
		_listStatPoint.Clear();
		if (maxStatPoint > 0)
		{
			if (_listStatPoint.Count == 0)
			{
				for (int i = 0; i < (int)CharacterInfoStatsCanvas.eStatsType.Amount; ++i)
					_listStatPoint.Add(0);
			}
			if (characterStatistics.ContainsKey("str"))
				_listStatPoint[0] = characterStatistics["str"];
			if (characterStatistics.ContainsKey("dex"))
				_listStatPoint[1] = characterStatistics["dex"];
			if (characterStatistics.ContainsKey("int"))
				_listStatPoint[2] = characterStatistics["int"];
			if (characterStatistics.ContainsKey("vit"))
				_listStatPoint[3] = characterStatistics["vit"];

			if (invalid == false)
			{
				int sumStatPoint = 0;
				for (int i = 0; i < _listStatPoint.Count; ++i)
					sumStatPoint += _listStatPoint[i];
				if (sumStatPoint > maxStatPoint)
				{
					PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidStatPoint, false, lb);
					invalid = true;
				}
			}
		}

		if (_transcendLevel >= 2)   /////ch 2
		{
			if (characterStatistics.ContainsKey("train"))
				_trainingValue = characterStatistics["train"];
			if (invalid == false && _trainingValue > CharacterInfoTrainingCanvas.TrainingMax)
			{
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidTraining, false, lb);
				invalid = true;
			}
		}

		_listWingGradeId.Clear();
		if (_transcendLevel >= 3)   /////ch 3
		{
			if (_listWingGradeId.Count == 0)
			{
				for (int i = 0; i < (int)CharacterInfoWingCanvas.eStatsType.Amount; ++i)
					_listWingGradeId.Add(0);
			}

			if (characterStatistics.ContainsKey("wgLk"))
				_wingLookId = characterStatistics["wgLk"];
			if (characterStatistics.ContainsKey("wgGr0"))
				_listWingGradeId[0] = characterStatistics["wgGr0"];
			if (characterStatistics.ContainsKey("wgGr1"))
				_listWingGradeId[1] = characterStatistics["wgGr1"];
			if (characterStatistics.ContainsKey("wgGr2"))
				_listWingGradeId[2] = characterStatistics["wgGr2"];
			if (characterStatistics.ContainsKey("wgGr3"))
				_listWingGradeId[3] = characterStatistics["wgGr3"];
			if (characterStatistics.ContainsKey("wgHd"))
				_wingHide = characterStatistics["wgHd"];
		}

		*/
	}

	public void SetPpCount(int ppCount)
	{
		if (pp < ppCount)
			_pp = ppCount;
	}

	public void SetCharacterCount(int characterCount)
	{
		if (count < characterCount)
			_count = characterCount;
	}

	void RefreshCachedStatus()
	{
		_mainStatusValue = 0;

		if (_level > 0)
		{
			ActorLevelTableData actorLevelTableData = TableDataManager.instance.FindActorLevelTableData(cachedActorTableData.grade, _level);
			if (actorLevelTableData != null)
				_mainStatusValue = actorLevelTableData.accumulatedAtk;
		}

		if (_transcend > 0)
		{
			ActorTranscendTableData actorTranscendTableData = TableDataManager.instance.FindActorTranscendTableData(cachedActorTableData.grade, _transcend);
			if (actorTranscendTableData != null)
				_mainStatusValue += actorTranscendTableData.accumulatedAtk;
		}
	}

	public void OnLevelUp(int targetLevel)
	{
		_level = targetLevel;
		RefreshCachedStatus();
		CharacterManager.instance.OnChangedStatus();
	}

	public void OnTranscendLevelUp(int targetLevel)
	{
		_transcend = targetLevel;
		RefreshCachedStatus();
		CharacterManager.instance.OnChangedStatus();
	}




	ActorTableData _cachedActorTableData = null;
	public ActorTableData cachedActorTableData
	{
		get
		{
			if (_cachedActorTableData == null)
				_cachedActorTableData = TableDataManager.instance.FindActorTableData(actorId);
			return _cachedActorTableData;
		}
	}
}