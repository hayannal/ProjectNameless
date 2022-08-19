using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using MEC;

public class MainCanvas : MonoBehaviour
{
	public static MainCanvas instance;

	public RectTransform[] basePositionRectTransformList;
	public RectTransform[] targetPositionRectTransformList;
	public float noInputTime = 15.0f;

	public GameObject challengeButtonObject;
	public GameObject cancelChallengeButtonObject;

	public GameObject inputRectObject;
	public CanvasGroup safeAreaCanvasGroup;
	public CanvasGroup questInfoCanvasGroup;

	public GameObject levelPassButtonObject;
	public CashEventButton[] cashEventButtonList;

	public RectTransform mailAlarmRootTransform;
	public RectTransform analysisAlarmRootTransform;
	public RectTransform cashShopAlarmRootTransform;
	public RectTransform levelPassAlarmRootTransform;

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
		// 언제 어느때든 누를 수 있다.
		OnPointerDown(null);
		Timing.RunCoroutine(ChallengeProcess());
	}

	bool _waitServerResponse;
	bool _enterGameServerFailure;
	bool _networkFailure;
	IEnumerator<float> ChallengeProcess()
	{
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
		cancelChallengeButtonObject.SetActive(true);
		StageManager.instance.InitializeStageFloor(PlayerData.instance.selectedStage, false);
		TeamManager.instance.HideForMoveMap(false);
		FadeCanvas.instance.FadeIn(0.5f);
	}

	public void OnClickCancelBossChallengeButton()
	{
		PlayFabApiManager.instance.RequestCancelBoss();
		OnPointerDown(null);
		ChangeStage(PlayerData.instance.selectedStage, true);
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

		challengeButtonObject.SetActive(true);
		cancelChallengeButtonObject.SetActive(false);
		StageManager.instance.InitializeStageFloor(stage, repeatMode);
		TeamManager.instance.HideForMoveMap(false);
		FadeCanvas.instance.FadeIn(0.5f);
	}
	#endregion

	#region Menu
	public void OnClickCharacterButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("CharacterCanvas", null);
	}

	public void OnEnterCharacterMenu(bool enter)
	{
		inputRectObject.SetActive(!enter);
		safeAreaCanvasGroup.alpha = enter ? 0.0f : 1.0f;
		safeAreaCanvasGroup.blocksRaycasts = !enter;
	}

	public bool IsHideState()
	{
		return (safeAreaCanvasGroup.alpha == 0.0f);
	}

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
	#endregion




	#region AlarmObject
	void RefreshAlarmObjectList()
	{
		//RefreshCashShopAlarmObject();
		//RefreshCharacterAlarmObject();
		RefreshMailAlarmObject();
		RefreshAnalysisAlarmObject();
		RefreshLevelPassAlarmObject();
	}

	public static bool IsAlarmCashShop()
	{
		bool result = false;
		/*
		if (DailyShopData.instance.GetTodayFreeItemData() != null && DailyShopData.instance.dailyFreeItemReceived == false)
			result = true;
		if (CurrencyData.instance.dailyDiaRemainCount > 0 && PlayerData.instance.sharedDailyPackageOpened == false)
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
		/*
		RefreshAlarmObject(IsAlarmCashShop(), cashShopAlarmRootTransform);
		*/
	}

	public static bool IsAlarmCharacter()
	{
		/*
		List<CharacterData> listCharacterData = PlayerData.instance.listCharacterData;
		for (int i = 0; i < listCharacterData.Count; ++i)
		{
			if (listCharacterData[i].IsAlarmState())
				return true;
		}
		*/
		return false;
	}

	static bool IsPlusAlarmCharacter()
	{
		/*
		List<CharacterData> listCharacterData = PlayerData.instance.listCharacterData;
		for (int i = 0; i < listCharacterData.Count; ++i)
		{
			if (listCharacterData[i].IsPlusAlarmState())
				return true;
		}
		*/
		return false;
	}

	public static bool IsTutorialPlusAlarmCharacter()
	{
		/*
		// 가지고 있는 캐릭터들의 레벨이 전부 1이면서 PlusAlarmState가 켜진 상태라면 초보자 전용 알람이라 생각하고 다른걸 띄워준다.
		bool levelOne = true;
		List<CharacterData> listCharacterData = PlayerData.instance.listCharacterData;
		for (int i = 0; i < listCharacterData.Count; ++i)
		{
			if (listCharacterData[i].powerLevel > 1)
			{
				levelOne = false;
				break;
			}
		}
		if (levelOne && IsPlusAlarmCharacter())
			return true;
		*/
		return false;
	}

	public void RefreshCharacterAlarmObject()
	{
		/*
		RefreshAlarmObject(false, characterAlarmRootTransform);

		bool showTutorialPlusAlarm = IsTutorialPlusAlarmCharacter();
		if (showTutorialPlusAlarm)
			AlarmObject.ShowTutorialPlusAlarm(alarmRootTransformList[(int)eButtonType.Character]);

		bool show = (showTutorialPlusAlarm == false && IsAlarmCharacter());
		if (show)
			RefreshAlarmObject(true, (int)eButtonType.Character);
		if (refreshLobbyAlarm)
			LobbyCanvas.instance.RefreshAlarmObject(eButtonType.Character, show);

		// 다른 DotMainMenu와 달리 Character버튼에서는 기본적인 느낌표 알람이 안뜨는 때에도 Plus알람을 체크해야한다.
		_reserveCharacterPlusAlarm = false;
		_reserveCharacterPlusAlarmOffset = false;
		if (show == false && showTutorialPlusAlarm == false && IsPlusAlarmCharacter())
			_reserveCharacterPlusAlarm = true;

		if (show && showTutorialPlusAlarm == false && IsPlusAlarmCharacter())
		{
			_reserveCharacterPlusAlarm = true;
			_reserveCharacterPlusAlarmOffset = true;
		}
		*/
	}

	bool _reserveCharacterPlusAlarm = false;
	bool _reserveCharacterPlusAlarmOffset = false;
	void UpdateCharacterPlusAlarm()
	{
		/*
		// DotMainMenuCanvas 생성될때 같은 프레임에 호출하면 tweenAnimation이 발동된채로 보여서 Update문에서 처리하게 해둔다.
		if (_reserveCharacterPlusAlarm)
		{
			if (_reserveCharacterPlusAlarmOffset)
			{
				AlarmObject.Show(alarmRootTransformList[(int)eButtonType.Character], false, false, true, false, null, true);
				_reserveCharacterPlusAlarmOffset = false;
			}
			else
			{
				// CharacterListCanvas에서 했던거처럼 tweenAnimation은 안쓰지만 ignoreAutoDisable은 굳이 할 필요 없어서 false로 해둔다.
				AlarmObject.Show(alarmRootTransformList[(int)eButtonType.Character], false, false, true);
			}
			_reserveCharacterPlusAlarm = false;
		}
		*/
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
		bool showLevelPass = (PlayerData.instance.playerLevel >= 5);
		levelPassButtonObject.SetActive(showLevelPass);


	}

	public void OnClickLevelPassButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("LevelPassCanvas", null);
	}
	#endregion

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
					CashEventButton.ShowEventCanvas(cashEventId);

				break;
			}
		}
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
					// 성공시엔 아무것도 하지 않는다.
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
			_paused = false;
		}
	}
}