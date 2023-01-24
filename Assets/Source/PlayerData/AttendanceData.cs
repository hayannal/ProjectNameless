using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class AttendanceData : MonoBehaviour
{
	public static AttendanceData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("AttendanceData")).AddComponent<AttendanceData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static AttendanceData _instance = null;



	// 출석은 동시에 한개만 처리
	public ObscuredString attendanceId { get; private set; }

	// 출석엔 쿨이 없다. 항상 진행. 대신 빨리 완료하면 보너스를 받고 다음 보상으로 갱신된다.
	public DateTime attendanceExpireTime { get; private set; }
	
	// 출석 횟수
	public ObscuredInt rewardReceiveCount { get; set; }

	// oneTime
	public List<string> listAttendanceOneTime { get; set; }

	// 오늘 출석체크 했다면
	public ObscuredBool todayReceiveRecorded { get; set; }

	void Update()
	{
		UpdateStarted();
	}

	bool _waitPacket = false;
	float _retryStartRemainTime = 0.0f;
	void UpdateStarted()
	{
		if (_waitPacket)
			return;
		if (WaitingNetworkCanvas.IsShow())
			return;

		if (_retryStartRemainTime > 0.0f)
		{
			_retryStartRemainTime -= Time.deltaTime;
			if (_retryStartRemainTime <= 0.0f)
			{
				_retryStartRemainTime = 0.0f;
				StartAttendance();
			}
		}
	}

	public void StartAttendance()
	{
		string startAttendanceId = GetNextRandomAttendanceId();
		AttendanceTypeTableData attendanceTypeTableData = TableDataManager.instance.FindAttendanceTypeTableData(startAttendanceId);
		if (attendanceTypeTableData == null)
			return;

		_waitPacket = true;
		PlayFabApiManager.instance.RequestStartAttendance(startAttendanceId, attendanceTypeTableData.givenTime, attendanceTypeTableData.oneTime, false, () =>
		{
			attendanceId = attendanceTypeTableData.attendanceId;

			if (MainCanvas.instance != null)
				MainCanvas.instance.attendanceButtonObject.SetActive(true);

			_waitPacket = false;
		}, () =>
		{
			_waitPacket = false;
			_retryStartRemainTime = 1.0f;
		});
	}

	public void OnRecvAttendanceData(Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		attendanceId = "";
		if (userReadOnlyData.ContainsKey("attendanceId"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["attendanceId"].Value) == false)
				attendanceId = userReadOnlyData["attendanceId"].Value;
		}

		if (userReadOnlyData.ContainsKey("attendanceExpDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["attendanceExpDat"].Value) == false)
			{
				DateTime expireDateTime = new DateTime();
				if (DateTime.TryParse(userReadOnlyData["attendanceExpDat"].Value, out expireDateTime))
					attendanceExpireTime = expireDateTime.ToUniversalTime();
			}
		}

		rewardReceiveCount = 0;
		if (userReadOnlyData.ContainsKey("attendanceRwdCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["attendanceRwdCnt"].Value, out intValue))
				rewardReceiveCount = intValue;
		}

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		if (listAttendanceOneTime == null)
			listAttendanceOneTime = new List<string>();
		listAttendanceOneTime.Clear();
		if (userReadOnlyData.ContainsKey("attendanceOneTimeLst"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["attendanceOneTimeLst"].Value) == false)
				listAttendanceOneTime = serializer.DeserializeObject<List<string>>(userReadOnlyData["attendanceOneTimeLst"].Value);
		}

		if (userReadOnlyData.ContainsKey("attendanceRwdRcvDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["attendanceRwdRcvDat"].Value) == false)
				OnRecvRepeatLoginInfo(userReadOnlyData["attendanceRwdRcvDat"].Value);
		}

		// 로그인 할때마다 시작 상태가 아니라면 초기화를 진행해야한다.
		// 대신 로그인 하자마자 보내면 디비에서 제대로 처리 못할 수 도 있을테니
		// 1초 뒤에 보내도록 한다.
		bool downloadConfirmed = false;
		if (userReadOnlyData.ContainsKey("downloadConfirm"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["downloadConfirm"].Value) == false)
				downloadConfirmed = true;
		}
		if (downloadConfirmed)
		{
			if (attendanceId == "" || ServerTime.UtcNow > attendanceExpireTime)
				_retryStartRemainTime = 0.5f;
		}
	}

	public void OnRecvAttendanceExpireInfo(string attendanceId, bool oneTime, string lastAttendanceExpireTimeString)
	{
		this.attendanceId = attendanceId;
		this.rewardReceiveCount = 0;

		DateTime lastAttendanceExpireTime = new DateTime();
		if (DateTime.TryParse(lastAttendanceExpireTimeString, out lastAttendanceExpireTime))
		{
			DateTime universalTime = lastAttendanceExpireTime.ToUniversalTime();
			attendanceExpireTime = universalTime;
		}

		if (oneTime)
		{
			if (listAttendanceOneTime.Contains(attendanceId) == false)
				listAttendanceOneTime.Add(attendanceId);
		}
	}

	void OnRecvRepeatLoginInfo(DateTime lastRepeatLoginEventRecordTime)
	{
		if (ServerTime.UtcNow.Year == lastRepeatLoginEventRecordTime.Year && ServerTime.UtcNow.Month == lastRepeatLoginEventRecordTime.Month && ServerTime.UtcNow.Day == lastRepeatLoginEventRecordTime.Day)
			todayReceiveRecorded = true;
		else
			todayReceiveRecorded = false;
	}

	public void OnRecvRepeatLoginInfo(string lastRepeatLoginEventRecordTimeString)
	{
		DateTime lastRepeatLoginEventRecordTime = new DateTime();
		if (DateTime.TryParse(lastRepeatLoginEventRecordTimeString, out lastRepeatLoginEventRecordTime))
		{
			DateTime universalTime = lastRepeatLoginEventRecordTime.ToUniversalTime();
			OnRecvRepeatLoginInfo(universalTime);
		}
	}


	public void OnRefreshDay()
	{
		if (PlayerData.instance.downloadConfirmed == false)
			return;

		// 부여받은적이 한번도 없다면 뭔가 이상한거다. 이럴땐 그냥 패스.
		if (attendanceId == "")
			return;

		// 오늘의 기록을 초기화
		todayReceiveRecorded = false;

		// 진행중인게 있다면 패스
		if (ServerTime.UtcNow < attendanceExpireTime)
			return;

		StartAttendance();
	}




	class RandomAttendanceTypeInfo
	{
		public AttendanceTypeTableData attendanceTypeTableData;
		public float sumWeight;
	}
	List<RandomAttendanceTypeInfo> _listAttendanceTypeInfo = null;
	public string GetNextRandomAttendanceId()
	{
		if (attendanceId == "")
			return "fr";

		if (_listAttendanceTypeInfo == null)
			_listAttendanceTypeInfo = new List<RandomAttendanceTypeInfo>();
		_listAttendanceTypeInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.attendanceTypeTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.attendanceTypeTable.dataArray[i].eventWeight;
			if (weight <= 0.0f)
				continue;

			DateTime startDateTime = new DateTime(TableDataManager.instance.attendanceTypeTable.dataArray[i].startYear, TableDataManager.instance.attendanceTypeTable.dataArray[i].startMonth, TableDataManager.instance.attendanceTypeTable.dataArray[i].startDay);
			if (ServerTime.UtcNow < startDateTime)
				continue;
			DateTime endDateTime = new DateTime(TableDataManager.instance.attendanceTypeTable.dataArray[i].endYear, TableDataManager.instance.attendanceTypeTable.dataArray[i].endMonth, TableDataManager.instance.attendanceTypeTable.dataArray[i].endDay);
			if (ServerTime.UtcNow > endDateTime)
				continue;

			if (TableDataManager.instance.attendanceTypeTable.dataArray[i].oneTime)
			{
				if (listAttendanceOneTime.Contains(TableDataManager.instance.attendanceTypeTable.dataArray[i].attendanceId))
					continue;
			}

			sumWeight += weight;
			RandomAttendanceTypeInfo newInfo = new RandomAttendanceTypeInfo();
			newInfo.attendanceTypeTableData = TableDataManager.instance.attendanceTypeTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listAttendanceTypeInfo.Add(newInfo);
		}

		if (_listAttendanceTypeInfo.Count == 0)
			return "";

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listAttendanceTypeInfo[_listAttendanceTypeInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listAttendanceTypeInfo.Count; ++i)
		{
			if (random <= _listAttendanceTypeInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listAttendanceTypeInfo[index].attendanceTypeTableData.attendanceId;
	}

}