using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using DG.Tweening;
using MEC;

public class RushDefenseEnterCanvas : MonoBehaviour
{
	public static RushDefenseEnterCanvas instance;

	const int SELECT_MAX = 3;

	public Transform titleTextTransform;
	public Text levelText;
	public DOTweenAnimation levelChangeTweenAnimation;
	public GameObject newObject;

	public Image leftButtonImage;
	public Image rightButtonImage;
	
	public Text suggestPowerLevelText;
	public Text stagePenaltyText;
	public GameObject selectStartText;
	public Text selectResultText;

	public Text priceText;
	public GameObject buttonObject;

	public GameObject rewardContentItemPrefab;
	public RectTransform rewardContentRootRectTransform;

	public class RewardCustomItemContainer : CachedItemHave<MissionCanvasRewardIcon>
	{
	}
	RewardCustomItemContainer _rewardContainer = new RewardCustomItemContainer();

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
		rewardContentItemPrefab.SetActive(false);

		/*
		if (EventManager.instance.reservedOpenBossBattleEvent)
		{
			UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
			{
				EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("BossBattleUI_PopupName"), UIString.instance.GetString("BossBattleUI_PopupDesc"), UIString.instance.GetString("BossBattleUI_PopupMore"), null, 0.785f);
			});
			EventManager.instance.reservedOpenBossBattleEvent = false;
			EventManager.instance.CompleteServerEvent(EventManager.eServerEvent.boss);
			LobbyCanvas.instance.HideSubMenuAlarmObject(0);
		}
		*/

		// 시작하자마자 트윈 제대로 발동하게 하려면 Start에서 한번 호출해줘야한다.
		levelChangeTweenAnimation.DORestart();
	}

	void OnEnable()
	{
		RefreshInfo();
		RefreshDifficulty();
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
	
	public int selectedDifficulty { get { return _selectedDifficulty; } }
	public int selectableMaxDifficulty { get { return _selectableMaxDifficulty; } }
	StageTableData _rushDefenseStageTableData;
	ObscuredInt _selectedDifficulty;
	ObscuredInt _selectableMaxDifficulty;
	void RefreshInfo()
	{
		int clearLevel = SubMissionData.instance.rushDefenseClearLevel;

		// 최대 가능 레벨은 클리어 한거보다 하나 더 위다.
		_selectableMaxDifficulty = clearLevel + 1;

		// 그래도 Max를 넘을 순 없다.
		if (_selectableMaxDifficulty > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxRushDefense"))
			_selectableMaxDifficulty = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxRushDefense");

		// 라스트가 있으면 라스트를 부르고 없으면 첫번째인 1로 설정.
		_selectedDifficulty = SubMissionData.instance.rushDefenseSelectedLevel;
		if (_selectedDifficulty == 0)
			_selectedDifficulty = 1;


		/*
		int currentBossId = ContentsData.instance.bossBattleId;
		if (currentBossId == 0)
		{
			// 0이라면 처음 보스배틀을 시작하는 유저일거다.
			// 1번 몬스터를 가져와서 셋팅한다.
			currentBossId = 1;
		}

		_bossBattleTableData = TableDataManager.instance.FindBossBattleData(currentBossId);
		if (_bossBattleTableData == null)
			return;

		int clearDifficulty = ContentsData.instance.GetBossBattleClearDifficulty(currentBossId.ToString());
		_selectedDifficulty = ContentsData.instance.GetBossBattleSelectedDifficulty(currentBossId.ToString());
		if (_selectedDifficulty == 0)
		{
			// _selectedDifficulty이면 한번도 플레이 안했다는거니 bossBattleTable에서 시작 챕터를 가져와야한다.
			_selectedDifficulty = _bossBattleTableData.chapter;
		}
		// 선택한게 클리어난이도+1 보다 크면 뭔가 이상한거다. 조정해준다.
		// 이제 챕터의 난이도에서 시작하게 되면서 이 로직을 사용할 수 없게 되었다.
		//if (_selectedDifficulty > (clearDifficulty + 1))
		//	_selectedDifficulty = (clearDifficulty + 1);

		int bossBattleCount = ContentsData.instance.GetBossBattleCount(currentBossId.ToString());


		StageTableData bossStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(_bossBattleTableData.chapter, _bossBattleTableData.stage, false);
		if (bossStageTableData == null)
			return;
		MapTableData bossMapTableData = BattleInstanceManager.instance.GetCachedMapTableData(bossStageTableData.firstFixedMap);
		if (bossMapTableData == null)
			return;
		// 챕터 테이블은 권장 레벨 표기를 위한거라 선택된 난이도로 구해오는게 맞다.
		ChapterTableData chapterTableData = TableDataManager.instance.FindChapterTableData(_selectedDifficulty);
		if (chapterTableData == null)
			return;

		_bossStageTableData = bossStageTableData;
		_bossMapTableData = bossMapTableData;
		_bossChapterTableData = chapterTableData;
		levelText.text = string.Format("<size=24>DIFFICULTY</size> {0}", _selectedDifficulty);
		newObject.SetActive(_selectedDifficulty > clearDifficulty);

		int selectableDifficultyCount = clearDifficulty - _bossBattleTableData.chapter + 2;
		changeDifficultyButtonObject.SetActive(selectableDifficultyCount > 1);
		_clearDifficulty = clearDifficulty;
		if (changeDifficultyButtonObject.activeSelf)
		{
			AlarmObject.Hide(alarmRootTransform);
			if (_selectedDifficulty != (clearDifficulty + 1) && ChangeDifficultyCanvasListItem.CheckSelectable(_clearDifficulty + 1) == 0)
				AlarmObject.Show(alarmRootTransform, false, false, true);
		}

		if (string.IsNullOrEmpty(bossMapTableData.bossName) == false)
		{
			AddressableAssetLoadManager.GetAddressableGameObject(string.Format("Preview_{0}", bossMapTableData.bossName), "Preview", (prefab) =>
			{
				_cachedPreviewObject = UIInstanceManager.instance.GetCachedObject(prefab, previewRootTransform);
			});
		}
		bossNameText.SetLocalizedText(UIString.instance.GetString(bossMapTableData.nameId));

		RefreshBossBattleCount(bossBattleCount);

		// 패널티를 구할땐 그냥 스테이지 테이블에서 구해오면 안되고 선택된 난이도의 1층을 구해와서 처리해야한다.
		StageTableData penaltyStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(_selectedDifficulty, 1, false);
		if (penaltyStageTableData == null)
			return;
		*/

		stagePenaltyText.gameObject.SetActive(false);
		/*
		string penaltyString = ChapterCanvas.GetPenaltyString(penaltyStageTableData);
		if (string.IsNullOrEmpty(penaltyString) == false)
		{
			stagePenaltyText.SetLocalizedText(penaltyString);
			stagePenaltyText.gameObject.SetActive(true);
		}
		*/

		RefreshPrice();
	}
	
	void RefreshPrice()
	{
		// 가격
		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyRushDefense");
		priceText.text = price.ToString("N0");
		_price = price;
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
			if (TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_PlayerActorId)
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
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("MissionUI_RushDefenseTitleMore"), 300, titleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickLeftButton()
	{
		if (_selectedDifficulty > 1)
		{
			_selectedDifficulty -= 1;
			RefreshDifficulty();
		}
	}

	public void OnClickRightButton()
	{
		if (_selectedDifficulty < _selectableMaxDifficulty)
		{
			_selectedDifficulty += 1;
			RefreshDifficulty();
		}
	}

	public int expectedReward { get { return _expectedReward; } }
	ObscuredInt _expectedReward;
	MissionModeTableData _missionModeTableData;
	List<MissionCanvasRewardIcon> _listMissionCanvasRewardIcon = new List<MissionCanvasRewardIcon>();
	void RefreshDifficulty()
	{
		levelText.text = _selectedDifficulty.ToString("N0");
		levelChangeTweenAnimation.DORestart();

		leftButtonImage.color = (_selectedDifficulty == 1) ? Color.gray : Color.white;
		rightButtonImage.color = (_selectedDifficulty == _selectableMaxDifficulty) ? Color.gray : Color.white;
		newObject.SetActive(_selectedDifficulty == _selectableMaxDifficulty && _selectedDifficulty > SubMissionData.instance.rushDefenseClearLevel);

		// 현재 선택된 난이도에 따른 보상을 보여준다.
		MissionModeTableData missionModeTableData = TableDataManager.instance.FindMissionModeTableData((int)SubMissionData.eSubMissionType.RushDefense, _selectedDifficulty);
		if (missionModeTableData == null)
			return;
		_missionModeTableData = missionModeTableData;

		for (int i = 0; i < _listMissionCanvasRewardIcon.Count; ++i)
			_listMissionCanvasRewardIcon[i].gameObject.SetActive(false);
		_listMissionCanvasRewardIcon.Clear();

		_expectedReward = 0;
		if (newObject.activeSelf)
		{
			// first
			if (string.IsNullOrEmpty(missionModeTableData.firstRewardType1) == false)
			{
				MissionCanvasRewardIcon missionCanvasRewardIcon = _rewardContainer.GetCachedItem(rewardContentItemPrefab, rewardContentRootRectTransform);
				missionCanvasRewardIcon.rewardIcon.RefreshReward(missionModeTableData.firstRewardType1, missionModeTableData.firstRewardValue1, missionModeTableData.firstRewardCount1);
				missionCanvasRewardIcon.firstObject.SetActive(true);
				_listMissionCanvasRewardIcon.Add(missionCanvasRewardIcon);
				_expectedReward += missionModeTableData.firstRewardCount1;
			}
			if (string.IsNullOrEmpty(missionModeTableData.firstRewardType2) == false)
			{
				MissionCanvasRewardIcon missionCanvasRewardIcon = _rewardContainer.GetCachedItem(rewardContentItemPrefab, rewardContentRootRectTransform);
				missionCanvasRewardIcon.rewardIcon.RefreshReward(missionModeTableData.firstRewardType2, missionModeTableData.firstRewardValue2, missionModeTableData.firstRewardCount2);
				missionCanvasRewardIcon.firstObject.SetActive(true);
				_listMissionCanvasRewardIcon.Add(missionCanvasRewardIcon);
				_expectedReward += missionModeTableData.firstRewardCount2;
			}
			if (string.IsNullOrEmpty(missionModeTableData.firstRewardType3) == false)
			{
				MissionCanvasRewardIcon missionCanvasRewardIcon = _rewardContainer.GetCachedItem(rewardContentItemPrefab, rewardContentRootRectTransform);
				missionCanvasRewardIcon.rewardIcon.RefreshReward(missionModeTableData.firstRewardType3, missionModeTableData.firstRewardValue3, missionModeTableData.firstRewardCount3);
				missionCanvasRewardIcon.firstObject.SetActive(true);
				_listMissionCanvasRewardIcon.Add(missionCanvasRewardIcon);
				_expectedReward += missionModeTableData.firstRewardCount3;
			}
		}

		// repeat
		if (string.IsNullOrEmpty(missionModeTableData.rewardType1) == false)
		{
			MissionCanvasRewardIcon missionCanvasRewardIcon = _rewardContainer.GetCachedItem(rewardContentItemPrefab, rewardContentRootRectTransform);
			missionCanvasRewardIcon.rewardIcon.RefreshReward(missionModeTableData.rewardType1, missionModeTableData.rewardValue1, missionModeTableData.rewardCount1);
			missionCanvasRewardIcon.firstObject.SetActive(false);
			_listMissionCanvasRewardIcon.Add(missionCanvasRewardIcon);
			_expectedReward += missionModeTableData.rewardCount1;
		}
		if (string.IsNullOrEmpty(missionModeTableData.rewardType2) == false)
		{
			MissionCanvasRewardIcon missionCanvasRewardIcon = _rewardContainer.GetCachedItem(rewardContentItemPrefab, rewardContentRootRectTransform);
			missionCanvasRewardIcon.rewardIcon.RefreshReward(missionModeTableData.rewardType2, missionModeTableData.rewardValue2, missionModeTableData.rewardCount2);
			missionCanvasRewardIcon.firstObject.SetActive(false);
			_listMissionCanvasRewardIcon.Add(missionCanvasRewardIcon);
			_expectedReward += missionModeTableData.rewardCount2;
		}
	}



	int _price;
	List<string> _listSelectedActorId;
	public List<string> listSelectedActorId { get { return _listSelectedActorId; } }
	public void OnClickYesButton()
	{
		if (_moveProcessed)
			return;

		int selectedActorCount = 0;
		if (_listSelectedActorId != null)
			selectedActorCount = _listSelectedActorId.Count;

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

		StageManager.instance.InitializeMissionStage(_missionModeTableData.stage);
		//TeamManager.instance.HideForMoveMap(false);
		TeamManager.instance.ClearTeamPlayerActorForMission();
		RecordLastCharacterList();

		yield return Timing.WaitForSeconds(0.2f);

		// 보스전처럼
		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		UIInstanceManager.instance.ShowCanvasAsync("RushDefenseMissionCanvas", () =>
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
		ObscuredPrefs.SetString(string.Format("_rdEnterCanvas_{0}", PlayFabApiManager.instance.playFabId), value);
	}

	List<string> GetCachedLastCharacterList()
	{
		string cachedLastCharacterList = ObscuredPrefs.GetString(string.Format("_rdEnterCanvas_{0}", PlayFabApiManager.instance.playFabId));
		if (string.IsNullOrEmpty(cachedLastCharacterList))
			return null;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		return serializer.DeserializeObject<List<string>>(cachedLastCharacterList);
	}
	#endregion
}