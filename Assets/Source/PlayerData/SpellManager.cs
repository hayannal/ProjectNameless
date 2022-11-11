using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

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
	class RandomGachaSpellInfo
	{
		public int grade;
		public int star;
		public float sumWeight;
	}
	List<RandomGachaSpellInfo> _listGachaSpellInfo = null;

	class RandomGachaSpellIdInfo
	{
		public string id;
		public float sumWeight;
	}
	List<RandomGachaSpellIdInfo> _listGachaSpellIdInfo = null;

	List<int> _listTotalSpellGachaStep = new List<int>();
	public string GetRandomGachaResult()
	{
		string totalSpellGachaStep = BattleInstanceManager.instance.GetCachedGlobalConstantString("TotalSpellGachaStep");
		if (_listTotalSpellGachaStep.Count == 0)
			StringUtil.SplitIntList(totalSpellGachaStep, ref _listTotalSpellGachaStep);

		int gachaStepIndex = -1;
		for (int i = _listTotalSpellGachaStep.Count - 1; i >= 0; --i)
		{
			if (spellTotalLevel >= _listTotalSpellGachaStep[i])
			{
				gachaStepIndex = i;
				break;
			}
		}
		if (gachaStepIndex == -1)
			return "";

		if (_listGachaSpellInfo == null)
			_listGachaSpellInfo = new List<RandomGachaSpellInfo>();
		_listGachaSpellInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.gachaSpellTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.gachaSpellTable.dataArray[i].probs[gachaStepIndex];
			if (weight <= 0.0f)
				continue;

			sumWeight += weight;
			RandomGachaSpellInfo newInfo = new RandomGachaSpellInfo();
			newInfo.grade = TableDataManager.instance.gachaSpellTable.dataArray[i].grade;
			newInfo.star = TableDataManager.instance.gachaSpellTable.dataArray[i].star;
			newInfo.sumWeight = sumWeight;
			_listGachaSpellInfo.Add(newInfo);
		}

		if (_listGachaSpellInfo.Count == 0)
			return "";

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listGachaSpellInfo[_listGachaSpellInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listGachaSpellInfo.Count; ++i)
		{
			if (random <= _listGachaSpellInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";

		if (_listGachaSpellIdInfo == null)
			_listGachaSpellIdInfo = new List<RandomGachaSpellIdInfo>();
		_listGachaSpellIdInfo.Clear();

		sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.skillTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.skillTable.dataArray[i].spell == false)
				continue;
			if (TableDataManager.instance.skillTable.dataArray[i].grade != _listGachaSpellInfo[index].grade || TableDataManager.instance.skillTable.dataArray[i].star != _listGachaSpellInfo[index].star)
				continue;

			sumWeight += 1.0f;
			RandomGachaSpellIdInfo newInfo = new RandomGachaSpellIdInfo();
			newInfo.id = TableDataManager.instance.skillTable.dataArray[i].id;
			newInfo.sumWeight = sumWeight;
			_listGachaSpellIdInfo.Add(newInfo);
		}

		if (_listGachaSpellIdInfo.Count == 0)
			return "";

		index = -1;
		random = UnityEngine.Random.Range(0.0f, _listGachaSpellIdInfo[_listGachaSpellIdInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listGachaSpellIdInfo.Count; ++i)
		{
			if (random <= _listGachaSpellIdInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listGachaSpellIdInfo[index].id;
	}

	List<ObscuredString> _listRandomObscuredId = new List<ObscuredString>();
	public List<ObscuredString> GetRandomIdList(int count)
	{
		_listRandomObscuredId.Clear();

		for (int i = 0; i < count; ++i)
			_listRandomObscuredId.Add(GetRandomGachaResult());

		return _listRandomObscuredId;
	}

	

	// 대부분의 아이템 획득은 이걸 써서 처리하게 될거다.
	public List<ItemInstance> OnRecvItemGrantResult(string jsonItemGrantResults, int expectCount = 0)
	{
		List<ItemInstance> listItemInstance = PlayFabApiManager.instance.DeserializeItemGrantResult(jsonItemGrantResults);

		int totalCount = 0;
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(listItemInstance[i].ItemId);
			if (skillTableData == null)
				continue;

			if (listItemInstance[i].UsesIncrementedBy != null)
				totalCount += (int)listItemInstance[i].UsesIncrementedBy;
		}
		if (expectCount != 0 && totalCount != expectCount)
			return null;

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
				if (listItemInstance[i].RemainingUses != null && listItemInstance[i].UsesIncrementedBy != null)
				{
					if (listItemInstance[i].RemainingUses - listItemInstance[i].UsesIncrementedBy == currentSpellData.count)
						currentSpellData.Initialize((int)listItemInstance[i].RemainingUses, listItemInstance[i].CustomData);
				}
			}
			else
			{
				SpellData newSpellData = new SpellData();
				newSpellData.uniqueId = listItemInstance[i].ItemInstanceId;
				newSpellData.spellId = listItemInstance[i].ItemId;
				newSpellData.Initialize((listItemInstance[i].RemainingUses != null) ? (int)listItemInstance[i].RemainingUses : 0, listItemInstance[i].CustomData);
				_listSpellData.Add(newSpellData);

				// 없는 마법이 추가될땐 스탯부터 다 다시 계산해야한다.
				OnChangedStatus();
				if (BattleInstanceManager.instance.playerActor != null)
					BattleInstanceManager.instance.playerActor.skillProcessor.AddSpell(newSpellData);
			}
		}
		return listItemInstance;
	}
	#endregion
}
