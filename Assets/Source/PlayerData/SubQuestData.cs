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
	
	// ������� ���� ����Ʈ Ƚ��
	public ObscuredInt todayQuestRewardedCount { get; set; }

	// ���� �������� ����Ʈ. ������ �ڽ��� ���� Index�� 0���� Step�� 
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
		// PlayerData.ResetData ȣ��Ǹ� �ٽ� ����� �����״� �÷��׵� �ʱ�ȭ ���ѳ��´�.
		_checkUnfixedQuestListInfo = false;

		// �ٸ� Unfixed ó�� �Ϸ翡 �� �� �ִ� ����Ʈ ��� �� 6���� ������ ������ ����ϰ� �ȴ�.
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

		// ���� �������� ����Ʈ�� ����. ���ÿ� 1���� ���డ���ϴ�.
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

		// ���̵� ����Ʈ ���� ��������Ʈ�� Ŭ���̾�Ʈ ĳ���� ����ϱ�� �Ѵ�.
		clientCacheIndex = ObscuredPrefs.GetInt("cachedSubQuestIndex");
		clientCacheCount = 0;
		if (currentQuestIndex == clientCacheIndex)
			clientCacheCount = ObscuredPrefs.GetInt("cachedSubQuestCount");

		_lastCachedQuestIndex = -1;
		_listSubQuestInfo = null;

		// Ŭ�� ���� �� ����Ʈ�� �Ϸ翡 �ѹ� �̸� ���صд�.
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

		// �ΰ��� ��Ʈ�� ����Ŵ�. �ΰ� �ȿ����� �ٸ� ����Ʈ�� �������Ѵ�.
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
			// ������ ���� ���� ����ŷ� ������.
			_listSubQuestInfo = _listSubQuestInfoForSend;

			// �����ߴٸ� ������ ���� �α��ο��� ����Ʈ ����Ʈ�� ����ߴٴ°Ŵ�
			// ����Ʈ ������ �ʱ�ȭ �ص� ���� ������.
			// ���⿡�� ó���ϸ� �������ڽ� �����ϰ��� �������� �ʾƵ� �Ǳ⶧���� �ϴ°� ������ ����.
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
		// ��Ŷ �ް��� �ٷ� Ȯ���ϵ��� ó��
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
		// Ÿ�Ե� �޶���ϰ� ���̵��� �޶���Ѵ�. ù��°���� ���̺��� �������� �̰�
		int firstIndex = UnityEngine.Random.Range(0, TableDataManager.instance.subQuestTable.dataArray.Length);
		questType1 = TableDataManager.instance.subQuestTable.dataArray[firstIndex].type;
		questNeedCountIndex1 = UnityEngine.Random.Range(0, TableDataManager.instance.subQuestTable.dataArray[firstIndex].needCount.Length);

		// �ι�°���� ù��°�� ������ ����Ʈ�� ���� �� �ȿ��� �̴´�.
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
		// �������� ����Ʈ�� �ǵ帮�� �ʱ�� �ϰ� ������ �Ϸ�Ƚ���� �ʱ�ȭ�ϱ�� �Ѵ�.
		todayQuestRewardedCount = 0;

		// ��� ����Ʈ�� �����Ұű� ������ �������� ����Ʈ�� �����ִٸ�
		if (currentQuestStep == eQuestStep.Proceeding)
		{
			// �ε����� 0������ �Űܳ��� ���������� ��� ����ϱ�� �Ѵ�.
			currentQuestIndex = 0;
			//currentQuestStep = eQuestStep.Select;
			//currentQuestProceedingCount = 0;
		}

		// �� Ÿ�ֿ̹� ������ ���� ������ ����Ʈ ����ó���� �Բ� ���ش�.
		RegisterQuestList();
	}

	public bool IsCompleteQuest()
	{
		// �̹� ������� ����Ʈ�� Complete�� �Ǵ����� �ʰ� rewarded count�� ���ԵǾ��ִ�.
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

		// ���� ȣ��Ǵ� �κ��̶� ĳ���� ����Ѵ�.
		QuestInfo selectedQuestInfo = null;
		if (_lastCachedQuestIndex == currentQuestIndex && _cachedQuestInfo != null)
			selectedQuestInfo = _cachedQuestInfo;
		else
		{
			selectedQuestInfo = FindQuestInfoByIndex(currentQuestIndex);
			_lastCachedQuestIndex = currentQuestIndex;
			_cachedQuestInfo = selectedQuestInfo;
		}

		// ���� �̷��µ��� null�̸� �̻��ѰŴ�.
		if (selectedQuestInfo == null)
			return;

		// �̹� �Ϸ��� ���¶�� �� �� �ʿ䵵 ����.
		if (GetProceedingCount() >= selectedQuestInfo.cnt)
		{
			#region Sync Max Count
			// �Ϸ��Ѱǵ� ���� ������ �������� �ִ°ſ��ٸ�
			// ������ ����ȭ Ÿ�� üũ�ؼ� ������ ����ȭ ���ѵд�.
			if (currentQuestIndex == clientCacheIndex && clientCacheCount > 0)
			{
				if (ServerTime.UtcNow > _lastSendTime + TimeSpan.FromSeconds(15))
				{
					_lastSendTime = ServerTime.UtcNow;

					// UI ������ ���ʿ� �����Ŵ�.
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

		// ���ǵ� üũ
		if ((GuideQuestData.eQuestClearType)selectedQuestInfo.tp != questClearType)
			return;

		// �ʹ� ��ȭ���� ���� �Ź� ������ ���� �׸���� Ŭ���̾�Ʈ ĳ���� �Բ� ����ϱ�� �Ѵ�.
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

			// ��Ŷ �����°� �н��Ҷ� �׳� UI ó���� �صΰ� �Ѿ���Ѵ�.
			OnQuestProceedingCount();
		}
		else
		{
			// ������ �׸���� �ٷ� �����°� �´�.
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

	// 30�ʿ� �ѹ����� ������ �������� ������
	DateTime _lastSendTime;
	bool CheckLastSendDelay(GuideQuestData.eQuestClearType questClearType)
	{
		// ���������� ���� �ð����κ��� 30�ʰ� ������ �ʾҴٸ�
		if (_lastSendTime + TimeSpan.FromSeconds(30) > ServerTime.UtcNow)
			return true;

		// �������� ������ �ȴ�. ������ �ð��� ����صд�.
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