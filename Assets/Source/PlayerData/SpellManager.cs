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

		// spell processor
		InitializeSpellInfo();

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

	public int GetSpellKindsCount()
	{
		return _listSpellData.Count;
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

	void Update()
	{
		UpdateSpellInfo();
	}


	#region Pick One
	public string PickOneAcquiredSpellId(bool acquired)
	{
		if (_listGachaSpellIdInfo == null)
			_listGachaSpellIdInfo = new List<RandomGachaSpellIdInfo>();
		_listGachaSpellIdInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.pickOneSpellTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.pickOneSpellTable.dataArray[i].acquired != acquired)
				continue;
			if (acquired && GetSpellData(TableDataManager.instance.pickOneSpellTable.dataArray[i].spellId) == null)
				continue;
			if (!acquired && GetSpellData(TableDataManager.instance.pickOneSpellTable.dataArray[i].spellId) != null)
				continue;

			sumWeight += 1.0f;
			RandomGachaSpellIdInfo newInfo = new RandomGachaSpellIdInfo();
			newInfo.id = TableDataManager.instance.pickOneSpellTable.dataArray[i].spellId;
			newInfo.sumWeight = sumWeight;
			_listGachaSpellIdInfo.Add(newInfo);
		}

		if (_listGachaSpellIdInfo.Count == 0)
			return "";

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listGachaSpellIdInfo[_listGachaSpellIdInfo.Count - 1].sumWeight);
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
	#endregion


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

			sumWeight += TableDataManager.instance.skillTable.dataArray[i].gachaWeight;
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

	public string GetRandomGradeGachaResult(int fixedStar)
	{
		if (_listGachaSpellIdInfo == null)
			_listGachaSpellIdInfo = new List<RandomGachaSpellIdInfo>();
		_listGachaSpellIdInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.skillTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.skillTable.dataArray[i].spell == false)
				continue;
			if (TableDataManager.instance.skillTable.dataArray[i].grade == 0 || TableDataManager.instance.skillTable.dataArray[i].star != fixedStar)
				continue;

			sumWeight += 1.0f;
			RandomGachaSpellIdInfo newInfo = new RandomGachaSpellIdInfo();
			newInfo.id = TableDataManager.instance.skillTable.dataArray[i].id;
			newInfo.sumWeight = sumWeight;
			_listGachaSpellIdInfo.Add(newInfo);
		}

		if (_listGachaSpellIdInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listGachaSpellIdInfo[_listGachaSpellIdInfo.Count - 1].sumWeight);
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
	public List<ObscuredString> GetRandomIdList(int count, int fixedStar = 0)
	{
		_listRandomObscuredId.Clear();

		for (int i = 0; i < count; ++i)
		{
			if (fixedStar == 0)
				_listRandomObscuredId.Add(GetRandomGachaResult());
			else
				_listRandomObscuredId.Add(GetRandomGradeGachaResult(fixedStar));
		}

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

				OnAddItem(newSpellData);
			}
		}
		return listItemInstance;
	}

	void OnAddItem(SpellData spellData)
	{
		// 없는 마법이 추가될땐 스탯부터 다 다시 계산해야한다.
		OnChangedStatus();
		AddSpellInfo(spellData);

		// hardcode ev13
		string cashEventId = "ev13";
		if (CashShopData.instance.IsShowEvent(cashEventId) && CashShopData.instance.unacquiredSpellSelectedId == spellData.spellId)
		{
			PlayFabApiManager.instance.RequestCloseCashEvent(cashEventId, () =>
			{
				if (MainCanvas.instance != null && MainCanvas.instance.gameObject.activeSelf)
					MainCanvas.instance.CloseCashEventButton(cashEventId);
				if (UnacquiredSpellSaleCanvas.instance != null && UnacquiredSpellSaleCanvas.instance.gameObject.activeSelf)
					UnacquiredSpellSaleCanvas.instance.gameObject.SetActive(false);
			});
		}
	}

	public void OnRecvPurchaseItem(string rewardValue, int rewardCount)
	{
		SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(rewardValue);
		if (skillTableData == null)
			return;

		SpellData currentSpellData = null;
		for (int i = 0; i < _listSpellData.Count; ++i)
		{
			if (_listSpellData[i].spellId == rewardValue)
			{
				currentSpellData = _listSpellData[i];
				break;
			}
		}

		if (currentSpellData != null)
			currentSpellData.SetCount(currentSpellData.count + rewardCount);
		else
		{
			SpellData newSpellData = new SpellData();
			newSpellData.uniqueId = "unfixedUniqueId";
			newSpellData.spellId = rewardValue;
			newSpellData.Initialize(rewardCount, null);
			_listSpellData.Add(newSpellData);

			OnAddItem(newSpellData);
		}
	}
	#endregion



	#region Spell Processor
	// 플레이어 캐릭터가 미션 진행하면서는 없을 수 있어서 SkillInfo를 SpellManager에서 관리하게 되었다.
	List<SkillProcessor.SkillInfo> _listSpellInfo;
	// 스펠 돌릴때의 주체가 될 액터를 셋팅해야 돌아가는 구조다.
	PlayerActor _playerActorForSpellProcessor;
	// 쿨타임 프로세서는 직접 가지고 있어야 액터한테 종속되지 않을 수 있다.
	public CooltimeProcessor cooltimeProcessor { get; set; }
	void InitializeSpellInfo()
	{
		cooltimeProcessor = GetComponent<CooltimeProcessor>();
		if (cooltimeProcessor == null) cooltimeProcessor = gameObject.AddComponent<CooltimeProcessor>();

		_listSpellInfo = new List<SkillProcessor.SkillInfo>();
		for (int i = 0; i < TableDataManager.instance.skillTable.dataArray.Length; ++i)
		{
			SkillTableData skillTableData = TableDataManager.instance.skillTable.dataArray[i];
			if (skillTableData.spell == false) continue;
			if (skillTableData.spell && TableDataManager.instance.skillTable.dataArray[i].actorId != "") continue;

			int skillLevel = GetSpellLevel(skillTableData.id);
			if (skillLevel == 0)
				continue;

			SkillProcessor.SkillInfo info = SkillProcessor.CreateSkillInfo(skillTableData, skillLevel);

			#region Passive Skill
			// 패시브라면 각각의 모든 캐릭터에 넣어야할거 같은데 지금 이렇게 만드는 스펠이 없긴 하다.
			// 필요해지면 추가하자.
			//if (info.skillType == eSkillType.Passive)
			//	InitializePassiveSkill(info);
			#endregion

			_listSpellInfo.Add(info);
		}
	}

	public void InitializeActorForSpellProcessor(PlayerActor playerActor)
	{
		_playerActorForSpellProcessor = playerActor;

		// 프리로드는 이때 해야한다.
		for (int i = 0; i < _listSpellInfo.Count; ++i)
		{
			if (_listSpellInfo[i].skillType != eSkillType.NonAni)
				continue;

			SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(_listSpellInfo[i].skillId);
			if (skillTableData == null)
				continue;

			for (int j = 0; j < skillTableData.effectAddress.Length; ++j)
			{
				AddressableAssetLoadManager.GetAddressableGameObject(skillTableData.effectAddress[j], "CommonEffect", (prefab) =>
				{
					BattleInstanceManager.instance.AddCommonPoolPreloadObjectList(prefab);
				});
			}
		}
	}

	public SkillProcessor.SkillInfo GetSpellInfo(string skillId)
	{
		for (int i = 0; i < _listSpellInfo.Count; ++i)
		{
			if (_listSpellInfo[i].skillId == skillId)
				return _listSpellInfo[i];
		}
		return null;
	}

	// 플레이 중에 배울때 중간에 추가해야해서 만든 함수다.
	void AddSpellInfo(SpellData spellData)
	{
		SkillTableData skillTableData = spellData.cachedSkillTableData;
		if (skillTableData == null)
			return;

		SkillProcessor.SkillInfo info = SkillProcessor.CreateSkillInfo(skillTableData, spellData.level);

		#region Passive Skill
		// 패시브라면 각각의 모든 캐릭터에 넣어야할거 같은데 지금 이렇게 만드는 스펠이 없긴 하다.
		// 필요해지면 추가하자.
		//if (info.skillType == eSkillType.Passive)
		//	InitializePassiveSkill(info);
		#endregion

		if (info.skillType == eSkillType.NonAni)
		{
			for (int j = 0; j < skillTableData.effectAddress.Length; ++j)
			{
				AddressableAssetLoadManager.GetAddressableGameObject(skillTableData.effectAddress[j], "CommonEffect", (prefab) =>
				{
					BattleInstanceManager.instance.AddCommonPoolPreloadObjectList(prefab);
				});
			}
		}
		_listSpellInfo.Add(info);

		// 플레이 중간에 얻을때는 쿨타임 한번 돌려서 리소스 로드가 마무리 되길 기다려본다. 스펠을 꽤 얻은 상황에선 거의 호출되지 않을 로직이다.
		cooltimeProcessor.ApplyCooltime(info.skillId, info.cooltime);
	}

	

	Cooltime _globalSpellCooltime;
	const float SpellDistance = 11.0f;
	void UpdateSpellInfo()
	{
		if (_playerActorForSpellProcessor == null)
			return;

		// 기본코드
		PlayerActor actor = _playerActorForSpellProcessor;

		// 미션 넘어갈때 플레이어 액터 꺼지게되면 초기화
		if (actor.gameObject == null || actor.gameObject.activeSelf == false)
			return;

		// 공격하는거랑 비슷하긴 한데 최종적으로 SkillProcessor에게 요청해서 스킬을 발동시킬거다.
		if (actor.actorStatus.IsDie())
			return;

		// 스킬이 동시에 다 나가는게 별로면 전역 딜레이라도 두는게 낫지 않을까.
		if (_globalSpellCooltime != null && _globalSpellCooltime.CheckCooltime())
			return;

		// 움직일 수 없다면 스킬도 안나가는게 맞는건가?
		if (actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
			return;

		// 스킬은 항상 자동으로 나가는게 가능하니까 이동이나 공격 상태를 검사할 필요가 없다.
		bool autoSkillUsable = true;

		// no target
		if (actor.playerAI.targetCollider == null)
			autoSkillUsable = false;

		if (!autoSkillUsable)
			return;

		// 타겟이 너무 멀면 스펠이 나가지 않게 해보자.
		Transform targetTransform = BattleInstanceManager.instance.GetTransformFromCollider(actor.playerAI.targetCollider);
		Vector3 diff = targetTransform.position - actor.cachedTransform.position;
		diff.y = 0.0f;
		if (diff.sqrMagnitude > SpellDistance * SpellDistance)
			return;

		if (UseRandomAutoSpell())
			_globalSpellCooltime = cooltimeProcessor.GetCooltime(GlobalSpellCooltimeId);
	}

	public void ApplyGlobalSpellCooltime(float duration)
	{
		cooltimeProcessor.ApplyCooltime(GlobalSpellCooltimeId, duration);
	}


	public static string GlobalSpellCooltimeId = "_globalSpellCooltime";
	public static float GlobalSpellCooltimeDuration = 1.0f;
	List<SkillProcessor.SkillInfo> _listTempSpellInfoForSelect = new List<SkillProcessor.SkillInfo>();
	bool UseRandomAutoSpell()
	{
		_listTempSpellInfoForSelect.Clear();
		for (int i = 0; i < _listSpellInfo.Count; ++i)
		{
			if (_listSpellInfo[i].skillType != eSkillType.NonAni)
				continue;
			if (cooltimeProcessor.CheckCooltime(_listSpellInfo[i].skillId))
				continue;
			_listTempSpellInfoForSelect.Add(_listSpellInfo[i]);
		}
		if (_listTempSpellInfoForSelect.Count == 0)
			return false;

		int index = Random.Range(0, _listTempSpellInfoForSelect.Count);
		_playerActorForSpellProcessor.skillProcessor.ApplyNonAniSkill(_listTempSpellInfoForSelect[index]);
		cooltimeProcessor.ApplyCooltime(_listTempSpellInfoForSelect[index].skillId, _listTempSpellInfoForSelect[index].cooltime);
		cooltimeProcessor.ApplyCooltime(GlobalSpellCooltimeId, GlobalSpellCooltimeDuration);
		return true;
	}

	public bool LevelUpSpell(string id)
	{
		SkillProcessor.SkillInfo findSkillInfo = null;
		for (int i = 0; i < _listSpellInfo.Count; ++i)
		{
			if (_listSpellInfo[i].skillId != id)
				continue;
			findSkillInfo = _listSpellInfo[i];
			break;
		}
		if (findSkillInfo == null)
			return false;



		return false;
	}
	#endregion


	#region Equip Skill
	List<SkillProcessor.SkillInfo> _listEquipSkillInfo;
	public void InitializeEquipSpellInfo()
	{
		if (_listEquipSkillInfo == null)
			_listEquipSkillInfo = new List<SkillProcessor.SkillInfo>();
		_listEquipSkillInfo.Clear();

		for (int i = 0; i < (int)EquipManager.eEquipSlotType.Amount; ++i)
		{
			EquipData equipData = EquipManager.instance.GetEquippedDataByType((EquipManager.eEquipSlotType)i);
			if (equipData == null)
				continue;

			string equipSkillId = equipData.GetUsableEquipSkillId();
			if (string.IsNullOrEmpty(equipSkillId))
				continue;

			SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(equipSkillId);
			if (skillTableData == null)
				continue;

			SkillProcessor.SkillInfo info = SkillProcessor.CreateSkillInfo(skillTableData, 1);

			if (info.skillType == eSkillType.NonAni)
			{
				for (int j = 0; j < skillTableData.effectAddress.Length; ++j)
				{
					AddressableAssetLoadManager.GetAddressableGameObject(skillTableData.effectAddress[j], "CommonEffect", (prefab) =>
					{
						BattleInstanceManager.instance.AddCommonPoolPreloadObjectList(prefab);
					});
				}
			}
			_listEquipSkillInfo.Add(info);

			// 마법무기 스킬은 시작과 동시에 쿨타임 들어간다.
			cooltimeProcessor.ApplyCooltime(info.skillId, info.cooltime);
		}

		// 초기화 끝나면 같은 시점에 UI도 초기화시켜둔다.
		UIInstanceManager.instance.ShowCanvasAsync("EquipSkillSlotCanvas", null);
	}

	public SkillProcessor.SkillInfo GetEquipSkillInfo(string skillId)
	{
		if (_listEquipSkillInfo == null)
			return null;

		for (int i = 0; i < _listEquipSkillInfo.Count; ++i)
		{
			if (_listEquipSkillInfo[i].skillId == skillId)
				return _listEquipSkillInfo[i];
		}
		return null;
	}

	public bool UseEquipSkill(SkillProcessor.SkillInfo skillInfo)
	{
		if (_playerActorForSpellProcessor == null)
			return false;

		PlayerActor actor = _playerActorForSpellProcessor;

		// 미션 넘어갈때 플레이어 액터 꺼지게되면 초기화
		if (actor.gameObject == null || actor.gameObject.activeSelf == false)
			return false;

		// Die까지 검사하는게 맞을 듯
		if (actor.actorStatus.IsDie())
			return false;

		actor.skillProcessor.ApplyNonAniSkill(skillInfo);
		cooltimeProcessor.ApplyCooltime(skillInfo.skillId, skillInfo.cooltime);
		return true;
	}

	public void ReinitializeEquipSkill()
	{
		if (_listEquipSkillInfo == null)
			return;

		if (EquipSkillSlotCanvas.instance != null && EquipSkillSlotCanvas.instance.gameObject.activeSelf)
			EquipSkillSlotCanvas.instance.ReinitializeSkillSlot();

		for (int i = 0; i < _listEquipSkillInfo.Count; ++i)
		{
			SkillProcessor.SkillInfo info = _listEquipSkillInfo[i];
			cooltimeProcessor.ApplyCooltime(info.skillId, info.cooltime);
		}
	}
	#endregion
}
