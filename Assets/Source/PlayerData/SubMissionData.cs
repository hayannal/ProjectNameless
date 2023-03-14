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
		BossDefense = 2
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

	// 미션 결과창 후 로비로 되돌아올때 로딩을 위한 변수
	public ObscuredBool readyToReopenMissionListCanvas { get; set; }

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

		readyToReopenMissionListCanvas = false;
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

	public void OnRefreshDay()
	{
		fortuneWheelDailyCount = 0;
		rushDefenseDailyCount = 0;
		bossDefenseDailyCount = 0;

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