using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
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

		if (_noInputRemainTime > 0.0f)
			_noInputRemainTime = 0.001f;

		yield return Timing.WaitForSeconds(0.1f);

		if (this == null)
			yield break;

		challengeButtonObject.SetActive(true);
		cancelChallengeButtonObject.SetActive(false);
		StageManager.instance.InitializeStageFloor(stage, repeatMode);
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

	bool IsHideState()
	{
		return (safeAreaCanvasGroup.alpha == 0.0f);
	}

	public void OnClickSpinButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("BettingCanvas", null);
	}

	public void OnClickMailButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("MailCanvas", null);
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