using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class MissionData : MonoBehaviour
{
	public static MissionData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("MissionData")).AddComponent<MissionData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static MissionData _instance = null;

	// 현재 진행중인 퀘스트 정보
	// 
	public Dictionary<int, int> _dicSevenDaysProceedingInfo = new Dictionary<int, int>();

	// 세븐데이즈는 동시에 1개의 미션만 처리
	public ObscuredInt sevenDaysId { get; private set; }

	// 시간은 이벤트와 마찬가지로 만료시간 쿨타임 둘다 있다.
	public DateTime sevenDaysExpireTime { get; private set; }
	public DateTime sevenDaysCoolExpireTime { get; private set; }

	// 현재 Sum포인트
	public ObscuredInt sevenDaysSumPoint { get; set; }

	// 리워드 리스트
	List<string> _listSevenDaysReward;
	// Sum리워드 리스트
	List<int> _listSevenDaysSumReward;

	#region SevenDays Total Cash Product
	List<int> _listSevenCashSlotPurchased;
	#endregion


	List<GuideQuestData.eQuestClearType> _listAvailableQuestType = new List<GuideQuestData.eQuestClearType>();
	void Awake()
	{
		// 이거 늘릴때는 서버의 StartSevenDays 함수 안의
		// init proceeding
		// 부분도 같이 수정해줘야한다.
		_listAvailableQuestType.Add(GuideQuestData.eQuestClearType.KillBossMonster);
		_listAvailableQuestType.Add(GuideQuestData.eQuestClearType.Analysis);
	}

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

		if (_retryStartRemainTime > 0.0f)
		{
			_retryStartRemainTime -= Time.deltaTime;
			if (_retryStartRemainTime <= 0.0f)
			{
				_retryStartRemainTime = 0.0f;
				StartSevenDays();
			}
		}
	}

	public void StartSevenDays()
	{
		if (PlayerData.instance.downloadConfirmed == false)
			return;

		int newIdIndex = -1;
		if (sevenDaysId == 0)
			newIdIndex = 0;
		else
		{
			// 최초가 아니라면 테이블을 검사해서 찾아본다.
			for (int i = 0; i < TableDataManager.instance.sevenDaysTypeTable.dataArray.Length; ++i)
			{
				if (TableDataManager.instance.sevenDaysTypeTable.dataArray[i].startYear != 0 && TableDataManager.instance.sevenDaysTypeTable.dataArray[i].endYear != 0)
				{
					DateTime startDateTime = new DateTime(TableDataManager.instance.sevenDaysTypeTable.dataArray[i].startYear, TableDataManager.instance.sevenDaysTypeTable.dataArray[i].startMonth, TableDataManager.instance.sevenDaysTypeTable.dataArray[i].startDay);
					if (ServerTime.UtcNow < startDateTime)
						continue;
					DateTime endDateTime = new DateTime(TableDataManager.instance.sevenDaysTypeTable.dataArray[i].endYear, TableDataManager.instance.sevenDaysTypeTable.dataArray[i].endMonth, TableDataManager.instance.sevenDaysTypeTable.dataArray[i].endDay);
					if (ServerTime.UtcNow > endDateTime)
						continue;

					newIdIndex = i;
					break;
				}
			}
		}
		if (newIdIndex == -1)
			return;
		SevenDaysTypeTableData sevenDaysTypeTableData = TableDataManager.instance.sevenDaysTypeTable.dataArray[newIdIndex];
		if (sevenDaysTypeTableData == null)
			return;

		_waitPacket = true;
		PlayFabApiManager.instance.RequestStartSevenDays(sevenDaysTypeTableData.groupId, sevenDaysTypeTableData.givenTime, sevenDaysTypeTableData.coolTime, () =>
		{
			sevenDaysId = sevenDaysTypeTableData.groupId;

			if (MainCanvas.instance != null)
				MainCanvas.instance.sevenDaysButtonObject.SetActive(true);

			_waitPacket = false;
		}, () =>
		{
			_waitPacket = false;
			_retryStartRemainTime = 3.0f;
		});
	}

	public void OnRecvMissionData(Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		sevenDaysId = 0;
		if (userReadOnlyData.ContainsKey("sevenDaysId"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["sevenDaysId"].Value, out intValue))
				sevenDaysId = intValue;
		}

		if (userReadOnlyData.ContainsKey("sevenDaysExpDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["sevenDaysExpDat"].Value) == false)
				OnRecvSevenDaysStartInfo(userReadOnlyData["sevenDaysExpDat"].Value);
		}

		if (userReadOnlyData.ContainsKey("sevenDaysCoolExpDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["sevenDaysCoolExpDat"].Value) == false)
				OnRecvSevenDaysCoolTimeInfo(userReadOnlyData["sevenDaysCoolExpDat"].Value);
		}

		sevenDaysSumPoint = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "sevenDaysSumPoint")
			{
				sevenDaysSumPoint = playerStatistics[i].Value;
				break;
			}
		}

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_dicSevenDaysProceedingInfo.Clear();
		if (sevenDaysId != 0 && ServerTime.UtcNow < sevenDaysExpireTime)
		{
			// 조건들을 불러야하는데
			// 하필 조건Enum이 0, 1, 2 순서대로 되어있지 않기 때문에
			// 이렇게 불러야할 조건들을 리스트로 해서 처리하기로 한다.
			for (int i = 0; i < _listAvailableQuestType.Count; ++i)
			{
				int intType = (int)_listAvailableQuestType[i];
				string key = string.Format("sevenDaysPrcdCnt_{0}", intType);
				if (userReadOnlyData.ContainsKey(key))
				{
					int intValue = 0;
					if (int.TryParse(userReadOnlyData[key].Value, out intValue))
						_dicSevenDaysProceedingInfo.Add(intType, intValue);
				}
			}

			// 획득 상태도 불러야한다.
			_listSevenDaysReward = null;
			if (userReadOnlyData.ContainsKey("sevenDaysRewardLst"))
			{
				string sevenDaysRewardLstString = userReadOnlyData["sevenDaysRewardLst"].Value;
				if (string.IsNullOrEmpty(sevenDaysRewardLstString) == false)
					_listSevenDaysReward = serializer.DeserializeObject<List<string>>(sevenDaysRewardLstString);
			}

			_listSevenDaysSumReward = null;
			if (userReadOnlyData.ContainsKey("sevenDaysSumRewardLst"))
			{
				string sevenDaysSumRewardLstString = userReadOnlyData["sevenDaysSumRewardLst"].Value;
				if (string.IsNullOrEmpty(sevenDaysSumRewardLstString) == false)
					_listSevenDaysSumReward = serializer.DeserializeObject<List<int>>(sevenDaysSumRewardLstString);
			}

			#region SevenDays Total Cash Product
			_listSevenCashSlotPurchased = null;
			if (userReadOnlyData.ContainsKey("sevenDaysCashSlotLst"))
			{
				string sevenDaysCashSlotLstString = userReadOnlyData["sevenDaysCashSlotLst"].Value;
				if (string.IsNullOrEmpty(sevenDaysCashSlotLstString) == false)
					_listSevenCashSlotPurchased = serializer.DeserializeObject<List<int>>(sevenDaysCashSlotLstString);
			}
			#endregion
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
			if (sevenDaysId == 0 || (ServerTime.UtcNow > sevenDaysExpireTime && ServerTime.UtcNow > sevenDaysCoolExpireTime))
				_retryStartRemainTime = 1.0f;
		}
	}

	public void OnRecvSevenDaysStartInfo(string lastSevenDaysExpireTimeString)
	{
		DateTime lastSevenDaysExpireTime = new DateTime();
		if (DateTime.TryParse(lastSevenDaysExpireTimeString, out lastSevenDaysExpireTime))
		{
			DateTime universalTime = lastSevenDaysExpireTime.ToUniversalTime();
			sevenDaysExpireTime = universalTime;
		}

		// 시작할때 카운트는 항상 초기화해두고 시작해야한다.
		_dicSevenDaysProceedingInfo.Clear();
		if (_listSevenDaysReward != null) _listSevenDaysReward.Clear();
		if (_listSevenDaysSumReward != null) _listSevenDaysSumReward.Clear();
		if (_listSevenCashSlotPurchased != null) _listSevenCashSlotPurchased.Clear();
		sevenDaysSumPoint = 0;
	}

	public void OnRecvSevenDaysCoolTimeInfo(string lastSevenDaysCoolExpireTimeString)
	{
		DateTime lastSevenDaysCoolExpireTime = new DateTime();
		if (DateTime.TryParse(lastSevenDaysCoolExpireTimeString, out lastSevenDaysCoolExpireTime))
		{
			DateTime universalTime = lastSevenDaysCoolExpireTime.ToUniversalTime();
			sevenDaysCoolExpireTime = universalTime;
		}
	}

	public bool IsOpenDay(int day)
	{
		// day 는 1, 2, 3, 4, 5, 6, 7 이렇게 들어온다.
		// 이걸 구하려면 이벤트 시작 일시를 구해와야한다.
		SevenDaysTypeTableData sevenDaysTypeTableData = TableDataManager.instance.FindSevenDaysTypeTableData(sevenDaysId);
		if (sevenDaysTypeTableData == null)
			return false;
		DateTime startTime = sevenDaysExpireTime - TimeSpan.FromSeconds(sevenDaysTypeTableData.givenTime);
		TimeSpan delta = ServerTime.UtcNow - startTime;
		return (day <= delta.Days + 1);
	}

	public void OnRefreshDay()
	{
		if (SevenDaysCanvas.instance != null && SevenDaysCanvas.instance.gameObject.activeSelf)
			SevenDaysCanvas.instance.RefreshLockObjectList();

		// 부여받은적이 한번도 없다면 뭔가 이상한거다. 이럴땐 그냥 패스.
		if (sevenDaysId == 0)
			return;

		// 진행중인게 있다면 패스
		if (ServerTime.UtcNow < sevenDaysExpireTime)
			return;

		// 쿨타임 도중이라면 패스
		if (ServerTime.UtcNow < sevenDaysCoolExpireTime)
			return;

		StartSevenDays();
	}

	public void OnQuestEvent(GuideQuestData.eQuestClearType questClearType, int addValue = 1)
	{
		// 진행중이지 않다면 패스
		if (sevenDaysId == 0 || ServerTime.UtcNow > sevenDaysExpireTime)
			return;

		// 타입 검사
		if (_listAvailableQuestType.Contains(questClearType) == false)
			return;

		PlayFabApiManager.instance.RequestSevenDaysProceedingCount((int)questClearType, addValue, GetProceedingCount((int)questClearType) + addValue, () =>
		{
			OnQuestProceedingCount();
		});
	}

	public void SetProceedingCount(int type, int addCount, int expectCount)
	{
		int currentCount = GetProceedingCount(type);
		if (currentCount + addCount == expectCount)
		{
			if (_dicSevenDaysProceedingInfo.ContainsKey(type))
				_dicSevenDaysProceedingInfo[type] = expectCount;
			else
				_dicSevenDaysProceedingInfo.Add(type, expectCount);
		}
	}

	public int GetProceedingCount(int type)
	{
		if (_dicSevenDaysProceedingInfo.ContainsKey(type))
			return _dicSevenDaysProceedingInfo[type];
		return 0;
	}

	void OnQuestProceedingCount()
	{
		// 테이블 검사해서 알람표시
		//GuideQuestInfo.instance.RefreshCountInfo();
		if (MainCanvas.instance != null && MainCanvas.instance.gameObject.activeSelf && MainCanvas.instance.IsHideState() == false)
			MainCanvas.instance.RefreshSevenDaysAlarmObject();
	}


	public bool IsGetSevenDaysReward(int day, int num)
	{
		if (_listSevenDaysReward == null)
			return false;

		string key = string.Format("{0}_{1}", day, num);
		return _listSevenDaysReward.Contains(key);
	}

	public List<string> OnRecvGetSevenDaysReward(int day, int num)
	{
		if (_listSevenDaysReward == null)
			_listSevenDaysReward = new List<string>();

		string key = string.Format("{0}_{1}", day, num);
		if (_listSevenDaysReward.Contains(key) == false)
			_listSevenDaysReward.Add(key);
		return _listSevenDaysReward;
	}

	public bool IsGetSevenDaysSumReward(int count)
	{
		if (_listSevenDaysSumReward == null)
			return false;

		return _listSevenDaysSumReward.Contains(count);
	}

	public List<int> OnRecvGetSevenDaysSumReward(int count)
	{
		if (_listSevenDaysSumReward == null)
			_listSevenDaysSumReward = new List<int>();

		if (_listSevenDaysSumReward.Contains(count) == false)
			_listSevenDaysSumReward.Add(count);
		return _listSevenDaysSumReward;
	}

	#region SevenDays Total Cash Product
	public bool IsPurchasedCashSlot(int index)
	{
		if (_listSevenCashSlotPurchased == null)
			return false;

		return _listSevenCashSlotPurchased.Contains(index);
	}

	public List<int> OnRecvPurchasedCashSlot(int index)
	{
		if (_listSevenCashSlotPurchased == null)
			_listSevenCashSlotPurchased = new List<int>();

		if (_listSevenCashSlotPurchased.Contains(index) == false)
			_listSevenCashSlotPurchased.Add(index);
		return _listSevenCashSlotPurchased;
	}
	#endregion
}
