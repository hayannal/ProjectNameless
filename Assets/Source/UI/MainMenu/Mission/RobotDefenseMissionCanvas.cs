using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using MEC;
using DG.Tweening;

public class RobotDefenseMissionCanvas : MonoBehaviour
{
	public static RobotDefenseMissionCanvas instance;

	public GameObject robotDefenseMissionPrefab;

	public Text selectPositionText;
	public Text dronePositionText;
	public Text dronePositionCountText;
	public GameObject droneBonusTextObject;
	public GameObject autoPositionButtonObject;

	public GameObject toggleContentItemPrefab;
	public RectTransform toggleRootRectTransform;

	public Text monsterKillText;
	public Text monsterKillCountText;
	public DOTweenAnimation monsterKillCountTextTweenAnimation;

	public GameObject resultRootObject;
	public GameObject resultContentRootObject;
	public Text resultMonsterKillCountText;
	public GameObject failureTextObject;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		toggleContentItemPrefab.SetActive(false);

		RefreshCharacterButton();
	}

	Transform _groundTransform;
	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		if (_groundTransform == null)
			_groundTransform = BattleInstanceManager.instance.GetCachedObject(robotDefenseMissionPrefab, StageManager.instance.GetSafeWorldOffset(), Quaternion.identity).transform;
		else
			_groundTransform.gameObject.SetActive(true);

		if (MainCanvas.instance != null)
		{
			MainCanvas.instance.OnClickCloseButton();
			// HideState를 풀어놔야 카메라 쉐이크가 동작한다.
			MainCanvas.instance.OnEnterCharacterMenu(false, true);
			MainCanvas.instance.challengeButtonObject.SetActive(false);
			MainCanvas.instance.inputRectObject.SetActive(false);
			MainCanvas.instance.FadeOutQuestInfoGroup(0.0f, 0.1f, false, true);
		}
	}

	void OnDisable()
	{
		// 씬을 종료하고 새 씬을 구축하러 나가는 로직으로 구현되어있기 때문에
		// 하단 라인들로 넘어갈 이유가 없다. 그러니 여기서 리턴시킨다.
		return;

		//MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;
		if (CustomFollowCamera.instance == null || CameraFovController.instance == null || MainCanvas.instance == null)
			return;
		if (CustomFollowCamera.instance.gameObject == null)
			return;

		//MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	float totalTime = 60.0f * 5.0f;
	public bool repeatSpawnFinished { get; set; }
	float _repeatSpawnRemainTime;
	void Update()
	{
		if (_repeatSpawnRemainTime > 0.0f)
		{
			_repeatSpawnRemainTime -= Time.deltaTime;
			if (_repeatSpawnRemainTime <= 0.0f)
			{
				_repeatSpawnRemainTime = 0.0f;

				// 더이상 리핏하지 않게 걸어둔다.
				repeatSpawnFinished = true;
			}
		}
	}

	public float GetStandardHpByPlayTime()
	{
		int minStandardHp = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RobotDefenseStandardHpMin");
		int maxStandardHp = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RobotDefenseStandardHpMax");
		//Debug.LogFormat("Ratio = {0}", (1.0f - (_repeatSpawnRemainTime / totalTime)));
		return (maxStandardHp - minStandardHp) * (1.0f - (_repeatSpawnRemainTime / totalTime)) + minStandardHp;
	}

	#region Auto Position
	public void OnClickAutoPositionButton()
	{
		if (dronePositionText.gameObject.activeSelf)
		{
			Timing.RunCoroutine(AutoDronePositionProcess());
			return;
		}

		// 위치별로 저장된 정보가 있을거다.
		// 저장된 정보를 쓰되 얻어오지 못한게 있거나 최초에는 인덱스대로 자동배치 해주기로 한다.
		Timing.RunCoroutine(AutoPositionProcess());
	}

	Dictionary<string, string> _dicAutoPositionLastInfo = new Dictionary<string, string>();
	public bool autoPositionProcessed { get; private set; }
	IEnumerator<float> AutoPositionProcess()
	{
		autoPositionProcessed = true;
		autoPositionButtonObject.SetActive(false);

		List<int> listRemainIndex = new List<int>();

		// 먼저 루프를 돌면서 캐싱이 제대로 되어있는 동료들부터 배치하고 남은 
		Dictionary<string, string> cachedLastPositionInfo = GetCachedLastPositionInfo();
		for (int i = 0; i < _listCharacterToggleButton.Count; ++i)
		{
			int lastPositionIndex = FindLastPositionIndex(cachedLastPositionInfo, _listCharacterToggleButton[i].actorId);
			if (lastPositionIndex == -1)
			{
				listRemainIndex.Add(i);
				continue;
			}
			OnValueChangedToggle(_listCharacterToggleButton[i].actorId);
			RobotDefenseMissionGround.instance.OnClickBox(lastPositionIndex);
			yield return Timing.WaitForSeconds(0.3f);
		}

		if (listRemainIndex.Count == 0)
		{
			autoPositionProcessed = false;
			yield break;
		}

		for (int i = 0; i < listRemainIndex.Count; ++i)
		{
			int currentIndex = listRemainIndex[i];
			string actorId = _listCharacterToggleButton[currentIndex].actorId;
			int emptyPositionIndex = RobotDefenseMissionGround.instance.GetEmptyWorldButtonIndex();
			if (emptyPositionIndex == -1)
			{
				// -1 나오면 뭔가 이상한거다.
				break;
			}
			OnValueChangedToggle(actorId);
			RobotDefenseMissionGround.instance.OnClickBox(emptyPositionIndex);
			yield return Timing.WaitForSeconds(0.3f);
		}

		autoPositionProcessed = false;
	}

	int FindLastPositionIndex(Dictionary<string, string> dicPositionInfo, string actorId)
	{
		if (dicPositionInfo == null || dicPositionInfo.Count == 0)
			return -1;

		Dictionary<string, string>.Enumerator e = dicPositionInfo.GetEnumerator();
		while (e.MoveNext())
		{
			if (e.Current.Value == actorId)
			{
				int key = 0;
				if (int.TryParse(e.Current.Key, out key))
					return key;
				return -1;
			}
		}
		return -1;
	}

	List<int> _listAutoDronePositionLastInfo = new List<int>();
	IEnumerator<float> AutoDronePositionProcess()
	{
		autoPositionButtonObject.SetActive(false);

		// 먼저 루프를 돌면서 캐싱된 자리부터 배치하고
		List<int> cachedLastDronePositionInfo = GetCachedLastDronePositionInfo();
		if (cachedLastDronePositionInfo != null)
		{
			for (int i = 0; i < cachedLastDronePositionInfo.Count; ++i)
			{
				int lastPositionIndex = cachedLastDronePositionInfo[i];
				RobotDefenseMissionGround.instance.OnClickBoxDrone(lastPositionIndex);
				yield return Timing.WaitForSeconds(0.3f);

				if (_droneSpawnCount >= _droneSpawnMaxCount)
					break;
			}
		}
		if (_droneSpawnCount >= _droneSpawnMaxCount)
			yield break;

		int remainCount = _droneSpawnMaxCount - _droneSpawnCount;
		for (int i = 0; i < remainCount; ++i)
		{
			int emptyPositionIndex = RobotDefenseMissionGround.instance.GetEmptyDroneWorldButtonIndex();
			if (emptyPositionIndex == -1)
			{
				// -1 나오면 뭔가 이상한거다.
				break;
			}
			RobotDefenseMissionGround.instance.OnClickBoxDrone(emptyPositionIndex);
			yield return Timing.WaitForSeconds(0.3f);
		}
	}
	#endregion

	public void OnClickBackButton()
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_BackToLobby"), UIString.instance.GetString("GameUI_BackToLobbyDescription"), () => {
			SubMissionData.instance.readyToReopenAdventureListCanvas = true;
			Screen.sleepTimeout = SleepTimeout.SystemSetting;
			SceneManager.LoadScene(0);
		});
	}

	List<CharacterToggleButton> _listCharacterToggleButton = new List<CharacterToggleButton>();
	void RefreshCharacterButton()
	{
		List<string> listSelectedActorId = RobotDefenseEnterCanvas.instance.listSelectedActorId;
		if (listSelectedActorId != null)
		{
			for (int i = 0; i < listSelectedActorId.Count; ++i)
			{
				GameObject newObject = Instantiate<GameObject>(toggleContentItemPrefab, toggleRootRectTransform);
				CharacterToggleButton newToggleButton = newObject.GetComponent<CharacterToggleButton>();
				newToggleButton.RefreshInfo(listSelectedActorId[i]);
				newObject.SetActive(true);
				_listCharacterToggleButton.Add(newToggleButton);
			}
		}

		// 기본적으로 0번탭을 보게 해준다.
		OnValueChangedToggle(listSelectedActorId[0]);
		_dicAutoPositionLastInfo.Clear();
	}

	public string currentSelectedActorId { get; set; }
	public void OnValueChangedToggle(string actorId)
	{
		currentSelectedActorId = actorId;
		for (int i = 0; i < _listCharacterToggleButton.Count; ++i)
			_listCharacterToggleButton[i].OnSelect(_listCharacterToggleButton[i].actorId == actorId);
	}

	int _spawnedCount = 0;
	public void OnSpawnActor(int positionIndex)
	{
		// 하나라도 배치하기 시작하면 오토 버튼은 꺼둔다.
		autoPositionButtonObject.SetActive(false);

		for (int i = 0; i < _listCharacterToggleButton.Count; ++i)
		{
			if (_listCharacterToggleButton[i].actorId == currentSelectedActorId)
			{
				_spawnedCount += 1;
				_listCharacterToggleButton[i].gameObject.SetActive(false);
				break;
			}
		}

		// 선택한 정보를 기억해둔다.
		if (_dicAutoPositionLastInfo.ContainsKey(positionIndex.ToString()) == false)
			_dicAutoPositionLastInfo.Add(positionIndex.ToString(), currentSelectedActorId);

		if (_spawnedCount == _listCharacterToggleButton.Count)
		{
			// 현재 배치한거에 따라 위치값을 캐싱해놔야한다.
			RecordLastPositionInfo();

			// 모두 스폰한거다. 이제 드론 배치를 시작.
			InitializeDronePositionInfo();
			return;
		}

		// 다음 항목을 선택해둔다.
		for (int i = 0; i < _listCharacterToggleButton.Count; ++i)
		{
			if (_listCharacterToggleButton[i].gameObject.activeSelf)
			{
				OnValueChangedToggle(_listCharacterToggleButton[i].actorId);
				break;
			}
		}
	}


	#region Drone Position
	ObscuredBool _applyBonus = false;
	int _droneSpawnCount;
	int _droneSpawnMaxCount;
	void InitializeDronePositionInfo()
	{
		_applyBonus = false;
		if (SubMissionData.instance.robotDefenseClearLevel == 0)
		{
			RobotDefenseStepTableData robotDefenseStepTableData = TableDataManager.instance.FindRobotDefenseStepTableData(1);
			if (SubMissionData.instance.robotDefenseKillCount < robotDefenseStepTableData.monCount)
				_applyBonus = true;
		}

		_droneSpawnCount = 0;
		_droneSpawnMaxCount = DroneUpgradeCanvas.Level2DroneCount(SubMissionData.instance.robotDefenseDroneCountLevel);
		if (_applyBonus) _droneSpawnMaxCount = 10;
		totalTime = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RobotDefenseTimeSec");

		selectPositionText.SetLocalizedText(UIString.instance.GetString("MissionUI_SelectPositionDrone"));
		dronePositionCountText.text = string.Format("{0} / {1}", _droneSpawnCount, GetCountMaxString());
		dronePositionCountText.gameObject.SetActive(true);
		dronePositionText.gameObject.SetActive(true);
		droneBonusTextObject.SetActive(_applyBonus);
		RobotDefenseMissionGround.instance.InitializeDronePosition();
		autoPositionButtonObject.SetActive(true);
	}

	string GetCountMaxString()
	{
		if (_applyBonus)
			return string.Format("<color=#68FF4C>{0}</color>", _droneSpawnMaxCount);

		return _droneSpawnMaxCount.ToString("N0");
	}

	public void OnSpawnDrone(int positionIndex)
	{
		// 하나라도 배치하기 시작하면 오토 버튼은 꺼둔다.
		autoPositionButtonObject.SetActive(false);

		_droneSpawnCount += 1;
		dronePositionCountText.text = string.Format("{0} / {1}", _droneSpawnCount, GetCountMaxString());

		// 선택한 정보를 기억해둔다.
		if (_listAutoDronePositionLastInfo.Contains(positionIndex) == false)
			_listAutoDronePositionLastInfo.Add(positionIndex);

		if (_droneSpawnCount == _droneSpawnMaxCount)
		{
			// 현재 배치한거에 따라 위치값을 캐싱해놔야한다.
			RecordLastDronePositionInfo();

			// 모두 스폰한거다. 플레이 시작.
			dronePositionCountText.gameObject.SetActive(false);
			dronePositionText.gameObject.SetActive(false);
			droneBonusTextObject.SetActive(false);
			selectPositionText.gameObject.SetActive(false);
			RobotDefenseMissionGround.instance.OnFinishSelect();
			_repeatSpawnRemainTime = totalTime;
			return;
		}
	}
	#endregion


	#region Other UI
	ObscuredInt _reuseCount;
	ObscuredInt _killCount;
	public const int FinishMonsterCount = 50;

	public void OnDieMonster(bool dieForReuse)
	{
		if (_finishProcessed)
			return;

		if (dieForReuse)
		{
			++_reuseCount;
			if (_reuseCount >= FinishMonsterCount)
				ClearMission();
			return;
		}

		if (_killCount == 0)
		{
			monsterKillCountText.gameObject.SetActive(true);
			monsterKillText.gameObject.SetActive(true);
		}
		_killCount += 1;
		monsterKillCountText.text = _killCount.ToString("N0");
		monsterKillCountTextTweenAnimation.DORestart();
	}
	#endregion


	#region ClearMission
	public void ClearMission()
	{
		StartCoroutine(FinishProcess());
	}

	bool _finishProcessed = false;
	IEnumerator FinishProcess()
	{
		_finishProcessed = true;
		Time.timeScale = 0.01f;

		resultRootObject.SetActive(true);

		UIInstanceManager.instance.ShowCanvasAsync("VictoryResultCanvas", () =>
		{
			if (_reuseCount >= FinishMonsterCount)
				VictoryResultCanvas.instance.victoryText.text = "FINISH";
		});
		yield return new WaitForSecondsRealtime(1.3f);

		PlayFabApiManager.instance.RequestEndRobotDefenseMission(_killCount, () =>
		{
			// 패킷 통과하면 다음 처리로 넘어간다.
			ShowResult();
		});
	}

	void ShowResult()
	{
		Time.timeScale = 0.0f;

		resultMonsterKillCountText.text = _killCount.ToString("N0");
		failureTextObject.SetActive(_reuseCount >= FinishMonsterCount);

		resultContentRootObject.SetActive(true);
	}

	public void OnClickResultExitButton()
	{
		if (resultContentRootObject.activeSelf == false)
			return;

		RobotDefenseEnterCanvas.s_killCountAddValue = _killCount;
		SubMissionData.instance.readyToReopenAdventureListCanvas = true;
		Screen.sleepTimeout = SleepTimeout.SystemSetting;
		Time.timeScale = 1.0f;
		SceneManager.LoadScene(0);
	}
	#endregion


	#region Record Position Info
	void RecordLastPositionInfo()
	{
		// 0번 포지션엔 누구 1번 포지션엔 누구 이렇게 저장해둔다.
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string value = serializer.SerializeObject(_dicAutoPositionLastInfo);
		ObscuredPrefs.SetString(string.Format("_rbdPosition_{0}", PlayFabApiManager.instance.playFabId), value);
	}

	Dictionary<string, string> GetCachedLastPositionInfo()
	{
		string cachedLastPositionInfo = ObscuredPrefs.GetString(string.Format("_rbdPosition_{0}", PlayFabApiManager.instance.playFabId));
		if (string.IsNullOrEmpty(cachedLastPositionInfo))
			return null;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		return serializer.DeserializeObject<Dictionary<string, string>>(cachedLastPositionInfo);
	}
	#endregion

	#region Record Drone Position Info
	void RecordLastDronePositionInfo()
	{
		// 0번 포지션엔 누구 1번 포지션엔 누구 이렇게 저장해둔다.
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string value = serializer.SerializeObject(_listAutoDronePositionLastInfo);
		ObscuredPrefs.SetString(string.Format("_rbdDroPosition_{0}", PlayFabApiManager.instance.playFabId), value);
	}

	List<int> GetCachedLastDronePositionInfo()
	{
		string cachedLastDronePositionInfo = ObscuredPrefs.GetString(string.Format("_rbdDroPosition_{0}", PlayFabApiManager.instance.playFabId));
		if (string.IsNullOrEmpty(cachedLastDronePositionInfo))
			return null;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		return serializer.DeserializeObject<List<int>>(cachedLastDronePositionInfo);
	}
	#endregion
}