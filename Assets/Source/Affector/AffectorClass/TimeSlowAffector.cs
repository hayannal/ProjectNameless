﻿using UnityEngine;
using System.Collections;
using ActorStatusDefine;
using UnityEngine.AI;

public class TimeSlowAffector : AffectorBase
{
	AffectorValueLevelTableData _teamTimeSlowAffectorValue;
	bool _forTeamAffectorValue;

	float _remainTime;
	Cooltime _attackCooltime;

	const float DefaultAngularSpeed = 3600.0f;

	public override void ExecuteAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		if (_actor == null)
			return;
		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		// team
		_forTeamAffectorValue = (affectorValueLevelTableData.sValue1 == "2");

		if (_forTeamAffectorValue == false)
			Time.timeScale = affectorValueLevelTableData.fValue2;

		_actor.actionController.animator.updateMode = AnimatorUpdateMode.UnscaledTime;
		_actor.baseCharacterController.angularSpeed = DefaultAngularSpeed * 2.0f;

		// 다른 어펙터들과 달리 PauseCanvas를 열때 빼곤 UnscaledTime으로 처리해야해서 remainTime 형태로 구현한다.
		// 이 어펙터 말고 나머지 어펙터들은 scaledTime 으로 처리된다. 
		_remainTime = affectorValueLevelTableData.fValue1;

		// 동료도 발동시켜야한다.
		if (affectorValueLevelTableData.sValue1 == "1")
		{
			if (_teamTimeSlowAffectorValue == null)
			{
				_teamTimeSlowAffectorValue = new AffectorValueLevelTableData();
				_teamTimeSlowAffectorValue.fValue1 = affectorValueLevelTableData.fValue1;
				//_teamTimeSlowAffectorValue.fValue2 = affectorValueLevelTableData.fValue2;

				// 동료꺼는 sValue에 2를 넣어서 구분시킨다.
				_teamTimeSlowAffectorValue.sValue1 = "2";
			}

			TeamManager.instance.ExecuteAffectorValueTeamMember(eAffectorType.TimeSlow, _teamTimeSlowAffectorValue);
		}
	}

	public override void OverrideAffector(AffectorValueLevelTableData affectorValueLevelTableData, HitParameter hitParameter)
	{
		_remainTime = affectorValueLevelTableData.fValue1;
	}

	public override void UpdateAffector()
	{
		// timeScale이 0이 되었다면 PauseCanvas를 켜거나 결과창이 뜬 상태일거다. 이땐 흐르지 않게 한다.
		if (Time.timeScale > 0.0f)
		{
			_remainTime -= Time.unscaledDeltaTime;
			if (_remainTime < 0.0f)
			{
				finalized = true;
				return;
			}
		}

		if (_actor.actorStatus.IsDie())
		{
			finalized = true;
			return;
		}

		if (_attackCooltime == null)
		{
			_attackCooltime = _actor.cooltimeProcessor.GetCooltime("Attack");
			if (_attackCooltime != null)
				_attackCooltime.useUnscaledTime = true;
		}
	}

	public override void FinalizeAffector()
	{
		if (_forTeamAffectorValue == false)
			Time.timeScale = 1.0f;

		_actor.actionController.animator.updateMode = AnimatorUpdateMode.Normal;
		_actor.baseCharacterController.angularSpeed = DefaultAngularSpeed;

		if (_attackCooltime != null)
			_attackCooltime.useUnscaledTime = false;

		if (_actor.actorStatus.IsDie())
			return;
	}

	public override void DisableAffector()
	{
		// 체험모드 끝나고나서 되돌려야해서 호출
		FinalizeAffector();
	}
}