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

	public string activePetId { get; set; }

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
			newPetData.SetCount((userInventory[i].RemainingUses != null) ? (int)userInventory[i].RemainingUses : 0);
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

		// status
		RefreshCachedStatus();
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
	#endregion

	public void OnRefreshDay()
	{
		dailySearchCount = 0;

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

	#region Pass
	public bool IsPetPass()
	{
		return false;
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

	

	List<string> _listTempPetId = new List<string>();
	public List<ObscuredString> GetExtraGainIdList(bool oneForFailure)
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
			for (int i = 0; i < count; ++i)
				_listRandomObscuredId.Add(GetRandomResult());
		}

		return _listRandomObscuredId;
	}

	public List<ItemInstance> OnRecvItemGrantResult(string jsonItemGrantResults, int expectCount = 0)
	{
		List<ItemInstance> listItemInstance = PlayFabApiManager.instance.DeserializeItemGrantResult(jsonItemGrantResults);

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
				}
			}
			else
			{
				PetData newPetData = new PetData();
				newPetData.uniqueId = listItemInstance[i].ItemInstanceId;
				newPetData.petId = listItemInstance[i].ItemId;
				newPetData.SetCount((listItemInstance[i].RemainingUses != null) ? (int)listItemInstance[i].RemainingUses : 0);
				_listPetData.Add(newPetData);

				// 없는 펫이 추가될땐 스탯부터 다 다시 계산해야한다.
				OnChangedStatus();
			}
		}
		return listItemInstance;
	}
	#endregion
}