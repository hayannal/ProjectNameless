using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class AnalysisData : MonoBehaviour
{
	public static AnalysisData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("AnalysisData")).AddComponent<AnalysisData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static AnalysisData _instance = null;



	// 초단위로 저장되는 경험치다. 이걸 받아와서 레벨을 구한다.
	public ObscuredInt analysisExp { get; private set; }
	public ObscuredInt analysisLevel { get; private set; }

	// stat
	public ObscuredInt cachedValue { get; set; }

	// 최초로 한번 분석을 시작하기 전까진 false로 되어있다.
	public ObscuredBool analysisStarted { get; set; }
	public DateTime analysisStartedTime { get; private set; }
	public DateTime analysisCompleteTime { get; private set; }


	void Update()
	{
		UpdateStarted();
		UpdateRemainTime();
	}

	bool _waitFirstStartPacket = false;
	float _retryFirstStartRemainTime = 0.0f;
	void UpdateStarted()
	{
		if (analysisStarted)
			return;

		if (_waitFirstStartPacket)
			return;

		if (_retryFirstStartRemainTime > 0.0f)
		{
			_retryFirstStartRemainTime -= Time.deltaTime;
			if (_retryFirstStartRemainTime <= 0.0f)
			{
				_retryFirstStartRemainTime = 0.0f;
				StartAnalysis();
			}
		}
	}

	public void StartAnalysis()
	{
		// 처음이라면 분석시작을 서버에 알려서 기록해야한다. 딱 한번만 날리는 패킷
		_waitFirstStartPacket = true;
		PlayFabApiManager.instance.RequestStartAnalysis(() =>
		{
			_waitFirstStartPacket = false;
		}, () =>
		{
			_waitFirstStartPacket = false;
			_retryFirstStartRemainTime = 3.0f;
		});
	}

	public void OnRecvAnalysisData(Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		analysisExp = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "analysisExp")
			{
				analysisExp = playerStatistics[i].Value;
				break;
			}
		}

		// 경험치를 받는 곳에서 미리 레벨을 계산해둔다. 연구와 달리 1부터 시작하는 구조다.
		analysisLevel = 0;
		RefreshAnalysisLevel();

		analysisStarted = false;
		if (userReadOnlyData.ContainsKey("anlyStrDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["anlyStrDat"].Value) == false)
				OnRecvAnalysisStartInfo(userReadOnlyData["anlyStrDat"].Value);
		}

		// 로그인 할때마다 analysisStarted 시작 상태가 아니라면 초기화를 진행해야한다.
		// 대신 로그인 하자마자 보내면 디비에서 제대로 처리 못할 수 도 있을테니
		// 1초 뒤에 보내도록 한다.
		if (analysisLevel == 1 && analysisStarted == false)
			_retryFirstStartRemainTime = 1.0f;
	}

	public void OnRecvAnalysisStartInfo(string lastAnalysisStartTimeString)
	{
		DateTime lastAnalysisStartTime = new DateTime();
		if (DateTime.TryParse(lastAnalysisStartTimeString, out lastAnalysisStartTime))
		{
			DateTime universalTime = lastAnalysisStartTime.ToUniversalTime();
			analysisStarted = true;
			analysisStartedTime = universalTime;

			AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(analysisLevel);
			if (analysisTableData != null)
			{
				analysisCompleteTime = analysisStartedTime + TimeSpan.FromSeconds(analysisTableData.maxTime);
				if (ServerTime.UtcNow < analysisCompleteTime)
					_needUpdate = true;
			}
		}
	}

	public void AddExp(int addExp)
	{
		if (addExp == 0)
			return;

		// 일반적인 분석 후 경험치 쌓이는 곳에서 호출된다.
		analysisExp += addExp;
		RefreshAnalysisLevel();
	}

	public void OnLevelUp(int targetLevel)
	{
		// 강제 레벨업 하는 곳에서 호출된다.
		AnalysisTableData targetAnalysisTableData = TableDataManager.instance.FindAnalysisTableData(targetLevel);
		if (targetAnalysisTableData == null)
			return;

		analysisExp = targetAnalysisTableData.requiredAccumulatedTime;
		RefreshAnalysisLevel();
	}

	void RefreshAnalysisLevel()
	{
		int maxLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel");
		for (int i = 0; i < TableDataManager.instance.analysisTable.dataArray.Length; ++i)
		{
			if (analysisExp < TableDataManager.instance.analysisTable.dataArray[i].requiredAccumulatedTime)
			{
				analysisLevel = TableDataManager.instance.analysisTable.dataArray[i].level - 1;
				break;
			}
			if (TableDataManager.instance.analysisTable.dataArray[i].level >= maxLevel)
			{
				analysisLevel = maxLevel;
				break;
			}
		}

		// status
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		cachedValue = 0;

		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(analysisLevel);
		if (analysisTableData == null)
			return;
		cachedValue = analysisTableData.accumulatedAtk;
	}

	#region Notification
	const int AnalysisNotificationId = 10002;
	public void ReserveAnalysisNotification()
	{
		if (analysisStarted == false)
			return;
		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(analysisLevel);
		if (analysisTableData == null)
			return;
		if (ServerTime.UtcNow > analysisCompleteTime)
			return;

		TimeSpan remainTime = analysisCompleteTime - ServerTime.UtcNow;
		DateTime deliveryTime = DateTime.Now.ToLocalTime() + TimeSpan.FromSeconds(remainTime.TotalSeconds);
		MobileNotificationWrapper.instance.SendNotification(AnalysisNotificationId, UIString.instance.GetString("SystemUI_AnalysisFullTitle"), UIString.instance.GetString("SystemUI_AnalysisFullBody"),
			deliveryTime, null, true, "my_custom_icon_id", "my_custom_large_icon_id");
	}

	public void CancelAnalysisNotification()
	{
		MobileNotificationWrapper.instance.CancelPendingNotificationItem(AnalysisNotificationId);
	}
	#endregion

	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		// 로그인 후 분석창 열지 않은 상태에서 완료시 로비에 알람표시를 하려면 여기서 시간 다됐는지 체크해야만 한다.
		if (analysisStarted == false)
			return;
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < analysisCompleteTime)
		{
		}
		else
		{
			_needUpdate = false;

			// 로비 Alarm 확인
			if (MainCanvas.instance != null)
				MainCanvas.instance.RefreshAnalysisAlarmObject();
		}
	}


	
	ObscuredInt _cachedSecond = 0;
	ObscuredInt _cachedResultGold = 0;
	public int cachedExpSecond { get { return _cachedSecond; } }
	public int cachedResultGold { get { return _cachedResultGold; } }
	List<DropProcessor> _listCachedDropProcessor = new List<DropProcessor>();
	public void PrepareAnalysis()
	{
		// UI 막혔을텐데 어떻게 호출한거지
		if (analysisStarted == false)
			return;
		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(analysisLevel);
		if (analysisTableData == null)
			return;

		TimeSpan diffTime = ServerTime.UtcNow - analysisStartedTime;
		int totalSeconds = Mathf.Min((int)diffTime.TotalSeconds, analysisTableData.maxTime);
		_cachedSecond = totalSeconds;
		Debug.LogFormat("Analysis Time = {0}", totalSeconds);

		// 쌓아둔 초로 하나씩 체크해봐야한다.
		// 제일 먼저 goldPerTime
		// 시간당 골드로 적혀있으니 초로 변환해서 계산하면 된다.
		float goldPerSec = analysisTableData.goldPerTime / 60.0f / 60.0f;
		_cachedResultGold = (int)(goldPerSec * totalSeconds);
		if (_cachedResultGold < 1)
			_cachedResultGold = 1;

		// 이렇게 계산된 second를 그냥 보내면 안되고 최고레벨 검사는 해놓고 보내야한다.
		AnalysisTableData maxAnalysisTableData = TableDataManager.instance.FindAnalysisTableData(BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"));
		int maxAnalysisExp = maxAnalysisTableData.requiredAccumulatedTime;
		if (analysisExp + _cachedSecond > maxAnalysisExp)
			_cachedSecond = maxAnalysisExp - analysisExp;

		// 패킷 전달한 준비는 끝.
	}
	
	public void ClearCachedInfo()
	{
		_cachedSecond = 0;
		_cachedResultGold = 0;
	}
}