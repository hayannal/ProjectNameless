using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class RobotDefenseMissionGround : MonoBehaviour
{
	public static RobotDefenseMissionGround instance;

	public GameObject spawnPointWaitEffectPrefab;
	public GameObject spawnEffectPrefab;
	public GameObject droneSpawnEffectPrefab;

	public Transform[] spawnPointTransformList;

	public Canvas worldCanvas;
	public Transform[] worldButtonTransformList;


	public Transform[] droneSpawnPointTransformList;
	public Transform[] droneWorldButtonTransformList;

	public const string HellCreeperId = "HellCreeper_Red";

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
		//Physics.IgnoreLayerCollision(Team.TEAM1_ACTOR_LAYER, Team.TEAM1_ACTOR_LAYER, false);
	}

	void OnDisable()
	{
		//Physics.IgnoreLayerCollision(Team.TEAM1_ACTOR_LAYER, Team.TEAM1_ACTOR_LAYER, true);
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
		SpawnTeamMember(index, RobotDefenseMissionCanvas.instance.currentSelectedActorId);
		RobotDefenseMissionCanvas.instance.OnSpawnActor(index);
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
		for (int i = 0; i < _listDroneSpawnPointEffectObject.Count; ++i)
			_listDroneSpawnPointEffectObject[i].SetActive(false);

		// 시작하자마자 보이지도 않는 곳에서 스폰된 몬스터에게 마법 쓰는게 이상해서 3초 딜레이를 주기로 해본다.
		SpellManager.instance.ApplyGlobalSpellCooltime(3.0f);

		// 여기서는 스펠은 안나가게 막기로 한다.
		//SpellManager.instance.InitializeEquipSpellInfo();
		StageManager.instance.SetCompleteWaitLoaded(false);
	}



	List<GameObject> _listDroneSpawnPointEffectObject = new List<GameObject>();
	public void InitializeDronePosition()
	{
		for (int i = 0; i < droneWorldButtonTransformList.Length; ++i)
			droneWorldButtonTransformList[i].position = new Vector3(droneSpawnPointTransformList[i].position.x, droneWorldButtonTransformList[i].position.y, droneSpawnPointTransformList[i].position.z);

		for (int i = 0; i < droneSpawnPointTransformList.Length; ++i)
		{
			GameObject newObject = BattleInstanceManager.instance.GetCachedObject(spawnPointWaitEffectPrefab, droneSpawnPointTransformList[i].position, Quaternion.identity);
			_listDroneSpawnPointEffectObject.Add(newObject);
		}
	}

	public void OnClickBoxDrone(int index)
	{
		if (_waitSpawn)
			return;

		Debug.LogFormat("World Canvas Button Input : {0}", index);

		_waitSpawn = true;
		SpawnDrone(index, CharacterData.s_DroneActorId);
		RobotDefenseMissionCanvas.instance.OnSpawnDrone(index);
		droneWorldButtonTransformList[index].gameObject.SetActive(false);
	}

	public int GetEmptyDroneWorldButtonIndex()
	{
		for (int i = 0; i < droneWorldButtonTransformList.Length; ++i)
		{
			if (droneWorldButtonTransformList[i].gameObject.activeSelf)
				return i;
		}
		return -1;
	}

	void SpawnDrone(int index, string actorId)
	{
		_selectedIndex = index;
		AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(actorId), "", OnLoadedDroneActor);
	}

	void OnLoadedDroneActor(GameObject prefab)
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
		playerActor.GetCollider().enabled = false;
		playerActor.playerAI.useTeamMemberAI = true;
		BattleInstanceManager.instance.OnInitializePlayerActor(playerActor, playerActor.actorId);
		OnFinishLoadedDroneActor(_selectedIndex, playerActor);
	}

	void OnFinishLoadedDroneActor(int selectedIndex, PlayerActor playerActor)
	{
		// 위치 설정
		playerActor.cachedTransform.position = droneSpawnPointTransformList[selectedIndex].position;
		_listDroneSpawnPointEffectObject[selectedIndex].SetActive(false);
		BattleInstanceManager.instance.GetCachedObject(droneSpawnEffectPrefab, droneSpawnPointTransformList[selectedIndex].position, Quaternion.identity);
		_waitSpawn = false;

		// TeamManager에 추가하지 않는다.
		//TeamManager.instance.AddTeamPlayerActorForMission(playerActor);

		// 버프 초기화도 패스
		//ChangeActorStatusAffector.Clear(playerActor.affectorProcessor);
	}



	public static bool IsOtherSide(string monsterId)
	{
		if (monsterId == HellCreeperId)
			return true;
		return false;
	}

	public Vector3 GetMonsterTargetPosition(Vector3 monsterTargetPosition)
	{
		monsterTargetPosition -= StageManager.instance.GetSafeWorldOffset();
		monsterTargetPosition.x *= -1.0f;
		monsterTargetPosition += StageManager.instance.GetSafeWorldOffset();
		return monsterTargetPosition;
	}
}