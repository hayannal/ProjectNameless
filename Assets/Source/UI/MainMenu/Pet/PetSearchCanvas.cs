using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;
using DG.Tweening;

public class PetSearchCanvas : MonoBehaviour
{
	public static PetSearchCanvas instance;

	public Transform roomCameraTransform;

	#region UI
	public Button backButton;
	public GameObject captureButtonObject;
	public Text captureBetterCountText;
	public Text captureBestCountText;
	public GameObject searchButtonObject;
	public GameObject inProgressBattleStartButtonObject;

	public Text countDownText;
	public DOTweenAnimation countDownTweenAnimation;
	public Transform battleStartTransform;
	public CanvasGroup battleStartCanvasGroup;
	public Text battleStartText;

	public GameObject attackLeftButtonObject;
	public GameObject attackRighthButtonObject;
	public GameObject touchEffectObject;
	public Text attackPercentText;
	public GameObject timerObject;
	public Image timerImage;

	//public GameObject selectCaptureRootObject;

	public GameObject petPassBonusCenterObject;
	public GameObject petPassBonusResultObject;

	public GameObject resultRootObject;
	public Text resultText;
	public GameObject exitButtonObject;
	public GameObject extraGetButtonObject;
	public GameObject confetiObject;
	#endregion
	

	Vector3 _rootOffsetPosition = new Vector3(0.0f, 0.0f, -200.0f);
	public Vector3 rootOffsetPosition { get { return _rootOffsetPosition; } }

	bool _roomCameraMode = false;
	//float _lastRendererResolutionFactor;
	//float _lastBloomResolutionFactor;
	Vector3 _lastCameraPosition;
	Quaternion _lastCameraRotation;
	Transform _groundTransform;
	EnvironmentSetting _environmentSetting;
	GameObject _prevEnvironmentSettingObject;
	float _defaultLightIntensity;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		// disable prev component
		CameraFovController.instance.enabled = false;
		CustomFollowCamera.instance.enabled = false;

		_lastCameraPosition = CustomFollowCamera.instance.cachedTransform.position;
		_lastCameraRotation = CustomFollowCamera.instance.cachedTransform.rotation;

		// ground setting
		_prevEnvironmentSettingObject = StageGround.instance.DisableCurrentEnvironmentSetting();
		if (_groundTransform == null)
		{
			_groundTransform = BattleInstanceManager.instance.GetCachedObject(ContentsPrefabGroup.instance.petSearchGroundPrefab, _rootOffsetPosition, Quaternion.identity).transform;
			_environmentSetting = _groundTransform.GetComponentInChildren<EnvironmentSetting>();
			_defaultLightIntensity = _environmentSetting.defaultDirectionalLightIntensity;
		}
		else
		{
			_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
			_groundTransform.gameObject.SetActive(true);
		}

		CustomFollowCamera.instance.cachedTransform.position = roomCameraTransform.localPosition + _rootOffsetPosition;
		CustomFollowCamera.instance.cachedTransform.rotation = roomCameraTransform.localRotation;

		// 미션 리스트 창에서 진입하는거라서
		//MainCanvas.instance.OnEnterCharacterMenu(true);

		#region UI
		InitializePhase();
		#endregion
	}

	void OnDisable()
	{
		// 원래라면 Pop하는게 맞지만 
		// 씬을 종료하고 새 씬을 구축하러 나가는 로직으로 구현되어있기 때문에
		// 하단 라인들로 넘어갈 이유가 없다. 그러니 여기서 리턴시킨다.
		return;


		// base code
		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
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
		if (_groundTransform == null)
			return;

		_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
		_groundTransform.gameObject.SetActive(false);
		_prevEnvironmentSettingObject.SetActive(true);

		CustomFollowCamera.instance.cachedTransform.position = _lastCameraPosition;
		CustomFollowCamera.instance.cachedTransform.rotation = _lastCameraRotation;

		CameraFovController.instance.enabled = true;
		CustomFollowCamera.instance.enabled = true;

		//MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	void Update()
	{
		//UpdateHeartPercent();
		UpdateTimerImage();
	}

	#region UI
	public void InitializePhase()
	{
		if (string.IsNullOrEmpty(PetManager.instance.activePetId))
		{
			captureButtonObject.SetActive(false);
			return;
		}

		int count = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.CaptureBetter);
		captureBetterCountText.text = count.ToString("N0");
		count = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.CaptureBest);
		captureBestCountText.text = count.ToString("N0");
		captureButtonObject.SetActive(true);
		if (PetManager.instance.IsCachedInProgressGame())
		{
			searchButtonObject.SetActive(false);
			inProgressBattleStartButtonObject.SetActive(true);
			return;
		}
		searchButtonObject.SetActive(true);
	}

	public void OnClickBackButton()
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_BackToLobby"), UIString.instance.GetString("GameUI_BackToLobbyDescription"), () => {
			SceneManager.LoadScene(0);
		});
	}

	public void OnClickCaptureButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("SelectCaptureCanvas", () =>
		{
			SelectCaptureCanvas.instance.RefreshInfo(false);
		});
	}

	public void OnClickSearchButton()
	{
		List<ObscuredString> listPetId = PetManager.instance.GetRandomIdList();
		PlayFabApiManager.instance.RequestSearchPetList(listPetId, OnRecvStartSearch);
	}

	void OnRecvStartSearch()
	{
		captureButtonObject.SetActive(false);
		searchButtonObject.SetActive(false);
		PetSearchGround.instance.StartSearch();
	}

	public void OnClickInProgressBattleStartButton()
	{
		// 이미 저장되어있는 정보로 search된 펫을 보여주는 상태일거다. 전투 시작 누르면 전투 페이즈로 넘기면 된다.
		backButton.interactable = false;
		captureButtonObject.SetActive(false);
		inProgressBattleStartButtonObject.SetActive(false);
		StartAttackPhase();
	}

	bool _waitFirstInput = false;
	bool _startFirstInputWithLeft = false;
	int _inputStep = 0;
	public void StartAttackPhase(bool useCountDown = true, bool initialize = true, bool extraChance = false)
	{
		Timing.RunCoroutine(StartEffectProcess(useCountDown, initialize, extraChance));
	}

	IEnumerator<float> StartEffectProcess(bool useCountDown = true, bool initialize = true, bool extraChance = false)
	{
		if (useCountDown)
		{
			ToastZigzagCanvas.instance.ShowToast(UIString.instance.GetString("PetUI_TouchLeftRight"), 1.5f, 0.8f, true);
			yield return Timing.WaitForSeconds(1.5f);

			Vector3 localScale = countDownText.transform.localScale;
			countDownText.gameObject.SetActive(true);
			countDownText.text = "2";
			yield return Timing.WaitForSeconds(0.4f);
			countDownTweenAnimation.DORestart();
			yield return Timing.WaitForSeconds(0.33f);
			countDownText.color = Color.white;
			countDownText.transform.localScale = localScale;
			countDownText.gameObject.SetActive(false);

			countDownText.gameObject.SetActive(true);
			countDownText.text = "1";
			yield return Timing.WaitForSeconds(0.4f);
			countDownTweenAnimation.DORestart();
			yield return Timing.WaitForSeconds(0.33f);
			countDownText.color = Color.white;
			countDownText.transform.localScale = localScale;
			countDownText.gameObject.SetActive(false);
		}

		// 
		PetSearchGround.instance.ShowPetGauge();
		battleStartTransform.gameObject.SetActive(true);
		_inputRemainTime = HeartInputTime;
		timerObject.SetActive(true);

		ToastZigzagCanvas.instance.ShowToast(UIString.instance.GetString("PetUI_TouchLeftRight"), 5.0f);
		if (initialize)
		{
			_attackPercent = 0;
			attackPercentText.text = "0%";
			attackPercentText.color = new Color(1.0f, 0.5f, 0.5f);
			attackPercentText.gameObject.SetActive(true);
		}

		// 공격 버튼을 활성화시켜둔다.
		attackLeftButtonObject.SetActive(true);
		attackRighthButtonObject.SetActive(true);
		_waitFirstInput = true;

		


		if (extraChance)
		{
			battleStartText.text = "EXTRA CHANCE";
		}

		yield return Timing.WaitForSeconds(1.0f);
		battleStartCanvasGroup.DOFade(0.0f, 0.4f);
		battleStartCanvasGroup.transform.DOScale(3.0f, 0.4f);
		yield return Timing.WaitForSeconds(0.4f);

		battleStartTransform.gameObject.SetActive(false);
		battleStartCanvasGroup.alpha = 1.0f;
		battleStartTransform.localScale = Vector3.one;
	}

	const float HeartInputTime = 5.0f;
	float _inputRemainTime;
	void UpdateTimerImage()
	{
		if (_inputRemainTime > 0.0f)
		{
			_inputRemainTime -= Time.deltaTime;
			if (_inputRemainTime <= 0.0f)
			{
				_inputRemainTime = 0.0f;
				timerObject.SetActive(false);
				OnEndAttackTime();
			}
			timerImage.fillAmount = _inputRemainTime / HeartInputTime;
		}
	}

	public void OnPointerDownAttackLeft(BaseEventData eventData)
	{
		if (_waitFirstInput)
		{
			_waitFirstInput = false;
			_startFirstInputWithLeft = true;
			_inputStep = 1;
			AddPercent();
			OnTouchEffect(eventData);
		}

		if (_inputStep == 1 && _startFirstInputWithLeft == false)
		{
			_inputStep = 0;
			AddPercent();
			OnTouchEffect(eventData);
		}
		if (_inputStep == 0 && _startFirstInputWithLeft == true)
		{
			_inputStep = 1;
			AddPercent();
			OnTouchEffect(eventData);
		}
	}

	public void OnPointerDownAttackRight(BaseEventData eventData)
	{
		if (_waitFirstInput)
		{
			_waitFirstInput = false;
			_startFirstInputWithLeft = false;
			_inputStep = 1;
			AddPercent();
			OnTouchEffect(eventData);
		}

		if (_inputStep == 1 && _startFirstInputWithLeft == true)
		{
			_inputStep = 0;
			AddPercent();
			OnTouchEffect(eventData);
		}
		if (_inputStep == 0 && _startFirstInputWithLeft == false)
		{
			_inputStep = 1;
			AddPercent();
			OnTouchEffect(eventData);
		}
	}

	void OnTouchEffect(BaseEventData eventData)
	{
		Vector2 effectLocalPosition = Vector2.zero;
		PointerEventData pointerEventData = eventData as PointerEventData;
		if (pointerEventData != null)
		{
			//Debug.LogFormat("position : {0}", pointerEventData.pressPosition);
			effectLocalPosition.x = pointerEventData.pressPosition.x / transform.localScale.x;
			effectLocalPosition.y = pointerEventData.pressPosition.y / transform.localScale.y;
			GameObject newObject = BattleInstanceManager.instance.GetCachedObject(touchEffectObject, touchEffectObject.transform.parent);
			newObject.GetComponent<RectTransform>().anchoredPosition = effectLocalPosition;
			newObject.SetActive(true);
		}

		PetSearchGround.instance.RefreshHeartParticleSystem();
	}

	ObscuredInt _attackPercent;
	void AddPercent()
	{
		_attackPercent += Random.Range(1, 10);
		if (_attackPercent >= 100) attackPercentText.color = new Color(0.5f, 1.0f, 1.0f);
		else if (_attackPercent >= 200) attackPercentText.color = new Color(1.0f, 1.0f, 0.5f);
		else if (_attackPercent >= 400) attackPercentText.color = new Color(0.5f, 0.5f, 1.0f);
		else if (_attackPercent >= 600) attackPercentText.color = new Color(0.5f, 1.0f, 0.5f);
		attackPercentText.text = string.Format("{0:N0}%", _attackPercent);
	}

	void OnEndAttackTime()
	{
		attackLeftButtonObject.SetActive(false);
		attackRighthButtonObject.SetActive(false);

		Timing.RunCoroutine(AttackGaugeProcess());
	}

	IEnumerator<float> AttackGaugeProcess()
	{
		attackPercentText.transform.DOPunchScale(new Vector3(0.6f, 0.6f, 0.6f), 0.2f, 5, 0).SetLoops(3);
		//attackPercentText.transform.DOScale(new Vector3(0.5f, 0.5f, 0.5f), 0.9f);

		yield return Timing.WaitForSeconds(1.0f);

		// 흔들흔들
		CustomFollowCamera.instance.cachedTransform.DOShakePosition(0.5f, 0.08f, 30, 90, false, false);
		yield return Timing.WaitForSeconds(0.8f);

		// 데미지 처리
		PetSearchGround.instance.OnAttack(_attackPercent);
		yield return Timing.WaitForSeconds(0.8f);

		// 턴 결과
		PetSearchGround.instance.TurnEnd();
	}

	public void ShowSelectCapture()
	{
		Timing.RunCoroutine(ShowSelectCaptureProcess());
	}

	IEnumerator<float> ShowSelectCaptureProcess()
	{
		ToastZigzagCanvas.instance.ShowToast(UIString.instance.GetString("PetUI_LetsCapture"), 1.5f, 0.8f, true);
		yield return Timing.WaitForSeconds(1.5f);

		UIInstanceManager.instance.ShowCanvasAsync("SelectCaptureCanvas", () =>
		{
			SelectCaptureCanvas.instance.RefreshInfo(true);
		});
	}

	public float prevPowerValue { get; set; }
	public void ShowResult(bool success, bool extraChanceExit)
	{
		resultText.SetLocalizedText(UIString.instance.GetString(success ? "PetUI_ResultSuccess" : "PetUI_ResultFailure"));

		exitButtonObject.SetActive(!extraChanceExit);
		extraGetButtonObject.SetActive(extraChanceExit);
		if (extraChanceExit && PetSearchGround.instance.extraGainByPetPass)
			petPassBonusResultObject.SetActive(true);

		confetiObject.SetActive(success && !extraChanceExit);

		if (confetiObject.activeSelf)
		{
			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			if (nextValue > prevPowerValue)
			{
				UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
				{
					ChangePowerCanvas.instance.ShowInfo(prevPowerValue, nextValue);
				});
			}
		}

		resultRootObject.SetActive(true);
	}

	public void OnClickResultExitButton()
	{
		SceneManager.LoadScene(0);
	}

	public void OnClickResultExtraGetButton()
	{
		resultRootObject.SetActive(false);
		petPassBonusResultObject.SetActive(false);
		PetSearchGround.instance.RotateCameraToExtraGain();
	}
	#endregion
}