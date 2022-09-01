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
	public DOTweenAnimation gaugeImageColorTweenAnimation;
	public GameObject eventPointRewardRootObject;
	public RewardIcon eventPointRewardIcon;	
	public RewardIcon eventPointSubRewardIcon;
	public GameObject eventPointRewardEffectObject;
	public GameObject eventPointRewardConditionCountRootObject;
	public Image eventPointIconImage;
	public DOTweenAnimation eventPointIconTweenAnimation;
	public Text eventPointRewardConditionCountText;
	public GameObject eventPointIconEffectObject;

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
		_defaultGaugeColor = gaugeImage.color;
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

		MoveTween(false);
		RefreshInfo();

		// refresh
		RefreshBet();
	}

	void OnDisable()
	{
		ObscuredPrefs.SetInt(OptionManager.instance.OPTION_ENERGY_ALARM, OptionManager.instance.energyAlarm);
	}

	bool _reserveGaugeMoveTweenAnimation;
	bool _updateAdjustRewardRootObject;
	bool _updateAdjustConditionCountRootObject;
	void Update()
	{
		if (_reserveGaugeMoveTweenAnimation)
		{
			gaugeImageTweenAnimation.DORestart();
			_reserveGaugeMoveTweenAnimation = false;
		}

		if (_updateAdjustRewardRootObject)
		{
			eventPointRewardRootObject.SetActive(false);
			eventPointRewardRootObject.SetActive(true);
		}

		if (_updateAdjustConditionCountRootObject)
		{
			eventPointRewardConditionCountRootObject.SetActive(false);
			eventPointRewardConditionCountRootObject.SetActive(true);
		}

		#region Energy
		UpdateFillRemainTime();
		UpdateRefresh();
		#endregion

		#region Event Point
		UpdateStartEventPoint();
		UpdateEventPointCountText();
		#endregion
	}

	public void RefreshInfo()
	{
		gachaText.text = UIString.instance.GetString("GachaUI_Gacha");

		RefreshEventPoint();
		RefreshAlarm();
	}

	#region Energy
	public void RefreshEnergy()
	{
		int current = CurrencyData.instance.energy;
		int max = CurrencyData.instance.energyMax;
		energyRatioSlider.value = (float)current / max;
		energyText.text = string.Format("{0:N0}/{1}", current, max);
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

		betText.text = string.Format("BOOST X{0}", _listBetValue[_currentBetRateIndex]);
	}

	public void OnClickBetButton()
	{
		++_currentBetRateIndex;
		if (_currentBetRateIndex >= _listBetValue.Count)
			_currentBetRateIndex = 0;

		RefreshBet();
	}
	#endregion

	void RefreshAlarm()
	{
		_notUserSetting = true;
		alarmSwitch.isOn = (OptionManager.instance.energyAlarm == 1);
		_notUserSetting = false;
	}
	
	#region Alarm
	bool _ignoreStartEvent = false;
	bool _notUserSetting = false;
	public void OnSwitchOnCompleteAlarm()
	{
		OptionManager.instance.energyAlarm = 1;
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
		CurrencyData.instance.ReserveEnergyNotification();
		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.SpinChargeAlarm);
#elif UNITY_IOS
		MobileNotificationWrapper.instance.CheckAuthorization(() =>
		{
			CurrencyData.instance.ReserveEnergyNotification();
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.SpinChargeAlarm);
		}, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_EnergyNotiAppleLast"), 2.0f);
			Timing.RunCoroutine(DelayedResetSwitch());
		});
#endif
	}

	#region Event Point
	EventPointTypeTableData _currentEventPointTypeTableData;
	EventPointRewardTableData _currentEventPointRewardTableData;
	TweenerCore<float, float, FloatOptions> _tweenReferenceForGauge;
	void RefreshEventPoint()
	{
		if (_tweenReferenceForGauge != null)
			_tweenReferenceForGauge.Kill();

		// gauge
		string currentEventPointId = CurrencyData.instance.eventPointId;
		if (currentEventPointId == "")
		{
			// 비어있으면 fr로 해서 최초 셋팅을 미리 해두기로 한다.
			currentEventPointId = "fr";
		}

		_currentEventPointTypeTableData = null;
		EventPointTypeTableData eventPointTypeTableData = TableDataManager.instance.FindEventPointTypeTableData(currentEventPointId);
		if (eventPointTypeTableData != null)
			_currentEventPointTypeTableData = eventPointTypeTableData;

		if (_currentEventPointTypeTableData == null)
			return;

		int current = CurrencyData.instance.eventPoint;
		int currentMax = 0;

		// current max
		_currentEventPointRewardTableData = null;
		for (int i = 0; i < TableDataManager.instance.eventPointRewardTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.eventPointRewardTable.dataArray[i].eventPointId != currentEventPointId)
				continue;

			if (CurrencyData.instance.eventPoint < TableDataManager.instance.eventPointRewardTable.dataArray[i].requiredAccumulatedEventPoint)
			{
				_currentEventPointRewardTableData = TableDataManager.instance.eventPointRewardTable.dataArray[i];
				currentMax = _currentEventPointRewardTableData.requiredEventPoint;
				int prevMax = _currentEventPointRewardTableData.requiredAccumulatedEventPoint - currentMax;
				current -= prevMax;
				break;
			}
		}
		if (_currentEventPointRewardTableData == null)
		{
			// 최고 리워드를 달성한건지 확인해본다.
			EventPointRewardTableData eventPointRewardTableData = TableDataManager.instance.FindEventPointRewardTableData(eventPointTypeTableData.eventPointId, _currentEventPointTypeTableData.lastRewardNum);
			if (eventPointRewardTableData != null && CurrencyData.instance.eventPoint >= eventPointRewardTableData.requiredAccumulatedEventPoint)
			{
				_currentEventPointRewardTableData = eventPointRewardTableData;
				currentMax = _currentEventPointRewardTableData.requiredEventPoint;
				int prevMax = _currentEventPointRewardTableData.requiredAccumulatedEventPoint - currentMax;
				current -= prevMax;
			}
		}
		if (_currentEventPointRewardTableData == null)
			return;

		if (currentMax == 0)
			currentMax = 1;
		eventPointRewardConditionCountText.text = string.Format("{0:N0} / {1:N0}", current, currentMax);
		_updateAdjustConditionCountRootObject = true;
		float ratio = (float)current / currentMax;
		gaugeImage.fillAmount = 0.0f;
		_tweenReferenceForGauge = DOTween.To(() => gaugeImage.fillAmount, x => gaugeImage.fillAmount = x, ratio, 0.5f).SetEase(Ease.OutQuad).SetDelay(0.3f);
		if (_started)
			gaugeImageTweenAnimation.DORestart();
		else
			_reserveGaugeMoveTweenAnimation = true;

		// reward 
		eventPointRewardIcon.RefreshReward(_currentEventPointRewardTableData.rewardType1, _currentEventPointRewardTableData.rewardValue1, _currentEventPointRewardTableData.rewardCount1);
		eventPointSubRewardIcon.RefreshReward(_currentEventPointRewardTableData.rewardType2, _currentEventPointRewardTableData.rewardValue2, _currentEventPointRewardTableData.rewardCount2);
		eventPointSubRewardIcon.cachedTransform.parent.gameObject.SetActive(false);
		_updateAdjustRewardRootObject = true;

		// eventPoint object icon and 3d Object
		AddressableAssetLoadManager.GetAddressableSprite(_currentEventPointTypeTableData.iconAddress, "Icon", (sprite) =>
		{
			eventPointIconImage.sprite = null;
			eventPointIconImage.sprite = sprite;
		});

		AddressableAssetLoadManager.GetAddressableGameObject(_currentEventPointTypeTableData.modelAddress, "Object", (prefab) =>
		{
			GachaObjects.instance.SetEventPointPrefab(prefab);
		});
	}

	bool _waitPacket = false;
	void UpdateStartEventPoint()
	{
		if (_waitPacket)
			return;
		if (GachaCanvas.instance.inputLockObject.activeSelf)
			return;
		if (WaitingNetworkCanvas.IsShow())
			return;
		if (CommonRewardCanvas.instance != null && CommonRewardCanvas.instance.gameObject.activeSelf)
			return;

		bool send = false;
		bool completeRefresh = false;
		if (CurrencyData.instance.eventPointId == "")
			send = true;
		if (!send && ServerTime.UtcNow > CurrencyData.instance.eventPointExpireTime)
			send = true;
		if (!send)
		{
			// 어차피 리스타트 패킷이 있으니 완료한 이벤트도 이 패킷을 이용해서 교체하기로 한다.
			if (_currentEventPointRewardTableData != null && CurrencyData.instance.eventPointId != "" && _currentEventPointTypeTableData != null)
			{
				if (_currentEventPointTypeTableData.lastRewardNum == _currentEventPointRewardTableData.num && CurrencyData.instance.eventPoint >= _currentEventPointRewardTableData.requiredAccumulatedEventPoint)
				{
					completeRefresh = true;
					send = true;
				}
			}
		}

		if (send == false)
			return;

		string startEventPointId = CurrencyData.instance.GetNextRandomEventPointId();
		EventPointTypeTableData eventPointTypeTableData = TableDataManager.instance.FindEventPointTypeTableData(startEventPointId);
		if (eventPointTypeTableData == null)
			return;

		_waitPacket = true;
		PlayFabApiManager.instance.RequestStartEventPoint(startEventPointId, eventPointTypeTableData.limitHour, eventPointTypeTableData.oneTime, completeRefresh, () =>
		{
			if (CurrencyData.instance.eventPointId != "fr")
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GachaUI_StartNewEventPoint"), 2.0f);

			_waitPacket = false;
			RefreshEventPoint();
		});
	}

	bool _updateEventPointCountText = false;
	float _updateEventPointCountTextRemainTime = 0.0f;
	int _updateEventPointCountMax;
	void UpdateEventPointCountText()
	{
		if (_updateEventPointCountText == false)
			return;

		if (_updateEventPointCountTextRemainTime > 0.0f)
		{
			_updateEventPointCountTextRemainTime -= Time.deltaTime;
			if (_updateEventPointCountTextRemainTime <= 0.0f)
				_updateEventPointCountTextRemainTime = 0.0f;
			return;
		}

		eventPointRewardConditionCountText.text = string.Format("{0:N0} / {1:N0}", (gaugeImage.fillAmount * _updateEventPointCountMax), _updateEventPointCountMax);
		_updateAdjustConditionCountRootObject = true;
		_updateEventPointCountTextRemainTime = 0.1f;
	}
	#endregion

	IEnumerator<float> DelayedResetSwitch()
	{
		yield return Timing.WaitForOneFrame;
		alarmSwitch.AnimateSwitch();
	}

	public void OnSwitchOffCompleteAlarm()
	{
		OptionManager.instance.energyAlarm = 0;
		alarmOnOffText.text = "OFF";
		alarmOnOffText.color = new Color(0.176f, 0.176f, 0.176f);

		if (_notUserSetting)
			return;
		if (_ignoreStartEvent)
		{
			_ignoreStartEvent = false;
			return;
		}

		CurrencyData.instance.CancelEnergyNotification();
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

			#region Event Point
			// event process
			if (_resultEvent > 0 && _currentEventPointTypeTableData != null && _currentEventPointRewardTableData != null)
			{
				int targetEventPoint = CurrencyData.instance.eventPoint + _resultEvent;
				for (int i = 0; i < TableDataManager.instance.eventPointRewardTable.dataArray.Length; ++i)
				{
					if (TableDataManager.instance.eventPointRewardTable.dataArray[i].eventPointId != _currentEventPointRewardTableData.eventPointId)
						continue;
					if (TableDataManager.instance.eventPointRewardTable.dataArray[i].num < _currentEventPointRewardTableData.num)
						continue;

					if (targetEventPoint < TableDataManager.instance.eventPointRewardTable.dataArray[i].requiredAccumulatedEventPoint)
						break;

					// sum reward
					EventPointRewardTableData rewardTableData = TableDataManager.instance.eventPointRewardTable.dataArray[i];
					switch (rewardTableData.rewardType1)
					{
						case "cu":
							switch (rewardTableData.rewardValue1)
							{
								case "GO": _resultGold += rewardTableData.rewardCount1; break;
								case "EN": _resultEnergy += rewardTableData.rewardCount1; break;
							}
							break;
					}

					// 두번째 리워드를 받을 수 있는 상황이라면
					if (false)
					{
						switch (rewardTableData.rewardType2)
						{
							case "cu":
								switch (rewardTableData.rewardValue2)
								{
									case "GO": _resultGold += rewardTableData.rewardCount2; break;
									case "EN": _resultEnergy += rewardTableData.rewardCount2; break;
								}
								break;
						}
					}

					// 혹시 이번이 보상의 마지막 단계인지 확인을 해보자.
					if (_currentEventPointTypeTableData.lastRewardNum == _currentEventPointRewardTableData.num && targetEventPoint >= _currentEventPointRewardTableData.requiredAccumulatedEventPoint)
						break;
				}
			}
			#endregion
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

		// Text 부터 
		GachaObjects.instance.SetResultText(false, 0);

		// 이펙트
		float waitTime = 5.0f;
		GameObject waitEffectObject = GachaObjects.instance.ShowSummonWaitEffect(_gachaResult, ref waitTime);
		yield return Timing.WaitForSeconds(waitTime);

		// 결과에 따른 소환 오브젝트들 소환
		GachaObjects.instance.ShowSummonResultObject(true, _gachaResult);

		switch (_gachaResult)
		{
			case eGachaResult.Gold1:
			case eGachaResult.Gold2:
			case eGachaResult.Gold10:
				GachaObjects.instance.SetResultText(true, _resultGold);
				break;
			case eGachaResult.Energy:
				GachaObjects.instance.SetResultText(true, _resultEnergy);
				break;
			case eGachaResult.EventPoint1:
			case eGachaResult.EventPoint2:
			case eGachaResult.EventPoint10:
				GachaObjects.instance.SetResultText(true, _resultEvent);
				break;
			case eGachaResult.BrokenEnergy1:
			case eGachaResult.BrokenEnergy2:
				GachaObjects.instance.SetResultText(true, _resultBrokenEnergy);
				break;
		}

		// 이펙트 마저 사라질 시간까지 대기 후
		yield return Timing.WaitForSeconds(1.0f);
		waitEffectObject.SetActive(false);

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
		else if (_gachaResult == eGachaResult.EventPoint1 || _gachaResult == eGachaResult.EventPoint2 || _gachaResult == eGachaResult.EventPoint10)
		{
			Timing.RunCoroutine(EventPointProcess());
		}
		else
		{
			// 쭉 화면 안쪽으로 당겨지는 애니를 수행해야한다.
			//
			GachaObjects.instance.GetObject(_gachaResult);

			// 다 끝났으면 인풋 막은거 복구
			GachaCanvas.instance.inputLockObject.SetActive(false);
			GachaCanvas.instance.backKeyButton.interactable = true;

			// 재화도 이때 갱신
			GachaCanvas.instance.currencySmallInfo.RefreshInfo();

			switch (_gachaResult)
			{
				case eGachaResult.Gold1:
				case eGachaResult.Gold2:
				case eGachaResult.Gold10:
				case eGachaResult.Energy:
				case eGachaResult.BrokenEnergy1:
				case eGachaResult.BrokenEnergy2:
					GachaObjects.instance.SetResultText(false, 0);
					break;
			}

			// 
			OnPostProcess();
		}
	}

	Color _defaultGaugeColor;
	IEnumerator<float> EventPointProcess()
	{
		// 쭉 화면 안쪽으로 당겨지는 애니를 수행하는건 우선 똑같다.
		GachaObjects.instance.GetObject(_gachaResult);

		if (_currentEventPointRewardTableData == null)
		{
			// 뭔가 잘못된거면
			GachaCanvas.instance.inputLockObject.SetActive(false);
			GachaCanvas.instance.backKeyButton.interactable = true;
			OnPostProcess();
			yield break;
		}

		if (CurrencyData.instance.eventPoint < _currentEventPointRewardTableData.requiredAccumulatedEventPoint)
		{
			// 이벤트 포인트가 축적만 되고 보상까지 도달하지 않았을때는 연출로만 나오고 끝나는 형태로 짠다.
			// 인풋도 바로 락건거 해제해둔다.
			GachaCanvas.instance.inputLockObject.SetActive(false);
			GachaCanvas.instance.backKeyButton.interactable = true;
			OnPostProcess();

			int current = CurrencyData.instance.eventPoint;
			int currentMax = _currentEventPointRewardTableData.requiredEventPoint;
			int prevMax = _currentEventPointRewardTableData.requiredAccumulatedEventPoint - currentMax;
			current -= prevMax;
			float ratio = (float)current / currentMax;

			yield return Timing.WaitForSeconds(0.8f);

			// 이번 단계의 끝을 넘지 못했으면 차는 연출만 하면서 바로 인풋 풀면 된다.
			if (_tweenReferenceForGauge != null)
				_tweenReferenceForGauge.Kill();

			_updateEventPointCountText = true;
			_updateEventPointCountMax = currentMax;
			gaugeImageColorTweenAnimation.DORestart();
			eventPointIconTweenAnimation.DORestart();
			eventPointIconEffectObject.SetActive(true);
			_tweenReferenceForGauge = DOTween.To(() => gaugeImage.fillAmount, x => gaugeImage.fillAmount = x, ratio, 0.3f).SetEase(Ease.OutQuad).OnComplete(() =>
			{
				_updateEventPointCountText = false;
				gaugeImageColorTweenAnimation.DOPause();
				eventPointIconTweenAnimation.DOPause();
				gaugeImage.color = _defaultGaugeColor;
				eventPointIconTweenAnimation.transform.localScale = Vector3.one;
				eventPointRewardConditionCountText.text = string.Format("{0:N0} / {1:N0}", current, currentMax);
				_updateAdjustConditionCountRootObject = true;
			});
		}
		else
		{
			// 오브젝트 획득을 기다리는건 여기도 마찬가지
			yield return Timing.WaitForSeconds(0.8f);

			// 여기서부터는 보상이 하나 이상은 들어가는 경우다.
			for (int i = 0; i < TableDataManager.instance.eventPointRewardTable.dataArray.Length; ++i)
			{
				if (TableDataManager.instance.eventPointRewardTable.dataArray[i].eventPointId != _currentEventPointRewardTableData.eventPointId)
					continue;
				if (TableDataManager.instance.eventPointRewardTable.dataArray[i].num < _currentEventPointRewardTableData.num)
					continue;

				bool lastProcess = false;
				if (CurrencyData.instance.eventPoint < TableDataManager.instance.eventPointRewardTable.dataArray[i].requiredAccumulatedEventPoint)
					lastProcess = true;

				int current = 0;
				int currentMax = TableDataManager.instance.eventPointRewardTable.dataArray[i].requiredEventPoint;
				float ratio = 0.0f;
				if (lastProcess == false)
				{
					current = currentMax;
					ratio = 1.0f;
				}

				if (lastProcess)
				{
					_currentEventPointRewardTableData = TableDataManager.instance.eventPointRewardTable.dataArray[i];
					current = CurrencyData.instance.eventPoint;
					currentMax = _currentEventPointRewardTableData.requiredEventPoint;
					int prevMax = _currentEventPointRewardTableData.requiredAccumulatedEventPoint - currentMax;
					current -= prevMax;
					ratio = (float)current / currentMax;
				}

				if (ratio == 0.0f)
				{
					// 다음 단계로 넘어갔는데 딱 0에서 끝난 형태다. 29/30 이었다가 다음꺼 0/10 같은거로 바뀐 상태
					// 이땐 그냥 종료시켜야한다.
					EndEventPointProcess();
					yield break;
				}

				// 마지막 단계를 제외하고선 꽉 채울거다.
				// 이번 단계의 끝을 넘지 못했으면 차는 연출만 하면서 바로 인풋 풀면 된다.
				if (_tweenReferenceForGauge != null)
					_tweenReferenceForGauge.Kill();

				_updateEventPointCountText = true;
				_updateEventPointCountMax = currentMax;
				gaugeImageColorTweenAnimation.DORestart();
				eventPointIconTweenAnimation.DORestart();
				eventPointIconEffectObject.SetActive(true);
				_tweenReferenceForGauge = DOTween.To(() => gaugeImage.fillAmount, x => gaugeImage.fillAmount = x, ratio, 0.25f).SetEase(Ease.OutQuad).OnComplete(() =>
				{
					_updateEventPointCountText = false;
					gaugeImageColorTweenAnimation.DOPause();
					eventPointIconTweenAnimation.DOPause();
					gaugeImage.color = _defaultGaugeColor;
					eventPointIconTweenAnimation.transform.localScale = Vector3.one;
					eventPointRewardConditionCountText.text = string.Format("{0:N0} / {1:N0}", current, currentMax);
					_updateAdjustConditionCountRootObject = true;
				});
	
				if (lastProcess)
				{
					EndEventPointProcess();
					yield break;
				}

				yield return Timing.WaitForSeconds(0.2f);

				// 보상 아이콘 점프 애니메이션
				eventPointRewardEffectObject.SetActive(true);
				eventPointRewardRootObject.transform.DOScale(new Vector3(1.7f, 1.7f, 1.7f), 0.25f).SetEase(Ease.OutQuad).OnComplete(() =>
				{
					eventPointRewardRootObject.transform.DOScale(new Vector3(1.0f, 1.0f, 1.0f), 0.2f).SetEase(Ease.InQuad);
				});

				// 대기
				yield return Timing.WaitForSeconds(0.6f);

				// 다음번 항목이 있으면 갱신해주면 된다.
				bool lastReward = false;
				if (_currentEventPointTypeTableData.lastRewardNum == TableDataManager.instance.eventPointRewardTable.dataArray[i].num)
					lastReward = true;


				if (lastReward == false && i + 1 < TableDataManager.instance.eventPointRewardTable.dataArray.Length)
				{
					// 완료가 되었다고 생각하면 다음 과정을 0 부터 시작
					gaugeImage.fillAmount = 0.0f;

					// reward
					eventPointRewardIcon.RefreshReward(TableDataManager.instance.eventPointRewardTable.dataArray[i + 1].rewardType1, TableDataManager.instance.eventPointRewardTable.dataArray[i + 1].rewardValue1, TableDataManager.instance.eventPointRewardTable.dataArray[i + 1].rewardCount1);
					eventPointSubRewardIcon.RefreshReward(TableDataManager.instance.eventPointRewardTable.dataArray[i + 1].rewardType2, TableDataManager.instance.eventPointRewardTable.dataArray[i + 1].rewardValue2, TableDataManager.instance.eventPointRewardTable.dataArray[i + 1].rewardCount2);
					eventPointSubRewardIcon.cachedTransform.parent.gameObject.SetActive(false);
					_updateAdjustRewardRootObject = true;

					// 카운트 교체
					eventPointRewardConditionCountText.text = string.Format("0 / {0:N0}", TableDataManager.instance.eventPointRewardTable.dataArray[i + 1].requiredEventPoint);
					_updateAdjustConditionCountRootObject = true;
					_updateEventPointCountTextRemainTime = 0.1f;
				}
				else
				{
					// 다음번 항목이 없다는건 이벤트포인트를 다 달성해서 다음거로 넘겨야한다는 것이다.
					// 가차 패킷에서는 이 처리를 하지 않지만
					// 가차 절차가 끝나자마자 Update에서 갱신 패킷을 보내게 될 것이다.
					if (lastReward)
					{
						EndEventPointProcess();
						yield break;
					}
				}
			}
		}
	}

	void EndEventPointProcess()
	{
		UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
		{
			GachaCanvas.instance.inputLockObject.SetActive(false);
			GachaCanvas.instance.backKeyButton.interactable = true;

			GachaCanvas.instance.currencySmallInfo.RefreshInfo();
			CommonRewardCanvas.instance.RefreshReward(_resultGold, _resultEnergy, () =>
			{
				OnPostProcess();
			});
		});
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

	#region PostProcess Gacha
	public void OnPostProcess()
	{
		// 여러가지 일들을 할텐데
		// 제일 먼저 하는건 Turn Refresh
		CheckNeedRefreshTurn();

		// 그리고 하는건 에너지 팝업 체크
		CheckCashEvent();
	}
	#endregion

	#region Refresh Turn
	void CheckNeedRefreshTurn()
	{
		if (_needRefreshTurn == false)
			return;

		GachaIndicatorCanvas.instance.SetValue(CurrencyData.instance.goldBoxTargetReward);
		GachaIndicatorCanvas.instance.gameObject.SetActive(false);
		GachaIndicatorCanvas.instance.gameObject.SetActive(true);

		_needRefreshTurn = false;
	}
	#endregion

	#region Cash Event
	void CheckCashEvent()
	{
		EventTypeTableData eventTypeTableData = TableDataManager.instance.FindEventTypeTableData("ev1");
		if (eventTypeTableData == null)
			return;

		if (CurrencyData.instance.energy > 0)
			return;

		if (CashShopData.instance.IsShowEvent(eventTypeTableData.id))
			return;

		if (UnityEngine.Random.value < 0.333f)
			return;

		PlayFabApiManager.instance.RequestOpenCashEvent(eventTypeTableData.id, eventTypeTableData.givenTime, () =>
		{
			MainCanvas.instance.ShowCashEvent(eventTypeTableData.id, true, true);
		});
	}
	#endregion
}