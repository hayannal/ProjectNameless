﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ActorStatusDefine;

public class ChangeActorStatusAffector : AffectorBase
{
	AffectorValueLevelTableData _teamChangeActorStatusAffectorValue;

	float _endTime;
	eActorStatus _eType;
	float _value;
	int _onDamageRemainCount;
	GameObject _onStartEffectPrefab;
	float _effectShowTime;

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
		{
			// something else? for breakable object
			return;
		}

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// lifeTime
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);

		_eType = (eActorStatus)affectorValueLevelTableData.iValue1;
		_value = affectorValueLevelTableData.fValue2;
		_onDamageRemainCount = affectorValueLevelTableData.iValue2;

		if (!string.IsNullOrEmpty(affectorValueLevelTableData.sValue4))
			_onStartEffectPrefab = FindPreloadObject(affectorValueLevelTableData.sValue4);
		if (_onStartEffectPrefab != null)
			BattleInstanceManager.instance.GetCachedObject(_onStartEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation, (affectorValueLevelTableData.iValue3 == 1) ? _actor.cachedTransform : null);
		_effectShowTime = Time.time;

		_actor.actorStatus.OnChangedStatus(_eType);


		// 동료도 발동시켜야한다.
		if (affectorValueLevelTableData.sValue1 == "1")
		{
			if (_teamChangeActorStatusAffectorValue == null)
			{
				_teamChangeActorStatusAffectorValue = new AffectorValueLevelTableData();
				_teamChangeActorStatusAffectorValue.fValue1 = affectorValueLevelTableData.fValue1;
				// fValue3의 수치만큼 곱해서 동료에 적용시킨다.
				_teamChangeActorStatusAffectorValue.fValue2 = affectorValueLevelTableData.fValue2 * affectorValueLevelTableData.fValue3;
				_teamChangeActorStatusAffectorValue.iValue1 = affectorValueLevelTableData.iValue1;
				_teamChangeActorStatusAffectorValue.iValue2 = affectorValueLevelTableData.iValue2;
				// 동료꺼니 sValue는 복사하지 않는다.
				//_teamChangeActorStatusAffectorValue.sValue4 = affectorValueLevelTableData.sValue4;
			}

			TeamManager.instance.ExecuteAffectorValueTeamMember(eAffectorType.ChangeActorStatus, _teamChangeActorStatusAffectorValue);
		}
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_endTime = CalcEndTime(affectorValueLevelTableData.fValue1);
		_onDamageRemainCount = affectorValueLevelTableData.iValue2;

		if (_onStartEffectPrefab != null)
		{
			// 얻은지 0.25초도 안되서 또 얻는거라면 이펙트는 보여주지 않기로 한다. 너무 겹쳐서 보기 안좋다.
			if (Time.time - _effectShowTime > 0.25f)
			{
				BattleInstanceManager.instance.GetCachedObject(_onStartEffectPrefab, _actor.cachedTransform.position, _actor.cachedTransform.rotation, (affectorValueLevelTableData.iValue3 == 1) ? _actor.cachedTransform : null);
				_effectShowTime = Time.time;
			}
		}
	}

	public override void UpdateAffector()
	{
		if (CheckEndTime(_endTime) == false)
		{
			if (_actor != null)
				_actor.actorStatus.OnChangedStatus(_eType);
			return;
		}
	}

	void OnDamage()
	{
		if (_onDamageRemainCount > 0)
		{
			_onDamageRemainCount -= 1;
			if (_onDamageRemainCount == 0)
			{
				finalized = true;
				_actor.actorStatus.OnChangedStatus(_eType);
			}
		}
	}

	public static void OnDamage(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listCallAffectorValueAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.ChangeActorStatus);
		if (listCallAffectorValueAffector == null)
			return;

		for (int i = 0; i < listCallAffectorValueAffector.Count; ++i)
		{
			if (listCallAffectorValueAffector[i].finalized)
				continue;
			ChangeActorStatusAffector changeActorStatusAffector = listCallAffectorValueAffector[i] as ChangeActorStatusAffector;
			if (changeActorStatusAffector == null)
				continue;
			changeActorStatusAffector.OnDamage();
		}
	}

	float GetValue(eActorStatus eType)
	{
		if (_eType == eType)
			return _value;
		return 0.0f;
	}

	public static float GetValue(AffectorProcessor affectorProcessor, eActorStatus eType)
	{
		List<AffectorBase> listChangeActorStatusAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.ChangeActorStatus);
		if (listChangeActorStatusAffector == null)
			return 0.0f;

		float result = 0.0f;
		for (int i = 0; i < listChangeActorStatusAffector.Count; ++i)
		{
			if (listChangeActorStatusAffector[i].finalized)
				continue;
			ChangeActorStatusAffector changeActorStatusAffector = listChangeActorStatusAffector[i] as ChangeActorStatusAffector;
			if (changeActorStatusAffector == null)
				continue;
			result += changeActorStatusAffector.GetValue(eType);
		}
		return result;
	}

	// 플레이어 리셋할때 제일 크게 영향받는 어펙터가 이거라서 이거만 삭제 함수를 만들어서 써보기로 한다.
	public static void Clear(AffectorProcessor affectorProcessor)
	{
		List<AffectorBase> listChangeActorStatusAffector = affectorProcessor.GetContinuousAffectorList(eAffectorType.ChangeActorStatus);
		if (listChangeActorStatusAffector == null)
			return;
		listChangeActorStatusAffector.Clear();
	}
}