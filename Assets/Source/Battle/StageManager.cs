#define USE_MAIN_SCENE

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if USE_MAIN_SCENE
using MEC;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#else
using SubjectNerd.Utilities;
#endif
using UnityEngine.SceneManagement;
using CodeStage.AntiCheat.ObscuredTypes;
using ActorStatusDefine;
using PlayFab;

public class StageManager : MonoBehaviour
{
	public static StageManager instance;

#if USE_MAIN_SCENE
#else
	// temp code
	public GameObject defaultPlaneSceneObject;
	public GameObject defaultGroundSceneObject;
#endif

	/*
	public GameObject gatePillarPrefab;
	public GameObject bossGatePillarPrefab;
	public GameObject challengeGatePillarPrefab;
	public GameObject characterInfoGroundPrefab;
	*/

	public GameObject stageGroundPrefab;

#if USE_MAIN_SCENE
#else
	[Reorderable] public GameObject[] planePrefabList;
	[Reorderable] public GameObject[] groundPrefabList;
	[Reorderable] public GameObject[] wallPrefabList;
	[Reorderable] public GameObject[] spawnFlagPrefabList;
#endif

	public ObscuredInt playChapter { get; set; }
	public ObscuredInt playStage { get; set; }

	public bool worldOffsetState { get; set; }
	public Vector3 worldOffset = new Vector3(100.0f, 0.0f, 0.0f);
	public Vector3 GetSafeWorldOffset() { return worldOffsetState ? worldOffset : Vector3.zero; }
	public bool noNavStage { get; set; }

	public static float RepeatModeInterval = 3.0f;

	public float currentMonstrStandardHp { get; set; }
	public float currentMonstrStandardDef { get; set; }

	void Awake()
	{
		instance = this;
	}

	void Update()
	{
		UpdateSpawn();
		UpdateReuseMonster();
		UpdateFailure();
	}

	public int currentFloor { get; set; }
	public bool repeatMode { get; set; }
	public void InitializeStageFloor(int floor, bool repeat)
	{
		StageIdTableData stageIdTableData = TableDataManager.instance.FindStageIdTableData(floor);
		if (stageIdTableData == null)
			return;

		currentFloor = floor;
		repeatMode = repeat;
		InitializeStage(repeatMode ? stageIdTableData.repeat : stageIdTableData.challenge, repeatMode);
	}

	void InitializeStage(int stage, bool repeat)
	{
		StageTableData stageTableData = TableDataManager.instance.FindStageTableData(stage);
		if (stageTableData == null)
			return;

		// StageGround가 심리스 형태의 로드를 관리하기 때문에 지형 로딩에 대해서는 일임한다.
		StageGround.instance.InitializeGround(stageTableData, repeat, false);
	}

	#region Mission
	public void InitializeMissionStage(int stage)
	{
		StageTableData stageTableData = TableDataManager.instance.FindStageTableData(stage);
		if (stageTableData == null)
			return;

		repeatMode = false;

		// StageGround가 심리스 형태의 로드를 관리하기 때문에 지형 로딩에 대해서는 일임한다.
		StageGround.instance.InitializeGround(stageTableData, false, true);
	}
	#endregion

	public void OnInstantiateMap(StageTableData stageTableData, bool missionMode)
	{
		// 해당 맵의 몬스터들을 프리로드 해야한다.
		// 로드가 끝나면 스폰을 시작
		Timing.RunCoroutine(LoadMonsterProcess(stageTableData, missionMode));
	}

	bool _processing = false;
	public bool processing { get { return _processing; } }
	IEnumerator<float> LoadMonsterProcess(StageTableData stageTableData, bool missionMode)
	{
		if (_processing)
			yield break;
		_processing = true;

		// 제일 먼저 스폰 정보를 파싱
		ParseSpawnInfo(stageTableData.spawnInfo);
		currentMonstrStandardHp = stageTableData.standardHp;
		currentMonstrStandardDef = stageTableData.standardDef;
		_monsterSpawnPosition.x = stageTableData.monsterSpawnx;
		_monsterSpawnPosition.z = stageTableData.monsterSpawnz;
		_monsterSpawnPosition += GetSafeWorldOffset();
		_monsterTargetPosition.x = stageTableData.monsterTargetx;
		_monsterTargetPosition.z = stageTableData.monsterTargetz;
		_monsterTargetPosition += GetSafeWorldOffset();

		// 몬스터 로딩까지 시켜놨으면 플레이어 포지션을 수정하고
		while (BattleInstanceManager.instance.playerActor == null)
			yield return Timing.WaitForOneFrame;
		BattleInstanceManager.instance.playerActor.cachedTransform.position = new Vector3(stageTableData.playerSpawnx, 0.0f, stageTableData.playerSpawnz) + GetSafeWorldOffset();

		#region Mission
		// 미션 전투에선 플레이어도 항상 사용하는게 아니다보니 켜두진 않는다. 이럴땐 위치만 옮겨둔다.
		if (missionMode == false)
		{
			if (BattleInstanceManager.instance.playerActor.gameObject.activeSelf == false)
				BattleInstanceManager.instance.playerActor.gameObject.SetActive(true);
		}
		#endregion

		CustomFollowCamera.instance.immediatelyUpdate = true;

		// 몬스터 로딩이 완료되면
		while (IsDoneLoadedMonsterList() == false)
			yield return Timing.WaitForOneFrame;

		// 미션 전투에서는 몬스터 스폰을 전투 컨트롤러가 제어한다. 그래서 몬스터의 스폰을 바로 시키지 않고 대기하게 한다.
		if (missionMode == false)
		{
			// 스폰 준비완료를 켜둔다.
			_waitLoadedComplete = false;
		}

		_processing = false;
	}

	public void SetCompleteWaitLoaded(bool complete)
	{
		_waitLoadedComplete = complete;
	}

	public class MonsterSpawnInfoBase
	{
		public virtual bool IsGroupInfo() { return false; }
	}

	public class MonsterSpawnInfo : MonsterSpawnInfoBase
	{
		public string monsterId;
		public int monsterSimpleId;
		public GameObject monsterPrefab;
		public int count;
		public float delay;
		public int useOverridePosition;
		public float overridePositionX;
		public float overridePositionZ;
	}

	public class GroupMonsterSpawnInfo : MonsterSpawnInfoBase
	{
		public override bool IsGroupInfo() { return true; }

		public string groupId;
		public int count;
	}

	List<MonsterSpawnInfoBase> _listMonsterSpawnInfo = new List<MonsterSpawnInfoBase>();
	Dictionary<string, List<MonsterSpawnInfo>> _dicGroupMonsterSpawnInfo = new Dictionary<string, List<MonsterSpawnInfo>>();

	Vector3 _monsterSpawnPosition = Vector3.zero;
	Vector3 _monsterTargetPosition = Vector3.zero;
	public Vector3 monsterTargetPosition { get { return _monsterTargetPosition; } }
	void ParseSpawnInfo(string spawnInfo)
	{
		_listMonsterSpawnInfo.Clear();
		_dicGroupMonsterSpawnInfo.Clear();

		string[] split = spawnInfo.Split(',');
		int currentIndex = 0;
		while (currentIndex < split.Length)
		{
			string id = split[currentIndex];
			if (id.Length > 0)
			{
				bool useGroupMonsterId = (id[0] == 'g');
				if (useGroupMonsterId)
				{
					// 그룹몹 아이디일땐 뒤에꺼 하나만 더 파싱하면 된다.
					GroupMonsterSpawnInfo groupMonsterSpawnInfo = new GroupMonsterSpawnInfo();
					groupMonsterSpawnInfo.groupId = id;
					int.TryParse(split[currentIndex + 1], out groupMonsterSpawnInfo.count);
					currentIndex += 2;

					// 대신 그룹몹은 그룹몹 정보를 따로 파싱해서 들고있어야한다.
					if (_dicGroupMonsterSpawnInfo.ContainsKey(id) == false)
					{
						MonsterGroupTableData monsterGroupTableData = TableDataManager.instance.FindMonsterGroupTableData(id);
						if (monsterGroupTableData != null)
						{
							List<MonsterSpawnInfo> listInfo = new List<MonsterSpawnInfo>();
							ParseGroupMonsterSpawnInfo(monsterGroupTableData.spawnInfo, listInfo);
							_dicGroupMonsterSpawnInfo.Add(id, listInfo);
						}
					}
					_listMonsterSpawnInfo.Add(groupMonsterSpawnInfo);
				}
				else if (id == "empty")
				{
					// empty일때는 
					MonsterSpawnInfo monsterSpawnInfo = new MonsterSpawnInfo();
					monsterSpawnInfo.monsterId = id;
					float.TryParse(split[currentIndex + 1], out monsterSpawnInfo.delay);
					_listMonsterSpawnInfo.Add(monsterSpawnInfo);
					currentIndex += 2;
				}
				else
				{
					MonsterSpawnInfo monsterSpawnInfo = ParseMonsterSpawnInfo(split, ref currentIndex);
					_listMonsterSpawnInfo.Add(monsterSpawnInfo);
				}
			}
		}

		// reset info
		ResetInfo();
		_waitLoadedComplete = true;
	}

	void ParseGroupMonsterSpawnInfo(string spawnInfo, List<MonsterSpawnInfo> listInfo)
	{
		string[] split = spawnInfo.Split(',');
		int currentIndex = 0;
		while (currentIndex < split.Length)
		{
			string id = split[currentIndex];

			// 그룹몹 파싱할땐 일반몹만 있는게 아니다. 여기서도 empty 체크해야한다.
			if (id == "empty")
			{
				// empty일때는 
				MonsterSpawnInfo monsterSpawnInfo = new MonsterSpawnInfo();
				monsterSpawnInfo.monsterId = id;
				float.TryParse(split[currentIndex + 1], out monsterSpawnInfo.delay);
				listInfo.Add(monsterSpawnInfo);
				currentIndex += 2;
				continue;
			}
			else
			{
				MonsterSpawnInfo monsterSpawnInfo = ParseMonsterSpawnInfo(split, ref currentIndex);
				listInfo.Add(monsterSpawnInfo);
			}
		}
	}

	static MonsterSpawnInfo ParseMonsterSpawnInfo(string[] split, ref int currentIndex)
	{
		MonsterSpawnInfo monsterSpawnInfo = new MonsterSpawnInfo();
		int.TryParse(split[currentIndex], out monsterSpawnInfo.monsterSimpleId);
		int.TryParse(split[currentIndex + 1], out monsterSpawnInfo.count);
		float.TryParse(split[currentIndex + 2], out monsterSpawnInfo.delay);
		int.TryParse(split[currentIndex + 3], out monsterSpawnInfo.useOverridePosition);
		if (monsterSpawnInfo.useOverridePosition == 1)
		{
			float.TryParse(split[currentIndex + 4], out monsterSpawnInfo.overridePositionX);
			float.TryParse(split[currentIndex + 5], out monsterSpawnInfo.overridePositionZ);
		}

		MonsterTableData monsterTableData = TableDataManager.instance.FindMonsterTableData(monsterSpawnInfo.monsterSimpleId);
		if (monsterTableData != null)
		{
			monsterSpawnInfo.monsterId = monsterTableData.monsterId;
			AddressableAssetLoadManager.GetAddressableGameObject(monsterTableData.monsterId, "Monster", (prefab) =>
			{
				monsterSpawnInfo.monsterPrefab = prefab;
			});
		}
		currentIndex += (monsterSpawnInfo.useOverridePosition == 1) ? 6 : 4;
		return monsterSpawnInfo;
	}
	
	// 
	bool IsDoneLoadedMonsterList()
	{
		// 파싱도 안됐는데 물어보는게 말이 되나?
		if (_listMonsterSpawnInfo.Count == 0)
			return false;
		
		for (int i = 0; i < _listMonsterSpawnInfo.Count; ++i)
		{
			if (_listMonsterSpawnInfo[i].IsGroupInfo())
			{
				GroupMonsterSpawnInfo groupMonsterSpawnInfo = _listMonsterSpawnInfo[i] as GroupMonsterSpawnInfo;
				if (_dicGroupMonsterSpawnInfo.ContainsKey(groupMonsterSpawnInfo.groupId) == false)
					return false;
				List<MonsterSpawnInfo> listInfo = _dicGroupMonsterSpawnInfo[groupMonsterSpawnInfo.groupId];
				for (int j = 0; j < listInfo.Count; ++j)
				{
					if (listInfo[j].monsterSimpleId == 0 && listInfo[j].monsterId == "empty" && listInfo[j].monsterPrefab == null)
						continue;
					if (listInfo[j].monsterPrefab == null)
						return false;
				}
			}
			else
			{
				MonsterSpawnInfo monsterSpawnInfo = _listMonsterSpawnInfo[i] as MonsterSpawnInfo;
				if (monsterSpawnInfo.monsterSimpleId == 0 && monsterSpawnInfo.monsterId == "empty" && monsterSpawnInfo.monsterPrefab == null)
					continue;
				if (monsterSpawnInfo.monsterPrefab == null)
					return false;
			}
		}
		return true;
	}

	void ResetInfo()
	{
		_currentSpawnIndex = 0;
		_currentSpawnIndexInGroup = 0;
		_currentSpawnCount = 0;
		_currentGroupCount = 0;
		_remainDelayTime = 0.0f;
		_spawnFinished = false;
	}

	bool _waitLoadedComplete;
	int _currentSpawnIndex;
	int _currentSpawnIndexInGroup;
	int _currentSpawnCount;
	int _currentGroupCount;
	float _remainDelayTime;
	bool _spawnFinished;
	void UpdateSpawn()
	{
		// 씬구축이 다 끝나고 화면 페이드까지 끝난다음에 몬스터가 나오는게 더 자연스러워보인다.
		if (LoadingCanvas.instance != null && LoadingCanvas.instance.gameObject.activeSelf)
			return;

		if (_waitLoadedComplete)
			return;
		if (_spawnFinished)
			return;
		if (_listMonsterSpawnInfo.Count == 0)
			return;

		_remainDelayTime -= Time.deltaTime;
		if (_remainDelayTime >= 0.0f)
			return;

		// 리스트에 있는거대로 쭉 돌리면 된다.
		bool nextStep = false;
		MonsterSpawnInfoBase infoBase = _listMonsterSpawnInfo[_currentSpawnIndex];
		if (infoBase.IsGroupInfo())
		{
			GroupMonsterSpawnInfo groupMonsterSpawnInfo = infoBase as GroupMonsterSpawnInfo;

			// 그룹일땐 그룹 정보 구해다가
			if (_dicGroupMonsterSpawnInfo.ContainsKey(groupMonsterSpawnInfo.groupId))
			{
				List<MonsterSpawnInfo> listGroupInfo = _dicGroupMonsterSpawnInfo[groupMonsterSpawnInfo.groupId];
				MonsterSpawnInfo monsterSpawnInfo = listGroupInfo[_currentSpawnIndexInGroup];
				_remainDelayTime += monsterSpawnInfo.delay;
				bool nextInGroup = false;
				if (monsterSpawnInfo.monsterSimpleId == 0 && monsterSpawnInfo.monsterPrefab == null)
				{
					++_currentSpawnIndexInGroup;
					nextInGroup = true;
				}
				else
				{
					SpawnMonster(monsterSpawnInfo, _monsterSpawnPosition, cachedTransform);
					++_currentSpawnCount;
					if (_currentSpawnCount >= monsterSpawnInfo.count)
					{
						++_currentSpawnIndexInGroup;
						nextInGroup = true;
						_currentSpawnCount = 0;
					}
				}
					
				if (nextInGroup && _currentSpawnIndexInGroup >= listGroupInfo.Count)
				{
					_currentSpawnIndexInGroup = 0;

					// 그룹이 1회 마무리된거다. 그룹의 카운트를 올려두고 이게 다 차면 다음 스텝으로 넘어가게 해야한다.
					++_currentGroupCount;
					if (_currentGroupCount >= groupMonsterSpawnInfo.count)
					{
						_currentGroupCount = 0;
						nextStep = true;
					}
				}
			}
		}
		else
		{
			MonsterSpawnInfo monsterSpawnInfo = infoBase as MonsterSpawnInfo;
			_remainDelayTime += monsterSpawnInfo.delay;
			if (monsterSpawnInfo.monsterSimpleId == 0 && monsterSpawnInfo.monsterPrefab == null)
			{
				nextStep = true;
			}
			else
			{
				SpawnMonster(monsterSpawnInfo, _monsterSpawnPosition, cachedTransform);
				++_currentSpawnCount;
				if (_currentSpawnCount >= monsterSpawnInfo.count)
				{
					_currentSpawnCount = 0;
					nextStep = true;
				}
			}
		}

		if (nextStep)
		{
			++_currentSpawnIndex;
			if (_currentSpawnIndex >= _listMonsterSpawnInfo.Count)
			{
				_currentSpawnIndex = 0;
				_spawnFinished = true;
			}
		}
	}

	void SpawnMonster(MonsterSpawnInfo monsterSpawnInfo, Vector3 spawnPosition, Transform parentTransform)
	{
		if (monsterSpawnInfo.useOverridePosition == 1)
		{
			spawnPosition.x = monsterSpawnInfo.overridePositionX;
			spawnPosition.z = monsterSpawnInfo.overridePositionZ;
			spawnPosition += GetSafeWorldOffset();
		}
		#region Mission
		if (RushDefenseMissionGround.instance != null && RushDefenseMissionGround.instance.gameObject.activeSelf)
			spawnPosition.x += RushDefenseMissionGround.instance.GetMonsterRandomSpawnOffsetX();
		#endregion
		GameObject newObject = BattleInstanceManager.instance.GetCachedObject(monsterSpawnInfo.monsterPrefab, spawnPosition + new Vector3(Random.value * 0.01f, 0.0f, Random.value * 0.01f), Quaternion.LookRotation(Vector3.back), parentTransform);
		MonsterActor monsterActor = newObject.GetComponent<MonsterActor>();
		if (monsterActor != null)
			monsterActor.checkOverlapPositionFrameCount = 100;
	}


	public void FinalizeStage()
	{
		ResetInfo();

		// 생성했던거 반대로 해야한다. 강제로 Finish처리해서 더이상 스폰되지 않게 막아둔다.
		_spawnFinished = true;
		_waitLoadedComplete = false;

		_listMonsterSpawnInfo.Clear();
		_dicGroupMonsterSpawnInfo.Clear();

		// 이미 생성한 것들도 클리어 시켜야한다.
		// 몬스터
		List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
		for (int i = listMonsterActor.Count - 1; i >= 0; --i)
		{
			// 아군 몬스터나 excludeMonsterCount 켜있는 소환체는 거리에 따라 삭제하지 않기로 한다.
			//if (listMonsterActor[i].team.teamId != (int)Team.eTeamID.DefaultMonster || listMonsterActor[i].excludeMonsterCount)
			//	continue;
			MonsterActor monsterActor = listMonsterActor[i];
			monsterActor.actorStatus.SetHpRatio(0.0f);
			monsterActor.DisableForNodeWar();
			monsterActor.gameObject.SetActive(false);
		}

		// 별도의 몬스터 HP 게이지를 쓰는 보스만 따로 처리
		BossMonsterGaugeCanvas.Hide();

		// 발사체
		BattleInstanceManager.instance.FinalizeAllHitObject();

		// 이펙트
		BattleInstanceManager.instance.FinalizeAllManagedEffectObject();

		// 이렇게 하면 히트오브젝트 뿐만 아니라 DieProcess 처리중인 몬스터도 사라지게 된다.
		// 예상하지 못한 문제들이 발생할거 같아서 이거로 하지 않고 좌우좌우 번갈아가면서 맵을 만들어내기로 한다.
		//BattleInstanceManager.instance.DisableAllCachedObject();

		// 스테이지
		StageGround.instance.FinalizeGround();

		// Path
		BattleInstanceManager.instance.FinalizePathFinderAgent();

		// 플레이어
		BattleInstanceManager.instance.playerActor.gameObject.SetActive(false);

		// 여기서 전환
		worldOffsetState ^= true;
	}

	public void OnDieMonster()
	{
		if (_failureProcessed)
			return;
		if (_spawnFinished == false)
			return;
		List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
		if (listMonsterActor.Count > 0)
			return;

		if (repeatMode)
		{
			BattleInstanceManager.instance.FinalizePathFinderAgent();
			ResetInfo();
			_remainDelayTime = RepeatModeInterval;
		}
		else
		{
			// 상황에 맞는 처리를 해야한다.
			if (CheatingListener.detectedCheatTable)
				return;

			#region Mission
			if (RushDefenseMissionCanvas.instance != null && RushDefenseMissionCanvas.instance.gameObject.activeSelf)
			{
				RushDefenseMissionCanvas.instance.ClearMission();
				return;
			}
			if (BossDefenseMissionCanvas.instance != null && BossDefenseMissionCanvas.instance.gameObject.activeSelf)
			{
				BossDefenseMissionCanvas.instance.ClearMission();
				return;
			}
			if (BossBattleMissionCanvas.instance != null && BossBattleMissionCanvas.instance.gameObject.activeSelf)
			{
				BossBattleMissionCanvas.instance.ClearMission();
				return;
			}
			#endregion

			int prevHighestClearStage = PlayerData.instance.highestClearStage;
			PlayFabApiManager.instance.RequestEndBoss(PlayerData.instance.selectedStage, currentFloor, () =>
			{
				int highestCharacterLevel = PlayerData.instance.highestClearStage;
				if (highestCharacterLevel > prevHighestClearStage)
					GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.ClearStage, highestCharacterLevel - prevHighestClearStage);

				// 패킷 통과하면 다음 처리로 넘어간다.
				Timing.RunCoroutine(ClearProcess());
			});
		}
	}

	IEnumerator<float> ClearProcess()
	{
		UIInstanceManager.instance.ShowCanvasAsync("VictoryResultCanvas", null);
		//ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_Clear"), 1.5f);

		yield return Timing.WaitForSeconds(1.0f);

		if (this == null)
			yield break;

		int changeStage = PlayerData.instance.selectedStage;

		// 맥스 스테이지 바로 아래 스테이지를 클리어하고 selected로 맥스스테이지에 도달하면 반복모드만 남게되는 형태다.
		// 이땐 로비로 돌아가면서 메세지박스를 띄운다.
		bool returnToLobby = (changeStage == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxStage"));

		if (returnToLobby)
		{
			// 로비로 돌아갈때는 아무것도 하지 않아도 될듯.
		}
		else
		{
			// fastBossClear라면 건너뛰기 적용. 이미 하나 올려져있는 상태니 1빼고 적용하면 된다.
			if (fastBossClear)
				changeStage += (BattleInstanceManager.instance.GetCachedGlobalConstantInt("FastClearJumpStep") - 1);

			// 클리어는 MaxStage - 1까지 가능하다.
			if (changeStage > (BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxStage") - 1))
				changeStage = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxStage") - 1);
		}

		BossStageNumberCanvas.instance.SetNextStage(changeStage);

		yield return Timing.WaitForSeconds(1.0f);

		if (this == null)
			yield break;

		// fastClear 활성화 중이라면 패킷 처리하고나서 selectedStage 표시를 이 타이밍에 갱신하는게 제일 적당해보인다.
		if (MainCanvas.instance != null && MainCanvas.instance.fastBossClearObject.activeSelf)
			MainCanvas.instance.fastBossClearCurrentStageValueText.text = PlayerData.instance.selectedStage.ToString("N0");

		MainCanvas.instance.ChangeStage(changeStage, returnToLobby);
	}

	void UpdateReuseMonster()
	{
		if (repeatMode == false)
			return;
		List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
		if (listMonsterActor.Count == 0)
			return;

		for (int i = listMonsterActor.Count - 1; i >= 0; --i)
		{
			Vector3 position = listMonsterActor[i].cachedTransform.position;
			float deltaZ = monsterTargetPosition.z - position.z;
			//(listMonsterActor[i].pathFinderController.agent.hasPath && listMonsterActor[i].pathFinderController.agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete)
			if (Mathf.Abs(deltaZ) < 1.0f)
			{
				MonsterActor monsterActor = listMonsterActor[i];
				monsterActor.actorStatus.SetHpRatio(0.0f);
				monsterActor.DisableForNodeWar();
				// Die 호출을 해서 마지막 시점을 알려야한다.
				OnDieMonster();
				monsterActor.gameObject.SetActive(false);
			}
		}
	}

	bool _failureProcessed = false;
	void UpdateFailure()
	{
		if (_failureProcessed)
			return;
		if (repeatMode)
			return;
		#region Mission
		if (BossBattleMissionCanvas.instance != null && BossBattleMissionCanvas.instance.gameObject.activeSelf)
			return;
		#endregion
		List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
		if (listMonsterActor.Count == 0)
			return;

		for (int i = listMonsterActor.Count - 1; i >= 0; --i)
		{
			Vector3 position = listMonsterActor[i].cachedTransform.position;
			float deltaZ = StageGround.instance.endLinePosition.z - position.z;
			if (deltaZ < 1.0f)
				continue;

			#region Mission
			if (RushDefenseMissionCanvas.instance != null && RushDefenseMissionCanvas.instance.gameObject.activeSelf)
			{
				StartCoroutine(MissionFailureProcess());
				return;
			}
			if (BossDefenseMissionCanvas.instance != null && BossDefenseMissionCanvas.instance.gameObject.activeSelf)
			{
				StartCoroutine(MissionFailureProcess());
				return;
			}
			#endregion
			StartCoroutine(FailureProcess());
			return;
		}
	}

	IEnumerator FailureProcess()
	{
		_failureProcessed = true;

		PlayFabApiManager.instance.RequestCancelBoss();
		Time.timeScale = 0.01f;

		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_BossFailure"), 2.0f);
		yield return new WaitForSecondsRealtime(1.7f);

		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true, true);
		yield return new WaitForSecondsRealtime(0.2f);

		FinalizeStage();
		TeamManager.instance.HideForMoveMap(true);

		yield return new WaitForSecondsRealtime(0.1f);
		
		if (MainCanvas.instance != null)
		{
			OnOffFastBossClear(false);
			MainCanvas.instance.challengeButtonObject.SetActive(true);
			MainCanvas.instance.bossBattleMenuRootObject.SetActive(false);
		}
		InitializeStageFloor(PlayerData.instance.selectedStage, true);
		TeamManager.instance.HideForMoveMap(false);
		SoundManager.instance.PlayLobbyBgm();
		if (EquipSkillSlotCanvas.instance != null && EquipSkillSlotCanvas.instance.gameObject.activeSelf)
			EquipSkillSlotCanvas.instance.gameObject.SetActive(false);
		FadeCanvas.instance.FadeIn(0.5f, true);

		// restore
		Screen.sleepTimeout = SleepTimeout.SystemSetting;

		if (MainCanvas.instance != null)
			MainCanvas.instance.OnPointerDown(null);

		Time.timeScale = 1.0f;
		_failureProcessed = false;

		yield return new WaitForSecondsRealtime(0.5f);
		CashShopData.instance.CheckStartEvent(CashShopData.eEventStartCondition.BossStageFailed);
	}

	public bool fastBossClear { get; private set; }
	public void OnOffFastBossClear(bool on)
	{
		fastBossClear = on;
		if (MainCanvas.instance != null && MainCanvas.instance.bossBattleMenuRootObject.activeSelf)
		{
			MainCanvas.instance.fastBossClearObject.SetActive(on);
			if (on)
				MainCanvas.instance.fastBossClearCurrentStageValueText.text = PlayerData.instance.selectedStage.ToString("N0");
		}
	}

	#region Mission
	// Fail쪽은 간단하니 여기 모아두기로 한다.
	IEnumerator MissionFailureProcess()
	{
		_failureProcessed = true;
		Time.timeScale = 0.01f;

		ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_BossFailure"), 2.0f);
		yield return new WaitForSecondsRealtime(1.7f);

		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true, true);
		yield return new WaitForSecondsRealtime(0.2f);

		// restore
		Screen.sleepTimeout = SleepTimeout.SystemSetting;
		Time.timeScale = 1.0f;
		_failureProcessed = false;

		SubMissionData.instance.readyToReopenMissionListCanvas = true;
		SceneManager.LoadScene(0);
	}
	#endregion

	/*

#if USE_MAIN_SCENE
	public void InitializeStage(int chapter, int stage)
	{
		playChapter = chapter;
		playStage = stage;

		// 씬의 시작에서 1챕터를 켜려고 할때 번들을 안받은 상태라면 로드가 실패할거다.
		// 그러니 0챕터를 대신 부르고 다운로드 대기 모드로 동작하게 처리한다.
		// 이땐 CalcNextStageInfo도 할 필요 없고 그냥 로비만 로딩한 후 못넘어가게 처리하면 된다.
		if (PlayerData.instance.lobbyDownloadState)
		{
			StageDataManager.instance.nextStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(0, 0, false);
			StageDataManager.instance.reservedNextMap = StageDataManager.instance.nextStageTableData.overridingMap;
			nextMapTableData = BattleInstanceManager.instance.GetCachedMapTableData(StageDataManager.instance.reservedNextMap);
			PrepareNextMap(nextMapTableData, StageDataManager.instance.nextStageTableData.environmentSetting);
			return;
		}

		if (MainSceneBuilder.s_buildReturnScrollUsedScene)
		{
			StageDataManager.instance.SetCachedMapData(ClientSaveData.instance.GetCachedMapData());
		}

		GetStageInfo(playChapter, playStage);
	}

	void GetStageInfo(int chapter, int stage)
	{
		StageDataManager.instance.CalcNextStageInfo(chapter, stage, PlayerData.instance.highestPlayChapter, PlayerData.instance.highestClearStage);

		if (StageDataManager.instance.existNextStageInfo)
		{
			nextMapTableData = BattleInstanceManager.instance.GetCachedMapTableData(StageDataManager.instance.reservedNextMap);
			if (nextMapTableData != null)
				PrepareNextMap(nextMapTableData, StageDataManager.instance.nextStageTableData.environmentSetting);
		}
	}

	// for switch challenge mode
	public void ChangeChallengeMode()
	{
		// 1. StageDataManager를 삭제
		// 2. StageDataManager.instance.CalcNextStageInfo 로 재계산. 이미 로비에 진입해있으니 0부터 할필요 없이 1부터 계산해서 새로운 맵을 로딩해둔다.
		// 이렇게 해두고나서 치고 들어가면 도전모드가 진행된다.
		StageDataManager.DestroyInstance();
		_handleNextPlanePrefab = _handleNextGroundPrefab = _handleNextWallPrefab = _handleNextSpawnFlagPrefab = _handleNextPortalFlagPrefab = _handleEnvironmentSettingPrefab = null;
		GetStageInfo(playChapter, playStage + 1);

		// 도전모드로 바뀔때 진행중이던 서브퀘가 있었다면
		if (QuestData.instance.currentQuestStep == QuestData.eQuestStep.Proceeding)
		{
			SubQuestInfo.instance.gameObject.SetActive(false);
			SubQuestInfo.instance.gameObject.SetActive(true);
		}
	}

	// for in progress game
	bool _reloadInProgressGame = false;
	public void ReloadStage(int targetStage)
	{
		// ClientSaveData의 IsLoadingInProgressGame함수는 EnterGame 응답받고나서 사용할 수 있는 함수라..
		// Reload임을 체크할 또 다른 변수하나가 필요해졌다.
		// 그래서 플래그 하나 추가해둔다.
		_reloadInProgressGame = true;
		_handleNextPlanePrefab = _handleNextGroundPrefab = _handleNextWallPrefab = _handleNextSpawnFlagPrefab = _handleNextPortalFlagPrefab = _handleEnvironmentSettingPrefab = null;
		//StageManager.instance.playChapter = playChapter;
		StageManager.instance.playStage = targetStage - 1;
		StageManager.instance.GetNextStageInfo();
		_reloadInProgressGame = false;
	}

	// for boss battle
	public void ReloadBossBattle(StageTableData bossStageTableData, MapTableData bossMapTableData)
	{
		_handleNextPlanePrefab = _handleNextGroundPrefab = _handleNextWallPrefab = _handleNextSpawnFlagPrefab = _handleNextPortalFlagPrefab = _handleEnvironmentSettingPrefab = null;

		// 보스전만큼은 랜덤 조명을 써보기 위해서 별도로 셋팅된 카오스 1챕터꺼를 쓰기로 한다.
		string[] environmentSettingList = null;
		StageTableData bossEnvStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(1, 1, true);
		if (bossEnvStageTableData != null)
			environmentSettingList = bossEnvStageTableData.environmentSetting;

		PrepareNextMap(bossMapTableData, environmentSettingList);
	}

	public void MoveToBossBattle(StageTableData bossStageTableData, MapTableData bossMapTableData, int difficulty)
	{
		// 맵을 로드할때는 보스 등장시점의 스테이지 테이블을 사용하고
		_currentStageTableData = bossStageTableData;
		StageDataManager.instance.nextStageTableData = null;
		InstantiateMap(bossMapTableData);

		// 7챕터같은 경우에는 스테이지가 50개 있지 않고 일부만 있으니 재연결 시켜줘야한다.
		int stage = bossStageTableData.stage;
		switch (bossStageTableData.stage)
		{
			case 1:
				stage = 10;
				break;
			case 2:
				stage = 20;
				break;
			case 3:
				stage = 30;
				break;
			case 4:
				stage = 40;
				break;
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
				stage = 50;
				break;
		}

		// 맵을 만들고나서 Difficulty에 따라서 챕터 난이도를 높여야한다.
		// 인자로 오는 Difficulty가 곧 실제 Difficulty니 chapter 자리에 넣으면 된다.
		// 그냥 스테이지를 부르니 중간보스들이 너무 약해지는 경향이 있는거 마지막 보스를 제외하곤 40을 불러본다.
		if (stage == 50) stage = 50;
		else stage = 40;
		StageTableData statBossStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(difficulty, stage, true);
		if (statBossStageTableData == null)
			return;
		_currentStageTableData = statBossStageTableData;
	}

	public void MoveToInvasion(StageTableData invasionStageTableData, MapTableData invasionMapTableData)
	{
		_currentStageTableData = invasionStageTableData;
		StageDataManager.instance.nextStageTableData = null;
		InstantiateMap(invasionMapTableData, true);
	}

	public void InstantiateInvasionSpawnFlag()
	{
		// 이미 위 호출로 로드되어있을거다. 아래 라인들만 실행하면 끝
		_currentWallObject = BattleInstanceManager.instance.GetCachedObject(_handleNextWallPrefab.Result, Vector3.zero, Quaternion.identity);

		if (_currentSpawnFlagObject != null)
			_currentSpawnFlagObject.SetActive(false);
		_currentSpawnFlagObject = Instantiate<GameObject>(_handleNextSpawnFlagPrefab.Result);
	}
#else
	void Start()
	{

		// temp code
		_currentPlaneObject = defaultPlaneSceneObject;
		_currentGroundObject = defaultGroundSceneObject;
		BattleInstanceManager.instance.GetCachedObject(gatePillarPrefab, new Vector3(3.0f, 0.0f, 1.0f), Quaternion.identity);

		// 차후에는 챕터의 0스테이지에서 시작하게 될텐데 0스테이지에서 쓸 맵을 알아내려면
		// 진입전에 아래 함수를 수행해서 캐싱할 수 있어야한다.
		// 방법은 세가지인데,
		// 1. static으로 빼서 데이터 처리만 먼저 할 수 있게 하는 방법
		// 2. DataManager 를 분리해서 데이터만 처리할 수 있게 하는 방법
		// 3. 스테이지 매니저가 언제나 살아있는 싱글톤 클래스가 되는 방법
		// 3은 다른 리소스도 들고있는데 살려둘 순 없으니 패스고 1은 너무 어거지다.
		// 결국 재부팅시 데이터 캐싱등의 처리까지 하려면 2번이 제일 낫다.
		bool result = StageDataManager.instance.CalcNextStageInfo(playChapter, playStage, lastClearChapter, lastClearStage);
		if (result)
		{
			string startMap = StageDataManager.instance.reservedNextMap;
			Debug.Log(startMap);
		}

		// get next stage info		
		GetNextStageInfo();
	}
#endif

	public void GetNextStageInfo()
	{
		if (playStage == GetCurrentMaxStage())
		{
			// last stage
			return;
		}
		if (PlayerData.instance.lobbyDownloadState)
			return;

		int nextStage = playStage + 1;
		GetStageInfo(playChapter, nextStage);
	}

	// 이건 9층 클리어 후 10층 보스가 나옴을 알리기 위해 빨간색 게이트 필라를 띄우는데 필요.
	public MapTableData nextMapTableData { get; private set; }

	// 이건 10층 클리어 후 20층 보스의 정보를 알기 위해 필요.
	public MapTableData nextBossMapTableData
	{
		get
		{
			int maxStage = GetCurrentMaxStage();
			for (int i = playStage + 1; i <= maxStage; ++i)
			{
				string reservedMap = StageDataManager.instance.GetCachedMap(i);
				if (reservedMap == "")
					continue;
				MapTableData mapTableData = BattleInstanceManager.instance.GetCachedMapTableData(reservedMap);
				if (mapTableData == null)
					continue;
				if (string.IsNullOrEmpty(mapTableData.bossName))
					continue;
				return mapTableData;
			}
			return null;
		}
	}

	public int GetMaxStage(int chapter, bool chaos)
	{
		if (chaos)
			return TableDataManager.instance.FindChapterTableData(chapter).maxChaosStage;
		return TableDataManager.instance.FindChapterTableData(chapter).maxStage;
	}

	public int GetCurrentMaxStage()
	{
		return GetMaxStage(playChapter, PlayerData.instance.currentChaosMode);
	}

	public float currentMonstrStandardHp { get { return 1.0f; } }// _currentStageTableData.standardHp; } }
	public float currentMonstrStandardAtk { get { return 1.0f; } }// _currentStageTableData.standardAtk; } }
	public float currentBossHpPer1Line { get { return 1.0f; } }// _currentStageTableData.standardHp * currentMapBossHpRatioPer1Line; } }
	public bool bossStage { get { return currentMapBossHpRatioPer1Line != 0.0f; } }
	public int addDropExp { get; private set; }

	StageTableData _currentStageTableData = null;
	public StageTableData currentStageTableData { get { return _currentStageTableData; } set { _currentStageTableData = value; } }
	public void MoveToNextStage(bool ignorePlus = false)
	{
		if (StageDataManager.instance.existNextStageInfo == false)
			return;

		if (ignorePlus == false)
			playStage += 1;

		_currentStageTableData = StageDataManager.instance.nextStageTableData;
		StageDataManager.instance.nextStageTableData = null;

		string currentMap = StageDataManager.instance.reservedNextMap;
		StageDataManager.instance.reservedNextMap = "";
		//Debug.LogFormat("CurrentMap = {0}", currentMap);

		//StageTestCanvas.instance.RefreshCurrentMapText(playChapter, playStage, currentMap);

		MapTableData mapTableData = BattleInstanceManager.instance.GetCachedMapTableData(currentMap);
		if (mapTableData != null)
		{
			addDropExp = mapTableData.dropExpAdd;

			if (BattleManager.instance != null)
				BattleManager.instance.OnPreInstantiateMap();
			
			InstantiateMap(mapTableData);
		}

		GetNextStageInfo();
	}

#if USE_MAIN_SCENE
	AsyncOperationGameObjectResult _handleNextPlanePrefab;
	AsyncOperationGameObjectResult _handleNextGroundPrefab;
	AsyncOperationGameObjectResult _handleNextWallPrefab;
	AsyncOperationGameObjectResult _handleNextSpawnFlagPrefab;
	AsyncOperationGameObjectResult _handleNextPortalFlagPrefab;
	AsyncOperationGameObjectResult _handleEnvironmentSettingPrefab;
	string _environmentSettingAddress;
	string _lastEnvironmentSettingAddress;
	void PrepareNextMap(MapTableData mapTableData, string[] environmentSettingList)
	{
		_handleNextPlanePrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.plane, "Map");
		_handleNextGroundPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.ground, "Map");
		_handleNextWallPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.wall, "Map");
		_handleNextSpawnFlagPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.spawnFlag, "Map");	// Spawn
		_handleNextPortalFlagPrefab = AddressableAssetLoadManager.GetAddressableGameObject(mapTableData.portalFlag, "Map");

		if (_reloadInProgressGame)
		{
			// 재진입 한다고 항상 여기 값이 적혀있는건 아니다.
			// lobby에서만 셋팅이 되어있고 이후 1층부터 쭉 environmentSetting값이 비어져있으면 저장하는 타이밍이 없어서 lobby 셋팅대로 쭉 가게된다. 
			// 그러니 읽을게 없으면 재진입때도 아무런 셋팅하지 않고 지나간다.
			string cachedEnvironmentSetting = ClientSaveData.instance.GetCachedEnvironmentSetting();
			if (string.IsNullOrEmpty(cachedEnvironmentSetting) == false)
			{
				_handleEnvironmentSettingPrefab = AddressableAssetLoadManager.GetAddressableGameObject(cachedEnvironmentSetting, "EnvironmentSetting");
				_environmentSettingAddress = cachedEnvironmentSetting;
				return;
			}
		}

		if (MainSceneBuilder.s_buildReturnScrollUsedScene)
		{
			_handleEnvironmentSettingPrefab = AddressableAssetLoadManager.GetAddressableGameObject(MainSceneBuilder.s_lastPowerSourceEnvironmentSettingAddress, "EnvironmentSetting");
			_environmentSettingAddress = MainSceneBuilder.s_lastPowerSourceEnvironmentSettingAddress;
			return;
		}

		// 환경은 위의 맵 정보와 달리 들어오면 설정하고 아니면 패스하는 형태다. 그래서 없을땐 null로 한다.
		if (environmentSettingList == null || environmentSettingList.Length == 0)
		{
			_handleEnvironmentSettingPrefab = null;
			_environmentSettingAddress = "";
		}
		else
		{
			string environmentSetting = environmentSettingList[Random.Range(0, environmentSettingList.Length)];
			_handleEnvironmentSettingPrefab = AddressableAssetLoadManager.GetAddressableGameObject(environmentSetting, "EnvironmentSetting");
			_environmentSettingAddress = environmentSetting;
		}
	}

	string _lastPlayerActorId = "";
	int _lastPlayerPowerSource = -1;
	AsyncOperationGameObjectResult _handlePowerSourcePrefab = null;
	public void PreparePowerSource()
	{
		if (_lastPlayerActorId == BattleInstanceManager.instance.playerActor.actorId)
			return;
		_lastPlayerActorId = BattleInstanceManager.instance.playerActor.actorId;

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_lastPlayerActorId);
		if (_lastPlayerPowerSource == actorTableData.powerSource)
			return;

		_lastPlayerPowerSource = actorTableData.powerSource;
		_handlePowerSourcePrefab = AddressableAssetLoadManager.GetAddressableGameObject(PowerSource.Index2Address(_lastPlayerPowerSource), "PowerSource");
	}

	public GameObject GetPreparedPowerSourcePrefab()
	{
		return _handlePowerSourcePrefab.Result;
	}

	public bool IsDoneLoadAsyncNextStage()
	{
		if (_handleEnvironmentSettingPrefab != null && _handleEnvironmentSettingPrefab.IsDone == false)
			return false;
		if (_handlePowerSourcePrefab == null || _handlePowerSourcePrefab.IsDone == false)
			return false;
		return (_handleNextPlanePrefab.IsDone && _handleNextGroundPrefab.IsDone && _handleNextWallPrefab.IsDone && _handleNextSpawnFlagPrefab.IsDone && _handleNextPortalFlagPrefab.IsDone);
	}
#endif
*/

	/*
	GameObject _currentPlaneObject;
	GameObject _currentGroundObject;
	GameObject _currentWallObject;
	GameObject _currentSpawnFlagObject;
	GameObject _currentPortalFlagObject;
	GameObject _currentEnvironmentSettingObject;
	float currentMapBossHpRatioPer1Line = 0.0f;
	void InstantiateMap(MapTableData mapTableData, bool ignoreSpawnFlag = false)
	{
#if USE_MAIN_SCENE
		if (mapTableData != null)
			currentMapBossHpRatioPer1Line = mapTableData.bossHpRatioPer1Line;

		if (_currentPlaneObject != null)
			_currentPlaneObject.SetActive(false);
		_currentPlaneObject = BattleInstanceManager.instance.GetCachedObject(_handleNextPlanePrefab.Result, Vector3.zero, Quaternion.identity);
		BattleInstanceManager.instance.planeCollider = _currentPlaneObject.GetComponent<Collider>();

		if (_currentGroundObject != null)
			_currentGroundObject.SetActive(false);
		_currentGroundObject = BattleInstanceManager.instance.GetCachedObject(_handleNextGroundPrefab.Result, Vector3.zero, Quaternion.identity);

		if (_currentWallObject != null)
			_currentWallObject.SetActive(false);
		if (ignoreSpawnFlag == false)
			_currentWallObject = BattleInstanceManager.instance.GetCachedObject(_handleNextWallPrefab.Result, Vector3.zero, Quaternion.identity);

		if (ignoreSpawnFlag == false)
		{
			if (_currentSpawnFlagObject != null)
				_currentSpawnFlagObject.SetActive(false);
			_currentSpawnFlagObject = Instantiate<GameObject>(_handleNextSpawnFlagPrefab.Result);
		}

		if (_currentPortalFlagObject != null)
			_currentPortalFlagObject.SetActive(false);
		_currentPortalFlagObject = BattleInstanceManager.instance.GetCachedObject(_handleNextPortalFlagPrefab.Result, Vector3.zero, Quaternion.identity);

		// 위의 맵 정보와 달리 테이블에 값이 있을때만 변경하는거라 이렇게 처리한다. 변경시에만 저장되는거라 변경을 안하면 저장된게 없을거다.
		if (_handleEnvironmentSettingPrefab != null)
		{
			if (_currentEnvironmentSettingObject != null)
			{
				_currentEnvironmentSettingObject.SetActive(false);
				_currentEnvironmentSettingObject = null;
			}
			_currentEnvironmentSettingObject = BattleInstanceManager.instance.GetCachedObject(_handleEnvironmentSettingPrefab.Result, null);
			bool lobby = false;
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby) lobby = true;
			if (lobby == false)
				ClientSaveData.instance.OnChangedEnvironmentSetting(_environmentSettingAddress);

			// 마지막으로 생성한 env address를 들고있는다.
			_lastEnvironmentSettingAddress = _environmentSettingAddress;
		}
#else
		for (int i = 0; i < planePrefabList.Length; ++i)
		{
			if (planePrefabList[i].name.ToLower() == mapTableData.plane.ToLower())
			{
				if (_currentPlaneObject != null)
					_currentPlaneObject.SetActive(false);
				_currentPlaneObject = BattleInstanceManager.instance.GetCachedObject(planePrefabList[i], Vector3.zero, Quaternion.identity);
				break;
			}
		}

		for (int i = 0; i < groundPrefabList.Length; ++i)
		{
			if (groundPrefabList[i].name.ToLower() == mapTableData.ground.ToLower())
			{
				if (_currentGroundObject != null)
					_currentGroundObject.SetActive(false);
				_currentGroundObject = BattleInstanceManager.instance.GetCachedObject(groundPrefabList[i], Vector3.zero, Quaternion.identity);
				break;
			}
		}

		for (int i = 0; i < wallPrefabList.Length; ++i)
		{
			if (wallPrefabList[i].name.ToLower() == mapTableData.wall.ToLower())
			{
				if (_currentWallObject != null)
					_currentWallObject.SetActive(false);
				_currentWallObject = BattleInstanceManager.instance.GetCachedObject(wallPrefabList[i], Vector3.zero, Quaternion.identity);
				break;
			}
		}

		for (int i = 0; i < spawnFlagPrefabList.Length; ++i)
		{
			if (spawnFlagPrefabList[i].name.ToLower() == mapTableData.spawnFlag.ToLower())
			{
				if (_currentSpawnFlagObject != null)
					_currentSpawnFlagObject.SetActive(false);
				_currentSpawnFlagObject = Instantiate<GameObject>(spawnFlagPrefabList[i]);
				break;
			}
		}
#endif

		// 배틀매니저는 존재하지 않을 수 있다. 로딩속도 때문에 처음 진입해서 천천히 생성시킨다. 시작맵이라 없어도 플레이가 가능하다.
		if (BattleManager.instance != null)
			BattleManager.instance.OnLoadedMap();
	}

	public void DeactiveCurrentMap()
	{
		if (_currentPlaneObject != null)
			_currentPlaneObject.SetActive(false);

		if (_currentGroundObject != null)
			_currentGroundObject.SetActive(false);

		if (_currentWallObject != null)
			_currentWallObject.SetActive(false);

		if (_currentSpawnFlagObject != null)
			_currentSpawnFlagObject.SetActive(false);

		if (_currentPortalFlagObject != null)
			_currentPortalFlagObject.SetActive(false);

		if (_currentEnvironmentSettingObject != null)
		{
			_currentEnvironmentSettingObject.SetActive(false);
			_currentEnvironmentSettingObject = null;
		}
	}
	*/
	/*
	public string GetCurrentSpawnFlagName()
	{
		if (_currentSpawnFlagObject != null)
		{
			string name = _currentSpawnFlagObject.name;
			name = name.Replace("SpawnFlag", "");
			name = name.Replace("(Clone)", "");
			return name;
		}
		return "";
	}

	public Vector3 currentGatePillarSpawnPosition { get; set; }
	public bool spawnPowerSourcePrefab { get; set; }
	public Vector3 currentPowerSourceSpawnPosition { get; set; }
	public Vector3 currentReturnScrollSpawnPosition { get; set; }
	*/

	/*
	#region PlayerLevel
	ObscuredInt _playerLevel = 1;
	public int playerLevel { get { return _playerLevel; } set { _playerLevel = value; } }
	ObscuredInt _playerExp = 0;
	public int needLevelUpCount { get; set; }
	public void AddExp(int exp)
	{
		if (_playerLevel == GetMaxStageLevel())
			return;

		_playerExp += exp;

		// level, bottom exp bar
		int maxStageLevel = GetMaxStageLevel();
		int level = 0;
		float percent = 0.0f;
		for (int i = _playerLevel; i < TableDataManager.instance.stageExpTable.dataArray.Length; ++i)
		{
			if (_playerExp < TableDataManager.instance.stageExpTable.dataArray[i].requiredAccumulatedExp)
			{
				int currentPeriodExp = _playerExp - TableDataManager.instance.stageExpTable.dataArray[i - 1].requiredAccumulatedExp;
				percent = (float)currentPeriodExp / (float)TableDataManager.instance.stageExpTable.dataArray[i].requiredExp;
				level = TableDataManager.instance.stageExpTable.dataArray[i].level - 1;
				break;
			}
			if (TableDataManager.instance.stageExpTable.dataArray[i].level >= maxStageLevel)
			{
				level = maxStageLevel;
				percent = 1.0f;
				break;
			}
		}
		if (level == 0)
		{
			// max
			level = maxStageLevel;
			percent = 1.0f;
		}
		needLevelUpCount = level - _playerLevel;
		LobbyCanvas.instance.RefreshExpPercent(percent, needLevelUpCount, (level == maxStageLevel));
		if (needLevelUpCount == 0)
			return;
		_playerLevel = level;

		if (GuideQuestData.instance.CheckIngameLevel(level))
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.IngameLevel);

		AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
		healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantFloat("LevelUpHeal") * needLevelUpCount;
		healAffectorValue.fValue3 += BattleInstanceManager.instance.playerActor.actorStatus.GetValue(eActorStatus.LevelUpHealAddRate) * needLevelUpCount;
		BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, BattleInstanceManager.instance.playerActor, false);

		BattleInstanceManager.instance.GetCachedObject(BattleManager.instance.playerLevelUpEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity, BattleInstanceManager.instance.playerActor.cachedTransform);
		LobbyCanvas.instance.RefreshLevelText(_playerLevel);

		// 먼저 전용전투팩 얻는걸 체크. 여러개 얻을 경우 대비해서 누적시켜서 호출한다.
		for (int i = _playerLevel - needLevelUpCount + 1; i <= _playerLevel; ++i)
		{
			string exclusiveLevelPackId = TableDataManager.instance.FindActorLevelPackByLevel(BattleInstanceManager.instance.playerActor.actorId, i);
			if (string.IsNullOrEmpty(exclusiveLevelPackId))
				continue;

			// 전용팩은 레벨팩 데이터 매니저에 넣으면 안된다.
			//LevelPackDataManager.instance.AddLevelPack(BattleInstanceManager.instance.playerActor.actorId, exclusiveLevelPackId);
			BattleInstanceManager.instance.playerActor.skillProcessor.AddLevelPack(exclusiveLevelPackId, true, i);
			LevelUpIndicatorCanvas.ShowExclusive(true, BattleInstanceManager.instance.playerActor.cachedTransform, exclusiveLevelPackId, i);
		}
		// 이후 레벨업 카운트만큼 처리
		LevelUpIndicatorCanvas.Show(true, BattleInstanceManager.instance.playerActor.cachedTransform, needLevelUpCount, 0, 0);
		ClientSaveData.instance.OnChangedRemainLevelUpCount(needLevelUpCount);

		Timing.RunCoroutine(LevelUpScreenEffectProcess());
	}

	IEnumerator<float> LevelUpScreenEffectProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 0.333f);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;

		FadeCanvas.instance.FadeIn(1.0f);
	}

	public int GetMaxStageLevel()
	{
		int maxStageLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxStageLevel");

		// 잠재력 개방에 따른 차이. 잠재력은 ActorStatus인가? 아니면 액터의 또다른 개방 정보인가. 결국 강화랑 저장할 곳이 또 뭔가가 필요할듯. 파워레벨 역시 스탯은 아닐듯.

		return maxStageLevel;
	}
	#endregion

	#region Return Scroll
	// 이번 플레이 중에 리턴 스크롤을 사용했는지를 기억. 사용했다면 다시 죽었을때는 귀환창이 뜨지 않아야한다.
	// 새 enterFlag 설정할 때 초기화 해줘야한다.
	public bool returnScrollUsed { get; set; }

	public bool IsUsableReturnScroll()
	{
		if (returnScrollUsed)
			return false;

		// 챕터1 플레이하는 초보자 유저들한테 주는 보너스. 스크롤이 없을테지만 1회 부활시켜준다.
		if (IsChapter1NewbieUser())
			return true;

		return (CurrencyData.instance.returnScroll > 0);
	}

	public static bool IsChapter1NewbieUser()
	{
		if (PlayerData.instance.highestPlayChapter == 1 && StageManager.instance.playChapter == 1 && PlayerData.instance.selectedChapter == 1)
			return true;
		return false;
	}

	public void UseReturnScroll()
	{
		returnScrollUsed = true;
		ClientSaveData.instance.OnChangedReturnScroll(true);
	}

	ObscuredBool _lastPowerSourceSaved = false;
	ObscuredInt _lastPowerSourceStage = 0;
	ObscuredString _lastPowerSourceActorId = "";
	public void SaveReturnScrollPoint()
	{
		// 먼저 스테이지 매니저 안에다가 기록해두고
		_lastPowerSourceSaved = true;
		_lastPowerSourceStage = playStage;
		_lastPowerSourceActorId = BattleInstanceManager.instance.playerActor.GetActorIdWithMercenary();

		// 재접시 복구해야하니 ClientSaveData에도 저장해둔다. 서버에 저장하는 방법은 전투 중간에 패킷을 보내야하는 경우가 생겨버리기땜에 하지 않기로 한다.
		ClientSaveData.instance.OnChangedLastPowerSourceSaved(true);
		ClientSaveData.instance.OnChangedLastPowerSourceStage(playStage);
		ClientSaveData.instance.OnChangedLastPowerSourceActorId(BattleInstanceManager.instance.playerActor.GetActorIdWithMercenary());

		// 한가지 문제가 있는데 일반적인 PowerSource 나오는 층에서는 환경셋팅이 비어져있는게 보통인데
		// 로비를 들리지 않고 씬을 구축해야하기때문에 환경셋팅을 어딘가 저장해놓을 필요가 생겼다.
		// 그런데 이건 ClientSaveData에 두기도 뭐한게 재진입 로직에서는 전혀 필요가 없다.
		// 그래서 별도의 위치에 기록해두고 부활씬으로 재구축할때 사용하기로 한다.
		MainSceneBuilder.s_lastPowerSourceEnvironmentSettingAddress = _lastEnvironmentSettingAddress;
	}

	public bool IsSavedReturnScrollPoint()
	{
		return _lastPowerSourceSaved;
	}

	public void SetReturnScrollForInProgressGame()
	{
		_lastPowerSourceSaved = ClientSaveData.instance.GetCachedLastPowerSourceSaved();
		_lastPowerSourceStage = ClientSaveData.instance.GetCachedLastPowerSourceStage();
		_lastPowerSourceActorId = ClientSaveData.instance.GetCachedLastPowerSourceActorId();
		returnScrollUsed = ClientSaveData.instance.GetCachedReturnScroll();
	}

	// 로딩 준비
	GameObject _returnScrollEffectPrefab = null;
	public void PrepareReturnScroll()
	{
		AddressableAssetLoadManager.GetAddressableGameObject("ReturnScrollEffect", "CommonEffect", (prefab) =>
		{
			_returnScrollEffectPrefab = prefab;
		});

		if (BattleInstanceManager.instance.playerActor.actorId != _lastPowerSourceActorId)
		{
			PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_lastPowerSourceActorId);
			if (playerActor == null)
				AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(_lastPowerSourceActorId), "Character");
		}
	}

	// 실제 이동하는 함수. ClientSaveData.MoveToInProgressGame 에서 가져와서 변형시켜 쓴다.
	public void ReturnLastPowerSourcePoint()
	{
		// 제일 먼저 할일은 클라에 캐싱된 전투 데이터를 수정하는 것. 이걸 해놔야 바로 꺼지더라도 복구할 수 있다.
		ClientSaveData.instance.OnChangedStage(_lastPowerSourceStage);
		ClientSaveData.instance.OnChangedBattleActor(_lastPowerSourceActorId);
		ClientSaveData.instance.OnChangedHpRatio(1.0f);
		ClientSaveData.instance.OnChangedSpRatio(1.0f);
		ClientSaveData.instance.OnChangedGatePillar(true);
		ClientSaveData.instance.OnChangedPowerSource(true);
		ClientSaveData.instance.OnChangedCloseSwap(false);

		// 사용한 BattleActor 리스트도 강제로 초기화
		List<string> listBattleActor = new List<string>();
		listBattleActor.Add(_lastPowerSourceActorId);
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string jsonBattleActorData = serializer.SerializeObject(listBattleActor);
		ClientSaveData.instance.OnChangedBattleActorData(jsonBattleActorData);

		// 마지막엔 returnScroll 사용한 것도 기억시켜둔다.
		UseReturnScroll();

		// 여기까지 했으면 진짜 연출 시작할거니 코루틴으로 넘어가서 처리한다.
		StartCoroutine(NextMapProcess());
	}

	System.Collections.IEnumerator NextMapProcess()
	{
		while (_returnScrollEffectPrefab == null)
			yield return new WaitForEndOfFrame();

		BattleInstanceManager.instance.GetCachedObject(_returnScrollEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity);

		yield return new WaitForSecondsRealtime(3.4f);

		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true, true);
		yield return new WaitForSecondsRealtime(0.2f);

		// 씬 이동으로 처리하기로 한다.
		// 씬 이동으로 하지 않으면 각종 몬스터 재활용 이슈부터 텔레포트 러쉬 소환부터 드랍오브젝트 보스HP UI까지 별 이슈가 다 걸린다.
		// 하나하나 테스트 하면서 검증할바엔 씬을 초기화 해서 넘어가기로 한다.
		yield return new WaitForSecondsRealtime(0.1f);
		Time.timeScale = 1.0f;
		MainSceneBuilder.s_buildReturnScrollUsedScene = true;
		SceneManager.LoadScene(0);
	}
	#endregion
	
	#region InProgressGame
	public int playerExp { get { return _playerExp; } }
	public void SetLevelExpForInProgressGame(int exp)
	{
		_playerExp = exp;

		// level, bottom exp bar
		int maxStageLevel = GetMaxStageLevel();
		int level = 0;
		float percent = 0.0f;
		for (int i = _playerLevel; i < TableDataManager.instance.stageExpTable.dataArray.Length; ++i)
		{
			if (_playerExp < TableDataManager.instance.stageExpTable.dataArray[i].requiredAccumulatedExp)
			{
				int currentPeriodExp = _playerExp - TableDataManager.instance.stageExpTable.dataArray[i - 1].requiredAccumulatedExp;
				percent = (float)currentPeriodExp / (float)TableDataManager.instance.stageExpTable.dataArray[i].requiredExp;
				level = TableDataManager.instance.stageExpTable.dataArray[i].level - 1;
				break;
			}
			if (TableDataManager.instance.stageExpTable.dataArray[i].level >= maxStageLevel)
			{
				level = maxStageLevel;
				percent = 1.0f;
				break;
			}
		}
		if (level == 0)
		{
			// max
			level = maxStageLevel;
			percent = 1.0f;
		}
		int levelUpCount = level - _playerLevel;
		LobbyCanvas.instance.SetLevelExpForInProgressGame(level, percent);
		if (levelUpCount == 0)
			return;
		_playerLevel = level;

		// 전용전투팩 얻는걸 체크. 스왑때 쓰려고 만든 함수를 대신 호출해서 처리한다.
		BattleInstanceManager.instance.playerActor.skillProcessor.CheckAllExclusiveLevelPack();
	}
	#endregion



	#region Battle Result
	public void OnSuccess()
	{
		// 챕터 +1 해두고 디비에 기록. highest 갱신.
		int nextChapter = playChapter + 1;

		// 최종 챕터 확인
		int chaosChapterLimit = BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosChapterLimit");
		if (nextChapter == chaosChapterLimit)
		{
			// 최종 챕터를 깬거라 더이상 진행하면 안되서
			// 곧바로 카오스 모드로 진입시켜야한다.
		}
	}
	#endregion

	*/


	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}
