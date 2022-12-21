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

	public void OnRefreshDay()
	{
		fortuneWheelDailyCount = 0;
	}
}