﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class FollowingFirstShotNwayGenerator : ContinuousHitObjectGeneratorBase
{
	[Header("FollowingFirstShotNwayGenerator")]
	public int wayNum = 1;
	public float betweenAngle = 5.0f;
	public float lineInterval;

	List<Quaternion> _listFirstShotRotation = new List<Quaternion>();
	int _remainCreateCount;
	float _remainLineIntervalTime;
	float _centerAngleY;

	public override void InitializeGenerator(MeHitObject meHit, Actor parentActor, StatusBase statusBase, int hitSignalIndexInAction, int repeatIndex, int repeatAddCountByLevelPack, Transform spawnTransform)
	{
		base.InitializeGenerator(meHit, parentActor, statusBase, hitSignalIndexInAction, repeatIndex, repeatAddCountByLevelPack, spawnTransform);

		_remainCreateCount = _initializedCreateCount;
		_remainLineIntervalTime = 0.0f;
		_listFirstShotRotation.Clear();

		if (_remainCreateCount == 0)
			gameObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{
		if (CheckChangeState())
		{
			gameObject.SetActive(false);
			return;
		}

		if (_parentActor.actorStatus.IsDie())
		{
			gameObject.SetActive(false);
			return;
		}

		if (_parentActor.affectorProcessor.IsContinuousAffectorType(eAffectorType.CannotAction))
			return;

		_remainLineIntervalTime -= Time.deltaTime;
		if (_remainLineIntervalTime < 0.0f)
		{
			_remainLineIntervalTime += lineInterval;

			for (int i = 0; i < wayNum; ++i)
			{
				if (_remainCreateCount == _initializedCreateCount)
				{
					// 제일 먼저 디폴트 발사체의 방향을 계산해서 center를 구한다.
					Vector3 targetPosition = HitObject.GetTargetPosition(_signal, _parentActor, _hitSignalIndexInAction);
					Vector3 position = cachedTransform.position;
					Quaternion rotation = Quaternion.LookRotation(HitObject.GetSpawnDirection(position, cachedTransform, _signal, cachedTransform, targetPosition, _parentActor.targetingProcessor));
					_centerAngleY = rotation.eulerAngles.y;
				}
				
				if (_listFirstShotRotation.Count < wayNum)
				{
					float baseAngle = wayNum % 2 == 0 ? _centerAngleY - (betweenAngle / 2f) : _centerAngleY;
					float angle = WavingNwayGenerator.GetShiftedAngle(i, baseAngle, betweenAngle);
					_listFirstShotRotation.Add(Quaternion.Euler(0.0f, angle, 0.0f));
				}
				
				Generate(cachedTransform.position, _listFirstShotRotation[i % wayNum]);

				_remainCreateCount -= 1;
				if (_remainCreateCount <= 0)
				{
					gameObject.SetActive(false);
					break;
				}
			}
		}

		if (_remainCreateCount <= 0)
			gameObject.SetActive(false);
	}
}