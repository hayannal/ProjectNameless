﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TableDataManager : MonoBehaviour
{
	public static TableDataManager instance;

	// temp 
	public ActionTable actionTable;
	public ControlTable controlTable;
	public AffectorValueTable affectorValueTable;
	public AffectorValueLevelTable affectorValueLevelTable;
	public ActorStateTable actorStateTable;
	
	public MonsterTable monsterTable;
	public MonsterGroupTable monsterGroupTable;
	public ActorTable actorTable;
	public SkillTable skillTable;
	public SkillLevelTable skillLevelTable;
	public ConditionValueTable conditionValueTable;
	public LevelPackTable levelPackTable;
	public LevelPackLevelTable levelPackLevelTable;
	public GlobalConstantFloatTable globalConstantFloatTable;
	public GlobalConstantIntTable globalConstantIntTable;
	public GlobalConstantStringTable globalConstantStringTable;
	public DropTable dropTable;
	public DamageRateTable damageRateTable;

	public StageIdTable stageIdTable;
	public StageTable stageTable;
	public StageBetTable stageBetTable;

	public ActorInfoTable actorInfoTable;
	public PlayerLevelTable playerLevelTable;
	public CostumeTable costumeTable;
	public ActorLevelTable actorLevelTable;
	public ActorTranscendTable actorTranscendTable;
	public SpellGradeLevelTable spellGradeLevelTable;
	public SpellTotalTable spellTotalTable;
	public ShopSpellTable shopSpellTable;
	public ShopActorTable shopActorTable;
	public ShopEquipTable shopEquipTable;
	public PickOneSpellTable pickOneSpellTable;
	public PickOneCharacterTable pickOneCharacterTable;
	public RelayPackTable relayPackTable;
	public FreePackageTable freePackageTable;

	public EventPointTypeTable eventPointTypeTable;
	public EventPointRewardTable eventPointRewardTable;
	public EventTypeTable eventTypeTable;
	public EventRewardTable eventRewardTable;
	public AnalysisTable analysisTable;
	public AnalysisDropTable analysisDropTable;
	public AnalysisBoostTable analysisBoostTable;

	public GuideQuestTable guideQuestTable;
	public SubQuestTable subQuestTable;
	public SummonTypeTable summonTypeTable;
	public ShopProductTable shopProductTable;
	public LevelPassTable levelPassTable;
	public BrokenEnergyTable brokenEnergyTable;
	public ConsumeItemTable consumeItemTable;
	public EnergyUsePaybackTable energyUsePaybackTable;
	public StageClearTable stageClearTable;

	public SevenDaysTypeTable sevenDaysTypeTable;
	public SevenDaysRewardTable sevenDaysRewardTable;
	public SevenSumTable sevenSumTable;

	public FestivalTypeTable festivalTypeTable;
	public FestivalCollectTable festivalCollectTable;
	public FestivalExchangeTable festivalExchangeTable;

	public GachaSpellTable gachaSpellTable;
	public GachaActorTable gachaActorTable;
	public GachaEquipTable gachaEquipTable;

	public PetTable petTable;
	public PetCountTable petCountTable;
	public PetCaptureTable petCaptureTable;
	public PetSaleTable petSaleTable;

	public AttendanceTypeTable attendanceTypeTable;
	public AttendanceRewardTable attendanceRewardTable;

	public EquipTable equipTable;
	public EquipLevelTable equipLevelTable;
	public EquipCompositeTable equipCompositeTable;
	public EquipGradeTable equipGradeTable;

	public MissionModeTable missionModeTable;
	public BossBattleTable bossBattleTable;
	public BossExpTable bossExpTable;
	public BossBattleDifficultyTable bossBattleDifficultyTable;
	public BossBattleRewardTable bossBattleRewardTable;
	public PointShopTable pointShopTable;
	public PointShopAtkTable pointShopAtkTable;

	public RobotDefenseStepTable robotDefenseStepTable;
	public DroneAtkTable droneAtkTable;

	void Awake()
	{
		instance = this;
	}

	public ActionTableData FindActionTableData(string actorId, string actionName)
	{
		for (int i = 0; i < actionTable.dataArray.Length; ++i)
		{
			if (actionTable.dataArray[i].actorId == actorId && actionTable.dataArray[i].actionName == actionName)
				return actionTable.dataArray[i];
		}
		return null;
	}

	public ControlTableData FindControlTableData(string controlId)
	{
		for (int i = 0; i < controlTable.dataArray.Length; ++i)
		{
			if (controlTable.dataArray[i].id == controlId)
				return controlTable.dataArray[i];
		}
		return null;
	}

	public AffectorValueTableData FindAffectorValueTableData(string affectorValueId)
	{
		for (int i = 0; i < affectorValueTable.dataArray.Length; ++i)
		{
			if (affectorValueTable.dataArray[i].id == affectorValueId)
				return affectorValueTable.dataArray[i];
		}
		return null;
	}

	public AffectorValueLevelTableData FindAffectorValueLevelTableData(string affectorValueId, int level)
	{
		for (int i = 0; i < affectorValueLevelTable.dataArray.Length; ++i)
		{
			if (affectorValueLevelTable.dataArray[i].affectorValueId == affectorValueId && affectorValueLevelTable.dataArray[i].level == level)
				return affectorValueLevelTable.dataArray[i];
		}
		return null;
	}

	public ActorStateTableData FindActorStateTableData(string actorStateId)
	{
		for (int i = 0; i < actorStateTable.dataArray.Length; ++i)
		{
			if (actorStateTable.dataArray[i].actorStateId == actorStateId)
				return actorStateTable.dataArray[i];
		}
		return null;
	}
	
	public MonsterTableData FindMonsterTableData(string monsterId)
	{
		for (int i = 0; i < monsterTable.dataArray.Length; ++i)
		{
			if (monsterTable.dataArray[i].monsterId == monsterId)
				return monsterTable.dataArray[i];
		}
		return null;
	}

	public MonsterTableData FindMonsterTableData(int simpleId)
	{
		for (int i = 0; i < monsterTable.dataArray.Length; ++i)
		{
			if (monsterTable.dataArray[i].simpleId == simpleId)
				return monsterTable.dataArray[i];
		}
		return null;
	}

	public MonsterGroupTableData FindMonsterGroupTableData(string groupMonsterId)
	{
		for (int i = 0; i < monsterGroupTable.dataArray.Length; ++i)
		{
			if (monsterGroupTable.dataArray[i].groupMonsterId == groupMonsterId)
				return monsterGroupTable.dataArray[i];
		}
		return null;
	}

	public ActorTableData FindActorTableData(string actorId)
	{
		for (int i = 0; i < actorTable.dataArray.Length; ++i)
		{
			if (actorTable.dataArray[i].actorId == actorId)
				return actorTable.dataArray[i];
		}
		return null;
	}

	public SkillTableData FindSkillTableData(string skillId)
	{
		for (int i = 0; i < skillTable.dataArray.Length; ++i)
		{
			if (skillTable.dataArray[i].id == skillId)
				return skillTable.dataArray[i];
		}
		return null;
	}

	public SkillLevelTableData FindSkillLevelTableData(string skillId, int level)
	{
		for (int i = 0; i < skillLevelTable.dataArray.Length; ++i)
		{
			if (skillLevelTable.dataArray[i].skillId == skillId && skillLevelTable.dataArray[i].level == level)
				return skillLevelTable.dataArray[i];
		}
		return null;
	}

	public ConditionValueTableData FindConditionValueTableData(string id)
	{
		for (int i = 0; i < conditionValueTable.dataArray.Length; ++i)
		{
			if (conditionValueTable.dataArray[i].id == id)
				return conditionValueTable.dataArray[i];
		}
		return null;
	}

	public LevelPackTableData FindLevelPackTableData(string levelPackId)
	{
		for (int i = 0; i < levelPackTable.dataArray.Length; ++i)
		{
			if (levelPackTable.dataArray[i].levelPackId == levelPackId)
				return levelPackTable.dataArray[i];
		}
		return null;
	}

	public LevelPackLevelTableData FindLevelPackLevelTableData(string levelPackId, int level)
	{
		for (int i = 0; i < levelPackLevelTable.dataArray.Length; ++i)
		{
			if (levelPackLevelTable.dataArray[i].levelPackId == levelPackId && levelPackLevelTable.dataArray[i].level == level)
				return levelPackLevelTable.dataArray[i];
		}
		return null;
	}

	public float GetGlobalConstantFloat(string id)
	{
		for (int i = 0; i < globalConstantFloatTable.dataArray.Length; ++i)
		{
			if (globalConstantFloatTable.dataArray[i].id == id)
				return globalConstantFloatTable.dataArray[i].value;
		}
		return 0.0f;
	}

	public int GetGlobalConstantInt(string id)
	{
		for (int i = 0; i < globalConstantIntTable.dataArray.Length; ++i)
		{
			if (globalConstantIntTable.dataArray[i].id == id)
				return globalConstantIntTable.dataArray[i].value;
		}
		return 0;
	}

	public string GetGlobalConstantString(string id)
	{
		for (int i = 0; i < globalConstantStringTable.dataArray.Length; ++i)
		{
			if (globalConstantStringTable.dataArray[i].id == id)
				return globalConstantStringTable.dataArray[i].value;
		}
		return "";
	}

	public DropTableData FindDropTableData(string dropId)
	{
		for (int i = 0; i < dropTable.dataArray.Length; ++i)
		{
			if (dropTable.dataArray[i].dropId == dropId)
				return dropTable.dataArray[i];
		}
		return null;
	}

	public DamageRateTableData FindDamageTableData(string type, int addCount, string actorId)
	{
		for (int i = 0; i < damageRateTable.dataArray.Length; ++i)
		{
			if (damageRateTable.dataArray[i].overrideActorId == actorId && damageRateTable.dataArray[i].number == addCount && damageRateTable.dataArray[i].id == type)
				return damageRateTable.dataArray[i];
		}
		for (int i = 0; i < damageRateTable.dataArray.Length; ++i)
		{
			if (damageRateTable.dataArray[i].number == addCount && damageRateTable.dataArray[i].id == type)
				return damageRateTable.dataArray[i];
		}
		return null;
	}

	public StageIdTableData FindStageIdTableData(int floor)
	{
		for (int i = 0; i < stageIdTable.dataArray.Length; ++i)
		{
			if (stageIdTable.dataArray[i].floor == floor)
				return stageIdTable.dataArray[i];
		}
		return null;
	}

	public StageTableData FindStageTableData(int stage)
	{
		for (int i = 0; i < stageTable.dataArray.Length; ++i)
		{
			if (stageTable.dataArray[i].stage == stage)
				return stageTable.dataArray[i];
		}
		return null;
	}

	public StageBetTableData FindStageBetTableData(int floor)
	{
		for (int i = 0; i < stageBetTable.dataArray.Length; ++i)
		{
			if (stageBetTable.dataArray[i].stage == floor)
				return stageBetTable.dataArray[i];
		}
		return null;
	}

	public ActorInfoTableData FindActorInfoTableData(string actorId)
	{
		for (int i = 0; i < actorInfoTable.dataArray.Length; ++i)
		{
			if (actorInfoTable.dataArray[i].actorId == actorId)
				return actorInfoTable.dataArray[i];
		}
		return null;
	}

	public PlayerLevelTableData FindPlayerLevelTableData(int level)
	{
		for (int i = 0; i < playerLevelTable.dataArray.Length; ++i)
		{
			if (playerLevelTable.dataArray[i].playerLevel == level)
				return playerLevelTable.dataArray[i];
		}
		return null;
	}

	public CostumeTableData FindCostumeTableData(string id)
	{
		for (int i = 0; i < costumeTable.dataArray.Length; ++i)
		{
			if (costumeTable.dataArray[i].costumeId == id)
				return costumeTable.dataArray[i];
		}
		return null;
	}

	public ActorLevelTableData FindActorLevelTableData(int grade, int level)
	{
		for (int i = 0; i < actorLevelTable.dataArray.Length; ++i)
		{
			if (actorLevelTable.dataArray[i].grade == grade && actorLevelTable.dataArray[i].level == level)
				return actorLevelTable.dataArray[i];
		}
		return null;
	}

	public ActorTranscendTableData FindActorTranscendTableData(int grade, int transcend)
	{
		for (int i = 0; i < actorTranscendTable.dataArray.Length; ++i)
		{
			if (actorTranscendTable.dataArray[i].grade == grade && actorTranscendTable.dataArray[i].transcend == transcend)
				return actorTranscendTable.dataArray[i];
		}
		return null;
	}

	public SpellGradeLevelTableData FindSpellGradeLevelTableData(int grade, int star, int level)
	{
		for (int i = 0; i < spellGradeLevelTable.dataArray.Length; ++i)
		{
			if (spellGradeLevelTable.dataArray[i].grade == grade && spellGradeLevelTable.dataArray[i].star == star && spellGradeLevelTable.dataArray[i].level == level)
				return spellGradeLevelTable.dataArray[i];
		}
		return null;
	}

	public SpellTotalTableData FindSpellTotalTableData(int level)
	{
		for (int i = 0; i < spellTotalTable.dataArray.Length; ++i)
		{
			if (spellTotalTable.dataArray[i].level == level)
				return spellTotalTable.dataArray[i];
		}
		return null;
	}

	public ShopSpellTableData FindShopSpellTableDataByIndex(int index)
	{
		if (index < shopSpellTable.dataArray.Length)
			return shopSpellTable.dataArray[index];

		return null;
	}

	public ShopActorTableData FindShopActorTableDataByIndex(int index)
	{
		if (index < shopActorTable.dataArray.Length)
			return shopActorTable.dataArray[index];

		return null;
	}

	public ShopEquipTableData FindShopEquipTableDataByIndex(int index)
	{
		if (index < shopEquipTable.dataArray.Length)
			return shopEquipTable.dataArray[index];

		return null;
	}

	public PickOneSpellTableData FindPickOneSpellTableData(bool acquired, string spellId)
	{
		for (int i = 0; i < pickOneSpellTable.dataArray.Length; ++i)
		{
			if (pickOneSpellTable.dataArray[i].acquired == acquired && pickOneSpellTable.dataArray[i].spellId == spellId)
				return pickOneSpellTable.dataArray[i];
		}
		return null;
	}

	public PickOneCharacterTableData FindPickOneCharacterTableData(int acquired, string actorId)
	{
		for (int i = 0; i < pickOneCharacterTable.dataArray.Length; ++i)
		{
			if (pickOneCharacterTable.dataArray[i].acquired == acquired && pickOneCharacterTable.dataArray[i].actorId == actorId)
				return pickOneCharacterTable.dataArray[i];
		}
		return null;
	}

	public EventPointTypeTableData FindEventPointTypeTableData(string id)
	{
		for (int i = 0; i < eventPointTypeTable.dataArray.Length; ++i)
		{
			if (eventPointTypeTable.dataArray[i].eventPointId == id)
				return eventPointTypeTable.dataArray[i];
		}
		return null;
	}

	public EventPointRewardTableData FindEventPointRewardTableData(string id, int num)
	{
		for (int i = 0; i < eventPointRewardTable.dataArray.Length; ++i)
		{
			if (eventPointRewardTable.dataArray[i].eventPointId == id && eventPointRewardTable.dataArray[i].num == num)
				return eventPointRewardTable.dataArray[i];
		}
		return null;
	}

	public EventTypeTableData FindEventTypeTableData(string id)
	{
		for (int i = 0; i < eventTypeTable.dataArray.Length; ++i)
		{
			if (eventTypeTable.dataArray[i].id == id)
				return eventTypeTable.dataArray[i];
		}
		return null;
	}

	public EventRewardTableData FindEventRewardTableData(string id, int num)
	{
		for (int i = 0; i < eventRewardTable.dataArray.Length; ++i)
		{
			if (eventRewardTable.dataArray[i].id == id && eventRewardTable.dataArray[i].num == num)
				return eventRewardTable.dataArray[i];
		}
		return null;
	}

	public AnalysisTableData FindAnalysisTableData(int level)
	{
		for (int i = 0; i < analysisTable.dataArray.Length; ++i)
		{
			if (analysisTable.dataArray[i].level == level)
				return analysisTable.dataArray[i];
		}
		return null;
	}

	public AnalysisDropTableData FindAnalysisDropTableData(string id)
	{
		for (int i = 0; i < analysisDropTable.dataArray.Length; ++i)
		{
			if (analysisDropTable.dataArray[i].id == id)
				return analysisDropTable.dataArray[i];
		}
		return null;
	}

	public AnalysisBoostTableData FindAnalysisBoostTableDataByIndex(int index)
	{
		if (index < analysisBoostTable.dataArray.Length)
			return analysisBoostTable.dataArray[index];

		return null;
	}

	public AnalysisBoostTableData FindAnalysisBoostTableDataByShopProductId(string shopProductId)
	{
		for (int i = 0; i < analysisBoostTable.dataArray.Length; ++i)
		{
			if (analysisBoostTable.dataArray[i].shopProductId == shopProductId)
				return analysisBoostTable.dataArray[i];
		}
		return null;
	}

	public GuideQuestTableData FindGuideQuestTableData(int id)
	{
		for (int i = 0; i < guideQuestTable.dataArray.Length; ++i)
		{
			if (guideQuestTable.dataArray[i].id == id)
				return guideQuestTable.dataArray[i];
		}
		return null;
	}

	public SubQuestTableData FindSubQuestTableData(int type)
	{
		for (int i = 0; i < subQuestTable.dataArray.Length; ++i)
		{
			if (subQuestTable.dataArray[i].type == type)
				return subQuestTable.dataArray[i];
		}
		return null;
	}

	public SummonTypeTableData FindeSummonTypeTableData(string summonId)
	{
		for (int i = 0; i < summonTypeTable.dataArray.Length; ++i)
		{
			if (summonTypeTable.dataArray[i].gachaId == summonId)
				return summonTypeTable.dataArray[i];
		}
		return null;
	}

	public ShopProductTableData FindShopProductTableData(string productId)
	{
		for (int i = 0; i < shopProductTable.dataArray.Length; ++i)
		{
			if (shopProductTable.dataArray[i].productId == productId)
				return shopProductTable.dataArray[i];
		}
		return null;
	}

	public ShopProductTableData FindShopProductTableDataByServerItemId(string serverItemId)
	{
		for (int i = 0; i < shopProductTable.dataArray.Length; ++i)
		{
			if (shopProductTable.dataArray[i].serverItemId == serverItemId)
				return shopProductTable.dataArray[i];
		}
		return null;
	}

	public LevelPassTableData FindLevelPassTableData(int level)
	{
		for (int i = 0; i < levelPassTable.dataArray.Length; ++i)
		{
			if (levelPassTable.dataArray[i].level == level)
				return levelPassTable.dataArray[i];
		}
		return null;
	}

	public BrokenEnergyTableData FindBrokenEnergyTableData(int level)
	{
		for (int i = 0; i < brokenEnergyTable.dataArray.Length; ++i)
		{
			if (brokenEnergyTable.dataArray[i].level == level)
				return brokenEnergyTable.dataArray[i];
		}
		return null;
	}

	public ConsumeItemTableData FindConsumeItemTableData(string id)
	{
		for (int i = 0; i < consumeItemTable.dataArray.Length; ++i)
		{
			if (consumeItemTable.dataArray[i].id == id)
				return consumeItemTable.dataArray[i];
		}
		return null;
	}

	public SevenDaysTypeTableData FindSevenDaysTypeTableData(int group)
	{
		for (int i = 0; i < sevenDaysTypeTable.dataArray.Length; ++i)
		{
			if (sevenDaysTypeTable.dataArray[i].groupId == group)
				return sevenDaysTypeTable.dataArray[i];
		}
		return null;
	}

	public SevenDaysRewardTableData FindSevenDaysRewardTableData(int group, int day, int num)
	{
		for (int i = 0; i < sevenDaysRewardTable.dataArray.Length; ++i)
		{
			if (sevenDaysRewardTable.dataArray[i].group == group && sevenDaysRewardTable.dataArray[i].day == day && sevenDaysRewardTable.dataArray[i].num == num)
				return sevenDaysRewardTable.dataArray[i];
		}
		return null;
	}

	public SevenSumTableData FindSevenDaysSumTableData(int group, int count)
	{
		for (int i = 0; i < sevenSumTable.dataArray.Length; ++i)
		{
			if (sevenSumTable.dataArray[i].groupId == group && sevenSumTable.dataArray[i].count == count)
				return sevenSumTable.dataArray[i];
		}
		return null;
	}

	public FestivalTypeTableData FindFestivalTypeTableData(int group)
	{
		for (int i = 0; i < festivalTypeTable.dataArray.Length; ++i)
		{
			if (festivalTypeTable.dataArray[i].groupId == group)
				return festivalTypeTable.dataArray[i];
		}
		return null;
	}

	public FestivalExchangeTableData FindFestivalExchangeTableData(int group, int num)
	{
		for (int i = 0; i < festivalExchangeTable.dataArray.Length; ++i)
		{
			if (festivalExchangeTable.dataArray[i].groupId == group && festivalExchangeTable.dataArray[i].num == num)
				return festivalExchangeTable.dataArray[i];
		}
		return null;
	}

	public PetTableData FindPetTableData(string petId)
	{
		for (int i = 0; i < petTable.dataArray.Length; ++i)
		{
			if (petTable.dataArray[i].petId == petId)
				return petTable.dataArray[i];
		}
		return null;
	}

	public PetCountTableData FindPetCountTableData(int star, int step)
	{
		for (int i = 0; i < petCountTable.dataArray.Length; ++i)
		{
			if (petCountTable.dataArray[i].star == star && petCountTable.dataArray[i].step == step)
				return petCountTable.dataArray[i];
		}
		return null;
	}

	public PetCaptureTableData FindPetCaptureTableDataByIndex(int index)
	{
		if (index < petCaptureTable.dataArray.Length)
			return petCaptureTable.dataArray[index];

		return null;
	}

	public PetCaptureTableData FindCaptureTableDataByShopProductId(string shopProductId)
	{
		for (int i = 0; i < petCaptureTable.dataArray.Length; ++i)
		{
			if (petCaptureTable.dataArray[i].shopProductId == shopProductId)
				return petCaptureTable.dataArray[i];
		}
		return null;
	}

	public PetSaleTableData FindPetSaleTableData(int star)
	{
		for (int i = 0; i < petSaleTable.dataArray.Length; ++i)
		{
			if (petSaleTable.dataArray[i].star == star)
				return petSaleTable.dataArray[i];
		}
		return null;
	}

	public AttendanceTypeTableData FindAttendanceTypeTableData(string id)
	{
		for (int i = 0; i < attendanceTypeTable.dataArray.Length; ++i)
		{
			if (attendanceTypeTable.dataArray[i].attendanceId == id)
				return attendanceTypeTable.dataArray[i];
		}
		return null;
	}

	public AttendanceRewardTableData FindAttendanceRewardTableData(string id, int num)
	{
		for (int i = 0; i < attendanceRewardTable.dataArray.Length; ++i)
		{
			if (attendanceRewardTable.dataArray[i].attendanceId == id && attendanceRewardTable.dataArray[i].num == num)
				return attendanceRewardTable.dataArray[i];
		}
		return null;
	}

	public EquipTableData FindEquipTableData(string equipGroup)
	{
		for (int i = 0; i < equipTable.dataArray.Length; ++i)
		{
			if (equipTable.dataArray[i].equipGroup == equipGroup)
				return equipTable.dataArray[i];
		}
		return null;
	}

	public EquipLevelTableData FindEquipLevelTableData(string equipId)
	{
		for (int i = 0; i < equipLevelTable.dataArray.Length; ++i)
		{
			if (equipLevelTable.dataArray[i].equipId == equipId)
				return equipLevelTable.dataArray[i];
		}
		return null;
	}

	public EquipLevelTableData FindEquipLevelTableDataByGrade(int grade, string equipGroup)
	{
		for (int i = 0; i < equipLevelTable.dataArray.Length; ++i)
		{
			if (equipLevelTable.dataArray[i].grade == grade && equipLevelTable.dataArray[i].equipGroup == equipGroup)
				return equipLevelTable.dataArray[i];
		}
		return null;
	}

	public EquipCompositeTableData FindEquipCompositeTableData(int rarity, int grade, int enhanceLevel)
	{
		for (int i = 0; i < equipCompositeTable.dataArray.Length; ++i)
		{
			if (equipCompositeTable.dataArray[i].rarity == rarity && equipCompositeTable.dataArray[i].grade == grade && equipCompositeTable.dataArray[i].compositeLevel == enhanceLevel)
				return equipCompositeTable.dataArray[i];
		}
		return null;
	}

	public EquipGradeTableData FindEquipGradeTableData(int grade)
	{
		for (int i = 0; i < equipGradeTable.dataArray.Length; ++i)
		{
			if (equipGradeTable.dataArray[i].grade == grade)
				return equipGradeTable.dataArray[i];
		}
		return null;
	}

	public MissionModeTableData FindMissionModeTableData(int missionType, int hard)
	{
		for (int i = 0; i < missionModeTable.dataArray.Length; ++i)
		{
			if (missionModeTable.dataArray[i].missionType == missionType && missionModeTable.dataArray[i].hard == hard)
				return missionModeTable.dataArray[i];
		}
		return null;
	}

	public BossBattleTableData FindBossBattleTableData(int id)
	{
		for (int i = 0; i < bossBattleTable.dataArray.Length; ++i)
		{
			if (bossBattleTable.dataArray[i].num == id)
				return bossBattleTable.dataArray[i];
		}
		return null;
	}

	public BossBattleDifficultyTableData FindBossBattleDifficultyTableData(int difficulty)
	{
		for (int i = 0; i < bossBattleDifficultyTable.dataArray.Length; ++i)
		{
			if (bossBattleDifficultyTable.dataArray[i].difficulty == difficulty)
				return bossBattleDifficultyTable.dataArray[i];
		}
		return null;
	}

	public BossBattleRewardTableData FindBossBattleRewardTableData(int id, int difficulty)
	{
		for (int i = 0; i < bossBattleRewardTable.dataArray.Length; ++i)
		{
			if (bossBattleRewardTable.dataArray[i].num == id && bossBattleRewardTable.dataArray[i].difficulty == difficulty)
				return bossBattleRewardTable.dataArray[i];
		}
		return null;
	}

	public PointShopTableData FindPointShopTableData(int typeId, int index)
	{
		for (int i = 0; i < pointShopTable.dataArray.Length; ++i)
		{
			if (pointShopTable.dataArray[i].productId == typeId && pointShopTable.dataArray[i].index == index)
				return pointShopTable.dataArray[i];
		}
		return null;
	}

	public PointShopAtkTableData FindPointShopAtkTableData(int level)
	{
		for (int i = 0; i < pointShopAtkTable.dataArray.Length; ++i)
		{
			if (pointShopAtkTable.dataArray[i].level == level)
				return pointShopAtkTable.dataArray[i];
		}
		return null;
	}

	public RobotDefenseStepTableData FindRobotDefenseStepTableData(int step)
	{
		for (int i = 0; i < robotDefenseStepTable.dataArray.Length; ++i)
		{
			if (robotDefenseStepTable.dataArray[i].step == step)
				return robotDefenseStepTable.dataArray[i];
		}
		return null;
	}

	public DroneAtkTableData FindDroneAtkTableData(int level)
	{
		for (int i = 0; i < droneAtkTable.dataArray.Length; ++i)
		{
			if (droneAtkTable.dataArray[i].level == level)
				return droneAtkTable.dataArray[i];
		}
		return null;
	}
}
