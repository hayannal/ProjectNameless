using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class BossBattleMissionGround : MonoBehaviour
{
	public static BossBattleMissionGround instance;

	public GameObject spawnEffectPrefab;
	public GameObject healEffectPrefab;

	void Awake()
	{
		instance = this;
	}
	
	void OnEnable()
	{
		_spawnRemainTime = 0.1f;
	}

	float _spawnRemainTime;
	float _showPlayerGauseCanvasRemainTime;
	void Update()
	{
		if (StageManager.instance == null)
			return;
		if (StageGround.instance == null)
			return;

		if (_spawnRemainTime > 0.0f)
		{
			if (StageManager.instance.processing && StageGround.instance.processing)
				return;
			_spawnRemainTime -= Time.deltaTime;
			if (_spawnRemainTime <= 0.0f)
			{
				_spawnRemainTime = 0.0f;
				SpawnSelectedLocalPlayerActor(BossBattleEnterCanvas.instance.selectedActorId);
			}
		}

		if (_showPlayerGauseCanvasRemainTime > 0.0f)
		{
			if (BattleInstanceManager.instance.playerActor != null && BattleInstanceManager.instance.playerActor.actorId == BossBattleEnterCanvas.instance.selectedActorId)
				_showPlayerGauseCanvasRemainTime -= Time.deltaTime;
			if (_showPlayerGauseCanvasRemainTime <= 0.0f)
			{
				BattleInstanceManager.instance.playerActor.InitializeCanvas();
				_showPlayerGauseCanvasRemainTime = 0.0f;
			}
		}
	}

	Vector3 _spawnPosition;
	public void SpawnSelectedLocalPlayerActor(string memberActorId)
	{
		// 이쯤이면 스테이지 모든 로딩이 다 완료된 상태일테니
		// 플레이어의 위치를 기억해두고 이쪽에 동료를 배치하기로한다.
		_spawnPosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;

		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(memberActorId);
		if (playerActor != null)
		{
			// 이미 생성되어있던 동료면 TeamMemberAI 를 끄고 재활성화 한다.
			playerActor.playerAI.useTeamMemberAI = false;
			playerActor.gameObject.SetActive(true);
			OnFinishLoadedPlayerActor(playerActor);
		}
		else
		{
			// 캐싱
			AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(memberActorId), "", OnLoadedPlayerActor);
		}
	}

	void OnLoadedPlayerActor(GameObject prefab)
	{
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(prefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(prefab);
#endif

		PlayerActor playerActor = newObject.GetComponent<PlayerActor>();
		if (playerActor == null)
			return;

		// 여기선 특이하게 동료가 홀로 메인 캐릭터가 되어서 전투하는거라 TeamMemberAI로 설정하지 않는다.
		//playerActor.playerAI.useTeamMemberAI = true;
		BattleInstanceManager.instance.OnInitializePlayerActor(playerActor, playerActor.actorId);
		OnFinishLoadedPlayerActor(playerActor);
	}

	void OnFinishLoadedPlayerActor(PlayerActor playerActor)
	{
		// 위치 설정. 이미 본체 캐릭터에 저장되어있을테니 그거 그냥 사용하면 된다.
		playerActor.cachedTransform.position = _spawnPosition;
		TailAnimatorUpdater.UpdateAnimator(playerActor.cachedTransform, 15);
		BattleInstanceManager.instance.GetCachedObject(spawnEffectPrefab, _spawnPosition, Quaternion.identity);
		_showPlayerGauseCanvasRemainTime = 0.1f;

		// camera
		CustomFollowCamera.instance.immediatelyPlayerBaseUpdate = true;
		CustomFollowCamera.instance.followPlayerPosition = true;
		ScreenJoystick.instance.GetComponent<NonDrawingGraphic>().enabled = true;

		// spell
		SpellManager.instance.InitializeActorForSpellProcessor(playerActor);
		SpellManager.instance.InitializeEquipSpellInfo();
		StageManager.instance.SetCompleteWaitLoaded(false);
	}
}