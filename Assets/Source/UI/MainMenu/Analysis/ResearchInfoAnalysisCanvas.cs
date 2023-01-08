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

public class ResearchInfoAnalysisCanvas : MonoBehaviour
{
	enum eAnalysisDropType
	{
		Spell = 1,
		Companion = 2,
		Equip = 3,
		Gem = 4,
		Energy = 5,
	}

	public static ResearchInfoAnalysisCanvas instance;

	public Transform analysisTextTransform;
	public Text analysisText;

	public RectTransform positionRectTransform;
	public Text levelText;
	public GameObject levelUpButtonObject;

	public Slider expGaugeSlider;
	public Image expGaugeImage;
	public DOTweenAnimation expGaugeColorTween;
	public Image expGaugeEndPointImage;

	public Text atkText;

	public Transform expBoostTextTransform;
	public Text boostRemainTimeText;

	public GameObject switchGroupObject;
	public SwitchAnim alarmSwitch;
	public Text alarmOnOffText;

	public Slider centerGaugeSlider;
	public Image centerGaugeFillImage;
	public Text maxTimeText;
	public Text percentText;
	public Text analyzingText;
	public Text completeText;
	public Text remainTimeText;

	public GameObject getButtonObject;
	public Image getButtonImage;
	public Text getButtonText;
	public RectTransform alarmRootTransform;

	public GameObject effectPrefab;

	void Awake()
	{
		instance = this;

		// caching
		_defaultExpGaugeColor = expGaugeImage.color;
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
		if (ObscuredPrefs.HasKey(OPTION_COMPLETE_ALARM))
			_onCompleteAlarmState = ObscuredPrefs.GetInt(OPTION_COMPLETE_ALARM) == 1;

		MoveTween(false);
		RefreshInfo();

		// 화면 전환이 없다보니 제대로 캐싱할 시간은 없고 오브젝트만 만들었다가 꺼두는 캐싱이라도 해둔다.
		if (_disableButton == false)
		{
			GameObject effectObject = BattleInstanceManager.instance.GetCachedObject(effectPrefab, ResearchObjects.instance.effectRootTransform);
			effectObject.SetActive(false);
		}
	}

	void OnDisable()
	{
		ObscuredPrefs.SetInt(OPTION_COMPLETE_ALARM, _onCompleteAlarmState ? 1 : 0);
	}

	void Update()
	{
		UpdateRemainTime();
		UpdatePercentText();
		UpdateExpGauge();
	}

	int _currentLevel;
	float _currentExpPercent;
	void RefreshInfo()
	{
		analysisText.text = UIString.instance.GetString("AnalysisUI_Analysis");

		RefreshAlarm();
		RefreshLevelInfo();
		RefreshBoostInfo();
	}

	bool _onCompleteAlarmState = false;
	string OPTION_COMPLETE_ALARM = "_option_analysis_alarm_key";
	void RefreshAlarm()
	{
		_notUserSetting = true;
		alarmSwitch.isOn = _onCompleteAlarmState;
		_notUserSetting = false;
	}

	ObscuredBool _maxTimeReached = false;
	AnalysisTableData _analysisTableData;
	void RefreshLevelInfo()
	{
		_currentLevel = AnalysisData.instance.analysisLevel;
		levelText.text = UIString.instance.GetString("GameUI_Lv", _currentLevel);

		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(_currentLevel);
		if (analysisTableData == null)
			return;

		_analysisTableData = analysisTableData;
		bool maxReached = (_currentLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"));

		// exp는 누적된 시간을 구해와서 현재 Required 에 맞게 변환해서 표시하면 된다.
		CalcExpPercent();
		expGaugeSlider.value = _currentExpPercent;
		expGaugeImage.color = maxReached ? new Color(1.0f, 1.0f, 0.25f, 1.0f) : _defaultExpGaugeColor;
		expGaugeEndPointImage.gameObject.SetActive(false);

		atkText.text = analysisTableData.accumulatedAtk.ToString("N0");

		int maxTimeMinute = analysisTableData.maxTime / 60;
		if (maxTimeMinute < 60)
			maxTimeText.text = string.Format("Max {0}m", maxTimeMinute);
		else
			maxTimeText.text = string.Format("Max {0}h", maxTimeMinute / 60);

		RefreshProcessGauge();
		RefreshGetButton();

		_needUpdate = false;
		_maxTimeReached = false;
		AlarmObject.Hide(alarmRootTransform);
		if (AnalysisData.instance.analysisStarted == false)
		{
			analyzingText.text = "";
			completeText.text = "";
			remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", maxTimeMinute / 60, maxTimeMinute % 60, 0);
			return;
		}

		DateTime finishTime = AnalysisData.instance.analysisStartedTime + TimeSpan.FromSeconds(_analysisTableData.maxTime);
		if (ServerTime.UtcNow < finishTime)
		{
			analyzingText.text = "";
			completeText.text = "";
			_progressOngoingString = UIString.instance.GetString("AnalysisUI_ProgressOngoing");
			_needUpdate = true;
		}
		else
		{
			analyzingText.text = "";
			completeText.text = UIString.instance.GetString("AnalysisUI_ProgressFull");
			remainTimeText.text = "00:00:00";
			_maxTimeReached = true;
			AlarmObject.Show(alarmRootTransform);
		}
	}

	void CalcExpPercent()
	{
		int level = 0;
		float percent = 0.0f;
		int maxLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel");
		for (int i = _currentLevel - 1; i < TableDataManager.instance.analysisTable.dataArray.Length; ++i)
		{
			if (AnalysisData.instance.analysisExp < TableDataManager.instance.analysisTable.dataArray[i].requiredAccumulatedTime)
			{
				int currentPeriodExp = AnalysisData.instance.analysisExp - TableDataManager.instance.analysisTable.dataArray[i - 1].requiredAccumulatedTime;
				percent = (float)currentPeriodExp / (float)TableDataManager.instance.analysisTable.dataArray[i].requiredTime;
				level = TableDataManager.instance.analysisTable.dataArray[i].level - 1;
				break;
			}
			if (TableDataManager.instance.analysisTable.dataArray[i].level >= maxLevel)
			{
				level = maxLevel;
				percent = 1.0f;
				break;
			}
		}

		_currentExpPercent = percent;
	}

	bool _disableButton = false;
	void RefreshGetButton()
	{
		_disableButton = false;

		if (AnalysisData.instance.analysisStarted == false)
		{
			getButtonImage.color = ColorUtil.halfGray;
			getButtonText.color = ColorUtil.halfGray;
			_disableButton = true;
			return;
		}

		bool confirmable = false;
		if (_analysisTableData != null)
		{
			if (centerGaugeSlider.value >= 0.5f)
				confirmable = true;
			TimeSpan diffTime = ServerTime.UtcNow - AnalysisData.instance.analysisStartedTime;
			if (diffTime.TotalMinutes > 10)
				confirmable = true;
		}

		_disableButton = !confirmable;
		getButtonImage.color = _disableButton ? ColorUtil.halfGray : Color.white;
		getButtonText.color = _disableButton ? ColorUtil.halfGray : Color.white;
	}

	void RefreshProcessGauge()
	{
		// centerGauge는 시작한 시간부터의 경과값을 변환해서 표시하면 된다.
		// 만약 아직 시작한 상태가 아니라면 0인 상태로 표시하면 될거다.
		int totalSeconds = 0;
		if (AnalysisData.instance.analysisStarted)
		{
			TimeSpan timeSpan = ServerTime.UtcNow - AnalysisData.instance.analysisStartedTime;
			totalSeconds = (int)timeSpan.TotalSeconds;
		}
		float processRatio = (float)totalSeconds / _analysisTableData.maxTime;
		if (processRatio > 1.0f) processRatio = 1.0f;
		centerGaugeSlider.value = processRatio;
		percentText.text = string.Format("{0:0.00}%", processRatio * 100.0f);
		centerGaugeFillImage.color = (processRatio >= 1.0f) ? new Color(1.0f, 1.0f, 0.0f, centerGaugeFillImage.color.a) : new Color(1.0f, 1.0f, 1.0f, centerGaugeFillImage.color.a);
	}

	public void RefreshBoostInfo()
	{
		int remainBoost = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.AnalysisBoost);
		if (remainBoost == 0)
		{
			boostRemainTimeText.text = string.Format("0h 0m 0s");
			boostRemainTimeText.color = Color.gray;
		}
		else
		{
			boostRemainTimeText.text = AnalysisResultCanvas.GetTimeString(remainBoost);
			boostRemainTimeText.color = new Color(0.0f, 1.0f, 1.0f);
		}
	}

	string _progressOngoingString = "";
	int _lastRemainTimeSecond = -1;
	bool _needUpdate = false;
	void UpdateRemainTime()
	{
		if (AnalysisData.instance.analysisStarted == false)
			return;
		if (_analysisTableData == null)
			return;
		if (_needUpdate == false)
			return;

		// 매프레임 계산하기엔 너무 부하가 심할수도 있으니 1초에 한번만 하기로 한다.
		DateTime finishTime = AnalysisData.instance.analysisStartedTime + TimeSpan.FromSeconds(_analysisTableData.maxTime);
		if (ServerTime.UtcNow < finishTime)
		{
			TimeSpan remainTime = finishTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				RefreshProcessGauge();
				remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours + ((remainTime.Days > 0) ? remainTime.Days * 24 : 0), remainTime.Minutes, remainTime.Seconds);
				analyzingText.text = string.Format("{0}{1}", _progressOngoingString, GetDotString(_lastRemainTimeSecond));
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;

				if (_disableButton)
					RefreshGetButton();
			}
		}
		else
		{
			_needUpdate = false;
			_maxTimeReached = true;
			RefreshProcessGauge();
			RefreshGetButton();
			analyzingText.text = "";
			completeText.text = UIString.instance.GetString("AnalysisUI_ProgressFull");
			remainTimeText.text = "00:00:00";
			AlarmObject.Show(alarmRootTransform);
		}
	}

	string GetDotString(int lastRemainTimeSecond)
	{
		int result = lastRemainTimeSecond % 3;
		switch (result)
		{
			case 0: return "...";
			case 1: return "..";
			case 2: return ".";
		}
		return ".";
	}

	float _percentTextZeroRemainTime = 0.0f;
	void UpdatePercentText()
	{
		if (_percentTextZeroRemainTime > 0.0f)
		{
			_percentTextZeroRemainTime -= Time.deltaTime;
			percentText.text = string.Format("{0:0.00}%", centerGaugeSlider.value * 100.0f);

			if (_percentTextZeroRemainTime <= 0.0f)
			{
				_percentTextZeroRemainTime = 0.0f;
				percentText.text = "0.00%";
			}
		}
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

	public static bool CheckAnalysis()
	{
		/*
		if (EventManager.instance.reservedOpenAnalysisEvent)
			return true;
		*/
		if (AnalysisData.instance.analysisStarted == false)
			return false;
		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(AnalysisData.instance.analysisLevel);
		if (analysisTableData == null)
			return false;

		DateTime completeTime = AnalysisData.instance.analysisStartedTime + TimeSpan.FromSeconds(analysisTableData.maxTime);
		if (ServerTime.UtcNow < completeTime)
		{
		}
		else
			return true;

		return false;
	}

	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("AnalysisUI_AnalysisMore"), 250, analysisTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickLevelUpButton()
	{
		if (_currentLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("AnalysisLevelUpCanvas", null);
	}

	public void OnClickBoostDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("AnalysisUI_ExpMore"), 250, expBoostTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickBuyBoostButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("AnalysisBoostCanvas", null);
	}

	public void OnClickRemainTimeButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("AnalysisUI_LeftTimeMore"), 250, remainTimeText.transform, new Vector2(15.0f, -35.0f));
	}

	public void OnClickButton()
	{
		if (AnalysisData.instance.analysisStarted == false)
		{
			return;
		}

		if (_disableButton)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("AnalysisUI_NotEnoughCondition"), 2.0f);
			return;
		}

		// 쌓아둔 게이지를 초로 환산해서 누적할 준비를 한다.
		// 최초에 2분 30초 돌리자마자 쌓으면 150 쌓게될거다.
		PrepareAnalysis();

		#region Max Exp
		// 계산된 second를 그냥 보내면 안되고 최고레벨 검사는 해놓고 보내야한다.
		int packetSecond = _cachedSecond;
		if (_cachedBoostUses > 0 && _cachedSecondBoosted > packetSecond)
			packetSecond = _cachedSecondBoosted;
		AnalysisTableData maxAnalysisTableData = TableDataManager.instance.FindAnalysisTableData(BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"));
		int maxAnalysisExp = maxAnalysisTableData.requiredAccumulatedTime;
		if (AnalysisData.instance.analysisExp + packetSecond > maxAnalysisExp)
			packetSecond = maxAnalysisExp - AnalysisData.instance.analysisExp;
		#endregion
		PlayFabApiManager.instance.RequestAnalysis(packetSecond, _cachedBoostUses, _cachedResultGold, _cachedResultDia, _cachedResultEnergy, _listResultEventItemIdForPacket, () =>
		{
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.Analysis);
			OnAnalysisResult();
			Timing.RunCoroutine(AnalysisResultProcess());
		});
	}

	public void OnAnalysisResult()
	{
		if (_maxTimeReached)
		{
			AlarmObject.Hide(alarmRootTransform);
			MainCanvas.instance.RefreshAnalysisAlarmObject();
		}
		else
		{
			if (_onCompleteAlarmState)
				AnalysisData.instance.CancelAnalysisNotification();
		}
	}


	#region Prepare
	ObscuredInt _cachedSecond = 0;
	ObscuredInt _cachedResultGold = 0;
	ObscuredInt _cachedResultDia = 0;
	ObscuredInt _cachedResultEnergy = 0;
	ObscuredInt _cachedBoostUses = 0;
	ObscuredInt _cachedSecondBoosted = 0;
	List<string> _listResultItemValue;
	List<int> _listResultItemCount;
	List<ObscuredString> _listResultEventItemIdForPacket;
	public int cachedExpSecond { get { return _cachedSecond; } }
	public int cachedResultGold { get { return _cachedResultGold; } }
	public int cachedResultDia { get { return _cachedResultDia; } }
	public int cachedResultEnergy { get { return _cachedResultEnergy; } }
	public int cachedBoostUses { get { return _cachedBoostUses; } }
	public int cachedExpSecondBoosted { get { return _cachedSecondBoosted; } }
	public List<string> cachedResultItemValue { get { return _listResultItemValue; } }
	public List<int> cachedResultItemCount { get { return _listResultItemCount; } }
	public void PrepareAnalysis()
	{
		// UI 막혔을텐데 어떻게 호출한거지
		if (AnalysisData.instance.analysisStarted == false)
			return;
		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(AnalysisData.instance.analysisLevel);
		if (analysisTableData == null)
			return;

		ClearCachedInfo();

		// ConsumeProcessor에 전달해야해서 클리어 목록에서 제외시켜둔다.
		if (_listResultItemValue == null)
			_listResultItemValue = new List<string>();
		_listResultItemValue.Clear();
		if (_listResultItemCount == null)
			_listResultItemCount = new List<int>();
		_listResultItemCount.Clear();

		TimeSpan diffTime = ServerTime.UtcNow - AnalysisData.instance.analysisStartedTime;
		int totalSeconds = Mathf.Min((int)diffTime.TotalSeconds, analysisTableData.maxTime);
		_cachedSecond = totalSeconds;
		Debug.LogFormat("Analysis Time = {0}", totalSeconds);

		// 부스트 적용 여부
		if (CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.AnalysisBoost) > 0)
		{
			int appliedAmount = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.AnalysisBoost);
			if (appliedAmount > totalSeconds)
				appliedAmount = totalSeconds;

			// 적용할 양을 캐싱하고 보상을 늘려놔야한다.
			_cachedBoostUses = appliedAmount;

			// 2를 적으면 두배로 받는거고 3을 적으면 3배가 되는게 맞으니 
			totalSeconds = totalSeconds + (_cachedBoostUses * (BattleInstanceManager.instance.GetCachedGlobalConstantInt("AnalysisBoostRate") - 1));
			_cachedSecondBoosted = totalSeconds;
		}

		// 쌓아둔 초로 하나씩 체크해봐야한다.
		// 제일 먼저 goldPerTime
		// 시간당 골드로 적혀있으니 초로 변환해서 계산하면 된다.
		float goldPerSec = analysisTableData.goldPerTime / 60.0f / 60.0f;
		_cachedResultGold = (int)(goldPerSec * totalSeconds);
		if (_cachedResultGold < 1)
			_cachedResultGold = 1;

		#region Period
		// period 가 있는 것들은 조금 다르게 처리한다.
		// 쌓아둔 초를 가지고 몇번이나 시도할 수 있는지 판단하면 된다.
		// 이 값이 1보다 크다면 그 횟수만큼 여러번 굴릴 수 있다는거고 1보다 작으면 확률로 굴리게 되는거다.
		float spellRate = (float)totalSeconds / analysisTableData.spellPeriod;
		int spellDropCount = (int)spellRate;
		float spellDropRate = spellRate - spellDropCount;
		if (spellDropRate > 0.0f)
		{
			if (UnityEngine.Random.value <= spellDropRate)
			{
				// 확률 검사를 통과하면 dropCount를 1회 올린다.
				++spellDropCount;
			}
		}
		int key = (int)eAnalysisDropType.Spell;
		AnalysisDropTableData analysisDropTableData = TableDataManager.instance.FindAnalysisDropTableData(key.ToString());
		if (analysisDropTableData != null)
		{
			for (int i = 0; i < spellDropCount; ++i)
			{
				if (UnityEngine.Random.value > analysisDropTableData.probability)
					continue;

				int count = UnityEngine.Random.Range(analysisDropTableData.minValue, analysisDropTableData.maxValue);
				_listResultItemValue.Add("Cash_sSpellGacha");
				_listResultItemCount.Add(count);
				for (int j = 0; j < count; ++j)
					_listResultEventItemIdForPacket.Add("Cash_sSpellGacha");
			}
		}

		float companionRate = (float)totalSeconds / analysisTableData.companionPeriod;
		int companionDropCount = (int)companionRate;
		float companionDropRate = companionRate - companionDropCount;
		if (companionDropRate > 0.0f)
		{
			if (UnityEngine.Random.value <= companionDropRate)
				++companionDropCount;
		}
		key = (int)eAnalysisDropType.Companion;
		analysisDropTableData = TableDataManager.instance.FindAnalysisDropTableData(key.ToString());
		if (analysisDropTableData != null)
		{
			for (int i = 0; i < companionDropCount; ++i)
			{
				if (UnityEngine.Random.value > analysisDropTableData.probability)
					continue;

				int count = UnityEngine.Random.Range(analysisDropTableData.minValue, analysisDropTableData.maxValue);
				_listResultItemValue.Add("Cash_sCharacterGacha");
				_listResultItemCount.Add(count);
				for (int j = 0; j < count; ++j)
					_listResultEventItemIdForPacket.Add("Cash_sCharacterGacha");
			}
		}

		float equipRate = (float)totalSeconds / analysisTableData.equipPeriod;
		int equipDropCount = (int)equipRate;
		float equipDropRate = equipRate - equipDropCount;
		if (equipDropRate > 0.0f)
		{
			if (UnityEngine.Random.value <= equipDropRate)
				++equipDropCount;
		}
		key = (int)eAnalysisDropType.Equip;
		analysisDropTableData = TableDataManager.instance.FindAnalysisDropTableData(key.ToString());
		if (analysisDropTableData != null)
		{
			for (int i = 0; i < equipDropCount; ++i)
			{
				if (UnityEngine.Random.value > analysisDropTableData.probability)
					continue;

				int count = UnityEngine.Random.Range(analysisDropTableData.minValue, analysisDropTableData.maxValue);
				_listResultItemValue.Add("Cash_sEquipGacha");
				_listResultItemCount.Add(count);
				for (int j = 0; j < count; ++j)
					_listResultEventItemIdForPacket.Add("Cash_sEquipGacha");
			}
		}

		// 다이아랑 에너지도 비슷한 방법으로 해본다.
		float diaRate = (float)totalSeconds / analysisTableData.gemPeriod;
		int diaDropCount = (int)diaRate;
		float diaDropRate = diaRate - diaDropCount;
		if (diaDropRate > 0.0f)
		{
			if (UnityEngine.Random.value <= diaDropRate)
				++diaDropCount;
		}
		key = (int)eAnalysisDropType.Gem;
		analysisDropTableData = TableDataManager.instance.FindAnalysisDropTableData(key.ToString());
		if (analysisDropTableData != null)
		{
			for (int i = 0; i < diaDropCount; ++i)
			{
				if (UnityEngine.Random.value > analysisDropTableData.probability)
					continue;

				int count = UnityEngine.Random.Range(analysisDropTableData.minValue, analysisDropTableData.maxValue);
				_cachedResultDia += count;
			}
		}

		// 다음은 에너지인데 에너지 역시 드랍없이 직접 계산하는 형태다.
		float energyRate = (float)totalSeconds / analysisTableData.energyPeriod;
		int energyDropCount = (int)energyRate;
		float energyDropRate = energyRate - energyDropCount;
		if (energyDropRate > 0.0f)
		{
			if (UnityEngine.Random.value <= energyDropRate)
				++energyDropCount;
		}
		key = (int)eAnalysisDropType.Energy;
		analysisDropTableData = TableDataManager.instance.FindAnalysisDropTableData(key.ToString());
		if (analysisDropTableData != null)
		{
			for (int i = 0; i < energyDropCount; ++i)
			{
				if (UnityEngine.Random.value > analysisDropTableData.probability)
					continue;

				int count = UnityEngine.Random.Range(analysisDropTableData.minValue, analysisDropTableData.maxValue);
				_cachedResultEnergy += count;
			}
		}
		#endregion

		// 패킷 전달한 준비는 끝.
	}

	public void ClearCachedInfo()
	{
		_cachedSecond = 0;
		_cachedResultGold = 0;
		_cachedResultDia = 0;
		_cachedResultEnergy = 0;
		_cachedBoostUses = 0;
		if (_listResultEventItemIdForPacket == null)
			_listResultEventItemIdForPacket = new List<ObscuredString>();
		_listResultEventItemIdForPacket.Clear();
	}
	#endregion


	#region Exp Percent Gauge
	public void RefreshExpPercent(float targetPercent, int levelUpCount)
	{
		_targetPercent = targetPercent;
		_levelUpCount = levelUpCount;

		float totalDiff = levelUpCount;
		totalDiff += (targetPercent - expGaugeSlider.value);
		_fillSpeed = totalDiff / LevelUpExpFillTime;
		_fillRemainTime = LevelUpExpFillTime;
		_changeCount = 0;
		_targetLevel = _currentLevel + levelUpCount;

		// 이미 맥스라면
		if (expGaugeSlider.value >= 1.0f)
			return;

		expGaugeColorTween.DORestart();
		expGaugeEndPointImage.color = new Color(expGaugeEndPointImage.color.r, expGaugeEndPointImage.color.g, expGaugeEndPointImage.color.b, _defaultExpGaugeColor.a);
		expGaugeEndPointImage.gameObject.SetActive(true);

		// 최대레벨로 되는 연출상황일땐 1을 빼놔야 괜히 한바퀴 돌아서 게이지가 차지 않고 최대치에 닿았을때 한번에 끝나게 된다.
		if (targetPercent >= 1.0f && _targetLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"))
			_levelUpCount -= 1;
	}

	Color _defaultExpGaugeColor;
	const float LevelUpExpFillTime = 0.6f;
	float _fillRemainTime;
	float _fillSpeed;
	float _targetPercent;
	int _levelUpCount;
	int _changeCount;
	int _targetLevel;
	void UpdateExpGauge()
	{
		if (_fillRemainTime > 0.0f)
		{
			_fillRemainTime -= Time.deltaTime;
			expGaugeSlider.value += _fillSpeed * Time.deltaTime;
			if (expGaugeSlider.value >= 1.0f && _levelUpCount > 0)
			{
				expGaugeSlider.value -= 1.0f;
				_levelUpCount -= 1;

				++_changeCount;
				levelText.text = UIString.instance.GetString("GameUI_Lv", _currentLevel + _changeCount);
			}

			if (_fillRemainTime <= 0.0f)
			{
				_fillRemainTime = 0.0f;
				expGaugeSlider.value = _targetPercent;
				levelText.text = UIString.instance.GetString("GameUI_Lv", _targetLevel);

				expGaugeColorTween.DOPause();
				bool maxReached = (_targetLevel == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxAnalysisLevel"));
				expGaugeImage.color = maxReached ? new Color(1.0f, 1.0f, 0.25f, 1.0f) : _defaultExpGaugeColor;
				expGaugeEndPointImage.DOFade(0.0f, 1.0f).SetEase(Ease.OutQuad);
			}
		}
	}
	#endregion

	IEnumerator<float> AnalysisResultProcess()
	{
		// 인풋 차단
		ResearchCanvas.instance.inputLockObject.SetActive(true);
		getButtonImage.color = ColorUtil.halfGray;
		getButtonText.color = ColorUtil.halfGray;

		// 시간 업뎃을 멈추고 게이지부터 내린다.
		_needUpdate = false;
		completeText.text = "";
		analyzingText.text = "";
		_percentTextZeroRemainTime = LevelUpExpFillTime;
		DOTween.To(() => centerGaugeSlider.value, x => centerGaugeSlider.value = x, 0.0f, LevelUpExpFillTime).SetEase(Ease.Linear);

		// 경험치 슬라이더도 함께 움직여야한다.
		bool showLevelUp = (AnalysisData.instance.analysisLevel - _currentLevel > 0);
		CalcExpPercent();
		RefreshExpPercent(_currentExpPercent, AnalysisData.instance.analysisLevel - _currentLevel);
		//yield return Timing.WaitForSeconds(LevelUpExpFillTime - 0.3f);


		// 오브젝트 정지
		ResearchObjects.instance.objectTweenAnimation.DOTogglePause();
		yield return Timing.WaitForSeconds(0.3f);

		// 이펙트
		BattleInstanceManager.instance.GetCachedObject(effectPrefab, ResearchObjects.instance.effectRootTransform);
		yield return Timing.WaitForSeconds(2.0f);


		// 마지막에 알람도 다시 예약. 이 잠깐의 연출이 나오는동안 앱을 종료시키면 예약이 안될수도 있는데 이런 경우는 패스하기로 한다.
		if (_onCompleteAlarmState)
			AnalysisData.instance.ReserveAnalysisNotification();


		// 결과창 로딩 후 열리는 타이밍에 마지막 처리를 전달
		Action action = () =>
		{
			// 여기서 다이아 갱신까지 다시 되게 한다.
			ResearchCanvas.instance.currencySmallInfo.RefreshInfo();
			RefreshLevelInfo();

			// 토글 복구
			ResearchObjects.instance.objectTweenAnimation.DOTogglePause();

			// 인풋 복구
			ResearchCanvas.instance.inputLockObject.SetActive(false);
		};


		// 보상 연출을 시작해야하는데 레벨업이 있을때와 없을때로 구분했었다가 안하기로 한다.
		// 풀스크린 메인창 하나로 간다.
		UIInstanceManager.instance.ShowCanvasAsync("AnalysisResultCanvas", () =>
		{
			AnalysisResultCanvas.instance.RefreshInfo(showLevelUp, _currentLevel);
			action.Invoke();
		});
	}
}