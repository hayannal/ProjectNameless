using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

public class PetManager : MonoBehaviour
{
	public static PetManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("PetManager")).AddComponent<PetManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static PetManager _instance = null;

	public ObscuredInt cachedValue { get; set; }

	public ObscuredInt dailySearchCount { get; set; }
	public ObscuredInt dailyHeartCount { get; set; }

	public string activePetId { get; set; }

	#region Pet Sale
	public ObscuredString petSaleId { get; set; }
	public DateTime petSaleExpireTime { get; set; }
	public DateTime petSaleCoolTimeExpireTime { get; set; }
	#endregion

	#region Pet Pass
	public DateTime petPassExpireTime { get; set; }
	#endregion

	public List<PetData> listPetData { get { return _listPetData; } }
	List<PetData> _listPetData = new List<PetData>();
	public void OnRecvPetInventory(List<ItemInstance> userInventory, Dictionary<string, UserDataRecord> userData, Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		ClearInventory();

		// 일일 탐색 카운트
		dailySearchCount = 0;
		if (userReadOnlyData.ContainsKey("petSchCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["petSchCnt"].Value, out intValue))
				dailySearchCount = intValue;
		}

		if (userReadOnlyData.ContainsKey("lasPetSchDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasPetSchDat"].Value) == false)
				OnRecvDailySearchInfo(userReadOnlyData["lasPetSchDat"].Value);
		}
		else
			dailySearchCount = 0;

		// 일일 하트 카운트
		dailyHeartCount = 0;
		if (userReadOnlyData.ContainsKey("petHrtCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["petHrtCnt"].Value, out intValue))
				dailyHeartCount = intValue;
		}

		if (userReadOnlyData.ContainsKey("lasPetHrtDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasPetHrtDat"].Value) == false)
				OnRecvDailyHeartInfo(userReadOnlyData["lasPetHrtDat"].Value);
		}
		else
			dailyHeartCount = 0;

		// list
		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId.StartsWith("Pet_") == false)
				continue;

			PetTableData petTableData = TableDataManager.instance.FindPetTableData(userInventory[i].ItemId);
			if (petTableData == null)
				continue;

			PetData newPetData = new PetData();
			newPetData.uniqueId = userInventory[i].ItemInstanceId;
			newPetData.petId = userInventory[i].ItemId;
			newPetData.Initialize((userInventory[i].RemainingUses != null) ? (int)userInventory[i].RemainingUses : 0, 0, userInventory[i].CustomData);
			_listPetData.Add(newPetData);
		}

		activePetId = "";
		if (userReadOnlyData.ContainsKey("activePetId"))
		{
			string petId = userReadOnlyData["activePetId"].Value;
			bool find = false;
			for (int i = 0; i < _listPetData.Count; ++i)
			{
				if (_listPetData[i].petId == petId)
				{
					find = true;
					break;
				}
			}
			if (find)
				activePetId = petId;
			else
			{
				activePetId = "";
				//PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidMainCharacter);
			}
		}

		#region InProgressGame
		_listInProgressSearchId.Clear();
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		if (userReadOnlyData.ContainsKey("inProgressPet"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["inProgressPet"].Value) == false)
			{
				List<string> listId = serializer.DeserializeObject<List<string>>(userReadOnlyData["inProgressPet"].Value);
				if (listId != null && listId.Count == 2)
				{
					for (int i = 0; i < listId.Count; ++i)
						_listInProgressSearchId.Add(listId[i]);
				}
			}
		}
		#endregion

		#region Pet Sale
		petSaleId = "";
		if (userReadOnlyData.ContainsKey("petSaleId"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["petSaleId"].Value) == false)
				petSaleId = userReadOnlyData["petSaleId"].Value;
		}

		if (userReadOnlyData.ContainsKey("petSaleExpDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["petSaleExpDat"].Value) == false)
			{
				DateTime expireDateTime = new DateTime();
				if (DateTime.TryParse(userReadOnlyData["petSaleExpDat"].Value, out expireDateTime))
					petSaleExpireTime = expireDateTime.ToUniversalTime();
			}
		}

		if (userReadOnlyData.ContainsKey("petSaleCoolExpDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["petSaleCoolExpDat"].Value) == false)
			{
				DateTime expireDateTime = new DateTime();
				if (DateTime.TryParse(userReadOnlyData["petSaleCoolExpDat"].Value, out expireDateTime))
					petSaleCoolTimeExpireTime = expireDateTime.ToUniversalTime();
			}
		}
		#endregion

		#region Pet Pass
		petPassExpireTime = new DateTime();
		if (userReadOnlyData.ContainsKey("petPassExpDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["petPassExpDat"].Value) == false)
				OnRecvPetPessExpireInfo(userReadOnlyData["petPassExpDat"].Value);
		}
		#endregion

		// status
		RefreshCachedStatus();
	}

	// 통계 개수 제한때문에 이렇게 별도로 호출해서 초기화 하는데
	// 혹시라도 기본 초기화에서 같이 필요하게 되면 
	// 추가로 받는 통계 기다렸다가 PlayerData.instance.OnRecvPlayerData 처리하는 부분에서 같이 처리하면 될거다.
	// 하트 호감도는 스탯에 영향 주는게 아니라서 이렇게 별도로 호출해도 상관없다.
	public void OnRecvAdditionalStatistics(List<StatisticValue> additional1PlayerStatistics, List<StatisticValue> additional2PlayerStatistics)
	{
		for (int i = 0; i < _listPetData.Count; ++i)
			_listPetData[i].SetHeart(FindHeartValue(additional1PlayerStatistics, additional2PlayerStatistics, _listPetData[i].petId));

		// status
		RefreshCachedStatus();
	}

	int FindHeartValue(List<StatisticValue> additional1PlayerStatistics, List<StatisticValue> additional2PlayerStatistics, string petId)
	{
		string id = string.Format("zzHeart_{0}", petId);
		for (int i = 0; i < additional1PlayerStatistics.Count; ++i)
		{
			if (additional1PlayerStatistics[i].StatisticName == id)
				return additional1PlayerStatistics[i].Value;
		}
		for (int i = 0; i < additional2PlayerStatistics.Count; ++i)
		{
			if (additional2PlayerStatistics[i].StatisticName == id)
				return additional2PlayerStatistics[i].Value;
		}
		return 0;
	}

	public void ClearInventory()
	{
		_listPetData.Clear();

		// status
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		cachedValue = 0;
		
		// pet status
		for (int i = 0; i < _listPetData.Count; ++i)
			cachedValue += _listPetData[i].mainStatusValue;
	}

	public void OnChangedStatus()
	{
		RefreshCachedStatus();
		PlayerData.instance.OnChangedStatus();
	}
	
	public PetData GetPetData(string id)
	{
		for (int i = 0; i < _listPetData.Count; ++i)
		{
			if (_listPetData[i].petId == id)
				return _listPetData[i];
		}
		return null;
	}

	public bool ContainsPet(string id)
	{
		for (int i = 0; i < _listPetData.Count; ++i)
		{
			if (_listPetData[i].petId == id)
				return true;
		}
		return false;
	}

	public int GetHighestPetCount()
	{
		int highestCount = 0;
		for (int i = 0; i < _listPetData.Count; ++i)
		{
			if (highestCount < _listPetData[i].count)
				highestCount = _listPetData[i].count;
		}
		return highestCount;
	}

	#region Daily
	void OnRecvDailySearchInfo(DateTime lastSearchTime)
	{
		if (ServerTime.UtcNow.Year == lastSearchTime.Year && ServerTime.UtcNow.Month == lastSearchTime.Month && ServerTime.UtcNow.Day == lastSearchTime.Day)
		{
			// 유효하면 읽어놨던 count값을 유지하고
			//dailySearchCount += 1;
		}
		else
			dailySearchCount = 0;
	}

	public void OnRecvDailySearchInfo(string lastSearchTimeString)
	{
		DateTime lastSearchTime = new DateTime();
		if (DateTime.TryParse(lastSearchTimeString, out lastSearchTime))
		{
			DateTime universalTime = lastSearchTime.ToUniversalTime();
			OnRecvDailySearchInfo(universalTime);
		}
	}

	void OnRecvDailyHeartInfo(DateTime lastHeartTime)
	{
		if (ServerTime.UtcNow.Year == lastHeartTime.Year && ServerTime.UtcNow.Month == lastHeartTime.Month && ServerTime.UtcNow.Day == lastHeartTime.Day)
		{
			// 유효하면 읽어놨던 count값을 유지하고
			//dailySearchCount += 1;
		}
		else
			dailyHeartCount = 0;
	}

	public void OnRecvDailyHeartInfo(string lastHeartTimeString)
	{
		DateTime lastHeartTime = new DateTime();
		if (DateTime.TryParse(lastHeartTimeString, out lastHeartTime))
		{
			DateTime universalTime = lastHeartTime.ToUniversalTime();
			OnRecvDailyHeartInfo(universalTime);
		}
	}
	#endregion

	public void OnRefreshDay()
	{
		dailySearchCount = 0;
		dailyHeartCount = 0;

		if (MainCanvas.instance != null)
			MainCanvas.instance.RefreshPetAlarmObject();
	}

	#region InProgressGame
	List<ObscuredString> _listInProgressSearchId = new List<ObscuredString>();
	public List<ObscuredString> GetInProgressSearchIdList() { return _listInProgressSearchId; }
	public bool IsCachedInProgressGame()
	{
		return (_listInProgressSearchId.Count > 0);
	}

	public void SetSearchInfo(List<ObscuredString> listRandomId)
	{
		_listInProgressSearchId.Clear();
		for (int i = 0; i < listRandomId.Count; ++i)
			_listInProgressSearchId.Add(listRandomId[i]);
	}
	#endregion

	#region Pet Sale
	public void OnRecvStartPetSale(string petSaleId, string petSaleExpireTimeString)
	{
		this.petSaleId = petSaleId;

		DateTime petSaleExpireTime = new DateTime();
		if (DateTime.TryParse(petSaleExpireTimeString, out petSaleExpireTime))
			this.petSaleExpireTime = petSaleExpireTime.ToUniversalTime();
	}

	public void OnRecvCoolTimePetSale(string petSaleCoolTimeExpireTimeString)
	{
		DateTime petSaleCoolTimeExpireTime = new DateTime();
		if (DateTime.TryParse(petSaleCoolTimeExpireTimeString, out petSaleCoolTimeExpireTime))
			this.petSaleCoolTimeExpireTime = petSaleCoolTimeExpireTime.ToUniversalTime();
	}

	public bool IsPetSale()
	{
		if (string.IsNullOrEmpty(petSaleId) == false && ServerTime.UtcNow < petSaleExpireTime)
			return true;
		return false;
	}

	public bool IsCoolTimePetSale()
	{
		if (ServerTime.UtcNow < petSaleCoolTimeExpireTime)
			return true;
		return false;
	}
	#endregion

	#region Pass
	public bool IsPetPass()
	{
		if (ServerTime.UtcNow < petPassExpireTime)
			return true;
		return false;
	}

	public void OnRecvPetPessExpireInfo(string lastPetPassExpireTimeString)
	{
		DateTime lastPetPassExpireTime = new DateTime();
		if (DateTime.TryParse(lastPetPassExpireTimeString, out lastPetPassExpireTime))
		{
			DateTime universalTime = lastPetPassExpireTime.ToUniversalTime();
			petPassExpireTime = universalTime;
		}
	}
	#endregion


	#region Grant
	public string GetFirstPetId()
	{
		List<string> listPetId = new List<string>();
		for (int i = 0; i < TableDataManager.instance.petTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.petTable.dataArray[i].star == 1)
				listPetId.Add(TableDataManager.instance.petTable.dataArray[i].petId);
		}
		return listPetId[UnityEngine.Random.Range(0, listPetId.Count)];
	}

	class RandomSearchPetIdInfo
	{
		public string id;
		public float sumWeight;
	}
	List<RandomSearchPetIdInfo> _listSearchPetIdInfo = null;
	public string GetRandomResult()
	{
		if (_listSearchPetIdInfo == null)
			_listSearchPetIdInfo = new List<RandomSearchPetIdInfo>();
		_listSearchPetIdInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.petTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.petTable.dataArray[i].meetWeight;
			if (weight <= 0.0f)
				continue;

			sumWeight += weight;
			RandomSearchPetIdInfo newInfo = new RandomSearchPetIdInfo();
			newInfo.id = TableDataManager.instance.petTable.dataArray[i].petId;
			newInfo.sumWeight = sumWeight;
			_listSearchPetIdInfo.Add(newInfo);
		}

		if (_listSearchPetIdInfo.Count == 0)
			return "";

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listSearchPetIdInfo[_listSearchPetIdInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listSearchPetIdInfo.Count; ++i)
		{
			if (random <= _listSearchPetIdInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listSearchPetIdInfo[index].id;
	}

	List<ObscuredString> _listRandomObscuredId = new List<ObscuredString>();
	public List<ObscuredString> GetRandomIdList()
	{
		_listRandomObscuredId.Clear();

		// search는 항상 2마리를 기본으로 한다.
		for (int i = 0; i < 2; ++i)
			_listRandomObscuredId.Add(GetRandomResult());

		//_listRandomObscuredId.Add("Pet_0002");
		//_listRandomObscuredId.Add("Pet_0003");

		return _listRandomObscuredId;
	}

	public string GetRandomResultByStar(int belowStar, bool forPetSale)
	{
		if (_listSearchPetIdInfo == null)
			_listSearchPetIdInfo = new List<RandomSearchPetIdInfo>();
		_listSearchPetIdInfo.Clear();

		int maxPetCountStep = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPetCountStep");

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.petTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.petTable.dataArray[i].star > belowStar)
				continue;

			if (forPetSale)
			{
				PetData petData = GetPetData(TableDataManager.instance.petTable.dataArray[i].petId);
				if (petData != null)
				{
					PetCountTableData petCountTableData = TableDataManager.instance.FindPetCountTableData(TableDataManager.instance.petTable.dataArray[i].star, maxPetCountStep);
					if (petCountTableData != null)
					{
						if (petData.count >= petCountTableData.max)
							continue;
					}
				}
			}

			// meetWeight대신에 균등하게 굴린다. 인자로 들어오는 등급 이하라면 다 동등한 확률이다.
			sumWeight += 1.0f;
			RandomSearchPetIdInfo newInfo = new RandomSearchPetIdInfo();
			newInfo.id = TableDataManager.instance.petTable.dataArray[i].petId;
			newInfo.sumWeight = sumWeight;
			_listSearchPetIdInfo.Add(newInfo);
		}

		if (_listSearchPetIdInfo.Count == 0)
			return "";

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listSearchPetIdInfo[_listSearchPetIdInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listSearchPetIdInfo.Count; ++i)
		{
			if (random <= _listSearchPetIdInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listSearchPetIdInfo[index].id;
	}



	List<string> _listTempPetId = new List<string>();
	public List<ObscuredString> GetExtraGainIdList(bool oneForFailure, int belowStar = 0)
	{
		_listRandomObscuredId.Clear();

		if (oneForFailure)
		{
			_listTempPetId.Clear();
			for (int i = 0; i < TableDataManager.instance.petTable.dataArray.Length; ++i)
			{
				if (TableDataManager.instance.petTable.dataArray[i].star == 1)
					_listTempPetId.Add(TableDataManager.instance.petTable.dataArray[i].petId);
			}
			_listRandomObscuredId.Add(_listTempPetId[UnityEngine.Random.Range(0, _listTempPetId.Count)]);
		}
		else
		{
			int count = UnityEngine.Random.Range(BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetExtraGainMin"), BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetExtraGainMax") + 1);
			if (belowStar == 0)
			{
				// belowStar가 0이라는건 belowStar 를 안쓰겠다는거와 같으니 기본 랜덤으로 해주고
				for (int i = 0; i < count; ++i)
					_listRandomObscuredId.Add(GetRandomResult());
			}
			else
			{
				// 특정 이하의 star만 원할때는 
				for (int i = 0; i < count; ++i)
					_listRandomObscuredId.Add(GetRandomResultByStar(belowStar, false));
			}
		}

		return _listRandomObscuredId;
	}

	public List<ItemInstance> OnRecvItemGrantResult(string jsonItemGrantResults, int expectCount = 0)
	{
		List<ItemInstance> listItemInstance = PlayFabApiManager.instance.DeserializeItemGrantResult(jsonItemGrantResults);

		int prevHighestPetCount = GetHighestPetCount();
		int totalCount = 0;
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			PetTableData petTableData = TableDataManager.instance.FindPetTableData(listItemInstance[i].ItemId);
			if (petTableData == null)
				continue;

			if (listItemInstance[i].UsesIncrementedBy != null)
				totalCount += (int)listItemInstance[i].UsesIncrementedBy;
		}
		if (expectCount != 0 && totalCount != expectCount)
		{
			Debug.LogWarningFormat("Expect Count Unmatched!! t : {0} / e : {1}", totalCount, expectCount);
			return null;
		}

		int addPetCount = 0;
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			PetTableData petTableData = TableDataManager.instance.FindPetTableData(listItemInstance[i].ItemId);
			if (petTableData == null)
				continue;

			PetData currentPetData = null;
			for (int j = 0; j < _listPetData.Count; ++j)
			{
				if (_listPetData[j].petId == listItemInstance[i].ItemId)
				{
					currentPetData = _listPetData[j];
					break;
				}
			}

			if (currentPetData != null)
			{
				// 수량 변경
				if (listItemInstance[i].RemainingUses != null && listItemInstance[i].UsesIncrementedBy != null)
				{
					if (listItemInstance[i].RemainingUses - listItemInstance[i].UsesIncrementedBy == currentPetData.count)
						currentPetData.SetCount((int)listItemInstance[i].RemainingUses);

					// 펫은 수량이 바뀌어도 스탯에 적용해야한다.
					OnChangedStatus();
				}
			}
			else
			{
				PetData newPetData = new PetData();
				newPetData.uniqueId = listItemInstance[i].ItemInstanceId;
				newPetData.petId = listItemInstance[i].ItemId;
				newPetData.Initialize((listItemInstance[i].RemainingUses != null) ? (int)listItemInstance[i].RemainingUses : 0, 0, listItemInstance[i].CustomData);
				_listPetData.Add(newPetData);

				// 없는 펫이 추가될땐 스탯부터 다 다시 계산해야한다.
				OnChangedStatus();

				addPetCount += 1;
			}
		}

		if (addPetCount > 0)
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.GatherPet, addPetCount);

		int highestPetCount = GetHighestPetCount();
		if (highestPetCount > prevHighestPetCount)
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.GatherPetCount, highestPetCount - prevHighestPetCount);

		return listItemInstance;
	}

	public void OnRecvPurchaseItem(string rewardValue, int rewardCount)
	{
		PetTableData petTableData = TableDataManager.instance.FindPetTableData(rewardValue);
		if (petTableData == null)
			return;

		PetData currentPetData = null;
		for (int i = 0; i < _listPetData.Count; ++i)
		{
			if (_listPetData[i].petId == rewardValue)
			{
				currentPetData = _listPetData[i];
				break;
			}
		}

		if (currentPetData != null)
		{
			int prevHighestPetCount = GetHighestPetCount();
			if ((currentPetData.count + rewardCount) > prevHighestPetCount)
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.GatherPetCount, (currentPetData.count + rewardCount) - prevHighestPetCount);

			currentPetData.SetCount(currentPetData.count + rewardCount);
		}
		else
		{
			PetData newPetData = new PetData();
			newPetData.uniqueId = "unfixedUniqueId";
			newPetData.petId = rewardValue;
			newPetData.Initialize(rewardCount, 0, null);
			_listPetData.Add(newPetData);

			// 없는 펫이 추가될땐 스탯부터 다 다시 계산해야한다.
			OnChangedStatus();

			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.GatherPet, rewardCount);
		}
	}
	#endregion
}