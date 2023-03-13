using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using MEC;

public class RushDefenseMissionCanvas : MonoBehaviour
{
	public static RushDefenseMissionCanvas instance;

	public GameObject rushDefenseMissionPrefab;

	public GameObject selectPositionTextObject;
	public GameObject autoPositionButtonObject;

	public GameObject toggleContentItemPrefab;
	public RectTransform toggleRootRectTransform;

	public GameObject resultRootObject;

	public GameObject rewardContentItemPrefab;
	public RectTransform rewardContentRootRectTransform;

	public class RewardCustomItemContainer : CachedItemHave<MissionCanvasRewardIcon>
	{
	}
	RewardCustomItemContainer _rewardContainer = new RewardCustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		toggleContentItemPrefab.SetActive(false);
		rewardContentItemPrefab.SetActive(false);

		RefreshCharacterButton();
	}

	Transform _groundTransform;
	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		if (_groundTransform == null)
			_groundTransform = BattleInstanceManager.instance.GetCachedObject(rushDefenseMissionPrefab, StageManager.instance.GetSafeWorldOffset(), Quaternion.identity).transform;
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

	#region Auto Position
	public void OnClickAutoPositionButton()
	{
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
			RushDefenseMissionGround.instance.OnClickBox(lastPositionIndex);
			yield return Timing.WaitForSeconds(0.5f);
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
			int emptyPositionIndex = RushDefenseMissionGround.instance.GetEmptyWorldButtonIndex();
			if (emptyPositionIndex == -1)
			{
				// -1 나오면 뭔가 이상한거다.
				break;
			}
			OnValueChangedToggle(actorId);
			RushDefenseMissionGround.instance.OnClickBox(emptyPositionIndex);
			yield return Timing.WaitForSeconds(0.5f);
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
	#endregion

	public void OnClickBackButton()
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_BackToLobby"), UIString.instance.GetString("GameUI_BackToLobbyDescription"), () => {
			SubMissionData.instance.readyToReopenMissionListCanvas = true;
			SceneManager.LoadScene(0);
		});
	}

	List<CharacterToggleButton> _listCharacterToggleButton = new List<CharacterToggleButton>();
	void RefreshCharacterButton()
	{
		List<string> listSelectedActorId = RushDefenseEnterCanvas.instance.listSelectedActorId;

		GameObject newPlayerObject = Instantiate<GameObject>(toggleContentItemPrefab, toggleRootRectTransform);
		CharacterToggleButton characterToggleButton = newPlayerObject.GetComponent<CharacterToggleButton>();
		characterToggleButton.RefreshInfo(CharacterData.s_PlayerActorId);
		newPlayerObject.SetActive(true);
		_listCharacterToggleButton.Add(characterToggleButton);

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
		OnValueChangedToggle(CharacterData.s_PlayerActorId);
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

			// 모두 스폰한거다. 이제 게임을 시작.
			selectPositionTextObject.SetActive(false);
			RushDefenseMissionGround.instance.OnFinishSelect();
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


	#region ClearMission
	public void ClearMission()
	{
		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyRushDefense");
		int selectedDifficulty = RushDefenseEnterCanvas.instance.selectedDifficulty;
		int selectableMaxDifficulty = RushDefenseEnterCanvas.instance.selectableMaxDifficulty;
		bool firstClear = (selectedDifficulty == selectableMaxDifficulty && selectedDifficulty > SubMissionData.instance.rushDefenseClearLevel);
		int addDia = RushDefenseEnterCanvas.instance.expectedReward;
		PlayFabApiManager.instance.RequestEndRushDefenseMission(firstClear, selectedDifficulty, addDia, price, () =>
		{
			// 패킷 통과하면 다음 처리로 넘어간다.
			Timing.RunCoroutine(ClearProcess(firstClear, selectedDifficulty));
		});
	}

	IEnumerator<float> ClearProcess(bool firstClear, int selectedDifficulty)
	{
		UIInstanceManager.instance.ShowCanvasAsync("VictoryResultCanvas", null);

		yield return Timing.WaitForSeconds(1.5f);

		if (this == null)
			yield break;

		ShowResult(firstClear, selectedDifficulty);
	}

	void ShowResult(bool firstClear, int selectedDifficulty)
	{
		MissionModeTableData missionModeTableData = TableDataManager.instance.FindMissionModeTableData((int)SubMissionData.eSubMissionType.RushDefense, selectedDifficulty);
		if (missionModeTableData == null)
			return;

		if (firstClear)
		{
			// first
			if (string.IsNullOrEmpty(missionModeTableData.firstRewardType1) == false)
			{
				MissionCanvasRewardIcon missionCanvasRewardIcon = _rewardContainer.GetCachedItem(rewardContentItemPrefab, rewardContentRootRectTransform);
				missionCanvasRewardIcon.rewardIcon.RefreshReward(missionModeTableData.firstRewardType1, missionModeTableData.firstRewardValue1, missionModeTableData.firstRewardCount1);
				missionCanvasRewardIcon.firstObject.SetActive(true);
			}
			if (string.IsNullOrEmpty(missionModeTableData.firstRewardType2) == false)
			{
				MissionCanvasRewardIcon missionCanvasRewardIcon = _rewardContainer.GetCachedItem(rewardContentItemPrefab, rewardContentRootRectTransform);
				missionCanvasRewardIcon.rewardIcon.RefreshReward(missionModeTableData.firstRewardType2, missionModeTableData.firstRewardValue2, missionModeTableData.firstRewardCount2);
				missionCanvasRewardIcon.firstObject.SetActive(true);
			}
			if (string.IsNullOrEmpty(missionModeTableData.firstRewardType3) == false)
			{
				MissionCanvasRewardIcon missionCanvasRewardIcon = _rewardContainer.GetCachedItem(rewardContentItemPrefab, rewardContentRootRectTransform);
				missionCanvasRewardIcon.rewardIcon.RefreshReward(missionModeTableData.firstRewardType3, missionModeTableData.firstRewardValue3, missionModeTableData.firstRewardCount3);
				missionCanvasRewardIcon.firstObject.SetActive(true);
			}
		}

		// repeat
		if (string.IsNullOrEmpty(missionModeTableData.rewardType1) == false)
		{
			MissionCanvasRewardIcon missionCanvasRewardIcon = _rewardContainer.GetCachedItem(rewardContentItemPrefab, rewardContentRootRectTransform);
			missionCanvasRewardIcon.rewardIcon.RefreshReward(missionModeTableData.rewardType1, missionModeTableData.rewardValue1, missionModeTableData.rewardCount1);
			missionCanvasRewardIcon.firstObject.SetActive(false);
		}
		if (string.IsNullOrEmpty(missionModeTableData.rewardType2) == false)
		{
			MissionCanvasRewardIcon missionCanvasRewardIcon = _rewardContainer.GetCachedItem(rewardContentItemPrefab, rewardContentRootRectTransform);
			missionCanvasRewardIcon.rewardIcon.RefreshReward(missionModeTableData.rewardType2, missionModeTableData.rewardValue2, missionModeTableData.rewardCount2);
			missionCanvasRewardIcon.firstObject.SetActive(false);
		}

		resultRootObject.SetActive(true);
	}

	public void OnClickResultExitButton()
	{
		SubMissionData.instance.readyToReopenMissionListCanvas = true;
		SceneManager.LoadScene(0);
	}
	#endregion


	#region Record Position Info
	void RecordLastPositionInfo()
	{
		// 0번 포지션엔 누구 1번 포지션엔 누구 이렇게 저장해둔다.
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string value = serializer.SerializeObject(_dicAutoPositionLastInfo);
		ObscuredPrefs.SetString(string.Format("_rdPosition_{0}", PlayFabApiManager.instance.playFabId), value);
	}

	Dictionary<string, string> GetCachedLastPositionInfo()
	{
		string cachedLastPositionInfo = ObscuredPrefs.GetString(string.Format("_rdPosition_{0}", PlayFabApiManager.instance.playFabId));
		if (string.IsNullOrEmpty(cachedLastPositionInfo))
			return null;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		return serializer.DeserializeObject<Dictionary<string, string>>(cachedLastPositionInfo);
	}
	#endregion
}