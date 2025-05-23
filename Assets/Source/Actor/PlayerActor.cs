﻿//#define CHEAT_RESURRECT

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;
using MEC;

public class PlayerActor : Actor
{
	public override bool IsPlayerActor() { return true; }

	public GameObject[] cachingObjectList;
	public Transform wingRootTransform;

	public SkillProcessor skillProcessor { get; private set; }
	public PlayerAI playerAI { get; private set; }
	//public CastingProcessor castingProcessor { get; private set; }
	public float actorRadius { get; private set; }
	public bool flying { get; private set; }

	void Awake()
	{
		InitializeComponent();
	}

	bool _started;
	void Start()
	{
		actorRadius = ColliderUtil.GetRadius(_collider);
		InitializeActor();
		_started = true;
	}

	#region Swap
	void OnEnable()
	{
		if (_started)
		{
			// 이미 생성당시에 날개 생성은 모두 완료했을거다. 여기서는 전투중에 꺼놨던 캐릭을 켰을때를 대비해서 Hide옵션 체크만 해도 충분하다.
			RefreshWingHide();
			RegisterBattleInstance();
		}
	}
	#endregion

	protected override void InitializeComponent()
	{
		base.InitializeComponent();

		affectorProcessor.dontClearOnDisable = true;

		actorStatus = GetComponent<ActorStatus>();
		if (actorStatus == null) actorStatus = gameObject.AddComponent<ActorStatus>();

		skillProcessor = GetComponent<SkillProcessor>();
		if (skillProcessor == null) skillProcessor = gameObject.AddComponent<SkillProcessor>();

		playerAI = GetComponent<PlayerAI>();
		if (playerAI == null) playerAI = gameObject.AddComponent<PlayerAI>();

		//castingProcessor = GetComponent<CastingProcessor>();
		//if (castingProcessor == null) castingProcessor = gameObject.AddComponent<CastingProcessor>();
	}

	protected override void InitializeActor()
	{
		base.InitializeActor();

		team.SetTeamId((int)Team.eTeamID.DefaultAlly, true, gameObject, Team.eTeamLayer.TEAM0_ACTOR_LAYER, false);
		actorStatus.InitializeActorStatus();
		skillProcessor.InitializeSkill();

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		targetingProcessor.sphereCastRadiusForCheckWall = actorTableData.targetingSphereRadius;
		targetingProcessor.checkNavMeshReachable = actorTableData.checkNavMeshReachable;
		targetingProcessor.checkBurrow = actorTableData.checkBurrow;
		targetingProcessor.checkGhost = actorTableData.checkGhost;
		flying = actorTableData.flying;

		// 처음 캐릭을 만들땐 생성까진 해두고 Hide여부는 SetActive로 제어하기로 한다.
		RefreshWing();
		RegisterBattleInstance();
	}

	public void InitializeCanvas()
	{
		PlayerGaugeCanvas.instance.InitializeGauge(this);
	}

	void RegisterBattleInstance()
	{
		// 등록하는 로직이 달라질거 같다.
		OnChangedMainCharacter();

		/*
		string addActorId = actorId;
		BattleInstanceManager.instance.OnInitializePlayerActor(this, addActorId);

		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);

		// 메인 캐릭터를 스왑할땐 항상 standbySwapPlayerActor 플래그가 켜있으니 이거로 구분하면 된다.
		// 이땐 Hp비율부터 레벨팩 등을 이전받아야한다.
		// 로비라면 할 필요 없으니 그냥 이전 캐릭터를 끄기만 하고 넘어간다.
		if (BattleInstanceManager.instance.standbySwapPlayerActor)
		{
			if (BattleInstanceManager.instance.playerActor != null)
			{
				if (lobby == false)
				{
					if (BattleManager.instance != null && BattleManager.instance.IsNodeWar())
					{
						// 바꾼 캐릭터에도 NodeWar용 레벨팩 적용
						NodeWarProcessor.ApplyNodeWarLevelPack(this);
					}
					else if (BattleManager.instance != null && BattleManager.instance.IsDefaultBattle())
					{
						// 한번이라도 썼던 캐릭터인지 확인
						bool firstEnter = false;//!BattleInstanceManager.instance.IsInBattlePlayerList(addActorId);

						// 레벨팩 이전
						LevelPackDataManager.instance.TransferLevelPackList(BattleInstanceManager.instance.playerActor, this);

						// 액터가 비활성화 될땐 항상 attackAniSpeedRatio값이 초기화 되니 재설정을 레벨팩 복구 직후 해준다.
						actorStatus.OnChangedStatus(eActorStatus.AttackSpeedAddRate);

						// Hp비율 Sp비율 이전
						float hpRatio = BattleInstanceManager.instance.playerActor.actorStatus.GetHPRatio();
						actorStatus.SetHpRatio(hpRatio);
						float spRatio = BattleInstanceManager.instance.playerActor.actorStatus.GetSPRatio();
						actorStatus.SetSpRatio(spRatio);
#if CHEAT_RESURRECT
						bool cheatDontDie = BattleInstanceManager.instance.playerActor.actorStatus.cheatDontDie;
						actorStatus.cheatDontDie = cheatDontDie;
#endif

						// 처음 스왑이라면 힐과 sp회복 적용
						if (firstEnter)
						{
							actorStatus.SetSpRatio(1.0f);

							AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
							healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("SwapHeal");
							healAffectorValue.fValue3 += affectorProcessor.actor.actorStatus.GetValue(eActorStatus.SwapHealAddRate);
							affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, affectorProcessor.actor, false);
							BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.healEffectPrefab, cachedTransform.position, Quaternion.identity, cachedTransform);

							ClientSaveData.instance.OnChangedHpRatio(affectorProcessor.actor.actorStatus.GetHPRatio());
							ClientSaveData.instance.OnChangedSpRatio(affectorProcessor.actor.actorStatus.GetSPRatio());

							Timing.RunCoroutine(ScreenHealEffectProcess());
						}

						// 스테이지 디버프
						if (BattleInstanceManager.instance.playerActor.currentStagePenaltyTableData != null)
							RefreshStagePenaltyAffector(BattleInstanceManager.instance.playerActor.currentStagePenaltyTableData.stagePenaltyId, false);
					}
				}

				BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);
			}

			// 이전받고 나서야 메인캐릭터로 교체.
			OnChangedMainCharacter();

			BattleInstanceManager.instance.standbySwapPlayerActor = false;

			BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.playerSpawnEffectPrefab, cachedTransform.position, Quaternion.identity);
		}
		else
		{
			// 처음 만들어지는 PlayerActor는 바로 등록하고 그게 아니라면 로비에서 다른 캐릭터 보여주려는 경우일거다.
			// 이 경우가 아닌데 캐릭이 추가로 등장하는거라면 씬에다 캐릭터 끌어서 추가했을 경우 일거다. 이럴땐 Change하지 않는다.
			if (BattleInstanceManager.instance.playerActor == null)
				OnChangedMainCharacter();
		}
		*/
	}

	IEnumerator<float> ScreenHealEffectProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 0.6f);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;

		BattleToastCanvas.instance.ShowToast(UIString.instance.GetString("AfterSwapUI_Heal"), 2.3f);
		FadeCanvas.instance.FadeIn(1.3f);
	}

	public void OnChangedMainCharacter()
	{
		if (playerAI.useTeamMemberAI)
			return;

		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false && BattleManager.instance != null && BattleManager.instance.IsDefaultBattle())
		{
			/*
			BattleInstanceManager.instance.AddBattlePlayer(GetActorIdWithMercenary());
			SoundManager.instance.PlayBattleBgm(actorId);
			*/
		}

		BattleInstanceManager.instance.playerActor = this;
		CustomFollowCamera.instance.targetTransform = cachedTransform;

		if (StageFloorInfoCanvas.instance != null && StageFloorInfoCanvas.instance.gameObject.activeSelf)
			StageFloorInfoCanvas.instance.RefreshCombatPower();
	}

	public override void OnChangedHP()
	{
		base.OnChangedHP();

		// 사실은 여기서 전투중에 피 깎일때만 저장을 해야하는데 이전값을 기억하고 있지도 않고있어서
		// 차라리 PlayerGaugeCanvas에게 위임해서 처리하기로 한다.
		PlayerGaugeCanvas.instance.OnChangedHP(this);

		if (BossBattleMissionCanvas.instance != null && BossBattleMissionCanvas.instance.gameObject.activeSelf)
			BossBattleMissionCanvas.instance.OnChangedHP();
	}

	public override void OnChangedSP()
	{
	}

	public override void OnDie()
	{
		base.OnDie();

		//CharacterController cc = GetComponent<CharacterController>();
		//if (cc != null) cc.enabled = false;

		if (BossBattleMissionCanvas.instance != null && BossBattleMissionCanvas.instance.gameObject.activeSelf)
			BossBattleMissionCanvas.instance.OnDiePlayer();
	}

	public override void EnableAI(bool enable)
	{
		playerAI.enabled = enable;
	}

	public void Resurrect()
	{
		if (actorStatus.IsDie() == false)
			return;

		// 우선은 연출없이
		actionController.PlayActionByActionName("Idle");
		actionController.idleAnimator.enabled = true;
		HitObject.EnableRigidbodyAndCollider(true, _rigidbody, _collider);
		actorStatus.AddHP(actorStatus.GetValue(eActorStatus.MaxHp));
	}


	/*
	#region Ultimate Indicator
	Transform _cachedUltimateIndicatorTransform;
	void ShowUltimateIndicator(bool show)
	{
		if (show)
		{
			if (_cachedUltimateIndicatorTransform == null)
				_cachedUltimateIndicatorTransform = BattleInstanceManager.instance.GetCachedObject(CommonBattleGroup.instance.ultimateCirclePrefab, cachedTransform.position, Quaternion.identity).transform;
			if (_cachedUltimateIndicatorTransform != null)
				_cachedUltimateIndicatorTransform.gameObject.SetActive(true);
		}
		else
		{
			if (_cachedUltimateIndicatorTransform != null)
				_cachedUltimateIndicatorTransform.gameObject.SetActive(false);
		}
	}

	void UpdateUltimateIndicator()
	{
		if (_cachedUltimateIndicatorTransform == null)
			return;
		if (_cachedUltimateIndicatorTransform.gameObject.activeSelf == false)
			return;
		_cachedUltimateIndicatorTransform.position = cachedTransform.position;
	}
	#endregion
	*/

	#region Wing
	GameObject _wingObject;
	void DisableWing()
	{
		if (_wingObject != null)
		{
			if (_wingObject.activeSelf)
				_wingObject.SetActive(false);
			_wingObject = null;
		}
	}

	public void RefreshWing()
	{
		/*
		CharacterData characterData = null;
		if (mercenary)
			characterData = MercenaryData.instance.GetCharacterData(GetActorIdWithMercenary());
		else
			characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null || characterData.HasWing() == false || wingRootTransform == null)
		{
			DisableWing();
			return;
		}

		WingLookTableData wingLookTableData = null;// TableDataManager.instance.FindWingLookTableData(characterData.wingLookId);
		if (wingLookTableData == null)
		{
			DisableWing();
			return;
		}

		AddressableAssetLoadManager.GetAddressableGameObject(wingLookTableData.prefabAddress, "Wing", (prefab) =>
		{
			// 장착중인 날개는 새로운거로 교체되는 시점에 하이드 시켜준다.
			DisableWing();

			_wingObject = BattleInstanceManager.instance.GetCachedObject(prefab, wingRootTransform);

			// 로드하고 나서는 항상 Hide 체크를 한다.
			RefreshWingHide();
		});
		*/
	}

	public void RefreshWingHide()
	{
		if (_wingObject == null)
			return;
		/*
		CharacterData characterData = null;
		if (mercenary)
			characterData = MercenaryData.instance.GetCharacterData(GetActorIdWithMercenary());
		else
			characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null || characterData.HasWing() == false)
			return;
		

		if (characterData.wingHide == false)
		{
			_wingObject.SetActive(true);
			return;
		}

		// hide상태일때는 현재 위치에 따라 나눠서 처리해야한다.
		bool hide = true;
		if (CharacterListCanvas.instance != null && StackCanvas.IsInStack(CharacterListCanvas.instance.gameObject, false) && StackCanvas.IsProcessHome() == false)
			hide = false;
		_wingObject.SetActive(!hide);
		*/
	}

	/*
	const float SpRegen_Interval = 2.2f;
	float _spRegenRemainTime = 0.0f;
	bool _showed = false;
	void UpdateSpRegenOnBoss()
	{
		if (actorStatus.GetValue(eActorStatus.SpRegenOnBoss) <= 0.0f)
			return;
		if (BossMonsterGaugeCanvas.IsShow() == false)
		{
			_showed = false;
			return;
		}

		// 초기화 호출을 받을 곳이 없어서 직접 감지하기로 한다.
		if (_showed == false)
		{
			_showed = true;
			_spRegenRemainTime = SpRegen_Interval;
			return;
		}

		if (_spRegenRemainTime > 0.0f)
		{
			_spRegenRemainTime -= Time.deltaTime;
			if (_spRegenRemainTime <= 0.0f)
			{
				_spRegenRemainTime += SpRegen_Interval;
				actorStatus.AddSP(actorStatus.GetValue(eActorStatus.SpRegenOnBoss));
			}
		}
	}
	*/
	#endregion
}
