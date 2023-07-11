using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

public class CharacterManager : MonoBehaviour
{
	public static CharacterManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("CharacterManager")).AddComponent<CharacterManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static CharacterManager _instance = null;

	#region Team Pass
	public DateTime teamPassExpireTime { get; set; }
	#endregion

	public ObscuredInt cachedValue { get; set; }

	public void OnRecvCharacterInventory(List<ItemInstance> userInventory, Dictionary<string, UserDataRecord> userData, Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		ClearInventory();

		// list
		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId.StartsWith("Actor") == false)
				continue;

			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(userInventory[i].ItemId);
			if (actorTableData == null)
				continue;

			int ppCount = FindPp(userInventory, actorTableData.actorId);

			CharacterData newCharacterData = new CharacterData();
			newCharacterData.uniqueId = userInventory[i].ItemInstanceId;
			newCharacterData.actorId = userInventory[i].ItemId;
			newCharacterData.Initialize((userInventory[i].RemainingUses != null) ? (int)userInventory[i].RemainingUses : 0, ppCount, userInventory[i].CustomData);
			_listCharacterData.Add(newCharacterData);
		}

		_listTeamPositionId.Clear();
		for (int i = 0; i < (int)TeamManager.ePosition.Amount; ++i)
			_listTeamPositionId.Add("");

		for (int i = 0; i < (int)TeamManager.ePosition.Amount; ++i)
		{
			string key = string.Format("teamPosition{0}Id", i);
			if (userReadOnlyData.ContainsKey(key))
			{
				string actorId = userReadOnlyData[key].Value;
				bool find = false;
				for (int j = 0; j < _listCharacterData.Count; ++j)
				{
					if (_listCharacterData[j].actorId == actorId)
					{
						find = true;
						break;
					}
				}
				if (find)
					_listTeamPositionId[i] = actorId;
			}
		}

		#region Team Pass
		teamPassExpireTime = new DateTime();
		if (userReadOnlyData.ContainsKey("teamPassExpDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["teamPassExpDat"].Value) == false)
				OnRecvTeamPessExpireInfo(userReadOnlyData["teamPassExpDat"].Value);
		}
		#endregion

		// status
		RefreshCachedStatus();
	}

	int FindPp(List<ItemInstance> userInventory, string actorId)
	{
		string ppId = string.Format("{0}pp", actorId);
		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId == ppId)
				return (userInventory[i].RemainingUses != null) ? (int)userInventory[i].RemainingUses : 0;
		}
		return 0;
	}

	public void ClearInventory()
	{
		_listCharacterData.Clear();

		// status
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		cachedValue = 0;

		// character status
		for (int i = 0; i < _listCharacterData.Count; ++i)
			cachedValue += _listCharacterData[i].mainStatusValue;
	}

	public void OnChangedStatus()
	{
		RefreshCachedStatus();
		PlayerData.instance.OnChangedStatus();
	}



	#region Character List
	List<CharacterData> _listCharacterData = new List<CharacterData>();
	public List<CharacterData> listCharacterData { get { return _listCharacterData; } }

	//public string leftCharacterId { get; set; }
	//public string rightCharacterId { get; set; }
	List<string> _listTeamPositionId = new List<string>();
	public List<string> listTeamPositionId { get { return _listTeamPositionId; } }

	public CharacterData GetCharacterData(string actorId)
	{
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			if (_listCharacterData[i].actorId == actorId)
				return _listCharacterData[i];
		}
		return null;
	}

	public bool ContainsActor(string actorId)
	{
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			if (_listCharacterData[i].actorId == actorId)
				return true;
		}
		return false;
	}

	public bool ContainsActorByGrade(int grade)
	{
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_listCharacterData[i].actorId);
			if (actorTableData.grade == grade)
				return true;
		}
		return false;
	}

	/*
	// 필요하면 그때 추가해서 사용하자. 지금은 TeamManager에서 현재 활성화 되는 캐릭터에만 재계산을 적용한다.
	public void ReinitializeActorStatus()
	{
		// 모든 캐릭터의 스탯을 재계산 하도록 알려야한다.
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_listCharacterData[i].actorId);
			if (playerActor == null)
				continue;
			playerActor.actorStatus.InitializeActorStatus();
		}
	}
	*/

	public int GetHighestCharacterLevel()
	{
		int highestLevel = 0;
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			if (highestLevel < _listCharacterData[i].level)
				highestLevel = _listCharacterData[i].level;
		}
		return highestLevel;
	}

	public bool HasEmptyTeamSlot()
	{
		if (_listTeamPositionId[(int)TeamManager.ePosition.Top] == "" || _listTeamPositionId[(int)TeamManager.ePosition.Mid] == "" || _listTeamPositionId[(int)TeamManager.ePosition.Bottom] == "")
			return true;
		return false;
	}

	public bool IsEmptyTeamSlot()
	{
		if (_listTeamPositionId[(int)TeamManager.ePosition.Top] == "" && _listTeamPositionId[(int)TeamManager.ePosition.Mid] == "" && _listTeamPositionId[(int)TeamManager.ePosition.Bottom] == "")
			return true;
		return false;
	}
	#endregion


	#region Pick One
	public string PickOneAcquiredActorId(AcquiredCharacterSaleCanvas.eAcquiredType acquiredType)
	{
		int maxTrascendPoint = BattleInstanceManager.instance.GetCachedGlobalConstantInt("GachaActorMaxTrp");

		if (_listGachaCharacterId == null)
			_listGachaCharacterId = new List<RandomGachaCharacterId>();
		_listGachaCharacterId.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.pickOneCharacterTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.pickOneCharacterTable.dataArray[i].acquired != (int)acquiredType)
				continue;
			if (acquiredType == AcquiredCharacterSaleCanvas.eAcquiredType.AcquiredCharacter || acquiredType == AcquiredCharacterSaleCanvas.eAcquiredType.AcquiredCharacterPp)
			{
				if (ContainsActor(TableDataManager.instance.pickOneCharacterTable.dataArray[i].actorId) == false)
					continue;
				if (acquiredType == AcquiredCharacterSaleCanvas.eAcquiredType.AcquiredCharacter)
				{
					// 풀 초월이라면 후보에서 삭제해야한다.
					CharacterData characterData = GetCharacterData(TableDataManager.instance.pickOneCharacterTable.dataArray[i].actorId);
					if (characterData != null && (characterData.transcendPoint + TableDataManager.instance.pickOneCharacterTable.dataArray[i].count) > maxTrascendPoint)
						continue;
				}
			}
			if (acquiredType == AcquiredCharacterSaleCanvas.eAcquiredType.UnacquiredCharacter)
			{
				if (ContainsActor(TableDataManager.instance.pickOneCharacterTable.dataArray[i].actorId))
					continue;
			}

			sumWeight += 1.0f;
			RandomGachaCharacterId newInfo = new RandomGachaCharacterId();
			newInfo.actorId = TableDataManager.instance.pickOneCharacterTable.dataArray[i].actorId;
			newInfo.sumWeight = sumWeight;
			_listGachaCharacterId.Add(newInfo);
		}

		if (_listGachaCharacterId.Count == 0)
			return "";

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listGachaCharacterId[_listGachaCharacterId.Count - 1].sumWeight);
		for (int i = 0; i < _listGachaCharacterId.Count; ++i)
		{
			if (random <= _listGachaCharacterId[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listGachaCharacterId[index].actorId;
	}
	#endregion

	#region Pass
	public bool IsTeamPass()
	{
		if (ServerTime.UtcNow < teamPassExpireTime)
			return true;
		return false;
	}

	public void OnRecvTeamPessExpireInfo(string lastTeamPassExpireTimeString)
	{
		DateTime lastTeamPassExpireTime = new DateTime();
		if (DateTime.TryParse(lastTeamPassExpireTimeString, out lastTeamPassExpireTime))
		{
			DateTime universalTime = lastTeamPassExpireTime.ToUniversalTime();
			teamPassExpireTime = universalTime;
		}
	}
	#endregion


	#region Grant
	class RandomGachaCharacterGrade
	{
		public int grade;
		public float sumWeight;
	}
	List<RandomGachaCharacterGrade> _listGachaCharacterGrade = null;
	class RandomGachaCharacterId
	{
		public string actorId;
		public float sumWeight;
	}
	List<RandomGachaCharacterId> _listGachaCharacterId = null;
	public string GetRandomNewCharacterGachaResult(bool applyPickUpCharacter)
	{
		int maxTrascendPoint = BattleInstanceManager.instance.GetCachedGlobalConstantInt("GachaActorMaxTrp");

		// 신캐를 뽑을 수 있는지부터 확인해야한다.
		// 그러려면 현재 뽑혀있는 캐릭터 수를 확인해야한다.
		bool allMaxTranscendPoint = true;
		int totalCharacterWithTranscendCount = 0;
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_PlayerActorId || TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_DroneActorId)
				continue;
			CharacterData characterData = GetCharacterData(TableDataManager.instance.actorTable.dataArray[i].actorId);
			if (characterData != null)
			{
				totalCharacterWithTranscendCount += 1;
				totalCharacterWithTranscendCount += characterData.transcendPoint;
				if (characterData.transcendPoint < maxTrascendPoint)
					allMaxTranscendPoint = false;
			}
			else
				allMaxTranscendPoint = false;
		}
		if (allMaxTranscendPoint)
		{
			// 모든 캐릭 모든 trp를 다 충족시켰다.
			// 이러면 뽑을게 없어진다.
			return "";
		}

		// 이후엔 등급별 확률을 구해야한다.
		if (_listGachaCharacterGrade == null)
			_listGachaCharacterGrade = new List<RandomGachaCharacterGrade>();
		_listGachaCharacterGrade.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.gachaActorTable.dataArray.Length; ++i)
		{
			float weight = 0.0f;
			if ((totalCharacterWithTranscendCount + _listTempNewCharacterId.Count) < TableDataManager.instance.gachaActorTable.dataArray[i].adjustProbs.Length)
			{
				// Adjust된 Prob로 돌리면 된다.
				weight = TableDataManager.instance.gachaActorTable.dataArray[i].adjustProbs[(totalCharacterWithTranscendCount + _listTempNewCharacterId.Count)];
				if (weight <= 0.0f)
					continue;
			}
			else
			{
				// 기본 prob로 돌리면 된다.
				weight = TableDataManager.instance.gachaActorTable.dataArray[i].prob;
				if (weight <= 0.0f)
					continue;
			}

			sumWeight += weight;
			RandomGachaCharacterGrade newInfo = new RandomGachaCharacterGrade();
			newInfo.grade = TableDataManager.instance.gachaActorTable.dataArray[i].grade;
			newInfo.sumWeight = sumWeight;
			_listGachaCharacterGrade.Add(newInfo);
		}
		if (_listGachaCharacterGrade.Count == 0)
			return "";

		// 여기 sumWeight는 드랍확률이 적용되어있는거라 sumWeight 최대값으로 범위를 돌리는게 아니라 Random.value로 결정해서 돌린다. 
		// 해당되지 않으면 캐릭이 나오지 않은거다.
		int index = -1;
		//float random = UnityEngine.Random.Range(0.0f, _listGachaCharacterGrade[_listGachaCharacterGrade.Count - 1].sumWeight);
		float random = UnityEngine.Random.value;
		for (int i = 0; i < _listGachaCharacterGrade.Count; ++i)
		{
			if (random <= _listGachaCharacterGrade[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		#region PickUp Character
		if (applyPickUpCharacter)
		{
			CashShopData.PickUpCharacterInfo info = CashShopData.instance.GetCurrentPickUpCharacterInfo();
			if (info != null && tempPickUpNotStreakCount == info.bc - 1)
			{
				// 이번에 굴리는게 픽업의 최종 보너스 단계라면 강제로 grade를 전설로 고정해야한다.
				for (int i = 0; i < TableDataManager.instance.gachaActorTable.dataArray.Length; ++i)
				{
					if (TableDataManager.instance.gachaActorTable.dataArray[i].grade == 2)
					{
						index = i;
						break;
					}
				}
			}
		}
		#endregion
		if (index == -1)
			return "";
		int selectedGrade = _listGachaCharacterGrade[index].grade;

		// 등급이 결정되었으면 등급안에서 다시 굴려야한다.
		if (_listGachaCharacterId == null)
			_listGachaCharacterId = new List<RandomGachaCharacterId>();
		_listGachaCharacterId.Clear();

		#region PickUp Character
		string pickUpCharacterId = "";
		if (applyPickUpCharacter)
		{
			CashShopData.PickUpCharacterInfo info = CashShopData.instance.GetCurrentPickUpCharacterInfo();
			if (info != null)
			{
				ActorTableData pickUpActorTableData = TableDataManager.instance.FindActorTableData(info.id);
				if (pickUpActorTableData != null)
				{
					if (selectedGrade == pickUpActorTableData.grade)
					{
						// 픽업을 제외한 나머지의 합산값을 구해야한다.
						pickUpCharacterId = pickUpActorTableData.actorId;
					}
				}
			}
		}

		float pickUpCharacterForceWeight = 0.0f;
		if (applyPickUpCharacter && string.IsNullOrEmpty(pickUpCharacterId) == false)
		{
			for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
			{
				if (TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_PlayerActorId || TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_DroneActorId)
					continue;
				if (TableDataManager.instance.actorTable.dataArray[i].actorId == pickUpCharacterId)
					continue;
				if (TableDataManager.instance.actorTable.dataArray[i].grade != selectedGrade)
					continue;
				CharacterData characterData = GetCharacterData(TableDataManager.instance.actorTable.dataArray[i].actorId);
				if (characterData != null && characterData.transcendPoint >= maxTrascendPoint)
					continue;

				// 한가지 예외상황이 있는데 캐릭터 5명을 채울때까진 겹쳐지지 않게 해주기로 해본다.
				if ((totalCharacterWithTranscendCount + _listTempNewCharacterId.Count) < 5)
				{
					if (characterData != null || _listTempNewCharacterId.Contains(TableDataManager.instance.actorTable.dataArray[i].actorId))
						continue;
				}

				// 각각의 확률은 동일한 1.0
				pickUpCharacterForceWeight += TableDataManager.instance.actorTable.dataArray[i].charGachaWeight;
			}
		}
		#endregion

		sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_PlayerActorId || TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_DroneActorId)
				continue;
			if (TableDataManager.instance.actorTable.dataArray[i].grade != selectedGrade)
				continue;
			CharacterData characterData = GetCharacterData(TableDataManager.instance.actorTable.dataArray[i].actorId);
			if (characterData != null && characterData.transcendPoint >= maxTrascendPoint)
				continue;

			// 한가지 예외상황이 있는데 캐릭터 11명을 채울때까진 겹쳐지지 않게 해주기로 해본다.
			if ((totalCharacterWithTranscendCount + _listTempNewCharacterId.Count) < 11)
			{
				if (characterData != null || _listTempNewCharacterId.Contains(TableDataManager.instance.actorTable.dataArray[i].actorId))
					continue;
			}

			// 각각의 확률은 동일한 1.0
			float weight = TableDataManager.instance.actorTable.dataArray[i].charGachaWeight;
			#region PickUp Character
			if (applyPickUpCharacter && TableDataManager.instance.actorTable.dataArray[i].actorId == pickUpCharacterId)
				weight = pickUpCharacterForceWeight;
			#endregion
			sumWeight += weight;
			RandomGachaCharacterId newInfo = new RandomGachaCharacterId();
			newInfo.actorId = TableDataManager.instance.actorTable.dataArray[i].actorId;
			newInfo.sumWeight = sumWeight;
			_listGachaCharacterId.Add(newInfo);
		}
		if (_listGachaCharacterId.Count == 0)
			return "";

		index = -1;
		random = UnityEngine.Random.Range(0.0f, _listGachaCharacterId[_listGachaCharacterId.Count - 1].sumWeight);
		for (int i = 0; i < _listGachaCharacterId.Count; ++i)
		{
			if (random <= _listGachaCharacterId[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listGachaCharacterId[index].actorId;
	}

	public string GetRandomCharacterPpGachaResult()
	{
		// 가지고 있는 캐릭터들에 한해서 pp뽑을 캐릭터를 고르면 된다.
		if (_listGachaCharacterId == null)
			_listGachaCharacterId = new List<RandomGachaCharacterId>();
		_listGachaCharacterId.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_PlayerActorId || TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_DroneActorId)
				continue;
			CharacterData characterData = GetCharacterData(TableDataManager.instance.actorTable.dataArray[i].actorId);
			if (characterData == null && _listTempNewCharacterId.Contains(TableDataManager.instance.actorTable.dataArray[i].actorId) == false)
				continue;

			// 각각의 확률은 동일한 1.0
			sumWeight += 1.0f;
			RandomGachaCharacterId newInfo = new RandomGachaCharacterId();
			newInfo.actorId = TableDataManager.instance.actorTable.dataArray[i].actorId;
			newInfo.sumWeight = sumWeight;
			_listGachaCharacterId.Add(newInfo);
		}
		if (_listGachaCharacterId.Count == 0)
			return "";

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listGachaCharacterId[_listGachaCharacterId.Count - 1].sumWeight);
		for (int i = 0; i < _listGachaCharacterId.Count; ++i)
		{
			if (random <= _listGachaCharacterId[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listGachaCharacterId[index].actorId;
	}

	#region PickUp Character
	public int tempPickUpNotStreakCount { get; private set; }
	#endregion
	List<ObscuredString> _listTempNewCharacterId = new List<ObscuredString>();
	List<ObscuredString> _listRandomObscuredId = new List<ObscuredString>();
	public List<ObscuredString> GetRandomIdList(int count, bool applyPickUpCharacter = false)
	{
		#region PickUp Character
		if (applyPickUpCharacter)
			tempPickUpNotStreakCount = CashShopData.instance.GetCurrentPickUpCharacterNotStreakCount();
		#endregion

		_listTempNewCharacterId.Clear();
		_listRandomObscuredId.Clear();
		for (int i = 0; i < count; ++i)
		{
			// trp 포함 캐릭터 아이디를 1회 뽑아본다.
			string newActorId = GetRandomNewCharacterGachaResult(applyPickUpCharacter);
			if (string.IsNullOrEmpty(newActorId) == false)
			{
				_listTempNewCharacterId.Add(newActorId);
				_listRandomObscuredId.Add(newActorId);
			}

			#region PickUp Character
			if (applyPickUpCharacter)
			{
				bool getTargetPickUp = false;
				if (string.IsNullOrEmpty(newActorId) == false)
				{
					ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(newActorId);
					if (actorTableData != null && actorTableData.grade == 2)
						getTargetPickUp = true;
				}
				if (getTargetPickUp)
					tempPickUpNotStreakCount = 0;
				else
					++tempPickUpNotStreakCount;
			}
			#endregion

			// pp는 현재 가지고 있는 캐릭터들 안에서만 돌리면 끝이다.
			// 장비나 스펠처럼 적혀있는 수량대로 뽑기로 하면서 캐릭터가 나오지 않으면 pp를 굴리는 형태로 바꾼다. 수량도 그러니 한개씩 처리.
			if (string.IsNullOrEmpty(newActorId))
			{
				string ppActorId = GetRandomCharacterPpGachaResult();
				_listRandomObscuredId.Add(string.Format("{0}pp", ppActorId));
			}
		}
		return _listRandomObscuredId;
	}



	public List<ItemInstance> OnRecvItemGrantResult(string jsonItemGrantResults, int expectCount = 0)
	{
		List<ItemInstance> listItemInstance = PlayFabApiManager.instance.DeserializeItemGrantResult(jsonItemGrantResults);

		int totalCount = 0;
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			if (listItemInstance[i].ItemId.Substring(listItemInstance[i].ItemId.Length - 2) == "pp")
				continue;

			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(listItemInstance[i].ItemId);
			if (actorTableData == null)
				continue;

			if (listItemInstance[i].UsesIncrementedBy != null)
				totalCount += (int)listItemInstance[i].UsesIncrementedBy;
		}
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			if (listItemInstance[i].ItemId.Substring(listItemInstance[i].ItemId.Length - 2) != "pp")
				continue;

			string itemId = listItemInstance[i].ItemId.Substring(0, listItemInstance[i].ItemId.Length - 2);
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(itemId);
			if (actorTableData == null)
				continue;

			if (listItemInstance[i].UsesIncrementedBy != null)
				totalCount += (int)listItemInstance[i].UsesIncrementedBy;
		}
		if (expectCount != 0 && totalCount != expectCount)
		{
			Debug.LogWarningFormat("Expect Count Unmatched!! t : {0} / e : {1}", totalCount, expectCount);
			return null;
		}

		int prevHighestCharacterLevel = CharacterManager.instance.GetHighestCharacterLevel();
		int addCharacterCount = 0;
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			if (listItemInstance[i].ItemId.Substring(listItemInstance[i].ItemId.Length - 2) == "pp")
				continue;

			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(listItemInstance[i].ItemId);
			if (actorTableData == null)
				continue;

			CharacterData currentCharacterData = null;
			for (int j = 0; j < _listCharacterData.Count; ++j)
			{
				if (_listCharacterData[j].actorId == listItemInstance[i].ItemId)
				{
					currentCharacterData = _listCharacterData[j];
					break;
				}
			}

			if (currentCharacterData != null)
			{
				// 초월 획득
				if (listItemInstance[i].RemainingUses != null && listItemInstance[i].UsesIncrementedBy != null)
				{
					if (listItemInstance[i].RemainingUses - listItemInstance[i].UsesIncrementedBy == currentCharacterData.count)
						currentCharacterData.SetCharacterCount((int)listItemInstance[i].RemainingUses);
				}
			}
			else
			{
				CharacterData newCharacterData = new CharacterData();
				newCharacterData.uniqueId = listItemInstance[i].ItemInstanceId;
				newCharacterData.actorId = listItemInstance[i].ItemId;
				newCharacterData.Initialize((listItemInstance[i].RemainingUses != null) ? (int)listItemInstance[i].RemainingUses : 0, 0, listItemInstance[i].CustomData);
				_listCharacterData.Add(newCharacterData);

				OnAddItem(newCharacterData);

				addCharacterCount += 1;
			}
		}

		if (addCharacterCount > 0)
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.GatherCharacter, addCharacterCount);

		int highestCharacterLevel = GetHighestCharacterLevel();
		if (prevHighestCharacterLevel == 0 && highestCharacterLevel > prevHighestCharacterLevel)
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.LevelUpCharacter, highestCharacterLevel - prevHighestCharacterLevel);

		// 두번째 루프 돌땐 pp들을 처리한다.
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			if (listItemInstance[i].ItemId.Substring(listItemInstance[i].ItemId.Length - 2) != "pp")
				continue;

			string itemId = listItemInstance[i].ItemId.Substring(0, listItemInstance[i].ItemId.Length - 2);
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(itemId);
			if (actorTableData == null)
				continue;

			CharacterData currentCharacterData = null;
			for (int j = 0; j < _listCharacterData.Count; ++j)
			{
				if (_listCharacterData[j].actorId == itemId)
				{
					currentCharacterData = _listCharacterData[j];
					break;
				}
			}

			if (currentCharacterData != null)
			{
				if (listItemInstance[i].RemainingUses != null && listItemInstance[i].UsesIncrementedBy != null)
				{
					if (listItemInstance[i].RemainingUses - listItemInstance[i].UsesIncrementedBy == currentCharacterData.pp)
						currentCharacterData.SetPpCount((int)listItemInstance[i].RemainingUses);
				}
			}
			else
			{
				// 존재하지 않는 캐릭의 pp는 들어오지 않을거다. 처리할 수 없다.
			}
		}
		return listItemInstance;
	}

	void OnAddItem(CharacterData characterData)
	{
		// 없는 캐릭이 추가될땐 스탯부터 다 다시 계산해야한다.
		OnChangedStatus();

		// hardcode ev15
		string cashEventId = "ev15";
		if (CashShopData.instance.IsShowEvent(cashEventId) && CashShopData.instance.unacquiredCharacterSelectedId == characterData.actorId)
		{
			PlayFabApiManager.instance.RequestCloseCashEvent(cashEventId, () =>
			{
				if (MainCanvas.instance != null && MainCanvas.instance.gameObject.activeSelf)
					MainCanvas.instance.CloseCashEventButton(cashEventId);
				if (UnacquiredCharacterSaleCanvas.instance != null && UnacquiredCharacterSaleCanvas.instance.gameObject.activeSelf)
					UnacquiredCharacterSaleCanvas.instance.gameObject.SetActive(false);
			});
		}
	}

	public void OnRecvPurchaseItem(string rewardValue, int rewardCount)
	{
		string actorId = "";
		ActorTableData actorTableData = null;
		if (rewardValue.Substring(rewardValue.Length - 2) != "pp")
			actorId = rewardValue;
		else if (rewardValue.Substring(rewardValue.Length - 2) == "pp")
			actorId = rewardValue.Substring(0, rewardValue.Length - 2);
		actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		if (actorTableData == null)
			return;

		CharacterData currentCharacterData = null;
		for (int i = 0; i < _listCharacterData.Count; ++i)
		{
			if (_listCharacterData[i].actorId == actorId)
			{
				currentCharacterData = _listCharacterData[i];
				break;
			}
		}

		int prevHighestCharacterLevel = CharacterManager.instance.GetHighestCharacterLevel();
		if (rewardValue.Substring(rewardValue.Length - 2) != "pp")
		{
			if (currentCharacterData != null)
				currentCharacterData.SetCharacterCount(currentCharacterData.count + rewardCount);
			else
			{
				CharacterData newCharacterData = new CharacterData();
				newCharacterData.uniqueId = "unfixedUniqueId";
				newCharacterData.actorId = rewardValue;
				newCharacterData.Initialize(rewardCount, 0, null);
				_listCharacterData.Add(newCharacterData);

				OnAddItem(newCharacterData);

				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.GatherCharacter);

				int highestCharacterLevel = GetHighestCharacterLevel();
				if (prevHighestCharacterLevel == 0 && highestCharacterLevel > prevHighestCharacterLevel)
					GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.LevelUpCharacter, highestCharacterLevel - prevHighestCharacterLevel);
			}
		}
		else if (rewardValue.Substring(rewardValue.Length - 2) == "pp")
		{
			if (currentCharacterData != null)
				currentCharacterData.SetPpCount(currentCharacterData.pp + rewardCount);
			else
			{
				// 존재하지 않는 캐릭의 pp. 가능한가?
			}
		}
	}
	#endregion
}
