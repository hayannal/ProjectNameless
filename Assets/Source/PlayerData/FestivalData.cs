using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class FestivalData : MonoBehaviour
{
	public static FestivalData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("FestivalData")).AddComponent<FestivalData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static FestivalData _instance = null;

	// 현재 진행중인 퀘스트 정보
	// 
	public Dictionary<int, int> _dicFestivalProceedingInfo = new Dictionary<int, int>();

	// 페스티벌도 동시에 1개의 미션만 처리
	public ObscuredInt festivalId { get; private set; }

	// 시간은 이벤트와 마찬가지로 만료시간 쿨타임 둘다 있다.
	public DateTime festivalExpireTime { get; private set; }
	public DateTime festivalExpire2Time { get; private set; }	// 이건 교환용 기간으로 사용한다.
	public DateTime festivalCoolExpireTime { get; private set; }

	// 현재 Sum포인트
	public ObscuredInt festivalSumPoint { get; set; }

	// Collect 리스트
	List<int> _listFestivalCollect;

	// Exchange 정보
	Dictionary<string, int> _dicFestivalExchange;

	#region Festival Total Cash Product
	List<int> _listFestivalCashSlotPurchased;
	#endregion


	List<GuideQuestData.eQuestClearType> _listAvailableQuestType = new List<GuideQuestData.eQuestClearType>();
	void Awake()
	{
		// 이거 늘릴때는 서버의 StartFestival 함수 안의
		// init proceeding
		// 부분도 같이 수정해줘야한다.
		_listAvailableQuestType.Add(GuideQuestData.eQuestClearType.Analysis);
		_listAvailableQuestType.Add(GuideQuestData.eQuestClearType.FreeFortuneWheel);
		_listAvailableQuestType.Add(GuideQuestData.eQuestClearType.UseEnergy);
		_listAvailableQuestType.Add(GuideQuestData.eQuestClearType.UseTicket);
		_listAvailableQuestType.Add(GuideQuestData.eQuestClearType.ClearRushDefense);
		_listAvailableQuestType.Add(GuideQuestData.eQuestClearType.ClearBossDefense);
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
				StartFestival();
			}
		}
	}

	public void StartFestival()
	{
		int newIdIndex = -1;
		for (int i = 0; i < TableDataManager.instance.festivalTypeTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.festivalTypeTable.dataArray[i].startYear != 0 && TableDataManager.instance.festivalTypeTable.dataArray[i].endYear != 0)
			{
				DateTime startDateTime = new DateTime(TableDataManager.instance.festivalTypeTable.dataArray[i].startYear, TableDataManager.instance.festivalTypeTable.dataArray[i].startMonth, TableDataManager.instance.festivalTypeTable.dataArray[i].startDay);
				if (ServerTime.UtcNow < startDateTime)
					continue;
				DateTime endDateTime = new DateTime(TableDataManager.instance.festivalTypeTable.dataArray[i].endYear, TableDataManager.instance.festivalTypeTable.dataArray[i].endMonth, TableDataManager.instance.festivalTypeTable.dataArray[i].endDay);
				if (ServerTime.UtcNow > endDateTime)
					continue;

				newIdIndex = i;
				break;
			}
		}
		if (newIdIndex == -1)
			return;
		FestivalTypeTableData festivalTypeTableData = TableDataManager.instance.festivalTypeTable.dataArray[newIdIndex];
		if (festivalTypeTableData == null)
			return;

		_waitPacket = true;
		PlayFabApiManager.instance.RequestStartFestival(festivalTypeTableData.groupId, festivalTypeTableData.collectGivenTime, festivalTypeTableData.exchangeGivenTime, festivalTypeTableData.coolTime, () =>
		{
			festivalId = festivalTypeTableData.groupId;

			if (MainCanvas.instance != null)
				MainCanvas.instance.festivalButtonObject.SetActive(true);

			_waitPacket = false;
		}, () =>
		{
			_waitPacket = false;
			_retryStartRemainTime = 3.0f;
		});
	}

	public void OnRecvFestivalData(Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		festivalId = 0;
		if (userReadOnlyData.ContainsKey("festivalId"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["festivalId"].Value, out intValue))
				festivalId = intValue;
		}

		if (userReadOnlyData.ContainsKey("festivalExpDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["festivalExpDat"].Value) == false)
				OnRecvFestivalStartInfo(userReadOnlyData["festivalExpDat"].Value);
		}

		if (userReadOnlyData.ContainsKey("festivalExp2Dat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["festivalExp2Dat"].Value) == false)
				OnRecvFestivalStart2Info(userReadOnlyData["festivalExp2Dat"].Value);
		}

		if (userReadOnlyData.ContainsKey("festivalCoolExpDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["festivalCoolExpDat"].Value) == false)
				OnRecvFestivalCoolTimeInfo(userReadOnlyData["festivalCoolExpDat"].Value);
		}

		festivalSumPoint = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "festivalSumPoint")
			{
				festivalSumPoint = playerStatistics[i].Value;
				break;
			}
		}

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_dicFestivalProceedingInfo.Clear();
		if (festivalId != 0 && ServerTime.UtcNow < festivalExpireTime)
		{
			// 조건들을 불러야하는데
			// 하필 조건Enum이 0, 1, 2 순서대로 되어있지 않기 때문에
			// 이렇게 불러야할 조건들을 리스트로 해서 처리하기로 한다.
			for (int i = 0; i < _listAvailableQuestType.Count; ++i)
			{
				int intType = (int)_listAvailableQuestType[i];
				string key = string.Format("festivalPrcdCnt_{0}", intType);
				if (userReadOnlyData.ContainsKey(key))
				{
					int intValue = 0;
					if (int.TryParse(userReadOnlyData[key].Value, out intValue))
						_dicFestivalProceedingInfo.Add(intType, intValue);
				}
			}

			// Collect 상태도 불러야한다.
			_listFestivalCollect = null;
			if (userReadOnlyData.ContainsKey("festivalCollectLst"))
			{
				string festivalCollectLstString = userReadOnlyData["festivalCollectLst"].Value;
				if (string.IsNullOrEmpty(festivalCollectLstString) == false)
					_listFestivalCollect = serializer.DeserializeObject<List<int>>(festivalCollectLstString);
			}

			// 교환 상태도 불러야한다.
			_dicFestivalExchange = null;
			if (userReadOnlyData.ContainsKey("festivalExchangeData"))
			{
				string festivalExchangeDataString = userReadOnlyData["festivalExchangeData"].Value;
				if (string.IsNullOrEmpty(festivalExchangeDataString) == false)
					_dicFestivalExchange = serializer.DeserializeObject<Dictionary<string, int>>(festivalExchangeDataString);
			}

			#region Festival Total Cash Product
			_listFestivalCashSlotPurchased = null;
			if (userReadOnlyData.ContainsKey("festivalCashSlotLst"))
			{
				string festivalCashSlotLstString = userReadOnlyData["festivalCashSlotLst"].Value;
				if (string.IsNullOrEmpty(festivalCashSlotLstString) == false)
					_listFestivalCashSlotPurchased = serializer.DeserializeObject<List<int>>(festivalCashSlotLstString);
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
			if (festivalId == 0 || (ServerTime.UtcNow > festivalExpireTime && ServerTime.UtcNow > festivalExpire2Time && ServerTime.UtcNow > festivalCoolExpireTime))
				_retryStartRemainTime = 1.0f;
		}
	}

	public void OnRecvFestivalStartInfo(string lastFestivalExpireTimeString)
	{
		DateTime lastFestivalExpireTime = new DateTime();
		if (DateTime.TryParse(lastFestivalExpireTimeString, out lastFestivalExpireTime))
		{
			DateTime universalTime = lastFestivalExpireTime.ToUniversalTime();
			festivalExpireTime = universalTime;
		}

		// 시작할때 카운트는 항상 초기화해두고 시작해야한다.
		_dicFestivalProceedingInfo.Clear();
		if (_listFestivalCollect != null) _listFestivalCollect.Clear();
		if (_dicFestivalExchange != null) _dicFestivalExchange.Clear();
		if (_listFestivalCashSlotPurchased != null) _listFestivalCashSlotPurchased.Clear();
		festivalSumPoint = 0;
	}

	public void OnRecvFestivalStart2Info(string lastFestivalExpire2TimeString)
	{
		DateTime lastFestivalExpire2Time = new DateTime();
		if (DateTime.TryParse(lastFestivalExpire2TimeString, out lastFestivalExpire2Time))
		{
			DateTime universalTime = lastFestivalExpire2Time.ToUniversalTime();
			festivalExpire2Time = universalTime;
		}
	}

	public void OnRecvFestivalCoolTimeInfo(string lastFestivalCoolExpireTimeString)
	{
		DateTime lastFestivalCoolExpireTime = new DateTime();
		if (DateTime.TryParse(lastFestivalCoolExpireTimeString, out lastFestivalCoolExpireTime))
		{
			DateTime universalTime = lastFestivalCoolExpireTime.ToUniversalTime();
			festivalCoolExpireTime = universalTime;
		}
	}
	
	public void OnRefreshDay()
	{
		if (PlayerData.instance.downloadConfirmed == false)
			return;

		//if (FestivalCanvas.instance != null && FestivalCanvas.instance.gameObject.activeSelf)
		//	FestivalCanvas.instance.RefreshLockObjectList();

		// 부여받은적이 한번도 없다면 뭔가 이상한거다. 이럴땐 그냥 패스.
		if (festivalId == 0)
			return;

		// 진행중인게 있다면 패스
		if (ServerTime.UtcNow < festivalExpireTime || ServerTime.UtcNow < festivalExpire2Time)
			return;

		// 쿨타임 도중이라면 패스
		if (ServerTime.UtcNow < festivalCoolExpireTime)
			return;

		StartFestival();
	}

	public void OnQuestEvent(GuideQuestData.eQuestClearType questClearType, int addValue = 1)
	{
		// 진행중이지 않다면 패스
		if (festivalId == 0 || ServerTime.UtcNow > festivalExpireTime)
			return;

		// 타입 검사
		if (_listAvailableQuestType.Contains(questClearType) == false)
			return;

		PlayFabApiManager.instance.RequestFestivalProceedingCount((int)questClearType, addValue, GetProceedingCount((int)questClearType) + addValue, () =>
		{
			OnQuestProceedingCount();
		});
	}

	public void SetProceedingCount(int type, int addCount, int expectCount)
	{
		int currentCount = GetProceedingCount(type);
		if (currentCount + addCount == expectCount)
		{
			if (_dicFestivalProceedingInfo.ContainsKey(type))
				_dicFestivalProceedingInfo[type] = expectCount;
			else
				_dicFestivalProceedingInfo.Add(type, expectCount);
		}
	}

	public int GetProceedingCount(int type)
	{
		if (_dicFestivalProceedingInfo.ContainsKey(type))
			return _dicFestivalProceedingInfo[type];
		return 0;
	}

	void OnQuestProceedingCount()
	{
		// 테이블 검사해서 알람표시
		//GuideQuestInfo.instance.RefreshCountInfo();
		if (MainCanvas.instance != null && MainCanvas.instance.gameObject.activeSelf && MainCanvas.instance.IsHideState() == false)
			MainCanvas.instance.RefreshFestivalAlarmObject();
	}

	public bool IsGetFestivalCollect(int num)
	{
		if (_listFestivalCollect == null)
			return false;
		return _listFestivalCollect.Contains(num);
	}

	public List<int> OnRecvGetFestivalCollect(int num)
	{
		if (_listFestivalCollect == null)
			_listFestivalCollect = new List<int>();

		if (_listFestivalCollect.Contains(num) == false)
			_listFestivalCollect.Add(num);
		return _listFestivalCollect;
	}

	#region Festival Exchange
	public int GetExchangeTime(int num)
	{
		if (_dicFestivalExchange == null)
			return 0;
		if (_dicFestivalExchange.ContainsKey(num.ToString()))
			return _dicFestivalExchange[num.ToString()];
		return 0;
	}
	public Dictionary<string, int> OnRecvFestivalExchange(int num, int count)
	{
		if (_dicFestivalExchange == null)
			_dicFestivalExchange = new Dictionary<string, int>();

		if (_dicFestivalExchange.ContainsKey(num.ToString()) == false)
			_dicFestivalExchange.Add(num.ToString(), count);
		else
			_dicFestivalExchange[num.ToString()] += count;
		return _dicFestivalExchange;
	}
	#endregion

	#region Festival Total Cash Product
	public bool IsPurchasedCashSlot(int index)
	{
		if (_listFestivalCashSlotPurchased == null)
			return false;

		return _listFestivalCashSlotPurchased.Contains(index);
	}

	public List<int> OnRecvPurchasedCashSlot(int index)
	{
		if (_listFestivalCashSlotPurchased == null)
			_listFestivalCashSlotPurchased = new List<int>();

		if (_listFestivalCashSlotPurchased.Contains(index) == false)
			_listFestivalCashSlotPurchased.Add(index);
		return _listFestivalCashSlotPurchased;
	}
	#endregion
}
