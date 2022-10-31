using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

// 아이템 전부에서 사용하는거라 공용으로
public class ItemGrantRequest
{
	public Dictionary<string, string> Data;
	public string ItemId;
}

public class GrantItemsToUsersResult
{
	public List<ItemInstance> ItemGrantResults;
}

public class SpellManager : MonoBehaviour
{
	public static SpellManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("SpellManager")).AddComponent<SpellManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static SpellManager _instance = null;

	public ObscuredInt spellTotalLevel { get; set; }
	public ObscuredInt cachedValue { get; set; }

	List<SpellData> _listSpellData = new List<SpellData>();
	public void OnRecvSpellInventory(List<ItemInstance> userInventory, Dictionary<string, UserDataRecord> userData, Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		ClearInventory();

		spellTotalLevel = 1;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			switch (playerStatistics[i].StatisticName)
			{
				case "spellLevel": spellTotalLevel = playerStatistics[i].Value; break;
			}
		}

		// list
		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId.StartsWith("Spell_") == false)
				continue;

			SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(userInventory[i].ItemId);
			if (skillTableData == null)
				continue;

			SpellData newSpellData = new SpellData();
			newSpellData.uniqueId = userInventory[i].ItemInstanceId;
			newSpellData.spellId = userInventory[i].ItemId;
			newSpellData.Initialize((userInventory[i].RemainingUses != null) ? (int)userInventory[i].RemainingUses : 0, userInventory[i].CustomData);
			_listSpellData.Add(newSpellData);
		}

		// 검증
		if (spellTotalLevel > 1)
		{
			SpellTotalTableData spellTotalTableData = TableDataManager.instance.FindSpellTotalTableData(spellTotalLevel);
			if (spellTotalTableData == null)
				spellTotalLevel = 1;
			else
			{
				if (GetSumSpellCount() < spellTotalTableData.requiredAccumulatedCount)
					spellTotalLevel = 1;
			}
		}

		// status
		RefreshCachedStatus();
	}

	public void ClearInventory()
	{
		_listSpellData.Clear();

		// status
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		cachedValue = 0;

		// total level status
		SpellTotalTableData spellTotalTableData = TableDataManager.instance.FindSpellTotalTableData(spellTotalLevel);
		if (spellTotalTableData != null)
			cachedValue = spellTotalTableData.accumulatedAtk;

		// spell level status
		for (int i = 0; i < _listSpellData.Count; ++i)
			cachedValue += _listSpellData[i].mainStatusValue;
	}

	public void OnChangedStatus()
	{
		RefreshCachedStatus();
		PlayerData.instance.OnChangedStatus();
	}

	public int GetSumSpellCount()
	{
		int sumSpellCount = 0;
		for (int i = 0; i < _listSpellData.Count; ++i)
			sumSpellCount += _listSpellData[i].count;
		return sumSpellCount;
	}

	public SpellData GetSpellData(string id)
	{
		for (int i = 0; i < _listSpellData.Count; ++i)
		{
			if (_listSpellData[i].spellId == id)
				return _listSpellData[i];
		}
		return null;
	}

	public int GetSpellLevel(string id)
	{
		SpellData spellData = GetSpellData(id);
		if (spellData != null)
			return spellData.level;
		return 0;
	}


	#region Total
	public void OnLevelUpTotalSpell(int targetLevel)
	{
		if ((spellTotalLevel + 1) == targetLevel)
			spellTotalLevel = targetLevel;
		OnChangedStatus();
	}
	#endregion


	#region Grant
	List<ItemGrantRequest> _listGrantRequest = new List<ItemGrantRequest>();
	public List<ItemGrantRequest> GenerateGrantInfo(List<string> listSpellId, ref string checkSum)
	{
		_listGrantRequest.Clear();

		for (int i = 0; i < listSpellId.Count; ++i)
		{
			ItemGrantRequest info = new ItemGrantRequest();
			info.ItemId = listSpellId[i];
			// 최초로 만들어질때만 Data 적용되고 이미 만들어진 아이템에는 적용되지 않으므로 기본값을 설정하면 된다.
			info.Data = new Dictionary<string, string>();
			info.Data.Add(SpellData.KeyLevel, "1");
			_listGrantRequest.Add(info);
		}

		if (_listGrantRequest.Count > 0)
		{
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			string jsonItemGrants = serializer.SerializeObject(_listGrantRequest);
			checkSum = PlayFabApiManager.CheckSum(jsonItemGrants);
		}

		// 임시 리스트를 가지고 있을 필요 없으니 클리어
		_listSpellId.Clear();

		return _listGrantRequest;
	}

	List<string> _listSpellId = new List<string>();
	public List<ItemGrantRequest> GenerateGrantRequestInfo(List<ObscuredString> listSpellId, ref string checkSum)
	{
		_listGrantRequest.Clear();
		if (listSpellId == null || listSpellId.Count == 0)
			return _listGrantRequest;

		_listSpellId.Clear();
		for (int i = 0; i < listSpellId.Count; ++i)
			_listSpellId.Add(listSpellId[i]);
		return GenerateGrantInfo(_listSpellId, ref checkSum);
	}

	public List<ItemGrantRequest> GenerateGrantRequestInfo(string spellId, ref string checkSum)
	{
		_listSpellId.Clear();
		_listSpellId.Add(spellId);
		return GenerateGrantInfo(_listSpellId, ref checkSum);
	}

	public List<ItemInstance> DeserializeItemGrantResult(string jsonItemGrantResults)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		GrantItemsToUsersResult result = serializer.DeserializeObject<GrantItemsToUsersResult>(jsonItemGrantResults);
		return result.ItemGrantResults;
	}

	// 대부분의 아이템 획득은 이걸 써서 처리하게 될거다.
	public void OnRecvItemGrantResult(string jsonItemGrantResults, int expectCount = 0)
	{
		List<ItemInstance> listItemInstance = DeserializeItemGrantResult(jsonItemGrantResults);
		if (expectCount != 0 && listItemInstance.Count != expectCount)
			return;

		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(listItemInstance[i].ItemId);
			if (skillTableData == null)
				continue;

			SpellData currentSpellData = null;
			for (int j = 0; j < _listSpellData.Count; ++j)
			{
				if (_listSpellData[j].spellId == listItemInstance[i].ItemId)
				{
					currentSpellData = _listSpellData[j];
					break;
				}
			}

			if (currentSpellData != null)
			{
				currentSpellData.Initialize((listItemInstance[i].RemainingUses != null) ? (int)listItemInstance[i].RemainingUses : 0, listItemInstance[i].CustomData);

				//if (BattleInstanceManager.instance.playerActor != null)
				//	BattleInstanceManager.instance.playerActor.skillProcessor.RefreshSpellLevel(currentSpellData);
			}
			else
			{
				SpellData newSpellData = new SpellData();
				newSpellData.uniqueId = listItemInstance[i].ItemInstanceId;
				newSpellData.spellId = listItemInstance[i].ItemId;
				newSpellData.Initialize((listItemInstance[i].RemainingUses != null) ? (int)listItemInstance[i].RemainingUses : 0, listItemInstance[i].CustomData);
				_listSpellData.Add(newSpellData);

				//if (BattleInstanceManager.instance.playerActor != null)
				//	BattleInstanceManager.instance.playerActor.skillProcessor.AddSpell(currentSpellData);
			}
		}
	}
	#endregion
}
