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

			CharacterData newCharacterData = new CharacterData();
			newCharacterData.uniqueId = userInventory[i].ItemInstanceId;
			newCharacterData.actorId = userInventory[i].ItemId;
			newCharacterData.Initialize((userInventory[i].RemainingUses != null) ? (int)userInventory[i].RemainingUses : 0, userInventory[i].CustomData);
			_listCharacterData.Add(newCharacterData);
		}

		leftCharacterId = rightCharacterId = "";
		if (userReadOnlyData.ContainsKey("leftCharacterId"))
		{
			string actorId = userReadOnlyData["leftCharacterId"].Value;
			bool find = false;
			for (int i = 0; i < _listCharacterData.Count; ++i)
			{
				if (_listCharacterData[i].actorId == actorId)
				{
					find = true;
					break;
				}
			}
			if (find)
				leftCharacterId = actorId;
			else
			{
				leftCharacterId = "";
				//PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidMainCharacter);
			}
		}

		if (userReadOnlyData.ContainsKey("rightCharacterId"))
		{
			string actorId = userReadOnlyData["rightCharacterId"].Value;
			bool find = false;
			for (int i = 0; i < _listCharacterData.Count; ++i)
			{
				if (_listCharacterData[i].actorId == actorId)
				{
					find = true;
					break;
				}
			}
			if (find)
				rightCharacterId = actorId;
			else
			{
				rightCharacterId = "";
				//PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidMainCharacter);
			}
		}

		// status
		RefreshCachedStatus();
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

	public string leftCharacterId { get; set; }
	public string rightCharacterId { get; set; }

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
	#endregion



	#region Grant
	List<ObscuredString> _listRandomObscuredId = new List<ObscuredString>();
	public List<ObscuredString> GetRandomIdList(int count)
	{
		_listRandomObscuredId.Clear();

		//for (int i = 0; i < count; ++i)
		//	_listRandomObscuredId.Add(GetRandomGachaResult());
		_listRandomObscuredId.Add("Actor0201");
		_listRandomObscuredId.Add("Actor1005");

		return _listRandomObscuredId;
	}



	public List<ItemInstance> OnRecvItemGrantResult(string jsonItemGrantResults, int expectCount = 0)
	{
		List<ItemInstance> listItemInstance = PlayFabApiManager.instance.DeserializeItemGrantResult(jsonItemGrantResults);

		int totalCount = 0;
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(listItemInstance[i].ItemId);
			if (actorTableData == null)
				continue;

			if (listItemInstance[i].UsesIncrementedBy != null)
				totalCount += (int)listItemInstance[i].UsesIncrementedBy;
		}
		if (expectCount != 0 && totalCount != expectCount)
			return null;

		for (int i = 0; i < listItemInstance.Count; ++i)
		{
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
				if (listItemInstance[i].RemainingUses != null && listItemInstance[i].UsesIncrementedBy != null)
				{
					if (listItemInstance[i].RemainingUses - listItemInstance[i].UsesIncrementedBy == currentCharacterData.count)
						currentCharacterData.Initialize((int)listItemInstance[i].RemainingUses, listItemInstance[i].CustomData);
				}
			}
			else
			{
				CharacterData newCharacterData = new CharacterData();
				newCharacterData.uniqueId = listItemInstance[i].ItemInstanceId;
				newCharacterData.actorId = listItemInstance[i].ItemId;
				newCharacterData.Initialize((listItemInstance[i].RemainingUses != null) ? (int)listItemInstance[i].RemainingUses : 0, listItemInstance[i].CustomData);
				_listCharacterData.Add(newCharacterData);

				// 없는 마법이 추가될땐 스탯부터 다 다시 계산해야한다.
				OnChangedStatus();
			}
		}
		return listItemInstance;
	}
	#endregion
}
