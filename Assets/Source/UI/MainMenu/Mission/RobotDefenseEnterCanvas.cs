using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using DG.Tweening;
using MEC;

public class RobotDefenseEnterCanvas : MonoBehaviour
{
	public static RobotDefenseEnterCanvas instance;

	const int SELECT_MAX = 2;
	public static int MINIMUM_COUNT = 2;

	public static int RobotDefenseRepeatStep = 99999;

	public Transform titleTextTransform;

	public Text stepText;
	public Slider monsterKillSlider;
	public Image monsterKillSliderImage;
	public Text monsterKillCountText;

	public GameObject selectStartText;
	public Text selectResultText;

	public Image enterButtonImage;
	public Text enterText;
	public Text enterRemainCountText;

	public RewardIcon rewardIcon1;
	public RewardIcon rewardIcon2;
	public RewardIcon rewardIconForDronePoint;

	public RectTransform levelUpAlarmRootTransform;
	public RectTransform upgradeAlarmRootTransform;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<MissionCanvasCharacterListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	void OnEnable()
	{
		RefreshInfo();
		RefreshStep();
		RefreshAlarmObject();
		RefreshCoolTime();
		RefreshGrid(true);

		StackCanvas.Push(gameObject);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		StackCanvas.Pop(gameObject);
	}

	void Update()
	{
		UpdateTargetKillValue();
		UpdateRemainCoolTime();
	}

	ObscuredInt _currentStep;
	ObscuredBool _repeatMode;
	StageTableData _robotDefenseStageTableData;
	void RefreshInfo()
	{
		int clearLevel = SubMissionData.instance.robotDefenseClearLevel;

		// 플레이 스텝은 클리어 레벨 + 1
		_currentStep = clearLevel + 1;

		// Max를 넘는다면 리핏모드로 처리해야한다.
		_repeatMode = (_currentStep > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxRobotDefense"));
		if (_repeatMode)
		{

		}


		enterRemainCountText.text = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("RobotDefenseDailyCount") - SubMissionData.instance.robotDefenseDailyCount).ToString();

		/*
		// 패널티를 구할땐 그냥 스테이지 테이블에서 구해오면 안되고 선택된 난이도의 1층을 구해와서 처리해야한다.
		StageTableData penaltyStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(_selectedDifficulty, 1, false);
		if (penaltyStageTableData == null)
			return;
		*/

		//stagePenaltyText.gameObject.SetActive(false);
		/*
		string penaltyString = ChapterCanvas.GetPenaltyString(penaltyStageTableData);
		if (string.IsNullOrEmpty(penaltyString) == false)
		{
			stagePenaltyText.SetLocalizedText(penaltyString);
			stagePenaltyText.gameObject.SetActive(true);
		}
		*/
	}

	List<CharacterData> _listTempCharacterData = new List<CharacterData>();
	List<MissionCanvasCharacterListItem> _listMissionCanvasCharacterListItem = new List<MissionCanvasCharacterListItem>();
	void RefreshGrid(bool onEnable)
	{
		for (int i = 0; i < _listMissionCanvasCharacterListItem.Count; ++i)
			_listMissionCanvasCharacterListItem[i].gameObject.SetActive(false);
		_listMissionCanvasCharacterListItem.Clear();

		_listTempCharacterData.Clear();
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_PlayerActorId || TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_DroneActorId)
				continue;
			if (CharacterManager.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId) == false)
				continue;
			_listTempCharacterData.Add(CharacterManager.instance.GetCharacterData(TableDataManager.instance.actorTable.dataArray[i].actorId));
		}

		if (_listTempCharacterData.Count > 0)
		{
			_listTempCharacterData.Sort(delegate (CharacterData x, CharacterData y)
			{
				if (x.cachedActorTableData.grade > y.cachedActorTableData.grade) return -1;
				else if (x.cachedActorTableData.grade < y.cachedActorTableData.grade) return 1;
				if (x.transcend > y.transcend) return -1;
				else if (x.transcend < y.transcend) return 1;
				if (x.level > y.level) return -1;
				else if (x.level < y.level) return 1;
				if (x.cachedActorTableData.orderIndex > y.cachedActorTableData.orderIndex) return 1;
				else if (x.cachedActorTableData.orderIndex < y.cachedActorTableData.orderIndex) return -1;
				return 0;
			});

			for (int i = 0; i < _listTempCharacterData.Count; ++i)
			{
				MissionCanvasCharacterListItem missionCanvasCharacterListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
				missionCanvasCharacterListItem.Initialize(_listTempCharacterData[i].actorId, _listTempCharacterData[i].level, _listTempCharacterData[i].transcend, true, OnClickListItem);
				_listMissionCanvasCharacterListItem.Add(missionCanvasCharacterListItem);
			}
		}

		if (_listSelectedActorId != null)
			_listSelectedActorId.Clear();

		selectStartText.gameObject.SetActive(_listMissionCanvasCharacterListItem.Count > 0);
		selectResultText.text = "";

		if (onEnable)
		{
			List<string> listLastCharacterId = GetCachedLastCharacterList();
			if (listLastCharacterId != null)
			{
				for (int i = 0; i < listLastCharacterId.Count; ++i)
					OnClickListItem(listLastCharacterId[i]);
			}
		}
		//else
		//	OnClickListItem(_selectedActorId);
	}

	public void OnClickListItem(string actorId)
	{
		if (_listSelectedActorId == null)
			_listSelectedActorId = new List<string>();

		if (_listSelectedActorId.Contains(actorId))
			_listSelectedActorId.Remove(actorId);
		else
		{
			if (_listSelectedActorId.Count == SELECT_MAX)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_CannotSelectMore"), 2.0f);
				return;
			}

			/*
			// 진입시 셀렉트 로직에서 불러오기가 막혀버리는 현상이 발생해서 주석처리 하기로 한다.
			#region Stage Limit
			if (_missionModeTableData.levelLimit > 0)
			{
				CharacterData characterData = CharacterManager.instance.GetCharacterData(actorId);
				if (characterData != null && characterData.level < _missionModeTableData.levelLimit)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_CannotEnterSelect"), 2.0f);
					return;
				}
			}
			else if (_missionModeTableData.transcendLimit > 0)
			{
				CharacterData characterData = CharacterManager.instance.GetCharacterData(actorId);
				if (characterData != null && characterData.transcend < _missionModeTableData.transcendLimit)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_CannotEnterSelect"), 2.0f);
					return;
				}
			}
			#endregion
			*/

			_listSelectedActorId.Add(actorId);
		}

		//bool recommanded = false;
		for (int i = 0; i < _listMissionCanvasCharacterListItem.Count; ++i)
		{
			bool showSelectObject = _listSelectedActorId.Contains(_listMissionCanvasCharacterListItem[i].characterCanvasListItem.actorId);
			_listMissionCanvasCharacterListItem[i].characterCanvasListItem.selectObject.SetActive(showSelectObject);

			if (showSelectObject)
			{
				int findIndex = _listSelectedActorId.IndexOf(_listMissionCanvasCharacterListItem[i].characterCanvasListItem.actorId);
				_listMissionCanvasCharacterListItem[i].SetNumber(true, findIndex + 1);
			}
			else
			{
				_listMissionCanvasCharacterListItem[i].SetNumber(false, -1);
			}
		}

		selectStartText.gameObject.SetActive(_listSelectedActorId.Count == 0);
		selectResultText.gameObject.SetActive(_listSelectedActorId.Count > 0);

		string firstText = "";
		//if (MainSceneBuilder.instance.lobby == false && BattleInstanceManager.instance.IsInBattlePlayerList(actorId))
		//	firstText = UIString.instance.GetString("GameUI_FirstSwapHealNotApplied");

		string secondText = "";
		/*
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar()) { }
		else if (_bossChapterTableData != null)
		{
			CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
			if (characterData.powerLevel > _bossChapterTableData.suggestedMaxPowerLevel)
				secondText = UIString.instance.GetString("BossUI_TooPowerfulToReward"); // GameUI_TooPowerfulToReward
			else if (characterData.powerLevel < _bossChapterTableData.suggestedPowerLevel && recommanded)
				secondText = UIString.instance.GetString("GameUI_TooWeakToBoss");
		}
		*/
		bool firstResult = string.IsNullOrEmpty(firstText);
		bool secondResult = string.IsNullOrEmpty(secondText);
		if (firstResult && secondResult)
			selectResultText.text = "";
		else if (firstResult == false && secondResult)
			selectResultText.SetLocalizedText(firstText);
		else if (firstResult && secondResult == false)
			selectResultText.SetLocalizedText(secondText);
		else
			selectResultText.SetLocalizedText(string.Format("{0}\n{1}", firstText, secondText));

		selectResultText.text = string.Format("{0} / {1}", _listSelectedActorId.Count, SELECT_MAX);
	}

	public void OnClickTitleInfoButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("MissionUI_RobotDefenseTitleMore"), 300, titleTextTransform, new Vector2(0.0f, -35.0f));
	}

	List<string> _listResultItemValue;
	List<int> _listResultItemCount;
	List<ObscuredString> _listResultEventItemIdForPacket;
	public void OnClickLevelUpButton()
	{
		if (_updateValueText)
			return;
		if (_robotDefenseStepTableData == null)
			return;

		if (monsterKillSlider.value < 1.0f)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_NotEnoughMonsterKill"), 2.0f);
			return;
		}

		// packet
		if (_listResultItemValue == null)
			_listResultItemValue = new List<string>();
		_listResultItemValue.Clear();
		if (_listResultItemCount == null)
			_listResultItemCount = new List<int>();
		_listResultItemCount.Clear();
		if (_listResultEventItemIdForPacket == null)
			_listResultEventItemIdForPacket = new List<ObscuredString>();
		_listResultEventItemIdForPacket.Clear();

		if (_robotDefenseStepTableData.rewardType1 == "it")
		{
			_listResultItemValue.Add(_robotDefenseStepTableData.rewardValue1);
			_listResultItemCount.Add(_robotDefenseStepTableData.rewardCount1);
			for (int j = 0; j < _robotDefenseStepTableData.rewardCount1; ++j)
				_listResultEventItemIdForPacket.Add(_robotDefenseStepTableData.rewardValue1);
		}
		if (_robotDefenseStepTableData.rewardType2 == "it")
		{
			_listResultItemValue.Add(_robotDefenseStepTableData.rewardValue2);
			_listResultItemCount.Add(_robotDefenseStepTableData.rewardCount2);
			for (int j = 0; j < _robotDefenseStepTableData.rewardCount2; ++j)
				_listResultEventItemIdForPacket.Add(_robotDefenseStepTableData.rewardValue2);
		}

		PlayFabApiManager.instance.RequestRobotDefenseStepUp(_robotDefenseStepTableData.monCount, _listResultEventItemIdForPacket, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);

			// stepTableData 새거로 갱신하기 전에 컨슘 먼저 등록
			ConsumeProductProcessor.instance.ConsumeGacha(_listResultItemValue, _listResultItemCount);

			RefreshInfo();
			RefreshStep();
			RefreshAlarmObject();
		});
	}

	public void OnClickUpgradeButton()
	{
		if (_updateValueText)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("DroneUpgradeCanvas", null);
	}

	public static int s_killCountAddValue = 0;
	RobotDefenseStepTableData _robotDefenseStepTableData;
	List<MissionCanvasRewardIcon> _listMissionCanvasRewardIcon = new List<MissionCanvasRewardIcon>();
	void RefreshStep()
	{
		if (_repeatMode)
		{
			stepText.text = string.Format("REPEAT < {0:N0} >", SubMissionData.instance.robotDefenseRepeatClearCount + 1);
			_robotDefenseStepTableData = TableDataManager.instance.FindRobotDefenseStepTableData(RobotDefenseRepeatStep);
		}
		else
		{
			stepText.text = string.Format("STEP < {0:N0} >", _currentStep);
			_robotDefenseStepTableData = TableDataManager.instance.FindRobotDefenseStepTableData(_currentStep);
		}
		if (_robotDefenseStepTableData == null)
			return;

		if (s_killCountAddValue > 0)
		{
			// 연출을 위한 상태라면 들어갈때 낮게 잡고 연출로 표시해준다.
			int prevKillCount = SubMissionData.instance.robotDefenseKillCount - s_killCountAddValue;
			monsterKillCountText.text = string.Format("{0:N0} / {1:N0}", prevKillCount, _robotDefenseStepTableData.monCount);
			SetTargetKillValue(prevKillCount, SubMissionData.instance.robotDefenseKillCount);

			float prevRatio = (float)prevKillCount / _robotDefenseStepTableData.monCount;
			if (prevRatio > 1.0f) prevRatio = 1.0f;
			monsterKillSlider.value = prevRatio;
			if (prevRatio < 1.0f)
			{
				float ratio = (float)SubMissionData.instance.robotDefenseKillCount / _robotDefenseStepTableData.monCount;
				if (ratio > 1.0f) ratio = 1.0f;
				DOTween.To(() => monsterKillSlider.value, x => monsterKillSlider.value = x, ratio, valueChangeTime).SetEase(Ease.OutQuad).OnComplete(() =>
				{
					monsterKillSlider.value = ratio;
					monsterKillSliderImage.color = (ratio < 1.0f) ? Color.white : new Color(1.0f, 1.0f, 0.15f);
					RefreshAlarmObject();
				});
			}

			s_killCountAddValue = 0;
		}
		else
		{
			monsterKillCountText.text = string.Format("{0:N0} / {1:N0}", SubMissionData.instance.robotDefenseKillCount, _robotDefenseStepTableData.monCount);
			float ratio = (float)SubMissionData.instance.robotDefenseKillCount / _robotDefenseStepTableData.monCount;
			if (ratio > 1.0f) ratio = 1.0f;
			monsterKillSlider.value = ratio;
			monsterKillSliderImage.color = (ratio < 1.0f) ? Color.white : new Color(1.0f, 1.0f, 0.15f);
		}

		// 현재 선택된 난이도에 따른 보상을 보여준다.
		for (int i = 0; i < _listMissionCanvasRewardIcon.Count; ++i)
			_listMissionCanvasRewardIcon[i].gameObject.SetActive(false);
		_listMissionCanvasRewardIcon.Clear();

		rewardIcon1.gameObject.SetActive(false);
		rewardIcon2.gameObject.SetActive(false);
		rewardIconForDronePoint.gameObject.SetActive(false);

		// reward
		if (string.IsNullOrEmpty(_robotDefenseStepTableData.rewardType1) == false)
		{
			rewardIcon1.RefreshReward(_robotDefenseStepTableData.rewardType1, _robotDefenseStepTableData.rewardValue1, _robotDefenseStepTableData.rewardCount1);
			rewardIcon1.gameObject.SetActive(true);
		}
		if (string.IsNullOrEmpty(_robotDefenseStepTableData.rewardType2) == false)
		{
			rewardIcon2.RefreshReward(_robotDefenseStepTableData.rewardType2, _robotDefenseStepTableData.rewardValue2, _robotDefenseStepTableData.rewardCount2);
			rewardIcon2.gameObject.SetActive(true);
		}
		if (_robotDefenseStepTableData.dronePoint > 0)
		{
			rewardIconForDronePoint.countText.text = _robotDefenseStepTableData.dronePoint.ToString("N0");
			rewardIconForDronePoint.gameObject.SetActive(true);
		}
	}

	public void SetTargetKillValue(int prevValue, int targetValue)
	{
		_currentValue = prevValue;
		_targetValue = targetValue;

		int diff = (int)(_targetValue - _currentValue);
		_valueChangeSpeed = diff / valueChangeTime;
		_updateValueText = true;
	}

	const float valueChangeTime = 1.2f;
	float _valueChangeSpeed = 0.0f;
	float _currentValue;
	int _lastValue;
	int _targetValue;
	bool _updateValueText;
	void UpdateTargetKillValue()
	{
		if (_updateValueText == false)
			return;

		_currentValue += _valueChangeSpeed * Time.deltaTime;
		int currentValueInt = (int)_currentValue;
		if (currentValueInt >= _targetValue)
		{
			currentValueInt = _targetValue;
			monsterKillCountText.text = string.Format("{0:N0} / {1:N0}", _targetValue, _robotDefenseStepTableData.monCount);
			_updateValueText = false;
		}
		if (currentValueInt != _lastValue)
		{
			_lastValue = currentValueInt;
			monsterKillCountText.text = string.Format("{0:N0} / {1:N0}", _lastValue, _robotDefenseStepTableData.monCount);
		}
	}

	public static bool IsUpgradableDrone()
	{
		if (DroneUpgradeCanvas.GetRemainDronePoint() > 0)
			return true;
		return false;
	}

	public void RefreshAlarmObject()
	{
		AlarmObject.Hide(levelUpAlarmRootTransform);
		if (monsterKillSlider.value >= 1.0f)
			AlarmObject.Show(levelUpAlarmRootTransform);

		AlarmObject.Hide(upgradeAlarmRootTransform);
		if (IsUpgradableDrone())
			AlarmObject.Show(upgradeAlarmRootTransform);
	}


	void RefreshCoolTime()
	{
		bool applyCoolTime = (ServerTime.UtcNow < SubMissionData.instance.robotDefenseCoolExpireTime);
		enterButtonImage.color = !applyCoolTime ? Color.white : ColorUtil.halfGray;
		enterText.color = !applyCoolTime ? Color.white : Color.gray;

		if (applyCoolTime)
		{
			_needUpdate = true;
			_coolExpireDateTime = SubMissionData.instance.robotDefenseCoolExpireTime;
			enterText.text = "";
		}
		else
		{
			_needUpdate = false;
			enterText.text = "ENTER";
		}
	}

	bool _needUpdate = false;
	DateTime _coolExpireDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateRemainCoolTime()
	{
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _coolExpireDateTime)
		{
			TimeSpan remainTime = _coolExpireDateTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				enterText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			_needUpdate = false;
			RefreshCoolTime();
		}
	}



	List<string> _listSelectedActorId;
	public List<string> listSelectedActorId { get { return _listSelectedActorId; } }
	public void OnClickYesButton()
	{
		if (_moveProcessed)
			return;

		if (AdventureListCanvas.IsAlarmRobotDefense() == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TodayCountComplete"), 2.0f);
			return;
		}

		if (_needUpdate)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_RobotDefenseCoolTime"), 2.0f);
			return;
		}

		int selectedActorCount = 0;
		if (_listSelectedActorId != null)
			selectedActorCount = _listSelectedActorId.Count;

		if (selectedActorCount < MINIMUM_COUNT)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_RobotDefenseSelectMinimum"), 2.0f);
			return;
		}

		bool checkLimitCondition = false;
		for (int i = 0; i < _listMissionCanvasCharacterListItem.Count; ++i)
		{
			if (_listMissionCanvasCharacterListItem[i].redTextRootObject.activeSelf)
			{
				checkLimitCondition = true;
				break;
			}
		}
		if (checkLimitCondition)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_CannotEnter"), 2.0f);
			return;
		}

		// selectable max count
		if (selectedActorCount < SELECT_MAX && selectedActorCount < _listMissionCanvasCharacterListItem.Count)
		{
			YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("MissionUI_SelectMoreConfirm"), () =>
			{
				Timing.RunCoroutine(BattleMoveProcess());
			});
			return;
		}

		Timing.RunCoroutine(BattleMoveProcess());
	}

	bool _moveProcessed;
	IEnumerator<float> BattleMoveProcess()
	{
		_moveProcessed = true;

		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		StageManager.instance.FinalizeStage();
		TeamManager.instance.HideForMoveMap(true);

		yield return Timing.WaitForSeconds(0.1f);

		if (this == null)
			yield break;

		StageManager.instance.InitializeMissionStage(BattleInstanceManager.instance.GetCachedGlobalConstantInt("RobotDefenseStageId"), true);
		//TeamManager.instance.HideForMoveMap(false);
		TeamManager.instance.ClearTeamPlayerActorForMission();
		RecordLastCharacterList();
		//PlayFabApiManager.instance.RequestSelectRobotDefenseMission(_selectedDifficulty);

		yield return Timing.WaitForSeconds(0.2f);

		CameraFovController.instance.enabled = false;
		CustomFollowCamera.instance.GetComponent<Camera>().fieldOfView += 13.0f;
		CustomFollowCamera.instance.cachedTransform.Rotate(-2.0f, 0.0f, 0.0f);
		CustomFollowCamera.instance.distanceToTarget -= 15.0f;
		CustomFollowCamera.instance.immediatelyUpdate = true;

		// 보스전처럼
		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		UIInstanceManager.instance.ShowCanvasAsync("RobotDefenseMissionCanvas", () =>
		{
			DelayedLoadingCanvas.Show(false);
			FadeCanvas.instance.FadeIn(0.5f);
		});

		_moveProcessed = false;
	}

	#region Record Last Character
	void RecordLastCharacterList()
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string value = serializer.SerializeObject(_listSelectedActorId);
		ObscuredPrefs.SetString(string.Format("_rbdEnterCanvas_{0}", PlayFabApiManager.instance.playFabId), value);
	}

	List<string> GetCachedLastCharacterList()
	{
		string cachedLastCharacterList = ObscuredPrefs.GetString(string.Format("_rbdEnterCanvas_{0}", PlayFabApiManager.instance.playFabId));
		if (string.IsNullOrEmpty(cachedLastCharacterList))
			return null;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		return serializer.DeserializeObject<List<string>>(cachedLastCharacterList);
	}
	#endregion
}