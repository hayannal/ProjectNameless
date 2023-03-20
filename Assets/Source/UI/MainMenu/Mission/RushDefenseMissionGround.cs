using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class RushDefenseMissionGround : MonoBehaviour
{
	public static RushDefenseMissionGround instance;

	public GameObject spawnPointWaitEffectPrefab;
	public GameObject spawnEffectPrefab;

	public Transform[] spawnPointTransformList;

	public Canvas worldCanvas;
	public Transform[] worldButtonTransformList;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	List<GameObject> _listSpawnPointEffectObject = new List<GameObject>();
	void OnEnable()
	{
		for (int i = 0; i < worldButtonTransformList.Length; ++i)
			worldButtonTransformList[i].position = new Vector3(spawnPointTransformList[i].position.x, worldButtonTransformList[i].position.y, spawnPointTransformList[i].transform.position.z);

		for (int i = 0; i < spawnPointTransformList.Length; ++i)
		{
			GameObject newObject = BattleInstanceManager.instance.GetCachedObject(spawnPointWaitEffectPrefab, spawnPointTransformList[i].position, Quaternion.identity);
			_listSpawnPointEffectObject.Add(newObject);
		}

		// 미션에 들어온다는건 이미 Layer가 셋팅된 상태일거다. 그냥 사용하면 된다.
		Physics.IgnoreLayerCollision(Team.TEAM1_ACTOR_LAYER, Team.TEAM1_ACTOR_LAYER, false);
		FloatingDamageTextRootCanvas.instance.ignoreDamageText = true;
	}

	void OnDisable()
	{
		Physics.IgnoreLayerCollision(Team.TEAM1_ACTOR_LAYER, Team.TEAM1_ACTOR_LAYER, true);
	}

	public void OnClickBox1()
	{
		OnClickBox(0);
	}

	public void OnClickBox2()
	{
		OnClickBox(1);
	}

	public void OnClickBox3()
	{
		OnClickBox(2);
	}

	public void OnClickBox4()
	{
		OnClickBox(3);
	}

	public void OnClickBox(int index)
	{
		if (_waitSpawn)
			return;

		Debug.LogFormat("World Canvas Button Input : {0}", index);

		_waitSpawn = true;
		SpawnTeamMember(index, RushDefenseMissionCanvas.instance.currentSelectedActorId);
		RushDefenseMissionCanvas.instance.OnSpawnActor(index);
		worldButtonTransformList[index].gameObject.SetActive(false);
	}

	public int GetEmptyWorldButtonIndex()
	{
		for (int i = 0; i < worldButtonTransformList.Length; ++i)
		{
			if (worldButtonTransformList[i].gameObject.activeSelf)
				return i;
		}
		return -1;
	}

	bool _waitSpawn = false;
	int _selectedIndex = -1;
	public void SpawnTeamMember(int index, string memberActorId)
	{
		if (memberActorId == CharacterData.s_PlayerActorId)
		{
			//SpellManager.instance.ApplyGlobalSpellCooltime(3.0f);
			BattleInstanceManager.instance.playerActor.gameObject.SetActive(true);
			OnFinishLoadedPlayerActor(index, BattleInstanceManager.instance.playerActor);
			return;
		}

		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(memberActorId);
		if (playerActor != null)
		{
			playerActor.gameObject.SetActive(true);
			OnFinishLoadedPlayerActor(index, playerActor);
		}
		else
		{
			// 캐싱
			_selectedIndex = index;
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
		playerActor.playerAI.useTeamMemberAI = true;
		BattleInstanceManager.instance.OnInitializePlayerActor(playerActor, playerActor.actorId);
		OnFinishLoadedPlayerActor(_selectedIndex, playerActor);
	}

	void OnFinishLoadedPlayerActor(int selectedIndex, PlayerActor playerActor)
	{
		// 위치 설정
		playerActor.cachedTransform.position = spawnPointTransformList[selectedIndex].position;
		TailAnimatorUpdater.UpdateAnimator(playerActor.cachedTransform, 15);
		_listSpawnPointEffectObject[selectedIndex].SetActive(false);
		BattleInstanceManager.instance.GetCachedObject(spawnEffectPrefab, spawnPointTransformList[selectedIndex].position, Quaternion.identity);
		_waitSpawn = false;

		// TeamManager에 강제 추가.
		if (playerActor.actorId != CharacterData.s_PlayerActorId)
			TeamManager.instance.AddTeamPlayerActorForMission(playerActor);

		// 여기서도 버프는 초기화 하는게 좋을거 같다.
		ChangeActorStatusAffector.Clear(playerActor.affectorProcessor);
	}

	public void OnFinishSelect()
	{
		for (int i = 0; i < _listSpawnPointEffectObject.Count; ++i)
			_listSpawnPointEffectObject[i].SetActive(false);

		// 시작하자마자 보이지도 않는 곳에서 스폰된 몬스터에게 마법 쓰는게 이상해서 3초 딜레이를 주기로 해본다.
		SpellManager.instance.ApplyGlobalSpellCooltime(3.0f);
		SpellManager.instance.InitializeEquipSpellInfo();
		StageManager.instance.SetCompleteWaitLoaded(false);
	}


	public float GetMonsterRandomSpawnOffsetX()
	{
		return Random.Range(-4.0f, 4.0f);
	}
}