using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class SubQuestData : MonoBehaviour
{
	public static SubQuestData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("SubQuestData")).AddComponent<SubQuestData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static SubQuestData _instance = null;

	public enum eQuestStep
	{
		Select,
		Proceeding,
	}

	public const int DailyMaxCount = 3;

	public class QuestInfo
	{
		public int idx;     // index 0 ~ 5
		public int tp;		// type
		public int cnt;     // need count
		public int rwd;     // reward
		public int dif;		// difficulty
	}
	List<QuestInfo> _listSubQuestInfo;
	
	// 보상까지 받은 퀘스트 횟수
	public ObscuredInt todayQuestRewardedCount { get; set; }

	// 현재 진행중인 퀘스트. 오리진 박스를 열때 Index는 0으로 Step은 
	public eQuestStep currentQuestStep { get; set; }
	public ObscuredInt currentQuestIndex { get; set; }
	public ObscuredInt currentQuestProceedingCount { get; set; }
	public ObscuredInt clientCacheIndex { get; set; }
	public ObscuredInt clientCacheCount { get; set; }

	public QuestInfo FindQuestInfoByIndex(int index)
	{
		if (CheckValidQuestList() == false)
			return null;

		for (int i = 0; i < _listSubQuestInfo.Count; ++i)
		{
			if (_listSubQuestInfo[i].idx == index)
				return _listSubQuestInfo[i];
		}
		return null;
	}

	public int GetProceedingCount()
	{
		int result = currentQuestProceedingCount;
		if (currentQuestIndex == clientCacheIndex)
			result += clientCacheCount;
		return result;
	}


	public void OnRecvQuestData(Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		// PlayerData.ResetData 호출되면 다시 여기로 들어올테니 플래그들 초기화 시켜놓는다.
		_checkUnfixedQuestListInfo = false;

		// 다른 Unfixed 처럼 하루에 할 수 있는 퀘스트 목록 총 6개를 서버로 보내서 등록하게 된다.
		if (userReadOnlyData.ContainsKey("lasUnfxQstDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasUnfxQstDat"].Value) == false)
				_lastUnfixedDateTimeString = userReadOnlyData["lasUnfxQstDat"].Value;
		}

		if (userReadOnlyData.ContainsKey("qstLst"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["qstLst"].Value) == false)
				_questListDataString = userReadOnlyData["qstLst"].Value;
		}

		// 현재 진행중인 퀘스트의 상태. 동시에 1개만 진행가능하다.
		currentQuestStep = eQuestStep.Select;
		currentQuestIndex = 0;
		if (userReadOnlyData.ContainsKey("qstIdx"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["qstIdx"].Value, out intValue))
			{
				currentQuestStep = eQuestStep.Proceeding;
				currentQuestIndex = intValue;
			}
		}

		currentQuestProceedingCount = 0;
		if (userReadOnlyData.ContainsKey("qstPrcdCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["qstPrcdCnt"].Value, out intValue))
				currentQuestProceedingCount = intValue;
		}

		todayQuestRewardedCount = 0;
		if (userReadOnlyData.ContainsKey("qstRwdCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["qstRwdCnt"].Value, out intValue))
				todayQuestRewardedCount = intValue;
		}

		// 가이드 퀘스트 말고 서브퀘스트도 클라이언트 캐싱을 사용하기로 한다.
		clientCacheIndex = ObscuredPrefs.GetInt("cachedSubQuestIndex");
		clientCacheCount = 0;
		if (currentQuestIndex == clientCacheIndex)
			clientCacheCount = ObscuredPrefs.GetInt("cachedSubQuestCount");

		_lastCachedQuestIndex = -1;
		_listSubQuestInfo = null;

		// 클라 구동 후 퀘스트는 하루에 한번 미리 정해둔다.
		CheckUnfixedQuestListInfo();
	}

	bool _checkIndicatorOnRecvQuestList;
	public bool CheckValidQuestList(bool checkIndicatorOnRecvQuestList = false)
	{
		if (checkIndicatorOnRecvQuestList)
			_checkIndicatorOnRecvQuestList = true;

		return (_listSubQuestInfo != null && _listSubQuestInfo.Count > 0);
	}

	#region Quest List
	string _lastUnfixedDateTimeString = "";
	string _questListDataString = "";
	bool _checkUnfixedQuestListInfo = false;
	void CheckUnfixedQuestListInfo()
	{
		if (_checkUnfixedQuestListInfo)
			return;

		bool needRegister = false;
		if (_lastUnfixedDateTimeString == "")
			needRegister = true;
		if (needRegister == false)
		{
			DateTime lastUnfixedItemDateTime = new DateTime();
			if (DateTime.TryParse(_lastUnfixedDateTimeString, out lastUnfixedItemDateTime))
			{
				DateTime universalTime = lastUnfixedItemDateTime.ToUniversalTime();
				if (ServerTime.UtcNow.Year == universalTime.Year && ServerTime.UtcNow.Month == universalTime.Month && ServerTime.UtcNow.Day == universalTime.Day)
				{
					var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
					_listSubQuestInfo = serializer.DeserializeObject<List<QuestInfo>>(_questListDataString);

					OnCompleteRecvQuestList();
				}
				else
					needRegister = true;
			}
		}
		_checkUnfixedQuestListInfo = true;

		if (needRegister == false)
			return;
		RegisterQuestList();
	}

	List<QuestInfo> _listSubQuestInfoForSend = new List<QuestInfo>();
	void RegisterQuestList()
	{
		_listSubQuestInfoForSend.Clear();

		// 두개씩 세트로 만들거다. 두개 안에서는 다른 퀘스트를 만들어야한다.
		for (int i = 0; i < DailyMaxCount; ++i)
		{
			int questType1 = 0;
			int questType2 = 0;
			int questNeedCountIndex1 = 0;
			int questNeedCountIndex2 = 0;

			CreateQuestPair(ref questType1, ref questNeedCountIndex1, ref questType2, ref questNeedCountIndex2);

			QuestInfo questInfo = new QuestInfo();
			questInfo.idx = i * 2;
			questInfo.tp = questType1;
			SubQuestTableData subQuestTableData = TableDataManager.instance.FindSubQuestTableData(questType1);
			questInfo.cnt = subQuestTableData.needCount[questNeedCountIndex1];
			questInfo.rwd = subQuestTableData.rewardGold[questNeedCountIndex1];
			questInfo.dif = questNeedCountIndex1;
			//SetSubCondition(questInfo);
			_listSubQuestInfoForSend.Add(questInfo);

			questInfo = new QuestInfo();
			questInfo.idx = i * 2 + 1;
			questInfo.tp = questType2;
			subQuestTableData = TableDataManager.instance.FindSubQuestTableData(questType2);
			questInfo.cnt = subQuestTableData.needCount[questNeedCountIndex2];
			questInfo.rwd = subQuestTableData.rewardGold[questNeedCountIndex2];
			questInfo.dif = questNeedCountIndex2;
			//SetSubCondition(questInfo);
			_listSubQuestInfoForSend.Add(questInfo);
		}

		PlayFabApiManager.instance.RequestRegisterQuestList(_listSubQuestInfoForSend, () =>
		{
			// 성공이 오면 새로 만든거로 덮어씌운다.
			_listSubQuestInfo = _listSubQuestInfoForSend;

			// 성공했다면 오늘의 최초 로그인에서 퀘스트 리스트를 등록했다는거니
			// 퀘스트 스텝을 초기화 해도 되지 않을까.
			// 여기에서 처리하면 오리진박스 오픈하고나서 설정하지 않아도 되기때문에 하는게 좋을거 같다.
			currentQuestIndex = 0;
			currentQuestStep = eQuestStep.Select;
			currentQuestProceedingCount = 0;
			todayQuestRewardedCount = 0;
			_lastCachedQuestIndex = -1;

			OnCompleteRecvQuestList();
		});
	}

	void OnCompleteRecvQuestList()
	{
		// 패킷 받고나서 바로 확인하도록 처리
		if (_checkIndicatorOnRecvQuestList)
		{
			if (SubQuestInfo.instance != null)
			{
				SubQuestInfo.instance.gameObject.SetActive(false);
				SubQuestInfo.instance.gameObject.SetActive(true);
			}
			if (SubQuestInfo.instance != null && IsCompleteQuest())
				SubQuestInfo.instance.RefreshAlarmObject();
			_checkIndicatorOnRecvQuestList = false;
		}
	}

	List<int> _listTempIndex = new List<int>();
	void CreateQuestPair(ref int questType1, ref int questNeedCountIndex1, ref int questType2, ref int questNeedCountIndex2)
	{
		// 타입도 달라야하고 난이도도 달라야한다. 첫번째꺼는 테이블에서 랜덤으로 뽑고
		int firstIndex = UnityEngine.Random.Range(0, TableDataManager.instance.subQuestTable.dataArray.Length);
		questType1 = TableDataManager.instance.subQuestTable.dataArray[firstIndex].type;
		questNeedCountIndex1 = UnityEngine.Random.Range(0, TableDataManager.instance.subQuestTable.dataArray[firstIndex].needCount.Length);

		// 두번째꺼는 첫번째껄 제외한 리스트를 만들어서 이 안에서 뽑는다.
		_listTempIndex.Clear();
		for (int i = 0; i < TableDataManager.instance.subQuestTable.dataArray.Length; ++i)
		{
			if (firstIndex == i)
				continue;
			_listTempIndex.Add(i);
		}
		int secondIndex = _listTempIndex[UnityEngine.Random.Range(0, _listTempIndex.Count)];
		questType2 = TableDataManager.instance.subQuestTable.dataArray[secondIndex].type;

		_listTempIndex.Clear();
		for (int i = 0; i < TableDataManager.instance.subQuestTable.dataArray[secondIndex].needCount.Length; ++i)
		{
			if (questNeedCountIndex1 == i)
				continue;
			_listTempIndex.Add(i);
		}
		questNeedCountIndex2 = _listTempIndex[UnityEngine.Random.Range(0, _listTempIndex.Count)];
	}
	
	#endregion

	public void OnRefreshDay()
	{
		// 진행중인 퀘스트는 건드리지 않기로 하고 오늘의 완료횟수만 초기화하기로 한다.
		todayQuestRewardedCount = 0;

		// 대신 리스트를 갱신할거기 때문에 진행중인 퀘스트가 남아있다면
		if (currentQuestStep == eQuestStep.Proceeding)
		{
			// 인덱스만 0번으로 옮겨놓고 다음날에도 계속 사용하기로 한다.
			currentQuestIndex = 0;
			//currentQuestStep = eQuestStep.Select;
			//currentQuestProceedingCount = 0;
		}

		// 이 타이밍에 다음날 새로 열리는 퀘스트 갱신처리도 함께 해준다.
		RegisterQuestList();
	}

	public bool IsCompleteQuest()
	{
		// 이미 보상받은 퀘스트는 Complete로 판단하지 않고 rewarded count에 포함되어있다.
		if (currentQuestStep != eQuestStep.Proceeding)
			return false;

		QuestInfo subQuestInfo = FindQuestInfoByIndex(currentQuestIndex);
		if (subQuestInfo == null)
			return false;
		return (GetProceedingCount() >= subQuestInfo.cnt);
	}

	int _lastCachedQuestIndex = -1;
	QuestInfo _cachedQuestInfo = null;
	public void OnQuestEvent(GuideQuestData.eQuestClearType questClearType, int addValue = 1)
	{
		if (currentQuestStep != eQuestStep.Proceeding)
			return;

		// 자주 호출되는 부분이라서 캐싱을 사용한다.
		QuestInfo selectedQuestInfo = null;
		if (_lastCachedQuestIndex == currentQuestIndex && _cachedQuestInfo != null)
			selectedQuestInfo = _cachedQuestInfo;
		else
		{
			selectedQuestInfo = FindQuestInfoByIndex(currentQuestIndex);
			_lastCachedQuestIndex = currentQuestIndex;
			_cachedQuestInfo = selectedQuestInfo;
		}

		// 설마 이랬는데도 null이면 이상한거다.
		if (selectedQuestInfo == null)
			return;

		// 이미 완료한 상태라면 더 할 필요도 없다.
		if (GetProceedingCount() >= selectedQuestInfo.cnt)
		{
			#region Sync Max Count
			// 완료한건데 아직 서버로 못보내고 있는거였다면
			// 마지막 동기화 타임 체크해서 강제로 동기화 시켜둔다.
			if (currentQuestIndex == clientCacheIndex && clientCacheCount > 0)
			{
				if (ServerTime.UtcNow > _lastSendTime + TimeSpan.FromSeconds(15))
				{
					_lastSendTime = ServerTime.UtcNow;

					// UI 갱신은 할필요 없을거다.
					PlayFabApiManager.instance.RequestQuestProceedingCount(clientCacheCount, () =>
					{
						clientCacheCount = 0;
						ObscuredPrefs.SetInt("cachedGuideQuestCount", clientCacheCount);
					});
				}
			}
			#endregion
			return;
		}

		// 조건들 체크
		if ((GuideQuestData.eQuestClearType)selectedQuestInfo.tp != questClearType)
			return;

		// 너무 변화량이 빨라서 매번 보내지 않을 항목들은 클라이언트 캐싱을 함께 사용하기로 한다.
		bool ignorePacket = false;
		if (GuideQuestData.IsUseClientCache(questClearType))
		{
			if (CheckLastSendDelay(questClearType))
				ignorePacket = true;
		}

		if (ignorePacket)
		{
			clientCacheIndex = currentQuestIndex;
			++clientCacheCount;
			ObscuredPrefs.SetInt("cachedSubQuestIndex", clientCacheIndex);
			ObscuredPrefs.SetInt("cachedSubQuestCount", clientCacheCount);

			// 패킷 보내는걸 패스할땐 그냥 UI 처리만 해두고 넘어가야한다.
			OnQuestProceedingCount();
		}
		else
		{
			// 나머지 항목들은 바로 보내는게 맞다.
			if (currentQuestIndex == clientCacheIndex)
				addValue += clientCacheCount;

			PlayFabApiManager.instance.RequestQuestProceedingCount(addValue, () =>
			{
				clientCacheCount = 0;
				ObscuredPrefs.SetInt("cachedSubQuestCount", clientCacheCount);

				OnQuestProceedingCount();
			});
		}
	}

	// 30초에 한번씩만 보내도 문제없지 않을까
	DateTime _lastSendTime;
	bool CheckLastSendDelay(GuideQuestData.eQuestClearType questClearType)
	{
		// 마지막으로 보낸 시간으로부터 30초가 지나지 않았다면
		if (_lastSendTime + TimeSpan.FromSeconds(30) > ServerTime.UtcNow)
			return true;

		// 지났으면 보내도 된다. 마지막 시간을 기록해둔다.
		_lastSendTime = ServerTime.UtcNow;
		return false;
	}

	void OnQuestProceedingCount()
	{
		SubQuestInfo.instance.RefreshCountInfo();

		if (QuestInfoCanvas.instance != null && QuestInfoCanvas.instance.gameObject.activeSelf)
			QuestInfoCanvas.instance.RefreshCountInfo();

		if (IsCompleteQuest())
			SubQuestInfo.instance.RefreshAlarmObject();
	}
}