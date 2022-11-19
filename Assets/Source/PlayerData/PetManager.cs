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
	public ObscuredInt searchExp { get; set; }
	public ObscuredInt searchLevel { get; set; }
	public ObscuredInt maxCountLevel { get; set; }

	public string activePetId { get; set; }

	public List<PetData> listPetData { get { return _listPetData; } }
	List<PetData> _listPetData = new List<PetData>();
	public void OnRecvPetInventory(List<ItemInstance> userInventory, Dictionary<string, UserDataRecord> userData, Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		ClearInventory();

		// 조우레벨 맥스수량레벨
		searchExp = 0;
		searchLevel = 1;
		maxCountLevel = 1;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			switch (playerStatistics[i].StatisticName)
			{
				case "petSearchExp": searchExp = playerStatistics[i].Value; break;
				case "petSearchLevel": searchLevel = playerStatistics[i].Value; break;
				case "petMaxCountLevel": maxCountLevel = playerStatistics[i].Value; break;
			}
		}

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
		if (userData.ContainsKey("activePetId"))
		{
			string petId = userData["activePetId"].Value;
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

		// 검증
		if (searchLevel > 1)
		{
		}

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
}