using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using MEC;

public class MainCanvas : MonoBehaviour
{
	public static MainCanvas instance;

	public RectTransform[] basePositionRectTransformList;
	public RectTransform[] targetPositionRectTransformList;
	public float noInputTime = 15.0f;

	public GameObject teamButtonObject;
	public GameObject petButtonObject;
	public GameObject equipButtonObject;

	public RectTransform challengeButtonRootTransform;
	public GameObject challengeButtonObject;
	public GameObject bossBattleMenuRootObject;
	public GameObject fastBossClearObject;
	public Text fastBossClearCurrentStageValueText;

	public GameObject inputRectObject;
	public CanvasGroup safeAreaCanvasGroup;
	public CanvasGroup questInfoCanvasGroup;

	public GameObject downloadButtonObject;

	public GameObject levelPassButtonObject;
	public GameObject brokenEnergyButtonObject;
	public GameObject sevenDaysButtonObject;
	public GameObject festivalButtonObject;
	public GameObject attendanceButtonObject;
	public GameObject firstPurchaseButtonObject;
	public Transform cashEventButtonRootTransform;
	public CashEventButton[] cashEventButtonList;


	public RectTransform playerAlarmRootTransform;
	public RectTransform spellAlarmRootTransform;
	public RectTransform characterAlarmRootTransform;
	public RectTransform petAlarmRootTransform;
	public RectTransform equipAlarmRootTransform;
	public RectTransform mailAlarmRootTransform;
	public RectTransform analysisAlarmRootTransform;
	public RectTransform gachaAlarmRootTransform;
	public RectTransform missionAlarmRootTransform;
	public RectTransform cashShopAlarmRootTransform;

	public RectTransform downloadAlarmRootTransform;
	public RectTransform levelPassAlarmRootTransform;
	public RectTransform sevenDaysAlarmRootTransform;
	public RectTransform festivalAlarmRootTransform;
	public RectTransform firstPurchaseAlarmRootTransform;
	public RectTransform attendanceAlarmRootTransform;
	public RectTransform energyPaybackAlarmRootTransform;   // ev6

	public RectTransform continuousProduct1AlarmRootTransform;  // ev4
	public RectTransform onePlusTwo1AlarmRootTransform;  // ev5


	List<Vector2> _listBasePosition = new List<Vector2>();
	void Awake()
	{
		instance = this;

		for (int i = 0; i < basePositionRectTransformList.Length; ++i)
			_listBasePosition.Add(basePositionRectTransformList[i].anchoredPosition);
	}

	void OnEnable()
	{
		_noInputRemainTime = noInputTime;

		for (int i = 0; i < basePositionRectTransformList.Length; ++i)
			basePositionRectTransformList[i].anchoredPosition = _listBasePosition[i];

		RefreshCashButton();
		RefreshCashAdditionalButton();
		RefreshMenuButton();
		RefreshAlarmObjectList();
	}

	// Start is called before the first frame update
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		UpdateNoInput();
	}

	float _noInputRemainTime = 0.0f;
	bool _buttonHideState = false;
	void UpdateNoInput()
	{
		if (IsHideState())
			return;

		if (_noInputRemainTime > 0.0f)
		{
			_noInputRemainTime -= Time.deltaTime;
			if (_noInputRemainTime <= 0.0f)
			{
				_buttonHideState = true;
				_noInputRemainTime = 0.0f;
			}
		}

		for (int i = 0; i < basePositionRectTransformList.Length; ++i)
			basePositionRectTransformList[i].anchoredPosition = Vector3.Lerp(basePositionRectTransformList[i].anchoredPosition, _buttonHideState ? targetPositionRectTransformList[i].anchoredPosition : _listBasePosition[i], Time.deltaTime * 5.0f);
	}

	public void OnPointerDown(BaseEventData baseEventData)
	{
		if (StageManager.instance.repeatMode == false)
			return;

		_buttonHideState = false;
		_noInputRemainTime = noInputTime;
	}


	public void OnClickBackButton()
	{
		if (LoadingCanvas.instance != null && LoadingCanvas.instance.gameObject.activeSelf)
			return;

		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
		{
			FullscreenYesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_ExitGame"), UIString.instance.GetString("GameUI_ExitGameDescription"), () => {
				Application.Quit();
			});
		}
		else
		{
			/*
			if (battlePauseButton.gameObject.activeSelf && battlePauseButton.interactable)
				OnClickBattlePauseButton();
			*/
		}
	}

	public void OnClickCloseButton()
	{
		if (_noInputRemainTime > 0.0f)
			_noInputRemainTime = 0.001f;
	}

	public void OnClickOptionButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("SettingCanvas", null);
	}


	#region Boss Challenge
	public void OnClickBossChallengeButton()
	{
		if (PlayerData.instance.selectedStage >= TableDataManager.instance.GetGlobalConstantInt("MaxStage"))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxStageWaitUpdate"), 2.0f);
			return;
		}

		if (_challengeProcessed)
			return;

		// 언제 어느때든 누를 수 있다.
		OnPointerDown(null);
		Timing.RunCoroutine(ChallengeProcess());
	}

	bool _challengeProcessed;
	bool _waitServerResponse;
	bool _enterGameServerFailure;
	bool _networkFailure;
	IEnumerator<float> ChallengeProcess()
	{
		_challengeProcessed = true;

		// 누른거와 동시에 패킷은 몰래 보내놓고
		PlayFabApiManager.instance.RequestEnterBoss((serverFailure) =>
		{
			if (_waitServerResponse)
			{
				// 인자값 에러. 서버 실패
				_enterGameServerFailure = serverFailure;
				_waitServerResponse = false;
			}
		}, () =>
		{
			if (_waitServerResponse)
			{
				// 그외 접속불가 네트워크 에러
				_networkFailure = true;
				_waitServerResponse = false;
			}
		});
		_waitServerResponse = true;

		// 화면 페이드 시작하고
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;

		StageManager.instance.FinalizeStage();
		TeamManager.instance.HideForMoveMap(true);

		if (_noInputRemainTime > 0.0f)
			_noInputRemainTime = 0.001f;

		yield return Timing.WaitForSeconds(0.1f);

		if (this == null)
			yield break;

		// 응답오면 모드 전환하고 페이드 풀어주면 되고
		// 응답 안오거나 에러로 되면 
		while (_waitServerResponse)
			yield return Timing.WaitForOneFrame;

		if (_enterGameServerFailure || _networkFailure)
		{
			FadeCanvas.instance.FadeIn(0.5f);
			_enterGameServerFailure = false;
			_networkFailure = false;
			yield break;
		}

		challengeButtonObject.SetActive(false);
		bossBattleMenuRootObject.SetActive(true);
		StageManager.instance.OnOffFastBossClear(false);
		StageManager.instance.InitializeStageFloor(PlayerData.instance.selectedStage, false);
		TeamManager.instance.HideForMoveMap(false);
		SpellManager.instance.InitializeEquipSpellInfo();
		FadeCanvas.instance.FadeIn(0.5f);

		// 보스전에 
		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		_challengeProcessed = false;
	}

	public void OnClickCancelBossChallengeButton()
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_BackToLobby"), UIString.instance.GetString("GameUI_BackToLobbyDescription"), () => {
			PlayFabApiManager.instance.RequestCancelBoss();
			OnPointerDown(null);
			ChangeStage(PlayerData.instance.selectedStage, true);
		});
	}

	public void ChangeStage(int stage, bool repeatMode)
	{
		Timing.RunCoroutine(ChangeStageProcess(stage, repeatMode));
	}

	IEnumerator<float> ChangeStageProcess(int stage, bool repeatMode)
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;

		StageManager.instance.FinalizeStage();
		TeamManager.instance.HideForMoveMap(true);

		if (_noInputRemainTime > 0.0f)
			_noInputRemainTime = 0.001f;

		yield return Timing.WaitForSeconds(0.1f);

		if (this == null)
			yield break;

		challengeButtonObject.SetActive(repeatMode);
		bossBattleMenuRootObject.SetActive(!repeatMode);
		StageManager.instance.InitializeStageFloor(stage, repeatMode);
		TeamManager.instance.HideForMoveMap(false);
		FadeCanvas.instance.FadeIn(0.5f);

		if (repeatMode)
		{
			if (EquipSkillSlotCanvas.instance != null && EquipSkillSlotCanvas.instance.gameObject.activeSelf)
				EquipSkillSlotCanvas.instance.gameObject.SetActive(false);

			// 반복모드로 돌아가는거라면 sleep모드 셋팅한 것도 풀어야한다.
			Screen.sleepTimeout = SleepTimeout.SystemSetting;

			OnPointerDown(null);
			if (stage == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxStage"))
			{
				// 최대 스테이지에 도달했음을 알린다.
				OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_MaxStageWaitUpdate"), null, -1, true);
			}
		}
		else
		{
			// 다음 보스로 넘어갈때 쿨타임 초기화 해야한다.
			SpellManager.instance.ReinitializeEquipSkill();
		}
	}
	#endregion

	#region Menu
	public void RefreshMenuButton()
	{
		teamButtonObject.SetActive(CharacterManager.instance.listCharacterData.Count > 0);
		petButtonObject.SetActive(PetManager.instance.listPetData.Count > 0);
		equipButtonObject.SetActive(EquipManager.instance.inventoryItemCount > 0);
	}

	public void OnClickCharacterButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("CharacterCanvas", null);
	}

	public void OnClickSpellButton()
	{
		if (SpellSpriteContainer.instance == null)
		{
			DelayedLoadingCanvas.Show(true);
			AddressableAssetLoadManager.GetAddressableGameObject("SpellSpriteContainer", "", (prefab) =>
			{
				BattleInstanceManager.instance.GetCachedObject(prefab, null);
				DelayedLoadingCanvas.Show(false);
				UIInstanceManager.instance.ShowCanvasAsync("SpellCanvas", null);
			});
		}
		else
			UIInstanceManager.instance.ShowCanvasAsync("SpellCanvas", null);
	}

	public void OnClickTeamButton()
	{
		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("CharacterListCanvas", null);
	}

	public void OnClickPetButton()
	{
		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		if (PetSpriteContainer.instance == null)
		{
			DelayedLoadingCanvas.Show(true);
			AddressableAssetLoadManager.GetAddressableGameObject("PetSpriteContainer", "", (prefab) =>
			{
				BattleInstanceManager.instance.GetCachedObject(prefab, null);
				DelayedLoadingCanvas.Show(false);
				MissionListCanvas.ShowCanvasAsyncWithPrepareGround("PetListCanvas", null);
			});
		}
		else
			MissionListCanvas.ShowCanvasAsyncWithPrepareGround("PetListCanvas", null);
	}

	public void OnClickEquipButton()
	{
		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		MissionListCanvas.ShowCanvasAsyncWithPrepareGround("EquipGroundCanvas", null);
	}

	public void OnEnterCharacterMenu(bool enter, bool ignoreStartEvent = false)
	{
		if (inputRectObject == null)
			return;
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

		inputRectObject.SetActive(!enter);
		safeAreaCanvasGroup.alpha = enter ? 0.0f : 1.0f;
		safeAreaCanvasGroup.blocksRaycasts = !enter;

		BossMonsterGaugeCanvas.OnEnterMenu(enter);

		if (enter == false)
		{
			if (ignoreStartEvent)
				return;

			RefreshAlarmObjectList();
			CashShopData.instance.CheckStartEvent(CashShopData.eEventStartCondition.OnCloseMainMenu);
		}
	}

	public bool IsHideState()
	{
		return (safeAreaCanvasGroup.alpha == 0.0f);
	}
	#endregion

	#region Sub Menu Button
	public void OnClickGachaButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("GachaCanvas", null);
	}

	public void OnClickAnalysisButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ResearchCanvas", null);
	} 

	public void OnClickMailButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("MailCanvas", null);
	}

	public void OnClickRankingButton()
	{
		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("RankingListCanvas", null);
	}

	public void OnClickContentsButton()
	{
		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("MissionListCanvas", null);
	}
	#endregion




	#region AlarmObject
	void RefreshAlarmObjectList()
	{
		RefreshCashShopAlarmObject();
		RefreshPlayerAlarmObject();
		RefreshSpellAlarmObject();
		RefreshCharacterAlarmObject();
		RefreshPetAlarmObject();
		RefreshEquipAlarmObject();
		RefreshMailAlarmObject();
		RefreshAnalysisAlarmObject();
		RefreshMissionAlarmObject();
		RefreshGachaAlarmObject();
		RefreshSevenDaysAlarmObject();
		RefreshFestivalAlarmObject();
		RefreshDownloadRewardAlarmObject();
		RefreshLevelPassAlarmObject();
		RefreshEnergyPaybackAlarmObject();
		RefreshContinuousProduct1AlarmObject();
		RefreshOnePlusTwo1AlarmObject();
		RefreshFirstPurchaseAlarmObject();
		RefreshAttendanceAlarmObject();
	}

	public static bool IsAlarmCashShop()
	{
		bool result = false;
		if (CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.DailyDiamond) > 0 && CashShopData.instance.dailyDiamondReceived == false)
			result = true;
		/*
		if (DailyShopData.instance.GetTodayFreeItemData() != null && DailyShopData.instance.dailyFreeItemReceived == false)
			result = true;
		if (PlayerData.instance.chaosFragmentCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosPowerPointsCost"))
		{
			for (int i = 0; i <= DailyShopData.ChaosSlotMax; ++i)
			{
				if (i <= DailyShopData.instance.chaosSlotUnlockLevel && DailyShopData.instance.IsPurchasedTodayChaosData(i) == false)
					return true;
			}
		}
		*/
		return result;
	}

	public void RefreshCashShopAlarmObject()
	{
		RefreshAlarmObject(IsAlarmCashShop(), cashShopAlarmRootTransform);
	}

	public static bool IsAlarmCharacter()
	{
		List<CharacterData> listCharacterData = CharacterManager.instance.listCharacterData;
		for (int i = 0; i < listCharacterData.Count; ++i)
		{
			if (listCharacterData[i].IsAlarmState())
				return true;
		}
		return false;
	}

	public void RefreshCharacterAlarmObject()
	{
		RefreshAlarmObject(IsAlarmCharacter(), characterAlarmRootTransform);
	}

	public static bool IsAlarmPet()
	{
		return PetListCanvas.CheckTodayHeart();
	}

	public void RefreshPetAlarmObject()
	{
		RefreshAlarmObject(IsAlarmPet(), petAlarmRootTransform);
	}

	public static bool IsAlarmPlayer()
	{
		return CharacterLevelCanvas.CheckLevelUp();
	}

	public void RefreshPlayerAlarmObject()
	{
		RefreshAlarmObject(IsAlarmPlayer(), playerAlarmRootTransform);
	}

	public static bool IsAlarmSpell()
	{
		return SpellCanvas.CheckLevelUp();
	}

	public void RefreshSpellAlarmObject()
	{
		RefreshAlarmObject(IsAlarmSpell(), spellAlarmRootTransform);
	}

	public static bool IsAlarmEquip()
	{
		return EquipGroundCanvas.CheckAutoEquip();
	}

	public void RefreshEquipAlarmObject()
	{
		RefreshAlarmObject(IsAlarmEquip(), equipAlarmRootTransform);
	}

	public static bool IsAlarmMail()
	{
		return MailData.instance.GetReceivableMailPresentCount() > 0;
	}

	public void RefreshMailAlarmObject()
	{
		RefreshAlarmObject(IsAlarmMail(), mailAlarmRootTransform);
	}

	public static bool IsAlarmAnalysis()
	{
		return ResearchInfoAnalysisCanvas.CheckAnalysis();
	}

	public void RefreshAnalysisAlarmObject()
	{
		RefreshAlarmObject(IsAlarmAnalysis(), analysisAlarmRootTransform);
	}

	public static bool IsAlarmMission()
	{
		if (MissionListCanvas.IsAlarmPetSearch() || MissionListCanvas.IsAlarmFortuneWheel())
			return true;
		return false;
	}

	public void RefreshMissionAlarmObject()
	{
		RefreshAlarmObject(IsAlarmMission(), missionAlarmRootTransform);
	}

	public static bool IsAlarmDownloadReward()
	{
		return (PlayerData.instance.downloadConfirmed && PlayerData.instance.downloadRewarded == false);
	}

	public void RefreshDownloadRewardAlarmObject()
	{
		RefreshAlarmObject(IsAlarmDownloadReward(), downloadAlarmRootTransform);
	}

	public static bool IsAlarmLevelPass()
	{
		if (CashShopData.instance.IsPurchasedFlag(CashShopData.eCashFlagType.LevelPass) == false)
		{
			if (CashShopData.instance.levelPassAlarmStateForNoPass)
				return true;
			return false;
		}

		bool getable = false;
		for (int i = 0; i < TableDataManager.instance.levelPassTable.dataArray.Length; ++i)
		{
			int level = TableDataManager.instance.levelPassTable.dataArray[i].level;
			if (level <= PlayerData.instance.playerLevel && CashShopData.instance.IsGetLevelPassReward(level) == false)
			{
				getable = true;
				break;
			}
		}

		return getable;
	}

	public void RefreshLevelPassAlarmObject()
	{
		RefreshAlarmObject(IsAlarmLevelPass(), levelPassAlarmRootTransform);
	}

	public static bool IsAlarmEnergyPayback()
	{
		bool getable = false;
		for (int i = 0; i < TableDataManager.instance.energyUsePaybackTable.dataArray.Length; ++i)
		{
			int use = TableDataManager.instance.energyUsePaybackTable.dataArray[i].use;
			if (use <= CashShopData.instance.energyUseForPayback && CashShopData.instance.IsGetEnergyPaybackReward(use) == false)
			{
				getable = true;
				break;
			}
		}

		return getable;
	}

	public void RefreshEnergyPaybackAlarmObject()
	{
		RefreshAlarmObject(IsAlarmEnergyPayback(), energyPaybackAlarmRootTransform);
	}

	public static bool IsAlarmSevenDays()
	{
		bool getable = false;
		for (int i = 0; i < TableDataManager.instance.sevenDaysRewardTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.sevenDaysRewardTable.dataArray[i].group != MissionData.instance.sevenDaysId)
				continue;
			if (MissionData.instance.IsOpenDay(TableDataManager.instance.sevenDaysRewardTable.dataArray[i].day) == false)
				continue;

			int currentCount = MissionData.instance.GetProceedingCount(TableDataManager.instance.sevenDaysRewardTable.dataArray[i].typeId);
			if (currentCount < TableDataManager.instance.sevenDaysRewardTable.dataArray[i].needCount)
				continue;
			if (MissionData.instance.IsGetSevenDaysReward(TableDataManager.instance.sevenDaysRewardTable.dataArray[i].day, TableDataManager.instance.sevenDaysRewardTable.dataArray[i].num))
				continue;
			
			getable = true;
			break;
		}
		if (getable == false)
		{
			// check sum reward
			for (int i = 0; i < TableDataManager.instance.sevenSumTable.dataArray.Length; ++i)
			{
				if (TableDataManager.instance.sevenSumTable.dataArray[i].groupId != MissionData.instance.sevenDaysId)
					continue;
				if (MissionData.instance.sevenDaysSumPoint < TableDataManager.instance.sevenSumTable.dataArray[i].count)
					continue;
				if (MissionData.instance.IsGetSevenDaysSumReward(TableDataManager.instance.sevenSumTable.dataArray[i].count))
					continue;

				getable = true;
				break;
			}
		}
		return getable;
	}

	public void RefreshSevenDaysAlarmObject()
	{
		RefreshAlarmObject(IsAlarmSevenDays(), sevenDaysAlarmRootTransform);
	}

	public static bool IsAlarmFestival()
	{
		if (FestivalTabCanvas.IsAlarmFestivalQuest() || FestivalTabCanvas.IsAlarmFestivalReward())
			return true;
		return false;
	}

	public void RefreshFestivalAlarmObject()
	{
		RefreshAlarmObject(IsAlarmFestival(), festivalAlarmRootTransform);
	}

	public static bool IsAlarmGacha()
	{
		return (CurrencyData.instance.energy >= CurrencyData.instance.energyMax);
	}

	public void RefreshGachaAlarmObject()
	{
		RefreshAlarmObject(IsAlarmGacha(), gachaAlarmRootTransform);
	}

	public static bool IsAlarmContinuousProduct1()
	{
		// hardcode ev4
		string cashEventId = "ev4";
		if (CashShopData.instance.IsShowEvent(cashEventId) == false)
			return false;

		int currentCompleteStep = CashShopData.instance.GetContinuousProductStep(cashEventId);
		string id = string.Format("{1}_conti_{0}", currentCompleteStep + 1, cashEventId);
		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(id);
		return (shopProductTableData != null && shopProductTableData.free);
	}

	public void RefreshContinuousProduct1AlarmObject()
	{
		RefreshAlarmObject(IsAlarmContinuousProduct1(), continuousProduct1AlarmRootTransform);
	}

	public static bool IsAlarmOnePlusTwo1()
	{
		// hardcode ev5
		string cashEventId = "ev5";
		if (CashShopData.instance.IsShowEvent(cashEventId) == false)
			return false;

		if (CashShopData.instance.IsGetOnePlusTwoReward(cashEventId, 0))
		{
			if (CashShopData.instance.IsGetOnePlusTwoReward(cashEventId, 1) == false || CashShopData.instance.IsGetOnePlusTwoReward(cashEventId, 2) == false)
				return true;
		}
		return false;
	}

	public void RefreshOnePlusTwo1AlarmObject()
	{
		RefreshAlarmObject(IsAlarmOnePlusTwo1(), onePlusTwo1AlarmRootTransform);
	}

	public static bool IsAlarmFirstPurchase()
	{
		if (PlayerData.instance.vtd > 0 && CashShopData.instance.firstPurchaseRewarded == false)
			return true;
		return false;
	}

	public void RefreshFirstPurchaseAlarmObject()
	{
		RefreshAlarmObject(IsAlarmFirstPurchase(), firstPurchaseAlarmRootTransform);
	}

	public static bool IsAlarmAttendance()
	{
		if (string.IsNullOrEmpty(AttendanceData.instance.attendanceId))
			return false;

		AttendanceTypeTableData attendanceTypeTableData = TableDataManager.instance.FindAttendanceTypeTableData(AttendanceData.instance.attendanceId);
		if (attendanceTypeTableData == null)
			return false;

		if (AttendanceData.instance.rewardReceiveCount < attendanceTypeTableData.lastRewardNum && AttendanceData.instance.todayReceiveRecorded == false)
			return true;

		return false;
	}

	public void RefreshAttendanceAlarmObject()
	{
		RefreshAlarmObject(IsAlarmAttendance(), attendanceAlarmRootTransform);
	}

	void RefreshAlarmObject(bool show, Transform alarmRootTransform)
	{
		if (show)
		{
			AlarmObject.Show(alarmRootTransform);
		}
		else
		{
			AlarmObject.Hide(alarmRootTransform);
		}
	}
	#endregion


	#region QuestInfo Gruop
	public void FadeOutQuestInfoGroup(float alpha, float duration, bool onlyFade, bool disableOnComplete)
	{
		DOTween.To(() => questInfoCanvasGroup.alpha, x => questInfoCanvasGroup.alpha = x, alpha, duration).SetEase(Ease.Linear).OnComplete(() =>
		{
			if (onlyFade)
				return;

			// Fade가 끝나고나서 상황에 맞게 초기화 해준다.
			if (disableOnComplete)
			{
				GuideQuestInfo.instance.gameObject.SetActive(false);
				//SubQuestInfo.instance.gameObject.SetActive(false);
			}
			else
			{
				GuideQuestInfo.instance.CloseInfo();
				//SubQuestInfo.instance.CloseInfo();
			}
		});
	}

	public void FadeInQuestInfoGroup(float alpha, float duration)    //, bool bossWar)
	{
		DOTween.To(() => questInfoCanvasGroup.alpha, x => questInfoCanvasGroup.alpha = x, alpha, duration).SetEase(Ease.Linear);
	}
	#endregion


	#region Cash Button
	public void RefreshCashButton()
	{
		bool showDownload = (PlayerData.instance.downloadConfirmed == false || PlayerData.instance.downloadRewarded == false);
		downloadButtonObject.SetActive(showDownload);

		bool showLevelPass = ((PlayerData.instance.playerLevel >= 5) && PlayerData.instance.downloadRewarded);
		levelPassButtonObject.SetActive(showLevelPass);

		bool showBrokenEnergy = (CurrencyData.instance.brokenEnergy > 0);
		brokenEnergyButtonObject.SetActive(showBrokenEnergy);

		bool showFirstPurchase = (CashShopData.instance.firstPurchaseRewarded == false);
		firstPurchaseButtonObject.SetActive(showFirstPurchase);

		bool showAttendance = (AttendanceData.instance.attendanceId != "");
		attendanceButtonObject.SetActive(showAttendance);
	}

	public void OnClickCashShopButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("CashShopCanvas", null);
	}

	public void OnClickDownloadButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("DownloadConfirmCanvas", null);
	}

	public void OnClickLevelPassButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("LevelPassCanvas", null);
	}

	public void OnClickBrokenEnergyButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("BrokenEnergyCanvas", null);
	}

	public void OnClickFirstPurchaseButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("FirstPurchaseCanvas", null);
	}

	public void OnClickAttendanceButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("AttendanceCanvas", null);
	}
	#endregion

	void RefreshCashAdditionalButton()
	{
		// sevenDays나 festival 둘다 전용 버튼 클래스 있어서 여기서 체크하지 않기로 한다.

		//bool showSevenDays = (MissionData.instance.sevenDaysId != 0 && ServerTime.UtcNow < MissionData.instance.sevenDaysExpireTime);
		//sevenDaysButtonObject.SetActive(showSevenDays);

		//bool showFestival = (FestivalData.instance.festivalId != 0 && ServerTime.UtcNow < FestivalData.instance.festivalExpireTime);
		//festivalButtonObject.SetActive(showFestival);
	}

	#region CashEvent
	public void ShowCashEvent(string cashEventId, bool showButton, bool showCanvas)
	{
		for (int i = 0; i < cashEventButtonList.Length; ++i)
		{
			if (cashEventButtonList[i].cashEventId == cashEventId)
			{
				if (showButton)
					cashEventButtonList[i].ShowButton(true);

				if (showCanvas)
				{
					// ev1은 HideState 신경쓰지 않고 보이게 한다.
					if (IsHideState() == false || cashEventId == "ev1")
						CashEventButton.ShowEventCanvas(cashEventId);
				}
				break;
			}
		}

		CheckCashEventButtonCount();
	}

	public void CloseCashEventButton(string cashEventId)
	{
		for (int i = 0; i < cashEventButtonList.Length; ++i)
		{
			if (cashEventButtonList[i].cashEventId == cashEventId)
			{
				cashEventButtonList[i].ShowButton(false);
				break;
			}
		}

		CheckCashEventButtonCount();
	}

	public void CheckCashEventButtonCount()
	{
		int count = 0;
		for (int i = 0; i < cashEventButtonRootTransform.childCount; ++i)
		{
			if (cashEventButtonRootTransform.GetChild(i).gameObject.activeSelf)
				++count;
		}

		challengeButtonRootTransform.anchoredPosition = new Vector2(0.0f, count > 9 ? -240.0f : -170.0f);
	}
	#endregion




	void OnApplicationPause(bool pauseStatus)
	{
		OnApplicationPauseNetwork(pauseStatus);
	}

	System.DateTime _pausedDateTime;
	bool _paused;
	void OnApplicationPauseNetwork(bool pauseStatus)
	{
		if (pauseStatus)
		{
			_paused = true;
			_pausedDateTime = System.DateTime.Now;
		}
		else
		{
			if (_paused == false)
				return;

			System.TimeSpan timeSpan = System.DateTime.Now - _pausedDateTime;
			//Debug.LogFormat("Delta Time : {0}", timeSpan.TotalSeconds);
			if (timeSpan.TotalMinutes > 10.0)
			{
				// 패킷을 보내서 유효한지 확인한다.
				PlayFabApiManager.instance.RequestNetworkOnce(() =>
				{
					// 성공시엔 예전엔 아무것도 하지 않았는데
					// 이젠 이벤트를 발생시켜야한다.
					//CashShopData.instance.CheckStartEvent(CashShopData.eEventStartCondition.OnApplicationPause);

				}, () =>
				{
					// 실패시에는 로비냐 아니냐에 따라 나눌까 하다가 
					// 어차피 둘다 팝업 띄우고 재시작 해야해서 내부 ErrorHandler에서 처리하기로 한다.
					//if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
					//{
					//}

					// 원래는 여기에서만 
					// ClientSaveData.instance.checkClientSaveDataOnEnterLobby 플래그라던가
					// PlayerData.instance.checkRestartScene 플래그를 만들어서 관리하려고 했었는데
					// 사실 10분 지난거 체크하는거 말고도 와이파이 바뀌거나 네트워크 오류로 인해서
					// 언제든지 씬 리셋이 되는 상황이 발생할 수 있기 때문에
					// PlayerData.instance.ResetData가 호출하면서 재로그인할때 각종 진입처리를 다시 하는게 맞았다.
					//
					// 진입처리에는 서버 이벤트도 있고 ClientSaveData도 있고 나중에는 이용약관 확인창까지 포함되는 바람에
					// 이러다보니 어차피 플래그는 ResetData에서 거는게 맞으며
					// 여기서는 그냥 CommonErrorHandler로 알아서 처리되고 넘어가면 끝인거로 처리하기로 한다.

				}, false);
			}
			else
			{
				//CashShopData.instance.CheckStartEvent(CashShopData.eEventStartCondition.OnApplicationPause);
			}
			_paused = false;
		}
	}
}