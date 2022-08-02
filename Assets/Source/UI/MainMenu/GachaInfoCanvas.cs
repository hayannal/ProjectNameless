using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Michsky.UI.Hexart;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class GachaInfoCanvas : MonoBehaviour
{
	public static GachaInfoCanvas instance;

	public Transform gachaTextTransform;
	public Text gachaText;

	public RectTransform positionRectTransform;
	
	public Slider expGaugeSlider;
	public Image expGaugeImage;
	public DOTweenAnimation expGaugeColorTween;
	public Image expGaugeEndPointImage;

	public GameObject switchGroupObject;
	public SwitchAnim alarmSwitch;
	public Text alarmOnOffText;

	public Text betText;

	public Slider energyRatioSlider;
	public Text energyText;
	public Text fillRemainTimeText;

	public GameObject gachaButtonObject;
	public Image gachaButtonImage;
	public Text gachaButtonText;
	public RectTransform alarmRootTransform;

	public GameObject effectPrefab;

	void Awake()
	{
		instance = this;

		// caching
		//_defaultExpGaugeColor = expGaugeImage.color;
	}

	// Start is called before the first frame update
	void Start()
	{
		/*
		if (EventManager.instance.reservedOpenAnalysisEvent)
		{
			UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
			{
				EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_AnalysisName"), UIString.instance.GetString("GameUI_AnalysisDesc"), UIString.instance.GetString("GameUI_AnalysisMore"), null, 0.785f);
			});
			EventManager.instance.reservedOpenAnalysisEvent = false;
			EventManager.instance.CompleteServerEvent(EventManager.eServerEvent.analysis);
			ResearchCanvas.instance.RefreshAlarmObjectList();
			DotMainMenuCanvas.instance.RefreshResearchAlarmObject();
		}
		*/

		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
		_ignoreStartEvent = true;
	}

	Vector2 _leftTweenPosition = new Vector2(-150.0f, 0.0f);
	Vector2 _rightTweenPosition = new Vector2(150.0f, 0.0f);
	TweenerCore<Vector2, Vector2, VectorOptions> _tweenReferenceForMove;
	void MoveTween(bool left)
	{
		if (_tweenReferenceForMove != null)
			_tweenReferenceForMove.Kill();

		positionRectTransform.gameObject.SetActive(false);
		positionRectTransform.gameObject.SetActive(true);
		positionRectTransform.anchoredPosition = left ? _leftTweenPosition : _rightTweenPosition;
		_tweenReferenceForMove = positionRectTransform.DOAnchorPos(Vector2.zero, 0.3f).SetEase(Ease.OutQuad);
	}


	void OnEnable()
	{
		RefreshEnergy();

		if (ObscuredPrefs.HasKey(OPTION_COMPLETE_ALARM))
			_onCompleteAlarmState = ObscuredPrefs.GetInt(OPTION_COMPLETE_ALARM) == 1;

		MoveTween(false);
		RefreshInfo();

		/*
		// 화면 전환이 없다보니 제대로 캐싱할 시간은 없고 오브젝트만 만들었다가 꺼두는 캐싱이라도 해둔다.
		if (_disableButton == false)
		{
			GameObject effectObject = BattleInstanceManager.instance.GetCachedObject(effectPrefab, ResearchObjects.instance.effectRootTransform);
			effectObject.SetActive(false);
		}
		*/

		// refresh
		RefreshBet();
	}

	void OnDisable()
	{
		ObscuredPrefs.SetInt(OPTION_COMPLETE_ALARM, _onCompleteAlarmState ? 1 : 0);
	}

	void Update()
	{
		#region Energy
		UpdateFillRemainTime();
		UpdateRefresh();
		#endregion
	}

	int _currentLevel;
	float _currentExpPercent;
	public void RefreshInfo()
	{
		gachaText.text = UIString.instance.GetString("GachaUI_Gacha");

		RefreshAlarm();
	}

	#region Energy
	public void RefreshEnergy()
	{
		int current = CurrencyData.instance.energy;
		int max = CurrencyData.instance.energyMax;
		energyRatioSlider.value = (float)current / max;
		energyText.text = string.Format("{0}/{1}", current, max);
		_lastCurrent = current;
		if (current >= max)
		{
			fillRemainTimeText.text = "";
			_needUpdate = false;
		}
		else
		{
			_nextFillDateTime = CurrencyData.instance.energyRechargeTime;
			_needUpdate = true;
			_lastRemainTimeSecond = -1;
		}
	}

	bool _needUpdate = false;
	System.DateTime _nextFillDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateFillRemainTime()
	{
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _nextFillDateTime)
		{
			System.TimeSpan remainTime = _nextFillDateTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				fillRemainTimeText.text = string.Format("{0}:{1:00}", remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			// 우선 클라단에서 하기로 했으니 서버랑 통신해서 바꾸진 않는다.
			// 대신 CurrencyData의 값과 비교하면서 바뀌는지 확인한다.
			_needUpdate = false;
			fillRemainTimeText.text = "0:00";
			_needRefresh = true;
		}
	}

	bool _needRefresh = false;
	int _lastCurrent;
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (_lastCurrent != CurrencyData.instance.energy)
		{
			RefreshEnergy();
			_needRefresh = false;
		}
	}
	#endregion

	#region Bet
	List<int> _listBetValue = new List<int>();
	int _currentBetRateIndex;
	void RefreshBet()
	{
		if (_listBetValue.Count == 0)
		{
			_listBetValue.Add(1);
			_listBetValue.Add(2);
			_listBetValue.Add(3);
			_listBetValue.Add(5);
			_listBetValue.Add(10);
			_listBetValue.Add(20);
			_currentBetRateIndex = 0;
		}

		betText.text = string.Format("BET X{0}", _listBetValue[_currentBetRateIndex]);
	}

	public void OnClickBetButton()
	{
		++_currentBetRateIndex;
		if (_currentBetRateIndex >= _listBetValue.Count)
			_currentBetRateIndex = 0;

		RefreshBet();
	}
	#endregion

	bool _onCompleteAlarmState = false;
	string OPTION_COMPLETE_ALARM = "_option_energy_alarm_key";
	void RefreshAlarm()
	{
		_notUserSetting = true;
		alarmSwitch.isOn = _onCompleteAlarmState;
		_notUserSetting = false;
	}
	
	#region Alarm
	bool _ignoreStartEvent = false;
	bool _notUserSetting = false;
	public void OnSwitchOnCompleteAlarm()
	{
		_onCompleteAlarmState = true;
		alarmOnOffText.text = "ON";
		alarmOnOffText.color = Color.white;

		if (_notUserSetting)
			return;
		if (_ignoreStartEvent)
		{
			_ignoreStartEvent = false;
			return;
		}

		// 최초 분석 시작도 안된상태에서 누르게 되면 리셋해둬야한다.
		if (AnalysisData.instance.analysisStarted == false)
		{
			//ToastCanvas.instance.ShowToast(UIString.instance.GetString("AnalysisUI_FirstStart"), 2.0f);
			Timing.RunCoroutine(DelayedResetSwitch());
			return;
		}

#if UNITY_ANDROID
		AnalysisData.instance.ReserveAnalysisNotification();
#elif UNITY_IOS
		MobileNotificationWrapper.instance.CheckAuthorization(() =>
		{
			AnalysisData.instance.ReserveAnalysisNotification();
		}, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_EnergyNotiAppleLast"), 2.0f);
			Timing.RunCoroutine(DelayedResetSwitch());
		});
#endif
	}

	IEnumerator<float> DelayedResetSwitch()
	{
		yield return Timing.WaitForOneFrame;
		alarmSwitch.AnimateSwitch();
	}

	public void OnSwitchOffCompleteAlarm()
	{
		_onCompleteAlarmState = false;
		alarmOnOffText.text = "OFF";
		alarmOnOffText.color = new Color(0.176f, 0.176f, 0.176f);

		if (_notUserSetting)
			return;
		if (_ignoreStartEvent)
		{
			_ignoreStartEvent = false;
			return;
		}

		AnalysisData.instance.CancelAnalysisNotification();
	}
	#endregion
	
	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("GachaUI_GachaMore"), 250, gachaTextTransform, new Vector2(0.0f, -35.0f));
	}
	
	public void OnClickButton()
	{
		
	}
}