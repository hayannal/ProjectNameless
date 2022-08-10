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

	public Image gaugeImage;
	public DOTweenAnimation gaugeImageTweenAnimation;
	public Text nextEventRewardText;

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

	// 서버에서 RoomType으로 체크하기 때문에 GoblinRoom 4번이나 GoldBoxRoom 5번이 바뀌면 서버 스크립트 Betting도 바꿔줘야한다.
	public enum eGachaResult
	{
		Gold1 = 1,
		Gold2 = 2,
		Gold10 = 3,
		GoldBoxRoom = 4,
		FindMonsterRoom = 5,
		Energy = 6,
		EventPoint1 = 7,
		EventPoint2 = 8,
		EventPoint10 = 9,
		BrokenEnergy1 = 10,
		BrokenEnergy2 = 11,

		Amount,
	}

	void Awake()
	{
		instance = this;

		// caching
		//_defaultExpGaugeColor = expGaugeImage.color;
	}

	// Start is called before the first frame update
	bool _started = false;
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
		_started = true;
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

		// refresh
		RefreshBet();
	}

	void OnDisable()
	{
		ObscuredPrefs.SetInt(OPTION_COMPLETE_ALARM, _onCompleteAlarmState ? 1 : 0);
	}

	bool _reserveGaugeMoveTweenAnimation;
	void Update()
	{
		if (_reserveGaugeMoveTweenAnimation)
		{
			gaugeImageTweenAnimation.DORestart();
			_reserveGaugeMoveTweenAnimation = false;
		}

		#region Energy
		UpdateFillRemainTime();
		UpdateRefresh();
		#endregion
	}

	int _currentLevel;
	float _currentExpPercent;
	TweenerCore<float, float, FloatOptions> _tweenReferenceForGauge;
	public void RefreshInfo()
	{
		gachaText.text = UIString.instance.GetString("GachaUI_Gacha");

		if (_tweenReferenceForGauge != null)
			_tweenReferenceForGauge.Kill();

		// gauge
		float ratio = 0.5f;
		gaugeImage.fillAmount = 0.0f;
		_tweenReferenceForGauge = DOTween.To(() => gaugeImage.fillAmount, x => gaugeImage.fillAmount = x, ratio, 0.5f).SetEase(Ease.OutQuad).SetDelay(0.3f);
		if (_started)
			gaugeImageTweenAnimation.DORestart();
		else
			_reserveGaugeMoveTweenAnimation = true;

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
	public int GetBetRate()
	{
		if (_currentBetRateIndex < _listBetValue.Count)
			return _listBetValue[_currentBetRateIndex];
		return 1;
	}
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
		int useEnergy = _listBetValue[_currentBetRateIndex];
		if (CurrencyData.instance.energy < useEnergy)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughEnergy"), 2.0f);
			return;
		}

		PrepareGacha();
		PrepareGoldBoxTarget();
		PlayFabApiManager.instance.RequestGacha(useEnergy, _resultGold, _resultEnergy, _resultBrokenEnergy, _resultEvent, _reserveRoomType, _refreshTurn, _refreshNewTurn, _refreshNewGold, (refreshTurnComplete) =>
		{
			// 턴 바꿔야하는걸 기억시켜두고 연출을 진행하면 된다.
			if (refreshTurnComplete)
				_needRefreshTurn = true;

			OnRecvSpinSlot();
		});
	}

	bool _needRefreshTurn = false;
	void OnRecvSpinSlot()
	{
		// Energy는 바로 차감 후
		RefreshEnergy();

		// 골드나 다이아는 여기서 갱신하면 안되고 이펙트 뜰때 해야한다.
		//currencySmallInfo.RefreshInfo();
		Timing.RunCoroutine(GachaProcess());
	}

	#region Prepare
	class RandomGachaTypeInfo
	{
		public GachaTypeTableData gachaTypeTableData;
		public float sumWeight;
	}
	List<RandomGachaTypeInfo> _listGachaTypeInfo = null;

	public eGachaResult GetRandomGachaResult()
	{
		if (_listGachaTypeInfo == null)
			_listGachaTypeInfo = new List<RandomGachaTypeInfo>();
		_listGachaTypeInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.gachaTypeTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.gachaTypeTable.dataArray[i].gachaWeight;
			if (weight <= 0.0f)
				continue;

			sumWeight += weight;
			RandomGachaTypeInfo newInfo = new RandomGachaTypeInfo();
			newInfo.gachaTypeTableData = TableDataManager.instance.gachaTypeTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listGachaTypeInfo.Add(newInfo);
		}

		if (_listGachaTypeInfo.Count == 0)
			return eGachaResult.Gold1;

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listGachaTypeInfo[_listGachaTypeInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listGachaTypeInfo.Count; ++i)
		{
			if (random <= _listGachaTypeInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return eGachaResult.Gold1;

		string id = _listGachaTypeInfo[index].gachaTypeTableData.gachaId;
		int intId = 0;
		int.TryParse(id, out intId);
		if (intId == 0)
			return eGachaResult.Gold1;

		return (eGachaResult)intId;
	}

	// 결과 저장
	eGachaResult _gachaResult;

	// 패킷으로 보내는 재화들
	ObscuredInt _resultGold;
	ObscuredInt _resultEnergy;
	ObscuredInt _resultBrokenEnergy;
	ObscuredInt _resultEvent;
	ObscuredInt _reserveRoomType;
	void PrepareGacha()
	{
		bool fixedResult = false;
		if (fixedResult)
		{
			_gachaResult = eGachaResult.Gold1;
		}
		else
		{
			_gachaResult = GetRandomGachaResult();
		}
		
		Debug.LogFormat("Betting Prepare : {0}", _gachaResult);

		// 리셋
		// 결과에 따라 미리미리 랜덤 굴릴것들은 굴려놔야 패킷으로 보낼 수 있다.
		_resultGold = _resultEnergy = _resultBrokenEnergy = _resultEvent = 0;
		_reserveRoomType = 0;
		int betRate = _listBetValue[_currentBetRateIndex];

		// 현재 맥스 층에 따른 베팅 테이블
		StageBetTableData stageBetTableData = TableDataManager.instance.FindStageBetTableData(PlayerData.instance.currentRewardStage);
		if (stageBetTableData == null)
		{
			Debug.LogErrorFormat("Not found StageBetTable! currentHighest = {0} / selected = {1}", PlayerData.instance.highestClearStage, PlayerData.instance.selectedStage);
			return;
		}

		if (_gachaResult == eGachaResult.FindMonsterRoom)
		{
			// 이건 스테이지 진행에 따른 테이블같은거로 될듯. 그 안에서 미리 결정해두고 사용자가 터치하면 보여준다.
			// 아래 GoldBoxRoom과 동일하게 여기서는 플래그만 걸고 획득 패킷은 나중에 보내기로 한다.
			_resultGold = 0;
			_reserveRoomType = (int)eGachaResult.FindMonsterRoom;
		}
		else if (_gachaResult == eGachaResult.GoldBoxRoom)
		{
			// 다른 패킷들과 달리 들어가서 플레이를 해야 보상을 제공하는 구조다.
			// 그러다보니 패킷 보낼때 골드를 보낼수가 없다.
			// 대신 골드를 보낼 수 있는 플래그 하나를 걸어두고 enterFlag처럼 이 값을 클라에게 돌려준다.
			// 이걸 보상패킷으로 보내면 된다.
			// 고블린 룸도 마찬가지 형태로 진행하기로 한다.
			_resultGold = 0;
			_reserveRoomType = (int)eGachaResult.GoldBoxRoom;

			// Prepare 후 패킷할때 리셋될테니 현재 저장된 골드값을 기억해두었다가
			// EndBettingRoom할때 사용하도록 한다.
			CurrencyData.instance.currentGoldBoxRoomReward = CurrencyData.instance.goldBoxTargetReward;
		}
		/*
		else if (IsAll(eSlotImage.SmallDiamond))
		{
			// 아마도 테이블에 따른 값일듯
			_resultDiamond = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Bet3Diamonds") * betRate;
		}
		*/
		else if (_gachaResult == eGachaResult.Energy)
		{
			_resultEnergy = BattleInstanceManager.instance.GetCachedGlobalConstantInt("GachaEnergy") * betRate;
		}
		else
		{
			switch (_gachaResult)
			{
				case eGachaResult.EventPoint1: _resultEvent = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Gacha1Event") * betRate; break;
				case eGachaResult.EventPoint2: _resultEvent = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Gacha2Events") * betRate; break;
				case eGachaResult.EventPoint10: _resultEvent = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Gacha3Events") * betRate; break;
				case eGachaResult.BrokenEnergy1: _resultBrokenEnergy = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Gacha1BrokenEnergy") * betRate; break;
				case eGachaResult.BrokenEnergy2: _resultBrokenEnergy = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Gacha2BrokenEnergys") * betRate; break;
			}

			// include 형태기 때문에 개수에 따라 결과가 달라질거다.
			// 테이블에 값이 있을테니 
			if (_gachaResult == eGachaResult.Gold1 || _gachaResult == eGachaResult.Gold2 || _gachaResult == eGachaResult.Gold10)
			{
				int tableResultGold = 0;
				switch (_gachaResult)
				{
					case eGachaResult.Gold1: tableResultGold = stageBetTableData.g1; break;
					case eGachaResult.Gold2: tableResultGold = stageBetTableData.g2; break;
					case eGachaResult.Gold10: tableResultGold = stageBetTableData.g3; break;
				}
				_resultGold = tableResultGold * betRate;
			}
		}
	}

	ObscuredBool _refreshTurn = false;
	ObscuredInt _refreshNewTurn = 0;
	ObscuredInt _refreshNewGold = 0;
	void PrepareGoldBoxTarget()
	{
		_refreshTurn = false;
		_refreshNewTurn = 0;
		_refreshNewGold = 0;

		// 현재 맥스 층에 따른 베팅 테이블
		StageBetTableData stageBetTableData = TableDataManager.instance.FindStageBetTableData(PlayerData.instance.currentRewardStage);
		if (stageBetTableData == null)
			return;

		// 마지막 남은 턴일때는 서버에 갱신을 알려야한다.
		if (CurrencyData.instance.goldBoxRemainTurn == 1)
			_refreshTurn = true;

		// 그런데 하나 예외 상황이 있다. 
		// 골드박스룸에 진입할때는 남은 턴에 상관없이 무조건 갱신해야한다.
		if (_reserveRoomType == (int)eGachaResult.GoldBoxRoom)
			_refreshTurn = true;

		// 최초 계정생성 후에는 한번이라도 골드박스로 진입할때까지 갱신 자체를 안할거다. 그러니 이런 예외처리는 필요없다.
		//if (CurrencyData.instance.bettingCount == 0)
		//	_refreshTurn = true;

		if (_refreshTurn)
		{
			_refreshNewTurn = UnityEngine.Random.Range(BattleInstanceManager.instance.GetCachedGlobalConstantInt("GoldBoxTurnMin"), BattleInstanceManager.instance.GetCachedGlobalConstantInt("GoldBoxTurnMax") + 1);
			_refreshNewGold = UnityEngine.Random.Range(stageBetTableData.goldBoxMin, stageBetTableData.goldBoxMax);
		}
	}
	#endregion


	#region Result Process
	IEnumerator<float> GachaProcess()
	{
		// 인풋 차단
		GachaCanvas.instance.inputLockObject.SetActive(true);
		GachaCanvas.instance.backKeyButton.interactable = false;

		// 이펙트
		GachaObjects.instance.effectRootTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(5.0f);

		// 결과에 따른 소환 오브젝트들 소환
		GachaObjects.instance.ShowSummonResultObject(true, _gachaResult);

		// 이펙트 마저 사라질 시간까지 대기 후
		yield return Timing.WaitForSeconds(1.0f);
		GachaObjects.instance.effectRootTransform.gameObject.SetActive(false);

		// 결과에 따른 소환 오브젝트에 따른 이펙트
		GachaObjects.instance.ShowSummonResultEffectObject(_gachaResult);

		// 씬 전환해야하는 건 이대로 씬 전환 하면 되고
		if (_gachaResult == eGachaResult.GoldBoxRoom)
		{
			Timing.RunCoroutine(RoomMoveProcess());
		}
		else if (_gachaResult == eGachaResult.FindMonsterRoom)
		{
			Timing.RunCoroutine(RoomMoveProcess());
		}
		else
		{
			// 쭉 화면 안쪽으로 당겨지는 애니를 수행해야한다.
			//
			GachaObjects.instance.GetObject(_gachaResult);

			// 다 끝났으면 인풋 막은거 복구
			GachaCanvas.instance.inputLockObject.SetActive(false);
			GachaCanvas.instance.backKeyButton.interactable = true;

			// 
			CheckNeedRefreshTurn();
		}
	}

	IEnumerator<float> RoomMoveProcess()
	{
		// 몬스터룸 선택되었다는 이펙트 같은거나 알림 표시 후
		string canvasAddress = "";
		switch (_gachaResult)
		{
			case eGachaResult.GoldBoxRoom:
				canvasAddress = "GoldBoxRoomCanvas";
				break;
			case eGachaResult.FindMonsterRoom:
				canvasAddress = "FindMonsterRoomCanvas";
				break;
		}

		// 이펙트 표시 시간만큼 잠시 대기
		yield return Timing.WaitForSeconds(1.0f);

		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 가차 창은 닫을거니까 복구
		GachaCanvas.instance.inputLockObject.SetActive(false);
		GachaCanvas.instance.backKeyButton.interactable = true;

		// 소환 오브젝트를 하이드 시킨다.
		GachaObjects.instance.ShowSummonResultObject(false, _gachaResult);

		// 대신 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);
		
		// 창을 닫고
		GachaCanvas.instance.gameObject.SetActive(false);

		while (GachaCanvas.instance.gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		UIInstanceManager.instance.ShowCanvasAsync(canvasAddress, () =>
		{
			// 
			DelayedLoadingCanvas.Show(false);
			FadeCanvas.instance.FadeIn(0.5f);
		});
	}
	#endregion

	#region Refresh Turn
	public void CheckNeedRefreshTurn()
	{
		if (_needRefreshTurn == false)
			return;

		GachaIndicatorCanvas.instance.SetValue(CurrencyData.instance.goldBoxTargetReward);
		GachaIndicatorCanvas.instance.gameObject.SetActive(false);
		GachaIndicatorCanvas.instance.gameObject.SetActive(true);
	}
	#endregion
}