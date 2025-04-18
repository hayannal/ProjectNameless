﻿//#define CHEAT_RESURRECT

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEngine.Networking;
using ActorStatusDefine;
using MecanimStateDefine;

public class ActorStatus : MonoBehaviour
{
	// 네트워크가 알아서 맞춰주는 SyncVar같은거로 동기화 하기엔 너무 위험하다. 타이밍을 재기 위해 더이상 이런식으로 하진 않는다.
	//[SyncVar(hook = "OnChangeHp")]
	//float _hp;

	// 이 statusBase가 캐싱 역할을 수행한다. UI에 표기되는 수치도 이 값을 로그화 시켜서 보여주는거다.
	StatusBase _statusBase;
	public StatusBase statusBase { get { return _statusBase; } }
	public Actor actor { get; private set; }
	public int actorLevel { get; private set; }

	public static float s_DefaultMaxHp = 3.9f;
	static float s_criticalPowerConstantA = 5.0f;
	static float s_criticalPowerConstantB = 3.0f;

	void Awake()
	{
		actor = GetComponent<Actor>();
	}

	// 로비에서 파워레벨이 바뀌든 연구소 장비가 바뀌든 이 함수 호출해주면 알아서 모든 스탯을 재계산하게 된다.
	public void InitializeActorStatus(int overrideLevel = -1)
	{
		if (_statusBase == null)
			_statusBase = new ActorStatusList();
		else
			_statusBase.ClearValue();

		if (overrideLevel == -1)
		{
			actorLevel = 1;
			if (CharacterData.s_PlayerActorId == actor.actorId)
			{
				actorLevel = PlayerData.instance.playerLevel;
			}
			else
			{
				//characterData = PlayerData.instance.GetCharacterData(actor.actorId);
				//if (characterData != null) actorLevel = characterData.powerLevel;
				// 팀원 캐릭터들도 기본 레벨은 동일한걸 받아와서 쓰기로 한다.
				actorLevel = PlayerData.instance.playerLevel;
			}
		}
		else
			actorLevel = overrideLevel;

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actor.actorId);

		_statusBase.valueList[(int)eActorStatus.MaxHp] = s_DefaultMaxHp;
		_statusBase.valueList[(int)eActorStatus.Attack] = GetPlayerBaseAttack();

		// analysis
		_statusBase.valueList[(int)eActorStatus.Attack] += AnalysisData.instance.cachedValue;

		// costume
		_statusBase.valueList[(int)eActorStatus.Attack] += CostumeManager.instance.cachedValue;

		// spell
		_statusBase.valueList[(int)eActorStatus.Attack] += SpellManager.instance.cachedValue;

		// team
		_statusBase.valueList[(int)eActorStatus.Attack] += CharacterManager.instance.cachedValue;

		// pet
		_statusBase.valueList[(int)eActorStatus.Attack] += PetManager.instance.cachedValue;

		// equip
		_statusBase.valueList[(int)eActorStatus.Attack] += EquipManager.instance.cachedValue;

		// pass
		_statusBase.valueList[(int)eActorStatus.Attack] += PassManager.instance.cachedValue;

		// bossbattle
		_statusBase.valueList[(int)eActorStatus.Attack] += SubMissionData.instance.cachedValueByBossBattle;

		// robot defense
		_statusBase.valueList[(int)eActorStatus.Attack] += SubMissionData.instance.cachedValueByRobotDefense;

		_statusBase.valueList[(int)eActorStatus.CombatPower] = _statusBase.valueList[(int)eActorStatus.Attack];
		_statusBase.valueList[(int)eActorStatus.AttackMulti] = actorTableData.multiAtk;

		_statusBase.valueList[(int)eActorStatus.AttackDelay] = actorTableData.attackDelay;
		_statusBase.valueList[(int)eActorStatus.MoveSpeed] = actorTableData.moveSpeed;
		_statusBase.valueList[(int)eActorStatus.MaxSp] = 0.0f;

		// 장비에 붙은 서브옵션들도 합산해줘야한다.
		for (int i = 0; i < _statusBase.valueList.Length; ++i)
			_statusBase.valueList[i] += EquipManager.instance.cachedEquipStatusList.valueList[i];

		// actor multi
		//_statusBase.valueList[(int)eActorStatus.MaxHp] *= actorTableData.multiHp;
		//_statusBase.valueList[(int)eActorStatus.Attack] *= actorTableData.multiAtk;

		//if (isServer)
		_statusBase._hp = _lastMaxHp = GetValue(eActorStatus.MaxHp);
		_statusBase._sp = 0.0f;

		OnChangedStatus();
	}

	public int GetPlayerBaseAttack()
	{
		int result = 0;

		PlayerLevelTableData playerLevelTableData = TableDataManager.instance.FindPlayerLevelTableData(actorLevel);
		result = playerLevelTableData.accumulatedAtk;

		int subLevel = PlayerData.instance.subLevel;
		for (int i = 1; i <= subLevel; ++i)
		{
			int addValue = 0;
			int value = (i - 1) / 9;
			switch (value)
			{
				case 0: addValue = BattleInstanceManager.instance.GetCachedGlobalConstantInt("SubLevelFightValueLine1"); break;
				case 1: addValue = BattleInstanceManager.instance.GetCachedGlobalConstantInt("SubLevelFightValueLine2"); break;
				default: addValue = BattleInstanceManager.instance.GetCachedGlobalConstantInt("SubLevelFightValueLine3"); break;
			}
			result += addValue;
		}
		return result;
	}

	public void InitializeMonsterStatus(bool eliteMonster, bool bossMonster)
	{
		if (_statusBase == null)
			_statusBase = new MonsterStatusList();
		else
			_statusBase.ClearValue();

		bool nodeWarCachingMonster = false;
		MonsterTableData monsterTableData = TableDataManager.instance.FindMonsterTableData(actor.actorId);
		float standardHp = StageManager.instance.currentMonstrStandardHp;
		float standardDef = StageManager.instance.currentMonstrStandardDef;
		float evadeRate = StageManager.instance.currentMonsterEvadeRate;
		float criticalDefenseRate = StageManager.instance.currentMonsterCriticalDefenseRate;
		float strikeDefenseRate = StageManager.instance.currentMonsterStrikeDefenseRate;
		if (actor.team.teamId == (int)Team.eTeamID.DefaultAlly)
		{
		}
		else if (RobotDefenseMissionCanvas.instance != null && RobotDefenseMissionCanvas.instance.gameObject.activeSelf)
		{
			standardHp = RobotDefenseMissionCanvas.instance.GetStandardHpByPlayTime();
			standardDef = 0.0f;
			evadeRate = criticalDefenseRate = strikeDefenseRate = 0.0f;
		}
		else if (BossBattleMissionCanvas.instance != null && BossBattleMissionCanvas.instance.gameObject.activeSelf)
		{
			BossBattleDifficultyTableData bossBattleDifficultyTableData = TableDataManager.instance.FindBossBattleDifficultyTableData(BossBattleEnterCanvas.instance.selectedDifficulty);
			if (bossBattleDifficultyTableData != null)
			{
				standardHp = bossBattleDifficultyTableData.standardHp;
				standardDef = bossBattleDifficultyTableData.standardDef;

				// 우선 보스배틀에선 오버라이드가 없으니 초기화해두기로 한다.
				evadeRate = criticalDefenseRate = strikeDefenseRate = 0.0f;
			}
		}
		else if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
		{
			/*
			NodeWarTableData nodeWarTableData = BattleManager.instance.GetSelectedNodeWarTableData();
			if (nodeWarTableData != null)
			{
				standardHp = nodeWarTableData.standardHp;
				standardAtk = nodeWarTableData.standardAtk;
				if (BattleManager.instance.IsFirstClearNodeWar() == false)
				{
					standardHp *= 0.7f;
					standardAtk *= 0.7f;
				}
			}
			else
			{
				nodeWarCachingMonster = true;
			}
			*/
		}
		_statusBase.valueList[(int)eActorStatus.MaxHp] = standardHp * monsterTableData.multiHp;
		if (StageFloorInfoCanvas.instance != null && StageFloorInfoCanvas.instance.gameObject.activeSelf)
		{
			// 이 옵션은 장비로만 올리게 되어있어서 BattleInstanceManager.instance.playerActor에서 구해오지 않고 직접 EquipManager에서 구해와서 적용하기로 한다.
			float hpDecreaseAddRate = 0.0f;
			if (bossMonster) hpDecreaseAddRate = EquipManager.instance.cachedEquipStatusList.valueList[(int)eActorStatus.BossMonsterHpDecreaseAddRate];
			else hpDecreaseAddRate = EquipManager.instance.cachedEquipStatusList.valueList[(int)eActorStatus.NormalMonsterHpDecreaseAddRate];
			if (hpDecreaseAddRate > 0.0f)
				_statusBase.valueList[(int)eActorStatus.MaxHp] *= (1.0f - hpDecreaseAddRate);
		}
		_statusBase.valueList[(int)eActorStatus.Attack] = 1.0f * monsterTableData.multiAtk;
		_statusBase.valueList[(int)eActorStatus.Defense] = standardDef;
		//_statusBase.valueList[(int)eActorStatus.AttackDelay] = monsterTableData.attackDelay;
		_statusBase.valueList[(int)eActorStatus.EvadeRate] = evadeRate;
		_statusBase.valueList[(int)eActorStatus.CriticalDefenseRate] = criticalDefenseRate;
		_statusBase.valueList[(int)eActorStatus.StrikeDefenseRate] = strikeDefenseRate;
		_statusBase.valueList[(int)eActorStatus.MoveSpeed] = monsterTableData.moveSpeed;

		if (RobotDefenseMissionCanvas.instance != null && RobotDefenseMissionCanvas.instance.gameObject.activeSelf)
		{
			_statusBase.valueList[(int)eActorStatus.MoveSpeed] *= (BattleInstanceManager.instance.GetCachedGlobalConstantInt("RobotDefenseMoveSpeedRate100") * 0.01f);
		}

		//if (isServer)
		_statusBase._hp = _lastMaxHp = GetValue(eActorStatus.MaxHp);
		if (nodeWarCachingMonster)
			_statusBase._hp = 0.0f;

		OnChangedStatus();
	}

	float _lastMaxHp = 0.0f;
	public void OnChangedStatus(eActorStatus eType = eActorStatus.ExAmount)
	{
		if (eType == eActorStatus.MoveSpeed || eType == eActorStatus.MoveSpeedAddRate || eType == eActorStatus.ExAmount)
			actor.baseCharacterController.speed = GetValue(eActorStatus.MoveSpeed);
		if (eType == eActorStatus.AttackSpeedAddRate || eType == eActorStatus.ExAmount)
			actor.actionController.OnChangedAttackSpeedAddRatio(GetValue(eActorStatus.AttackSpeedAddRate));

		// 로비에서는 장비 변경해도 만피로 유지되면서 이 함수 호출되는데 항상 ExAmount로 올거다.
		// 그래서 ExAmount로 올땐 처리하지 않고 인게임 내에서 MaxHp 관련 스탯으로 올때만 처리해주면 된다.
		if (eType == eActorStatus.MaxHp || eType == eActorStatus.MaxHpAddRate)
		{
			float maxHp = GetValue(eActorStatus.MaxHp);
			if (maxHp > _lastMaxHp)
			{
				AddHP(maxHp - _lastMaxHp);
			}
			else
			{
				if (GetHP() > maxHp)
					AddHP(maxHp - GetHP());
				else
					actor.OnChangedHP();
			}
			_lastMaxHp = maxHp;
		}
	}

	public float GetCachedValue(eActorStatus eType)
	{
		if ((int)eType >= _statusBase.valueList.Length)
			return 0.0f;

		return _statusBase.valueList[(int)eType];
	}

	public float GetValue(eActorStatus eType)
	{
		float value = 0.0f;
		if ((int)eType < _statusBase.valueList.Length)
			value += _statusBase.valueList[(int)eType];
		value += ChangeActorStatusAffector.GetValue(actor.affectorProcessor, eType);

		float addRate = 0.0f;
		switch (eType)
		{
			case eActorStatus.MaxHp:
				addRate = GetValue(eActorStatus.MaxHpAddRate);
				if (addRate != 0.0f) value *= (1.0f + addRate);
				break;
			case eActorStatus.Attack:
				addRate = GetValue(eActorStatus.AttackAddRate);
				if (addRate != 0.0f) value *= (1.0f + addRate);
				break;
			case eActorStatus.AttackDelay:
				float attackSpeedAddRate = GetValue(eActorStatus.AttackSpeedAddRate);
				if (attackSpeedAddRate != 0.0f) value /= (1.0f + attackSpeedAddRate);
				break;
			case eActorStatus.EvadeRate:
				value += PositionBuffAffector.GetEvadeAddRate(actor.affectorProcessor);
				value += OnMoveBuffAffector.GetEvadeAddRate(actor.affectorProcessor);
				break;
			case eActorStatus.MoveSpeed:
				// 0으로 고정시키면 ai에서 아예 Move애니 대신 Idle이 나오게 된다. 그래서 0보다는 큰 값으로 설정해둔다.
				if (actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction)) value = 0.00001f;
				// 0에 수렴할수록 아예 회전조차 보이지 않게 되서 랜덤무브의 방향전환이 보이지 않게 된다. 그래서 값을 좀더 높여둔다.
				if (actor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotMove)) value = 0.05f;
				addRate = GetValue(eActorStatus.MoveSpeedAddRate);
				if (addRate != 0.0f) value *= (1.0f + addRate);
				break;
			case eActorStatus.CriticalRate:
				float criticalPower1 = GetValue(eActorStatus.CriticalPower);
				if (criticalPower1 != 0.0f) value += (criticalPower1 / (criticalPower1 * s_criticalPowerConstantA / s_criticalPowerConstantB + BattleInstanceManager.instance.GetCachedGlobalConstantFloat("DefaultCriticalDamageRate")));
				break;
			case eActorStatus.CriticalDamageAddRate:
				float criticalPower2 = GetValue(eActorStatus.CriticalPower);
				if (criticalPower2 != 0.0f) value += (criticalPower2 * s_criticalPowerConstantA / s_criticalPowerConstantB);
				break;
			case eActorStatus.AttackAddRate:
				value += AddAttackByHpAffector.GetValue(actor.affectorProcessor);
				value += PositionBuffAffector.GetAttackAddRate(actor.affectorProcessor);
				value += AddAttackByContinuousKillAffector.GetValue(actor.affectorProcessor);
				value += OnMoveBuffAffector.GetAttackAddRate(actor.affectorProcessor);
				break;
			//case eActorStatus.SpGainAddRate:
			//	value += AddSpGainByHpAffector.GetValue(actor.affectorProcessor);
			//	break;
		}
		return value;
	}

	public float GetMaxHpWithoutLevelPack()
	{
		float value = GetCachedValue(eActorStatus.MaxHp);
		float addRate = GetCachedValue(eActorStatus.MaxHpAddRate);
		if (addRate != 0.0f) value *= (1.0f + addRate);
		return value;
	}

	#region Stats Point
	#endregion

	#region Wing
	#endregion

	static float LnAtkConstant1 = 123.315173118822f;
	static float LnAtkConstant2 = -282.943679363379f;
	public float GetDisplayAttack()
	{
		return GetDisplayAttack(GetValue(eActorStatus.Attack));
	}

	public static float GetDisplayAttack(float value)
	{
		float result = LnAtkConstant1 * Mathf.Log(value) + LnAtkConstant2;
		return result;
	}

	static float LnHpConstant1 = 197.304276990115f;
	static float LnHpConstant2 = -766.858870654696f;
	public float GetDisplayMaxHp()
	{
		return GetDisplayMaxHp(GetValue(eActorStatus.MaxHp));
	}

	public static float GetDisplayMaxHp(float value)
	{
		float result = LnHpConstant1 * Mathf.Log(value) + LnHpConstant2;
		return result;
	}

	public void GetNextPowerLevelDisplayValue(ref float nextAttack, ref float nextMaxHp)
	{
		InitializeActorStatus(actorLevel + 1);
		nextAttack = GetDisplayAttack();
		nextMaxHp = GetDisplayMaxHp();
		InitializeActorStatus();
	}

	public bool IsDie()
	{
		return GetHP() <= 0;
	}

	public float GetHP()
	{
		return _statusBase._hp;
	}

	public float GetSP()
	{
		return _statusBase._sp;
	}

	//void OnChangeHp(float hp)
	//{
	//	Debug.Log("OnChange HP : " + hp.ToString());
	//}


	#region For HitObject
	public void CopyStatusBase(ref StatusBase targetStatusBase)
	{
		//if (targetStatusBase == null) targetStatusBase = new StatusBase();

		int minLength = Mathf.Min(_statusBase.valueList.Length, targetStatusBase.valueList.Length);
		for (int i = 0; i < targetStatusBase.valueList.Length; ++i)
		{
			if (i < minLength)
				targetStatusBase.valueList[i] = GetValue((eActorStatus)i);
			else
				targetStatusBase.valueList[i] = 0.0f;
		}

		targetStatusBase._hp = _statusBase._hp;
		targetStatusBase._sp = _statusBase._sp;
	}
	#endregion

	/*
	// Current Status with Buff
	public virtual float GetStatus(BaseStatus.eStatus eType)
	{
		switch(eType)
		{
		case BaseStatus.eStatus.MaxHP:
			return GetCalcStatus(eType);
		case BaseStatus.eStatus.Attack:
			return GetCalcStatus(BaseStatus.eStatus.Attack) * (1.0f + GetCalcStatus(BaseStatus.eStatus.AttackRatio));
		case BaseStatus.eStatus.Defense:mf
			return GetCalcStatus(eType);

		case BaseStatus.eStatus.Critical:
		case BaseStatus.eStatus.Evade:
			return GetCalcStatus(eType);
		default:
			return GetCalcStatus(eType);
		}
		// with Buff
		//BuffAffector.CheckBuff(eType, GetCalcStatus(eType));
		//BuffAffector.CheckBuff(eType, GetCalcStatus(eType), GetCalcStatus(BaseStatus.eStatus.AttackRatio));
	}

	protected virtual float GetCalcStatus(BaseStatus.eStatus eType)
	{
		if ((int)eType < m_ActorStatus.Values.Length)
			return m_ActorStatus.Values[(int)eType];
		return 0.0f;
	}
	*/

#if CHEAT_RESURRECT
	public bool cheatDontDie { get; set; }
	public int cheatDontDieUseCount { get; private set; }
#endif
	public virtual void AddHP(float addHP)
	{
		if (addHP == 0.0f)
			return;

#if UNITY_EDITOR
		if (HUDDPS.isActive && actor.IsMonsterActor() && addHP < 0.0f)
		{
			float damage = -addHP;
			float overDamage = damage - _statusBase._hp;
			if (overDamage < 0.0f) overDamage = 0.0f;
			damage -= overDamage;
			HUDDPS.instance.AddDamage(damage, overDamage);
		}
#endif
		_statusBase._hp += addHP;
		_statusBase._hp = Mathf.Clamp(_statusBase._hp, 0, GetValue(eActorStatus.MaxHp));

		bool onDie = false;
		if (_statusBase._hp <= 0)
		{
			// 애니 중에 죽으면 정말 이상하게 보이는 상황이 있다. 공중같이.
			// 이럴때를 대비해서 Die를 무시하고 hp를 1로 복구시켜주는 DontDie 애니 시그널을 추가해둔다. 불굴의 의지로 자세(애니)를 유지.
			bool dontDie = false;
			if (actor.actionController.mecanimState.IsState((int)eMecanimState.DontDie) || ImmortalWillAffector.CheckImmortal(actor.affectorProcessor))
				dontDie = true;
#if CHEAT_RESURRECT
			if (cheatDontDie)
			{
				dontDie = true;
				cheatDontDieUseCount += 1;
				Debug.LogFormat("Cheat Resurrect Count = {0}", cheatDontDieUseCount);
			}
#endif
			if (dontDie)
			{
				_statusBase._hp = 1.0f;
			}
			else
			{
				onDie = true;
			}
#if CHEAT_RESURRECT
			if (cheatDontDie)
				_statusBase._hp = GetValue(eActorStatus.MaxHp);
#endif
		}
		actor.OnChangedHP();
		if (onDie) actor.OnDie();
	}

	public float GetHPRatio()
	{
		return GetHP() / GetValue(eActorStatus.MaxHp);
	}


	public virtual void AddSP(float addSP)
	{
		_statusBase._sp += addSP;
		_statusBase._sp = Mathf.Clamp(_statusBase._sp, 0, GetValue(eActorStatus.MaxSp));
		actor.OnChangedSP();
		if (_statusBase._sp <= 0)
			_statusBase._sp = 0.0f;
	}

	public float GetSPRatio()
	{
		return GetSP() / GetValue(eActorStatus.MaxSp);
	}


	// for Swap
	public void SetHpRatio(float hpRatio)
	{
		_statusBase._hp = GetValue(eActorStatus.MaxHp) * hpRatio;
	}

	public void SetSpRatio(float ratio)
	{
		_statusBase._sp = GetValue(eActorStatus.MaxSp) * ratio;
	}


	// for Experience
	public void ChangeExperienceMode(PlayerActor attackerPlayerActor)
	{
		float value = attackerPlayerActor.actorStatus.GetValue(eActorStatus.Attack);
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(attackerPlayerActor.actorId);
		if (actorTableData == null)
			return;
		float result = value * 1.2f / actorTableData.multiAtk;
		_statusBase._hp = _statusBase.valueList[(int)eActorStatus.MaxHp] = result;
	}
}
