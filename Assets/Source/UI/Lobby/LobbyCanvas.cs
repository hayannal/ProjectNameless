using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MEC;

public class LobbyCanvas : MonoBehaviour
{
	public static LobbyCanvas instance;

	public Button dotMainMenuButton;

	public CanvasGroup subMenuCanvasGroup;
	public CanvasGroup subMenuHorizontalCanvasGroup;
	public DOTweenAnimation[] subMenuButtonTweenAnimationList;
	public RectTransform[] subMenuAlarmRootTransformList;

	public GameObject rightTopRootObject;
	public Button lobbyOptionButton;
	public Button timeSpaceHomeButton;
	public CanvasGroup questInfoCanvasGroup;
	public Button battlePauseButton;
	public Text levelText;
	public Slider expGaugeSlider;
	public RectTransform expGaugeRectTransform;
	public Image expGaugeImage;
	public DOTweenAnimation expGaugeColorTween;
	public Image expGaugeEndPointImage;
	public RectTransform alarmRootTransform;
	public GameObject fastClearSmallToastObject;
	public Text fastClearText;
	public DOTweenAnimation fastClearTweenAnimation;
	public GameObject noHitClearSmallToastObject;
	public Text noHitClearText;
	public DOTweenAnimation noHitClearTweenAnimation;

	void Awake()
	{
		instance = this;
	}

	void Update()
	{
		/*
		UpdateExpGauge();
		UpdateExpGaugeHeight();
		*/
		UpdateSubMenu();
	}

	void Start()
	{
		battlePauseButton.gameObject.SetActive(false);
		levelText.gameObject.SetActive(false);
		expGaugeSlider.gameObject.SetActive(false);
		expGaugeEndPointImage.gameObject.SetActive(false);
		/*
		_defaultExpGaugeHeight = expGaugeRectTransform.sizeDelta.y;
		_defaultExpGaugeColor = expGaugeImage.color;
		*/

		fastClearText.text = UIString.instance.GetString("GameUI_FastClearPoint");
		noHitClearText.text = UIString.instance.GetString("GameUI_NoHitClearPoint");
	}
	
	public void OnClickDotButton()
	{
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
			TitleCanvas.instance.FadeTitle();

		/*
		if (ContentsManager.IsTutorialChapter())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_PossibleAfterTraining"), 2.0f);
			return;
		}

		if (PlayerData.instance.lobbyDownloadState)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_PossibleAfterDownload"), 2.0f);
			return;
		}

		if (NodeWarPortal.instance != null && NodeWarPortal.instance.enteredPortal)
			return;

		if (DotMainMenuCanvas.instance != null)
		{
			DotMainMenuCanvas.instance.targetTransform = BattleInstanceManager.instance.playerActor.cachedTransform;
			DotMainMenuCanvas.instance.ToggleShow();
			return;
		}
		*/

		UIInstanceManager.instance.ShowCanvasAsync("DotMainMenuCanvas", () => {

			if (this == null) return;
			if (gameObject == null) return;
			if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby == false) return;

			/*
			DotMainMenuCanvas.instance.targetTransform = BattleInstanceManager.instance.playerActor.cachedTransform;
			*/
			SoundManager.instance.PlaySFX("7DotOpen");
		});
	}

	#region Sub Menu
	bool IsActiveSubMenu(int index)
	{
		// 현재 개방된 컨텐츠에 따라 보일지 말지 결정해야하는데 0번 토벌전은 최초에 등장하는거라 항상 Active로 처리해야한다.
		if (index == 0)
			return true;

		switch (index)
		{
			/*
			case 1: if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Invasion)) return true; break;
			case 2: if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Ranking) && RankingData.instance.disableRanking == false) return true; break;
			*/
		}
		return false;
	}

	bool IsShowAlarmSubMenu(int index)
	{
		switch (index)
		{
			/*
			case 0: if (EventManager.instance.reservedOpenBossBattleEvent) return true; break;
			case 1: if (EventManager.instance.reservedOpenInvasionEvent) return true; break;
			*/
		}
		return false;
	}

	public void HideSubMenuAlarmObject(int i)
	{
		AlarmObject.Hide(subMenuAlarmRootTransformList[i]);
	}

	// 코루틴 Async로딩에 토글로 동작하는거라 동기를 맞추려면 이렇게 해야만 했다.
	public void OnShowDotMainMenu(bool show)
	{
		/*
		// 장비보상이기 때문에 TimeSpace로 체크한다.
		if (ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapterStage.TimeSpace) == false)
			return;
		*/

		if (show)
		{
			if (subMenuButtonTweenAnimationList[0].gameObject.activeSelf)
			{
				for (int i = 0; i < subMenuButtonTweenAnimationList.Length; ++i)
				{
					if (IsActiveSubMenu(i) == false)
						continue;
					subMenuButtonTweenAnimationList[i].DOPlayForward();
				}
				_closeRemainTime = 0.0f;
			}
			else
			{
				for (int i = 0; i < subMenuButtonTweenAnimationList.Length; ++i)
				{
					if (IsActiveSubMenu(i) == false)
						continue;
					subMenuButtonTweenAnimationList[i].gameObject.SetActive(true);

					AlarmObject.Hide(subMenuAlarmRootTransformList[i]);
					if (IsShowAlarmSubMenu(i))
						AlarmObject.Show(subMenuAlarmRootTransformList[i], true, true);
				}
				return;
			}
		}
		else
		{
			if (subMenuButtonTweenAnimationList[0].gameObject.activeSelf)
			{
				if (_closeRemainTime > 0.0f)
					return;

				for (int i = 0; i < subMenuButtonTweenAnimationList.Length; ++i)
				{
					if (IsActiveSubMenu(i) == false)
						continue;
					subMenuButtonTweenAnimationList[i].DOPlayBackwards();
				}
				_closeRemainTime = 0.6f;
			}
		}
	}

	void HideSubMenuButtonList()
	{
		for (int i = 0; i < subMenuButtonTweenAnimationList.Length; ++i)
			subMenuButtonTweenAnimationList[i].gameObject.SetActive(false);
	}

	float _closeRemainTime;
	void UpdateSubMenu()
	{
		if (_closeRemainTime > 0.0f)
		{
			_closeRemainTime -= Time.deltaTime;
			if (_closeRemainTime <= 0.0f)
			{
				_closeRemainTime = 0.0f;
				HideSubMenuButtonList();
			}
		}
	}

	public void OnClickBossBattleButton()
	{
		if (_closeRemainTime > 0.0f)
			return;
		/*
		if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CannotGoBattleContents"), 2.0f);
			return;
		}
		*/

		UIInstanceManager.instance.ShowCanvasAsync("BossBattleEnterCanvas", null);
	}

	public void OnClickInvasionButton()
	{
		if (_closeRemainTime > 0.0f)
			return;
		/*
		if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CannotGoBattleContents"), 2.0f);
			return;
		}
		*/

		UIInstanceManager.instance.ShowCanvasAsync("InvasionEnterCanvas", null);
	}

	public void OnClickRankingButton()
	{
		if (_closeRemainTime > 0.0f)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("RankingCanvas", null);
	}
	#endregion

	public void OnClickLobbyOptionButton()
	{
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null && TitleCanvas.instance.gameObject.activeSelf)
			TitleCanvas.instance.FadeTitle();

		UIInstanceManager.instance.ShowCanvasAsync("SettingCanvas", null);
	}

	public void OnClickTimeSpaceHomeButton()
	{
		/*
		TimeSpacePortal.instance.HomeProcessByCanvas();
		*/
	}

	public void OnClickBattlePauseButton()
	{
		PauseCanvas.instance.gameObject.SetActive(true);
		PauseCanvas.instance.ShowBattlePauseSimpleMenu(false);
	}

	public void OnClickBackButton()
	{
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby && TitleCanvas.instance != null)
			TitleCanvas.instance.FadeTitle();

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
			if (battlePauseButton.gameObject.activeSelf && battlePauseButton.interactable)
				OnClickBattlePauseButton();
		}
	}


	public void OnExitLobby()
	{
		dotMainMenuButton.gameObject.SetActive(false);
		lobbyOptionButton.gameObject.SetActive(false);
		subMenuCanvasGroup.gameObject.SetActive(false);
		battlePauseButton.gameObject.SetActive(true);
		expGaugeSlider.value = 0.0f;
		expGaugeSlider.gameObject.SetActive(true);
		expGaugeEndPointImage.gameObject.SetActive(false);
		//RefreshLevelText(1);

		/*
		if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
			DotMainMenuCanvas.instance.gameObject.SetActive(false);
		*/
	}

	public void OnEnterMainMenu(bool enter)
	{
		dotMainMenuButton.gameObject.SetActive(!enter);
		rightTopRootObject.SetActive(!enter);
		subMenuCanvasGroup.alpha = enter ? 0.0f : 1.0f;
		subMenuCanvasGroup.blocksRaycasts = !enter;
	}

	public void OnEnterTimeSpace(bool enter)
	{
		lobbyOptionButton.gameObject.SetActive(!enter);
		timeSpaceHomeButton.gameObject.SetActive(enter);
		subMenuHorizontalCanvasGroup.alpha = enter ? 0.35f : 1.0f;

		/*
		if (enter)
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.EnterTimeSpace);
		*/
	}

	#region Home
	public static void Home()
	{
		StackCanvas.Home();

		if (instance != null)
			instance.HideSubMenuButtonList();

		/*
		// 하필 홈을 눌렀는데 서브 로비인 시공간에 가있었다면 예외처리.
		// StackCanvas 안에 하려다가 프레임워크기도 하고
		// ChapterCanvas 같은 경우엔 또 할필요가 없기 때문에 선별해서 넣기로 한다.
		if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
			TimeSpacePortal.instance.HomeProcessByCanvas();
		*/
	}
	#endregion


	

	/*
	#region AlarmObject
	bool _isTutorialPlusAlarmCharacter = false;
	bool _isAlarmCashShop = false;
	bool _isAlarmCharacter = false;
	bool _isAlarmResearch = false;
	bool _isAlarmMail = false;
	bool _isAlarmBalance = false;
	public void RefreshAlarmObject()
	{
		_isTutorialPlusAlarmCharacter = DotMainMenuCanvas.IsTutorialPlusAlarmCharacter();
		_isAlarmCashShop = DotMainMenuCanvas.IsAlarmCashShop();
		_isAlarmCharacter = DotMainMenuCanvas.IsAlarmCharacter();
		_isAlarmResearch = DotMainMenuCanvas.IsAlarmResearch();
		_isAlarmMail = DotMainMenuCanvas.IsAlarmMail();
		_isAlarmBalance = DotMainMenuCanvas.IsAlarmBalance();

		bool showAlarm = false;
		if (_isAlarmCashShop || _isAlarmCharacter || _isAlarmResearch || _isAlarmMail || _isAlarmBalance) showAlarm = true;
		if (ContentsManager.IsTutorialChapter() || PlayerData.instance.lobbyDownloadState) showAlarm = false;

		AlarmObject.Hide(alarmRootTransform);
		if (_isTutorialPlusAlarmCharacter)
			AlarmObject.ShowTutorialPlusAlarm(alarmRootTransform);
		else if (showAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);

		GuideQuestInfo.instance.RefreshAlarmObject();

		// GuideQuest와 달리 데이터가 유효한지 보고 호출해야한다.
		if (QuestData.instance.CheckValidQuestList(false) == true)
			SubQuestInfo.instance.RefreshAlarmObject();
	}

	public void RefreshAlarmObject(DotMainMenuCanvas.eButtonType changedType, bool changedValue)
	{
		// 위 함수의 경우 모든 조건을 다 검사해야하다보니 불필요한 연산을 할때가 많다.
		// 처음 로비에 입장할때야 다 검사하는게 맞는데 그 이후부터는 변경되는 것만 반영하면 되기 때문.
		switch (changedType)
		{
			case DotMainMenuCanvas.eButtonType.Shop: _isAlarmCashShop = changedValue; break;
			case DotMainMenuCanvas.eButtonType.Character: _isAlarmCharacter = changedValue; break;
			case DotMainMenuCanvas.eButtonType.Research: _isAlarmResearch = changedValue; break;
			case DotMainMenuCanvas.eButtonType.Mail: _isAlarmMail = changedValue; break;
			case DotMainMenuCanvas.eButtonType.Balance: _isAlarmBalance = changedValue; break;
		}
		bool showAlarm = false;
		if (_isAlarmCashShop || _isAlarmCharacter || _isAlarmResearch || _isAlarmMail || _isAlarmBalance) showAlarm = true;
		if (ContentsManager.IsTutorialChapter() || PlayerData.instance.lobbyDownloadState) showAlarm = false;

		AlarmObject.Hide(alarmRootTransform);
		if (_isTutorialPlusAlarmCharacter)
			AlarmObject.ShowTutorialPlusAlarm(alarmRootTransform);
		else if (showAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);
	}

	public void RefreshTutorialPlusAlarmObject()
	{
		_isTutorialPlusAlarmCharacter = DotMainMenuCanvas.IsTutorialPlusAlarmCharacter();

		bool showAlarm = false;
		if (_isAlarmCashShop || _isAlarmCharacter || _isAlarmResearch || _isAlarmMail || _isAlarmBalance) showAlarm = true;
		if (ContentsManager.IsTutorialChapter() || PlayerData.instance.lobbyDownloadState) showAlarm = false;

		AlarmObject.Hide(alarmRootTransform);
		if (_isTutorialPlusAlarmCharacter)
			AlarmObject.ShowTutorialPlusAlarm(alarmRootTransform);
		else if (showAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);
	}
	#endregion
	*/






	void OnApplicationPause(bool pauseStatus)
	{
		OnApplicationPauseCanvas(pauseStatus);
		OnApplicationPauseNetwork(pauseStatus);
	}

	void OnApplicationPauseCanvas(bool pauseStatus)
	{
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
			return;
		/*
		if (SwapCanvas.instance != null && SwapCanvas.instance.gameObject.activeSelf)
			return;
		if (ReturnScrollConfirmCanvas.instance != null && ReturnScrollConfirmCanvas.instance.gameObject.activeSelf)
			return;
		*/
		if (battlePauseButton.gameObject.activeSelf == false)
			return;
		if (battlePauseButton.interactable == false)
			return;
		if (FullscreenYesNoCanvas.IsShow())
			return;

		if (pauseStatus)
		{
			PauseCanvas.instance.gameObject.SetActive(true);
			PauseCanvas.instance.ShowBattlePauseSimpleMenu(true);
		}
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
