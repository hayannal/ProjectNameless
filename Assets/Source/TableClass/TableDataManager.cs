using System.Collections;
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

	public EventTypeTable eventTypeTable;
	public EventRewardTable eventRewardTable;
	public AnalysisTable analysisTable;

	public GuideQuestTable guideQuestTable;
	public GachaTypeTable gachaTypeTable;

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

	public GuideQuestTableData FindGuideQuestTableData(int id)
	{
		for (int i = 0; i < guideQuestTable.dataArray.Length; ++i)
		{
			if (guideQuestTable.dataArray[i].id == id)
				return guideQuestTable.dataArray[i];
		}
		return null;
	}
}
