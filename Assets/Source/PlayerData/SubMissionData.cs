using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class SubMissionData : MonoBehaviour
{
	public static SubMissionData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("SubMissionData")).AddComponent<SubMissionData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static SubMissionData _instance = null;

	#region Fortune Wheel
	public ObscuredInt fortuneWheelDailyCount { get; set; }
	#endregion

	public enum eSubMissionType
	{
		RushDefense = 1,
		BossDefense = 2,
		GoldDefense = 3,
		RobotDefense = 4,
	}

	#region Rush Defense
	public ObscuredInt rushDefenseClearLevel { get; set; }
	public ObscuredInt rushDefenseSelectedLevel { get; set; }
	public ObscuredInt rushDefenseDailyCount { get; set; }
	#endregion

	#region Boss Defense
	public ObscuredInt bossDefenseClearLevel { get; set; }
	public ObscuredInt bossDefenseSelectedLevel { get; set; }
	public ObscuredInt bossDefenseDailyCount { get; set; }
	#endregion

	#region Gold Defense
	public ObscuredInt goldDefenseClearLevel { get; set; }
	public ObscuredInt goldDefenseSelectedLevel { get; set; }
	public ObscuredInt goldDefenseDailyCount { get; set; }
	#endregion

	#region Robot Defense
	public ObscuredInt robotDefenseKillCount { get; set; }
	public ObscuredInt robotDefenseClearLevel { get; set; }
	public ObscuredInt robotDefenseRepeatClearCount { get; set; }
	public ObscuredInt robotDefenseDailyCount { get; set; }
	// 드론 카운트 레벨
	public ObscuredInt robotDefenseDroneCountLevel { get; set; }
	// 드론 공격력 레벨
	public ObscuredInt robotDefenseDroneAttackLevel { get; set; }
	#endregion

	#region Boss Battle
	public ObscuredInt bossBattleId { get; set; }
	Dictionary<string, int> _dicBossBattleClearDifficulty = new Dictionary<string, int>();
	Dictionary<string, int> _dicBossBattleSelectedDifficulty = new Dictionary<string, int>();
	Dictionary<string, int> _dicBossBattleCount = new Dictionary<string, int>();
	// 클라 전용 변수. 보스가 갱신되었음을 다음번 창 열릴때 알린다.
	public ObscuredBool newBossRefreshed { get; set; }
	// 보스전 결과창 후 로비로 되돌아올때 로딩을 위한 변수
	public ObscuredBool readyToPreloadBossBattleEnterCanvas { get; set; }
	// 제한 횟수는 아니고 보너스 횟수
	public ObscuredInt bossBattleDailyCount { get; set; }
	// 마지막으로 클리어한 보스Id. 왕관체크에 사용한다.
	public ObscuredInt bossBattleClearId { get; set; }
	// 포인트샵 포인트
	public ObscuredInt bossBattlePoint { get; set; }
	// 
	public ObscuredInt bossBattleAttackLevel { get; set; }
	#endregion

	// 미션 결과창 후 로비로 되돌아올때 로딩을 위한 변수
	public ObscuredBool readyToReopenMissionListCanvas { get; set; }
	public ObscuredBool readyToReopenAdventureListCanvas { get; set; }

	// 토벌 추가하면서 공격력이 들어가게 되었다.
	public ObscuredInt cachedValueByBossBattle { get; set; }

	// 드론전투에서도 공격력이 들어가게 되었다.
	public ObscuredInt cachedValueByRobotDefense { get; set; }

	public void OnRecvSubMissionData(Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		#region Fortune Wheel
		// 룰렛 카운트
		fortuneWheelDailyCount = 0;
		if (userReadOnlyData.ContainsKey("frtWhlCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["frtWhlCnt"].Value, out intValue))
				fortuneWheelDailyCount = intValue;
		}

		if (userReadOnlyData.ContainsKey("lasFrtWhlDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasFrtWhlDat"].Value) == false)
				OnRecvDailyWheelInfo(userReadOnlyData["lasFrtWhlDat"].Value);
		}
		else
			fortuneWheelDailyCount = 0;
		#endregion

		#region Rush Defense
		rushDefenseDailyCount = 0;
		if (userReadOnlyData.ContainsKey("rushDefenseCount"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["rushDefenseCount"].Value, out intValue))
				rushDefenseDailyCount = intValue;
		}

		if (userReadOnlyData.ContainsKey("lasRusDefDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasRusDefDat"].Value) == false)
				OnRecvDailyRushDefenseInfo(userReadOnlyData["lasRusDefDat"].Value);
		}
		else
			rushDefenseDailyCount = 0;

		rushDefenseSelectedLevel = 0;
		if (userReadOnlyData.ContainsKey("rushDefenseSelectedLevel"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["rushDefenseSelectedLevel"].Value, out intValue))
				rushDefenseSelectedLevel = intValue;
		}

		rushDefenseClearLevel = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "rushDefenseClearLevel")
			{
				rushDefenseClearLevel = playerStatistics[i].Value;
				break;
			}
		}
		#endregion

		#region Boss Defense
		bossDefenseDailyCount = 0;
		if (userReadOnlyData.ContainsKey("bossDefenseCount"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["bossDefenseCount"].Value, out intValue))
				bossDefenseDailyCount = intValue;
		}

		if (userReadOnlyData.ContainsKey("lasBosDefDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasBosDefDat"].Value) == false)
				OnRecvDailyBossDefenseInfo(userReadOnlyData["lasBosDefDat"].Value);
		}
		else
			bossDefenseDailyCount = 0;

		bossDefenseSelectedLevel = 0;
		if (userReadOnlyData.ContainsKey("bossDefenseSelectedLevel"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["bossDefenseSelectedLevel"].Value, out intValue))
				bossDefenseSelectedLevel = intValue;
		}

		bossDefenseClearLevel = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "bossDefenseClearLevel")
			{
				bossDefenseClearLevel = playerStatistics[i].Value;
				break;
			}
		}
		#endregion

		#region Gold Defense
		goldDefenseDailyCount = 0;
		if (userReadOnlyData.ContainsKey("goldDefenseCount"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["goldDefenseCount"].Value, out intValue))
				goldDefenseDailyCount = intValue;
		}

		if (userReadOnlyData.ContainsKey("lasGolDefDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasGolDefDat"].Value) == false)
				OnRecvDailyGoldDefenseInfo(userReadOnlyData["lasGolDefDat"].Value);
		}
		else
			goldDefenseDailyCount = 0;

		goldDefenseSelectedLevel = 0;
		if (userReadOnlyData.ContainsKey("goldDefenseSelectedLevel"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["goldDefenseSelectedLevel"].Value, out intValue))
				goldDefenseSelectedLevel = intValue;
		}

		goldDefenseClearLevel = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "goldDefenseClearLevel")
			{
				goldDefenseClearLevel = playerStatistics[i].Value;
				break;
			}
		}
		#endregion

		#region Robot Defense
		robotDefenseDailyCount = 0;
		if (userReadOnlyData.ContainsKey("robotDefenseCount"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["robotDefenseCount"].Value, out intValue))
				robotDefenseDailyCount = intValue;
		}

		if (userReadOnlyData.ContainsKey("lasRobDefDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasRobDefDat"].Value) == false)
				OnRecvDailyRobotDefenseInfo(userReadOnlyData["lasRobDefDat"].Value);
		}
		else
			robotDefenseDailyCount = 0;

		robotDefenseKillCount = 0;
		if (userReadOnlyData.ContainsKey("robotDefenseKillCount"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["robotDefenseKillCount"].Value, out intValue))
				robotDefenseKillCount = intValue;
		}

		robotDefenseRepeatClearCount = 0;
		if (userReadOnlyData.ContainsKey("robotDefenseRepeatClearCount"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["robotDefenseRepeatClearCount"].Value, out intValue))
				robotDefenseRepeatClearCount = intValue;
		}

		robotDefenseClearLevel = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "robotDefenseClearLevel")
			{
				robotDefenseClearLevel = playerStatistics[i].Value;
				break;
			}
		}

		robotDefenseDroneCountLevel = 1;
		if (userReadOnlyData.ContainsKey("robotDefenseDroneCountLevel"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["robotDefenseDroneCountLevel"].Value, out intValue))
				robotDefenseDroneCountLevel = intValue;
		}

		robotDefenseDroneAttackLevel = 1;
		if (userReadOnlyData.ContainsKey("robotDefenseDroneAttackLevel"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["robotDefenseDroneAttackLevel"].Value, out intValue))
				robotDefenseDroneAttackLevel = intValue;
		}

		if (DroneUpgradeCanvas.GetRemainDronePoint() < 0)
		{
			// 음수가 나왔다는건 뭔가 이상하다는거다. 포인트 분배한 레벨을 강제로 1로 만들어버린다.
			robotDefenseDroneCountLevel = robotDefenseDroneAttackLevel = 1;
		}
		#endregion

		#region Boss Battle
		bossBattleId = 0;
		if (userReadOnlyData.ContainsKey("bossBattleId"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["bossBattleId"].Value, out intValue))
				bossBattleId = intValue;
		}

		// difficulty
		string bossBattleRecord = "";
		if (userReadOnlyData.ContainsKey("bossBattleClLv"))
			bossBattleRecord = userReadOnlyData["bossBattleClLv"].Value;
		OnRecvBossBattleClearData(bossBattleRecord);

		bossBattleRecord = "";
		if (userReadOnlyData.ContainsKey("bossBattleSeLv"))
			bossBattleRecord = userReadOnlyData["bossBattleSeLv"].Value;
		OnRecvBossBattleSelectData(bossBattleRecord);

		bossBattleRecord = "";
		if (userReadOnlyData.ContainsKey("bossBattleCnt"))
			bossBattleRecord = userReadOnlyData["bossBattleCnt"].Value;
		OnRecvBossBattleCountData(bossBattleRecord);

		newBossRefreshed = false;
		readyToPreloadBossBattleEnterCanvas = false;

		// count. bossBattleCnt와 이름이 비슷하긴 한데 하나는 json이고 하나는 dailyCount다.
		bossBattleDailyCount = 0;
		if (userReadOnlyData.ContainsKey("bossBattleCount"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["bossBattleCount"].Value, out intValue))
				bossBattleDailyCount = intValue;
		}

		if (userReadOnlyData.ContainsKey("lasBosBatDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasBosBatDat"].Value) == false)
				OnRecvDailyBossBattleInfo(userReadOnlyData["lasBosBatDat"].Value);
		}
		else
			bossBattleDailyCount = 0;

		bossBattleClearId = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "bossBattleClearLevel")
			{
				bossBattleClearId = playerStatistics[i].Value;
				break;
			}
		}

		bossBattlePoint = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "bossBattlePoint")
			{
				bossBattlePoint = playerStatistics[i].Value;
				break;
			}
		}

		// spellTotalLevel 처럼 Lv.1 로 시작한다.
		bossBattleAttackLevel = 1;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "bossBattleAttackLevel")
			{
				bossBattleAttackLevel = playerStatistics[i].Value;
				break;
			}
		}
		#endregion

		readyToReopenMissionListCanvas = false;

		// status
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		cachedValueByBossBattle = 0;
		cachedValueByRobotDefense = 0;

		// boss battle level status
		PointShopAtkTableData pointShopAtkTableData = TableDataManager.instance.FindPointShopAtkTableData(bossBattleAttackLevel);
		if (pointShopAtkTableData != null)
			cachedValueByBossBattle = pointShopAtkTableData.accumulatedAtk;

		// robot defense level status
		DroneAtkTableData droneAtkTableData = TableDataManager.instance.FindDroneAtkTableData(robotDefenseDroneAttackLevel);
		if (droneAtkTableData != null)
			cachedValueByRobotDefense = droneAtkTableData.accumulatedAtk;
	}

	public void OnChangedStatus()
	{
		RefreshCachedStatus();
		PlayerData.instance.OnChangedStatus();
	}

	public void OnLevelUpPointShopAttack(int targetLevel)
	{
		bossBattleAttackLevel = targetLevel;
		OnChangedStatus();
	}

	#region Fortune Wheel
	void OnRecvDailyWheelInfo(DateTime lastWheelTime)
	{
		if (ServerTime.UtcNow.Year == lastWheelTime.Year && ServerTime.UtcNow.Month == lastWheelTime.Month && ServerTime.UtcNow.Day == lastWheelTime.Day)
		{
			// 유효하면 읽어놨던 count값을 유지하고
			//fortuneWheelDailyCount += 1;
		}
		else
			fortuneWheelDailyCount = 0;
	}

	public void OnRecvDailyWheelInfo(string lastWheelTimeString)
	{
		DateTime lastWheelTime = new DateTime();
		if (DateTime.TryParse(lastWheelTimeString, out lastWheelTime))
		{
			DateTime universalTime = lastWheelTime.ToUniversalTime();
			OnRecvDailyWheelInfo(universalTime);
		}
	}
	#endregion

	#region Rush Defense
	void OnRecvDailyRushDefenseInfo(DateTime lastRushDefenseTime)
	{
		if (ServerTime.UtcNow.Year == lastRushDefenseTime.Year && ServerTime.UtcNow.Month == lastRushDefenseTime.Month && ServerTime.UtcNow.Day == lastRushDefenseTime.Day)
		{
			// 유효하면 읽어놨던 count값을 유지하고
		}
		else
			rushDefenseDailyCount = 0;
	}

	public void OnRecvDailyRushDefenseInfo(string lastRushDefenseTimeString)
	{
		DateTime lastRushDefenseTime = new DateTime();
		if (DateTime.TryParse(lastRushDefenseTimeString, out lastRushDefenseTime))
		{
			DateTime universalTime = lastRushDefenseTime.ToUniversalTime();
			OnRecvDailyRushDefenseInfo(universalTime);
		}
	}
	#endregion

	#region Boss Defense
	void OnRecvDailyBossDefenseInfo(DateTime lastBossDefenseTime)
	{
		if (ServerTime.UtcNow.Year == lastBossDefenseTime.Year && ServerTime.UtcNow.Month == lastBossDefenseTime.Month && ServerTime.UtcNow.Day == lastBossDefenseTime.Day)
		{
			// 유효하면 읽어놨던 count값을 유지하고
		}
		else
			bossDefenseDailyCount = 0;
	}

	public void OnRecvDailyBossDefenseInfo(string lastBossDefenseTimeString)
	{
		DateTime lastBossDefenseTime = new DateTime();
		if (DateTime.TryParse(lastBossDefenseTimeString, out lastBossDefenseTime))
		{
			DateTime universalTime = lastBossDefenseTime.ToUniversalTime();
			OnRecvDailyBossDefenseInfo(universalTime);
		}
	}
	#endregion

	#region Gold Defense
	void OnRecvDailyGoldDefenseInfo(DateTime lastGoldDefenseTime)
	{
		if (ServerTime.UtcNow.Year == lastGoldDefenseTime.Year && ServerTime.UtcNow.Month == lastGoldDefenseTime.Month && ServerTime.UtcNow.Day == lastGoldDefenseTime.Day)
		{
			// 유효하면 읽어놨던 count값을 유지하고
		}
		else
			goldDefenseDailyCount = 0;
	}

	public void OnRecvDailyGoldDefenseInfo(string lastGoldDefenseTimeString)
	{
		DateTime lastGoldDefenseTime = new DateTime();
		if (DateTime.TryParse(lastGoldDefenseTimeString, out lastGoldDefenseTime))
		{
			DateTime universalTime = lastGoldDefenseTime.ToUniversalTime();
			OnRecvDailyGoldDefenseInfo(universalTime);
		}
	}
	#endregion

	#region Robot Defense
	void OnRecvDailyRobotDefenseInfo(DateTime lastRobotDefenseTime)
	{
		if (ServerTime.UtcNow.Year == lastRobotDefenseTime.Year && ServerTime.UtcNow.Month == lastRobotDefenseTime.Month && ServerTime.UtcNow.Day == lastRobotDefenseTime.Day)
		{
			// 유효하면 읽어놨던 count값을 유지하고
		}
		else
			robotDefenseDailyCount = 0;
	}

	public void OnRecvDailyRobotDefenseInfo(string lastRobotDefenseTimeString)
	{
		DateTime lastRobotDefenseTime = new DateTime();
		if (DateTime.TryParse(lastRobotDefenseTimeString, out lastRobotDefenseTime))
		{
			DateTime universalTime = lastRobotDefenseTime.ToUniversalTime();
			OnRecvDailyRobotDefenseInfo(universalTime);
		}
	}

	public void StepUpRobotDefense(int useKillCount)
	{
		int currentStep = robotDefenseClearLevel + 1;
		bool repeatMode = (currentStep > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxRobotDefense"));

		RobotDefenseStepTableData robotDefenseStepTableData = null;
		if (repeatMode)
			robotDefenseStepTableData = TableDataManager.instance.FindRobotDefenseStepTableData(RobotDefenseEnterCanvas.RobotDefenseRepeatStep);
		else
			robotDefenseStepTableData = TableDataManager.instance.FindRobotDefenseStepTableData(currentStep);
		if (robotDefenseStepTableData == null)
			return;
		if (robotDefenseStepTableData.monCount != useKillCount)
			return;

		robotDefenseKillCount -= useKillCount;

		if (repeatMode)
		{
			robotDefenseRepeatClearCount += 1;
		}
		else
		{
			robotDefenseClearLevel += 1;
		}
	}

	public void OnLevelUpRobotDefenseAtkLevel(int targetLevel)
	{
		robotDefenseDroneAttackLevel = targetLevel;
		OnChangedStatus();
	}
	#endregion

	#region Boss Battle
	void OnRecvBossBattleClearData(string json)
	{
		_dicBossBattleClearDifficulty.Clear();
		if (string.IsNullOrEmpty(json))
			return;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_dicBossBattleClearDifficulty = serializer.DeserializeObject<Dictionary<string, int>>(json);
	}

	void OnRecvBossBattleSelectData(string json)
	{
		_dicBossBattleSelectedDifficulty.Clear();
		if (string.IsNullOrEmpty(json))
			return;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_dicBossBattleSelectedDifficulty = serializer.DeserializeObject<Dictionary<string, int>>(json);
	}

	void OnRecvBossBattleCountData(string json)
	{
		_dicBossBattleCount.Clear();
		if (string.IsNullOrEmpty(json))
			return;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_dicBossBattleCount = serializer.DeserializeObject<Dictionary<string, int>>(json);
	}

	public int GetBossBattleClearDifficulty(string id)
	{
		if (_dicBossBattleClearDifficulty.ContainsKey(id))
			return _dicBossBattleClearDifficulty[id];
		return 0;
	}

	public int GetBossBattleSelectedDifficulty(string id)
	{
		if (_dicBossBattleSelectedDifficulty.ContainsKey(id))
			return _dicBossBattleSelectedDifficulty[id];

		// 선택 데이터가 없으면 분명 처음 열린걸꺼다. 이때는 0을 리턴해준다.
		return 0;
	}

	public int GetBossBattleCount(string id)
	{
		if (_dicBossBattleCount.ContainsKey(id))
			return _dicBossBattleCount[id];
		return 0;
	}

	public void ClearBossBattleDifficulty(int difficulty)
	{
		int id = bossBattleId;
		if (id == 0) id = 1;
		string key = id.ToString();
		if (_dicBossBattleClearDifficulty.ContainsKey(key))
			_dicBossBattleClearDifficulty[key] = difficulty;
		else
			_dicBossBattleClearDifficulty.Add(key, difficulty);
	}

	public void SelectBossBattleDifficulty(int difficulty)
	{
		int id = bossBattleId;
		if (id == 0) id = 1;
		string key = id.ToString();
		if (_dicBossBattleSelectedDifficulty.ContainsKey(key))
			_dicBossBattleSelectedDifficulty[key] = difficulty;
		else
			_dicBossBattleSelectedDifficulty.Add(key, difficulty);
	}

	public void AddBossBattleCount()
	{
		int id = bossBattleId;
		if (id == 0) id = 1;
		string key = id.ToString();
		if (_dicBossBattleCount.ContainsKey(key))
		{
			int value = _dicBossBattleCount[key];
			if (value < GetMaxXpExp())
				_dicBossBattleCount[key]++;
		}
		else
			_dicBossBattleCount.Add(key, 1);
	}

	int GetMaxXpExp()
	{
		for (int i = 1; i < TableDataManager.instance.bossExpTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.bossExpTable.dataArray[i].xpLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattleXpLevel"))
				return TableDataManager.instance.bossExpTable.dataArray[i].requiredAccumulatedExp;
		}
		return 0;
	}


	public int GetNextKingBossId()
	{
		// 확인차 체크하는건데 현재 상대하는 보스가 최종 클리어라고 적혀있는거보다 작거나 같진 않을거다.
		// 이럴땐 그냥 랜덤을 리턴
		int id = bossBattleId;
		if (id == 0) id = 1;
		if (id <= bossBattleClearId)
		{
			// something wrong
			return GetNextRandomBossId();
		}

		// 등장하는 마지막 왕관 보스를 처리한거면 더이상 왕관이 나올 수 없다.
		if (id >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattle"))
		{
			// 이럴때도 그냥 랜덤 리턴
			return GetNextRandomBossId();
		}
		return id + 1;
	}

	List<int> _listNextRandomBossId = new List<int>();
	public int GetNextRandomBossId()
	{
		_listNextRandomBossId.Clear();

		// 현재 설정된 보스를 제외한 나머지 중에서 
		int prevBossBattleId = bossBattleId;
		if (prevBossBattleId == 0)
			prevBossBattleId = 1;
		for (int i = 0; i < TableDataManager.instance.bossBattleTable.dataArray.Length; ++i)
		{
			// 왕관 몬스터는 랜덤 리스트에는 추가하지 않고 클리어한 범위 안에서 추가한다.
			if (TableDataManager.instance.bossBattleTable.dataArray[i].num <= bossBattleClearId)
			{
				if (prevBossBattleId != TableDataManager.instance.bossBattleTable.dataArray[i].num)
					_listNextRandomBossId.Add(TableDataManager.instance.bossBattleTable.dataArray[i].num);
			}
			else
				break;
		}
		if (_listNextRandomBossId.Count == 0)
			return prevBossBattleId;
		return _listNextRandomBossId[UnityEngine.Random.Range(0, _listNextRandomBossId.Count)];
	}

	public void OnClearBossBattle(int selectedDifficulty, int clearDifficulty, int nextBossId)
	{
		// 현재 선택한 레벨이 최고레벨일때랑 아닐때랑 나뉜다.
		if (selectedDifficulty <= clearDifficulty)
		{
			// 최고 클리어 난이도보다 낮거나 같은 난이도를 클리어. 이미 클리어한 곳을 클리어하는거니 아무것도 하지 않는다.
		}
		else
		{
			// record
			bool firstClear = false;
			if (clearDifficulty == 0)
				firstClear = true;
			else if (selectedDifficulty == (clearDifficulty + 1))
				firstClear = true;

			if (firstClear)
			{
				ClearBossBattleDifficulty(selectedDifficulty);

				int currentBossId = bossBattleId;
				if (currentBossId == 0)
					currentBossId = 1;
				if (GetBossBattleClearDifficulty(currentBossId.ToString()) == selectedDifficulty)
				{
					// 난이도의 최대 범위를 넘지않는 한도 내에서
					// 그러나 최대 범위 넘지 않더라도 7챕터를 깨지 않으면 난이도 8 이상으로는 올릴 수 없도록 해야한다.
					int nextDifficulty = selectedDifficulty + 1;
					if (nextDifficulty > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattleDifficulty"))
						nextDifficulty = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattleDifficulty");

					if (selectedDifficulty != nextDifficulty)
					{
						selectedDifficulty = nextDifficulty;
						SelectBossBattleDifficulty(selectedDifficulty);
					}

					if (bossBattleClearId < currentBossId)
						bossBattleClearId = currentBossId;
				}
			}
		}

		// 난이도 처리가 다 끝난 후 다음 보스 아이디를 갱신해야한다.
		bossBattleId = nextBossId;
		newBossRefreshed = true;
	}
	#endregion

	#region BossBattle
	void OnRecvDailyBossBattleInfo(DateTime lastBossBattleTime)
	{
		if (ServerTime.UtcNow.Year == lastBossBattleTime.Year && ServerTime.UtcNow.Month == lastBossBattleTime.Month && ServerTime.UtcNow.Day == lastBossBattleTime.Day)
		{
			// 유효하면 읽어놨던 count값을 유지하고
		}
		else
			bossBattleDailyCount = 0;
	}

	public void OnRecvDailyBossBattleInfo(string lastBossBattleTimeString)
	{
		DateTime lastBossBattleTime = new DateTime();
		if (DateTime.TryParse(lastBossBattleTimeString, out lastBossBattleTime))
		{
			DateTime universalTime = lastBossBattleTime.ToUniversalTime();
			OnRecvDailyBossBattleInfo(universalTime);
		}
	}
	#endregion

	public void OnRefreshDay()
	{
		fortuneWheelDailyCount = 0;
		rushDefenseDailyCount = 0;
		bossDefenseDailyCount = 0;
		goldDefenseDailyCount = 0;
		robotDefenseDailyCount = 0;
		bossBattleDailyCount = 0;

		if (MainCanvas.instance != null)
			MainCanvas.instance.RefreshMissionAlarmObject();

		// 여기는 0회때랑 1회때랑 처리 로직이 다르기때문에 창을 갱신해주는게 맞다.
		if (MissionListCanvas.instance != null && MissionListCanvas.instance.gameObject.activeSelf)
		{
			MissionListCanvas.instance.gameObject.SetActive(false);
			MissionListCanvas.instance.gameObject.SetActive(true);
		}
		if (FortuneWheelCanvas.instance != null && FortuneWheelCanvas.instance.gameObject.activeSelf)
		{
			FortuneWheelCanvas.instance.RefreshInfo();
			FortuneWheelCanvas.instance.fortuneWheelRootObject.SetActive(false);
			FortuneWheelCanvas.instance.fortuneWheelRootObject.SetActive(true);
		}
	}
}