using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
using MEC;

public class BettingCanvas : MonoBehaviour
{
	public static BettingCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public Sprite[] slotSpriteList;
	public Sprite[] slotBlurSpriteList;

	public Button backKeyButton;
	public GameObject inputLockObject;

	public GameObject[] contentItemPrefabList;
	public RectTransform[] contentRootRectTransformList;
	public GridLayoutGroup gridLayoutGroup;

	public Transform goldBoxTargetRootTransform;
	public Text goldBoxTargetValueText;
	public Text bettingResultText;
	public DOTweenAnimation bettingResultTweenAnimation;
	public Transform bettingResultTransform;
	public Text betText;

	public Slider spinRatioSlider;
	public Text spinText;
	public Text fillRemainTimeText;

	public GameObject[] smallGoldEffectObjectList;
	public GameObject bigGoldEffectObject;
	public GameObject spinEffectObject;
	public GameObject diamondEffectObject;
	public GameObject ticketEffectObject;

	// 이벤트 이펙트 역시 여기에 있는데 패치하게 된다면 이걸 교체해서 캔버스를 패치하게 될거 같다.
	public GameObject eventEffectObject;

	public class CustomItemContainer : CachedItemHave<BettingCanvasListItem>
	{
	}
	CustomItemContainer[] _containerList;

	// 서버에서 RoomType으로 체크하기 때문에 GoldBoxRoom 4번이나 GoblinRoom 5번이 바뀌면 서버 스크립트 Betting도 바꿔줘야한다.
	public enum eSlotImage
	{ 
		SmallGold = 0,
		BigGold = 1,
		SmallSpin = 2,
		SmallDiamond = 3,

		GoldBoxRoom = 4,
		GoblinRoom = 5,
		Ticket = 6,

		Event = 7,

		Amount,
	}

	void Awake()
	{
		instance = this;

		_containerList = new CustomItemContainer[contentRootRectTransformList.Length];
		for (int i = 0; i < _containerList.Length; ++i)
			_containerList[i] = new CustomItemContainer();
	}

	void Start()
	{
		for (int i = 0; i < contentItemPrefabList.Length; ++i)
			contentItemPrefabList[i].SetActive(false);

		// 최초에 입장시 해준다.
		InitializeSlot();

		// 최초 설정
		bettingResultText.text = "";
		goldBoxTargetValueText.text = string.Format("{0:N0}", CurrencyData.instance.goldBoxTargetReward);
		_needAdjustRect = true;
	}

	void OnEnable()
	{
		RefreshSpin();

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(true);

		// 한번 셋팅된 상태에서 창을 다시 켤때는 굳이 리셋할 필요 없으니 냅두는게 맞다.
		//InitializeSlot();

		// refresh
		RefreshBet();
	}

	void OnDisable()
	{
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
		if (MainCanvas.instance == null)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	bool _needAdjustRect;
	void Update()
	{
		if (_needAdjustRect)
		{
			goldBoxTargetRootTransform.gameObject.SetActive(false);
			goldBoxTargetRootTransform.gameObject.SetActive(true);
			_needAdjustRect = false;
		}

		UpdateSlot();

		#region Spin
		UpdateFillRemainTime();
		UpdateRefresh();
		#endregion
	}

	List<List<BettingCanvasListItem>> _listListItem = new List<List<BettingCanvasListItem>>();
	List<int> _listSlotInfo = new List<int>();
	float _maxSize;
	void InitializeSlot()
	{
		// 최초 구성은 그냥 랜덤 구성이다.
		for (int i = 0; i < (int)eSlotImage.Amount; ++i)
		{
			_listSlotInfo.Add(i);
		}
		ObjectUtil.Shuffle<int>(_listSlotInfo);

		// 구성이 끝났으면 상단 중단 하단에다가 같은 내용을 채워넣는다.
		// 채울땐 한개씩 밀어서 같은 내용을 채우면 된다.
		// 
		for (int i = 0; i < contentRootRectTransformList.Length; ++i)
		{
			List<BettingCanvasListItem> listBettingCanvasListItem = new List<BettingCanvasListItem>();
			_listListItem.Add(listBettingCanvasListItem);

			// 앞 뒤에다가 
			BettingCanvasListItem bettingCanvasListItem1 = _containerList[i].GetCachedItem(contentItemPrefabList[i], contentRootRectTransformList[i]);
			bettingCanvasListItem1.Initialize(_listSlotInfo[_listSlotInfo.Count - 1]);
			listBettingCanvasListItem.Add(bettingCanvasListItem1);

			for (int j = 0; j < _listSlotInfo.Count; ++j)
			{
				BettingCanvasListItem bettingCanvasListItem = _containerList[i].GetCachedItem(contentItemPrefabList[i], contentRootRectTransformList[i]);
				bettingCanvasListItem.Initialize(_listSlotInfo[j]);
				listBettingCanvasListItem.Add(bettingCanvasListItem);
			}

			BettingCanvasListItem bettingCanvasListItem2 = _containerList[i].GetCachedItem(contentItemPrefabList[i], contentRootRectTransformList[i]);
			bettingCanvasListItem2.Initialize(_listSlotInfo[0]);
			listBettingCanvasListItem.Add(bettingCanvasListItem2);
		}

		// 기본 위치는 한칸씩 밀려서 해두면 된다.
		for (int i = 0; i < contentRootRectTransformList.Length; ++i)
		{
			contentRootRectTransformList[i].anchoredPosition = new Vector2((i+1) * -gridLayoutGroup.cellSize.x - gridLayoutGroup.cellSize.x * 0.5f, contentRootRectTransformList[i].anchoredPosition.y);
		}
		_maxSize = gridLayoutGroup.cellSize.x * (int)eSlotImage.Amount;
	}

	void SwitchSlotImage(int index, bool blurImage)
	{
		if (index < _listListItem.Count)
		{
			List<BettingCanvasListItem> listBettingCanvasListItem = _listListItem[index];
			for (int i = 0; i < listBettingCanvasListItem.Count; ++i)
				listBettingCanvasListItem[i].SwitchBlurImage(blurImage);
		}
	}

	public void OnClickSpinButton()
	{
		int useSpin = _listBetValue[_currentBetRateIndex];
		if (CurrencyData.instance.spin < useSpin)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughSpin"), 2.0f);
			return;
		}

		PrepareBetting();
		PrepareGoldBoxTarget();
		PlayFabApiManager.instance.RequestBetting(useSpin, _resultGold, _resultDiamond, _resultSpin, _resultTicket, _resultEvent, _reserveRoomType, _refreshTurn, _refreshNewTurn, _refreshNewGold, (refreshTurnComplete) =>
		{
			// 턴 바꿔야하는걸 기억시켜두고 연출을 진행하면 된다.
			_needRefreshTurn = true;

			OnRecvSpinSlot();
		});
	}

	bool _needRefreshTurn = false;
	void OnRecvSpinSlot()
	{
		// Spin은 바로 차감 후
		RefreshSpin();

		// 골드나 다이아는 여기서 갱신하면 안되고 이펙트 뜰때 해야한다.
		//currencySmallInfo.RefreshInfo();
		Timing.RunCoroutine(SlotProcess());
	}


	void UpdateSlot()
	{
		// 플래그가 켜지면 회전속도를 줄여나간다.
		if (_listSpinSpeed.Count == contentRootRectTransformList.Length)
		{
			for (int i = 0; i < contentRootRectTransformList.Length; ++i)
			{
				if (_listSpinSpeed[i] != 0.0f)
				{
					contentRootRectTransformList[i].anchoredPosition = new Vector2(contentRootRectTransformList[i].anchoredPosition.x - _listSpinSpeed[i] * Time.deltaTime, contentRootRectTransformList[i].anchoredPosition.y);
					if (contentRootRectTransformList[i].anchoredPosition.x < (-_maxSize - gridLayoutGroup.cellSize.x))
						contentRootRectTransformList[i].anchoredPosition = new Vector2(contentRootRectTransformList[i].anchoredPosition.x + _maxSize, contentRootRectTransformList[i].anchoredPosition.y);
				}

				if (_listSpinAccel[i] != 0.0f)
				{
					_listSpinSpeed[i] = _listSpinSpeed[i] - _listSpinAccel[i] * Time.deltaTime;
					float limitSpeed = 600.0f;					
					if (_listSpinSpeed[i] < limitSpeed)
						_listSpinSpeed[i] = limitSpeed;
				}

				if (_listStopWaitFlag[i])
				{
					if (_listStopAnimationFlag[i] == false)
					{
						// 거리로만 검사하면 프레임 낮은 기기에서 제대로 처리 못할 수도 있다.
						// 그러니 일정시간 이상 돌면 강제로 발동되게 해야한다.
						float diff = contentRootRectTransformList[i].anchoredPosition.x - _listStopPosition[i];
						if (diff * diff < 40.0f * 40.0f || _forceStopFlag)
						{
							if (_forceStopFlag)
							{
								Debug.Log("Stop Slot by forceStop Timer");
								_forceStopFlag = false;
							}

							//if (i == 2 && _listSpinSpeed[i] > 900.0f)
							//	continue;

							_listStopAnimationFlag[i] = true;
							_lastIndex = i;
							contentRootRectTransformList[i].anchoredPosition = new Vector2(_listStopPosition[i], contentRootRectTransformList[i].anchoredPosition.y);
							contentRootRectTransformList[i].DOShakeAnchorPos(0.5f, new Vector2(80.0f, 0.0f)).onComplete += () => { _listStopWaitFlag[_lastIndex] = false; };
							SwitchSlotImage(i, false);

							_listSpinSpeed[i] = _listSpinAccel[i] = 0.0f;
							_forceStopRemainTime = 0.0f;

							//Debug.Log("1111");
						}
					}	
				}
			}
		}

		if (_forceStopRemainTime > 0.0f)
		{
			_forceStopRemainTime -= Time.deltaTime;
			if (_forceStopRemainTime <= 0.0f)
			{
				_forceStopFlag = true;
				_forceStopRemainTime = 0.0f;
			}
		}
	}
	int _lastIndex = 1;


	bool IsAll(eSlotImage slotImage)
	{
		for (int i = 0; i < _listTargetValue.Count; ++i)
		{
			if ((eSlotImage)_listTargetValue[i] != slotImage)
				return false;
		}
		return true;
	}

	bool IsInclude(eSlotImage slotImage)
	{
		for (int i = 0; i < _listTargetValue.Count; ++i)
		{
			if ((eSlotImage)_listTargetValue[i] == slotImage)
				return true;
		}
		return false;
	}


	List<float> _listSpinSpeed = new List<float>();
	List<float> _listSpinAccel = new List<float>();
	List<bool> _listStopWaitFlag = new List<bool>();
	List<bool> _listStopAnimationFlag = new List<bool>();
	List<float> _listStopPosition = new List<float>();
	bool _forceStopFlag = false;
	float _forceStopRemainTime = 0.0f;
	IEnumerator<float> SlotProcess()
	{
		if (_listSpinSpeed.Count == 0)
		{
			for (int i = 0; i < contentRootRectTransformList.Length; ++i)
			{
				_listSpinSpeed.Add(0.0f);
				_listSpinAccel.Add(0.0f);
				_listStopWaitFlag.Add(false);
				_listStopAnimationFlag.Add(false);
				_listStopPosition.Add(0.0f);
			}
		}

		// 인풋 차단
		inputLockObject.SetActive(true);
		backKeyButton.interactable = false;

		// 스케일
		//bettingResultTweenAnimation.DOPlayBackwards();
		bettingResultTransform.DOScale(0.0f, 0.2f);

		// 슬롯들을 회전시킨다.
		// 회전 속도는 일정량 빠른 상태에서 조금씩 차이나는 정도다.
		_listSpinSpeed[0] = Random.Range(2000.0f, 2050.0f);
		SwitchSlotImage(0, true);
		yield return Timing.WaitForSeconds(Random.Range(0.03f, 0.1f));
		_listSpinSpeed[1] = Random.Range(2000.0f, 2050.0f);
		SwitchSlotImage(1, true);
		yield return Timing.WaitForSeconds(Random.Range(0.03f, 0.1f));
		_listSpinSpeed[2] = Random.Range(2000.0f, 2050.0f);
		SwitchSlotImage(2, true);

		// 랜덤 대기 후
		yield return Timing.WaitForSeconds(Random.Range(0.3f, 0.5f));

		// 첫번째 부터 멈춰본다.
		// 세번째 칸과 달리 첫번째와 두번째는 순식간에 멈춰야 오래 안기다리는거처럼 느끼게 된다.
		// 그러니 0.2초 안에 속도를 줄여야하고
		// 타겟 슬롯에 다다를때 바로 멈추는 연출로 바꿔야한다.

		int targetSlot = _listTargetValue[0];
		_listSpinAccel[0] = Random.Range(400.0f, 600.0f);
		_listStopPosition[0] = (targetSlot + 1) * -gridLayoutGroup.cellSize.x - gridLayoutGroup.cellSize.x * 0.5f;
		_listStopWaitFlag[0] = true;
		_listStopAnimationFlag[0] = false;
		_forceStopFlag = false;
		_forceStopRemainTime = 1.0f;

		// 판정은 업데이트 로직에서 하게될거다.
		while (_listStopWaitFlag[0])
			yield return Timing.WaitForOneFrame;


		// 두번째 슬롯도 비슷하게 해본다.
		yield return Timing.WaitForSeconds(Random.Range(0.2f, 0.4f));

		// 그게 아니라면 랜덤하게
		targetSlot = _listTargetValue[1];
		_listSpinAccel[1] = Random.Range(400.0f, 600.0f);
		_listStopPosition[1] = (targetSlot + 1) * -gridLayoutGroup.cellSize.x - gridLayoutGroup.cellSize.x * 0.5f;
		_listStopWaitFlag[1] = true;
		_listStopAnimationFlag[1] = false;
		_forceStopFlag = false;
		_forceStopRemainTime = 1.0f;
		while (_listStopWaitFlag[1])
			yield return Timing.WaitForOneFrame;


		// 세번째 슬롯은 다 맞출때와 아닐때를 구분해서 조금 다르게 처리해야한다.
		// 1번 슬롯 2번 슬롯의 결과에 따라 좀더 기대감을 표현해야하므로
		// 테이블에서 오버라이드된 값이 있다면 그걸 가져와 쓰기로 한다.

		yield return Timing.WaitForSeconds(Random.Range(0.3f, 0.7f));

		// 
		targetSlot = _listTargetValue[2];
		_listSpinAccel[2] = Random.Range(400.0f, 600.0f);
		_listStopPosition[2] = (targetSlot + 1) * -gridLayoutGroup.cellSize.x - gridLayoutGroup.cellSize.x * 0.5f;
		_listStopWaitFlag[2] = true;
		_listStopAnimationFlag[2] = false;
		_forceStopFlag = false;
		_forceStopRemainTime = 3.0f;
		while (_listStopWaitFlag[2])
			yield return Timing.WaitForOneFrame;


		/*
		float diff = targetSlotPosition - contentRootRectTransformList[0].anchoredPosition.x;
		float duration = Random.Range(0.2f, 0.25f);

		// 거꾸로 돌아갈순 없으니 
		if (diff < 0.0f)
			diff += _maxSize;

		_listSpinAccel[0] = (30.0f - _listSpinSpeed[0]) / duration;
		*/

		//{
		// 타겟 위치를 계산한 다음
		// 차이를 구하면 값이 나올텐데
		// 대략적인 범위마다 값을 정해서 감속을 시켜본다.
		/*
		if (distance < 400)
		{

		}
		else if (distance < 500)
		{

		}
		*/

		// 감속이 시작되면 정지할때가 되었는지에 대한 검사를 해야하니
		// 플래그를 켜서 인접하는지를 봐야한다.
		// 근데 그냥 인접만 검사하면 두바퀴는 못돌고 한바퀴만 돌테니
		// 감속이 어느정도 되었을때만 멈추게 해야할거다.

		/*
		// 터치하여 나가기 보여주고
		messageTextObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.5f);

		exitObject.SetActive(true);

		// Refresh
		_targetCharacterBaseLevel = GetReachablePowerLevel(_targetPpCharacter.pp, 0);
		myBalancePpValueText.text = PlayerData.instance.balancePp.ToString("N0");
		RefreshSliderPrice();
		DotMainMenuCanvas.instance.RefreshCharacterAlarmObject();
		*/

		// 결과에 따른 이펙트 처리
		RefreshResultText();
		BasicResultEffect();
		currencySmallInfo.RefreshInfo();

		// 추가 연출이 필요한 것
		if (IsInclude(eSlotImage.Event))
		{
			// 골드나 다른 이펙트 연출이 끝나는걸 기다렸다가
			yield return Timing.WaitForSeconds(0.7f);

			// 이벤트 연출을 하고
			eventEffectObject.SetActive(false);
			eventEffectObject.SetActive(true);

			// 잠시 대기 후 끝내도록 한다.
			yield return Timing.WaitForSeconds(0.3f);
		}

		inputLockObject.SetActive(false);
		backKeyButton.interactable = true;

		// 씬전환이 필요할테니 별도로 처리하기로 한다.
		if (IsAll(eSlotImage.GoblinRoom))
		{
			
		}
		else if (IsAll(eSlotImage.GoldBoxRoom))
		{
			Timing.RunCoroutine(GoldBoxRoomMoveProcess());
		}
		else
		{
			// 씬이동하지 않을땐 
			CheckNeedRefreshTurn();
		}
	}

	public void CheckNeedRefreshTurn()
	{
		if (_needRefreshTurn == false)
			return;

		Timing.RunCoroutine(RefreshGoldBoxTargetValueProcess());
	}

	IEnumerator<float> RefreshGoldBoxTargetValueProcess()
	{
		goldBoxTargetRootTransform.DOLocalMoveX(-400.0f, 0.5f);

		yield return Timing.WaitForSeconds(0.5f);

		goldBoxTargetValueText.text = string.Format("{0:N0}", CurrencyData.instance.goldBoxTargetReward);
		goldBoxTargetRootTransform.localPosition = new Vector3(400.0f, goldBoxTargetRootTransform.localPosition.y, goldBoxTargetRootTransform.localPosition.z);

		goldBoxTargetRootTransform.DOLocalMoveX(0.0f, 0.5f);
	}

	#region Packet
	// 패킷 보내기전에 클라가 먼저 굴림을 결정해야한다.
	List<int> _listTargetValue = new List<int>();

	// 패킷으로 보내는 재화들
	ObscuredInt _resultGold;
	ObscuredInt _resultDiamond;
	ObscuredInt _resultSpin;
	ObscuredInt _resultTicket;
	ObscuredInt _resultEvent;
	ObscuredInt _reserveRoomType;
	void PrepareBetting()
	{
		if (_listTargetValue.Count == 0)
		{
			for (int i = 0; i < contentRootRectTransformList.Length; ++i)
			{
				_listTargetValue.Add(0);
			}
		}

		bool fixedResult = false;
		if (fixedResult)
		{
			_listTargetValue[0] = 3;
		}
		else
		{
			// 그게 아니라면 랜덤하게
			_listTargetValue[0] = Random.Range(0, (int)eSlotImage.Amount);
		}

		// 두번째 슬롯 세번째 슬롯도 마찬가지다.
		_listTargetValue[1] = Random.Range(0, (int)eSlotImage.Amount);
		_listTargetValue[2] = Random.Range(0, (int)eSlotImage.Amount);

		//_listTargetValue[0] = (int)eSlotImage.GoldBoxRoom;
		//_listTargetValue[1] = (int)eSlotImage.GoldBoxRoom;
		//_listTargetValue[2] = (int)eSlotImage.GoldBoxRoom;

		Debug.LogFormat("Betting Prepare : {0} {1} {2}", _listTargetValue[0], _listTargetValue[1], _listTargetValue[2]);

		// 리셋
		// 결과에 따라 미리미리 랜덤 굴릴것들은 굴려놔야 패킷으로 보낼 수 있다.
		_resultGold = _resultDiamond = _resultSpin = _resultTicket = _resultEvent = 0;
		_reserveRoomType = 0;
		int betRate = _listBetValue[_currentBetRateIndex];

		// 현재 맥스 층에 따른 베팅 테이블
		StageBetTableData stageBetTableData = TableDataManager.instance.FindStageBetTableData(PlayerData.instance.currentRewardStage);
		if (stageBetTableData == null)
		{
			Debug.LogErrorFormat("Not found StageBetTable! currentHighest = {0} / selected = {1}", PlayerData.instance.highestClearStage, PlayerData.instance.selectedStage);
			return;
		}

		if (IsAll(eSlotImage.Ticket))
		{
			// 테이블이든 랜덤이든 뭔가로 결정
			_resultTicket = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Bet3Tickets") * betRate;
		}
		else if (IsAll(eSlotImage.GoblinRoom))
		{
			// 이건 스테이지 진행에 따른 테이블같은거로 될듯. 그 안에서 미리 결정해두고 사용자가 터치하면 보여준다.
			// 아래 GoldBoxRoom과 동일하게 여기서는 플래그만 걸고 획득 패킷은 나중에 보내기로 한다.
			_resultGold = 0;
			_reserveRoomType = (int)eSlotImage.GoblinRoom;
		}
		else if (IsAll(eSlotImage.GoldBoxRoom))
		{
			// 다른 패킷들과 달리 들어가서 플레이를 해야 보상을 제공하는 구조다.
			// 그러다보니 패킷 보낼때 골드를 보낼수가 없다.
			// 대신 골드를 보낼 수 있는 플래그 하나를 걸어두고 enterFlag처럼 이 값을 클라에게 돌려준다.
			// 이걸 보상패킷으로 보내면 된다.
			// 고블린 룸도 마찬가지 형태로 진행하기로 한다.
			_resultGold = 0;
			_reserveRoomType = (int)eSlotImage.GoldBoxRoom;

			// Prepare 후 패킷할때 리셋될테니 현재 저장된 골드값을 기억해두었다가
			// EndBettingRoom할때 사용하도록 한다.
			CurrencyData.instance.currentGoldBoxRoomReward = CurrencyData.instance.goldBoxTargetReward;
		}
		else if (IsAll(eSlotImage.SmallDiamond))
		{
			// 아마도 테이블에 따른 값일듯
			_resultDiamond = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Bet3Diamonds") * betRate;
		}
		else if (IsAll(eSlotImage.SmallSpin))
		{
			_resultSpin = BattleInstanceManager.instance.GetCachedGlobalConstantInt("Bet3Spins") * betRate;
		}
		else
		{
			// include 형태기 때문에 개수에 따라 결과가 달라질거다.
			// 테이블에 값이 있을테니 
			int smallCount = 0;
			for (int i = 0; i < _listTargetValue.Count; ++i)
			{
				if (_listTargetValue[i] == (int)eSlotImage.SmallGold)
					++smallCount;
			}
			int bigCount = 0;
			for (int i = 0; i < _listTargetValue.Count; ++i)
			{
				if (_listTargetValue[i] == (int)eSlotImage.BigGold)
					++bigCount;
			}
			if (smallCount > 0 || bigCount > 0)
			{
				int tableResultGold = 0;
				if (bigCount == 0)
				{
					switch (smallCount)
					{
						case 1: tableResultGold = stageBetTableData.s1; break;
						case 2: tableResultGold = stageBetTableData.s2; break;
						case 3: tableResultGold = stageBetTableData.s3; break;
					}
				}
				else if (smallCount == 0)
				{
					switch (bigCount)
					{
						case 1: tableResultGold = stageBetTableData.b1; break;
						case 2: tableResultGold = stageBetTableData.b2; break;
						case 3: tableResultGold = stageBetTableData.b3; break;
					}
				}
				else
				{
					if (smallCount == 1 && bigCount == 1)
						tableResultGold = stageBetTableData.s1b1;
					else if (smallCount == 1 && bigCount == 2)
						tableResultGold = stageBetTableData.s1b2;
					else if (smallCount == 2 && bigCount == 1)
						tableResultGold = stageBetTableData.s2b1;
				}
				_resultGold = tableResultGold * betRate;
			}

			// Event Point
			int eventCount = 0;
			for (int i = 0; i < _listTargetValue.Count; ++i)
			{
				if (_listTargetValue[i] == (int)eSlotImage.Event)
					++eventCount;
			}
			string eventCountKey = "";
			switch (eventCount)
			{
				case 1: eventCountKey = "Bet1Event"; break;
				case 2: eventCountKey = "Bet2Events"; break;
				case 3: eventCountKey = "Bet3Events"; break;
			}
			if (!string.IsNullOrEmpty(eventCountKey))
				_resultEvent = BattleInstanceManager.instance.GetCachedGlobalConstantInt(eventCountKey) * betRate;
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
		if (_reserveRoomType == (int)eSlotImage.GoldBoxRoom)
			_refreshTurn = true;

		// 최초 계정생성 후에는 한번이라도 골드박스로 진입할때까지 갱신 자체를 안할거다. 그러니 이런 예외처리는 필요없다.
		//if (CurrencyData.instance.bettingCount == 0)
		//	_refreshTurn = true;

		if (_refreshTurn)
		{
			_refreshNewTurn = Random.Range(BattleInstanceManager.instance.GetCachedGlobalConstantInt("GoldBoxTurnMin"), BattleInstanceManager.instance.GetCachedGlobalConstantInt("GoldBoxTurnMax") + 1);
			_refreshNewGold = Random.Range(stageBetTableData.goldBoxMin, stageBetTableData.goldBoxMax);
		}
	}
	#endregion

	#region EndEffect
	void RefreshResultText()
	{
		string resultString = "";
		if (_resultTicket > 0)
		{
			if (_resultTicket == 1) resultString = "TICKET";
			else resultString = string.Format("{0} TICKETS", _resultTicket);
		}
		else if (_reserveRoomType == (int)eSlotImage.GoblinRoom)
		{
			resultString = "GOBLIN ROOM";
		}
		else if (_reserveRoomType == (int)eSlotImage.GoldBoxRoom)
		{
			resultString = "GOLDBOX ROOM";
		}
		else if (_resultDiamond > 0)
		{
			if (_resultDiamond == 1) resultString = "DIAMOND";
			else resultString = string.Format("{0} DIAMONDS", _resultDiamond);
		}
		else if (_resultSpin > 0)
		{
			if (_resultSpin == 1) resultString = "SPIN";
			else resultString = string.Format("{0} SPINS", _resultSpin);
		}
		else
		{
			if (_resultGold > 0)
				resultString = string.Format("{0:N0}", _resultGold);
			else if (_resultEvent > 0)
			{
				if (_resultEvent == 1) resultString = "EVENT POINT";
				else resultString = string.Format("{0} EVENT POINTS", _resultEvent);
			}
		}

		if (!string.IsNullOrEmpty(resultString))
		{
			bettingResultText.text = resultString;
			bettingResultTweenAnimation.DORestart();
		}
	}

	void BasicResultEffect()
	{
		// 골드나 다이아만 나올때는 이펙트 처리만 하지만
		// 특수 결과로 나올땐 다음 프로세스를 진행해야한다.
		// 기본 이펙트는 인풋 막는거 없이 바로 보여지고 넘어가는 것들이다.
		if (IsAll(eSlotImage.Ticket))
		{
			// 이펙트만
			ticketEffectObject.SetActive(false);
			ticketEffectObject.SetActive(true);
		}
		/*
		else if (IsAll(eSlotImage.GoblinRoom))
		{
			// 씬전환 필요. 두개는 씬전환이 필요하다
			// Basic함수 말고 다른쪽에서 처리하기로 한다.
		}
		else if (IsAll(eSlotImage.GoldBoxRoom))
		{
		}
		*/
		else if (IsAll(eSlotImage.SmallDiamond))
		{
			// 이펙트만
			diamondEffectObject.SetActive(false);
			diamondEffectObject.SetActive(true);
		}
		else if (IsAll(eSlotImage.SmallSpin))
		{
			// 이펙트만
			spinEffectObject.SetActive(false);
			spinEffectObject.SetActive(true);
		}
		else
		{
			// 골드는 나오는 양에 따라서 이펙트를 나눠서 쓰기로 한다.
			int smallCount = 0;
			for (int i = 0; i < _listTargetValue.Count; ++i)
			{
				if (_listTargetValue[i] == (int)eSlotImage.SmallGold)
					++smallCount;
			}
			int bigCount = 0;
			for (int i = 0; i < _listTargetValue.Count; ++i)
			{
				if (_listTargetValue[i] == (int)eSlotImage.BigGold)
					++bigCount;
			}

			// 카운트에 따라 이펙트 인덱스를 선택한다.
			if (bigCount > 0)
			{
				bigGoldEffectObject.SetActive(false);
				bigGoldEffectObject.SetActive(true);
			}
			else if (smallCount > 0)
			{
				int index = smallCount - 1;
				smallGoldEffectObjectList[index].SetActive(false);
				smallGoldEffectObjectList[index].SetActive(true);
			}
		}
	}

	IEnumerator<float> GoldBoxRoomMoveProcess()
	{
		// 인풋 차단
		inputLockObject.SetActive(true);
		backKeyButton.interactable = false;

		// 골드박스 선택되었다는 이펙트 같은거나 알림 표시 후
		
		// 이펙트 표시 시간만큼 잠시 대기
		yield return Timing.WaitForSeconds(1.0f);

		// 페이드 하면서
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 새로운 전용 캔버스를 호출해둔다.
		// 전용 캔버스가 알아서 StackCanvas형태로 호출되면서 BettingCanvas를 끄게 될거다.
		// 여기서 새로운 그라운드도 만들어질거다.
		UIInstanceManager.instance.ShowCanvasAsync("GoldBoxRoomCanvas", () =>
		{
			// 페이드 풀면서 BettingCanvas는 종료시킬 준비를 하고
			inputLockObject.SetActive(false);
			backKeyButton.interactable = true;

			// 
			FadeCanvas.instance.FadeIn(0.5f);
		});
	}
	#endregion


	#region Spin
	public void RefreshSpin()
	{
		if (PlayerData.instance.clientOnly)
			return;

		int current = CurrencyData.instance.spin;
		int max = CurrencyData.instance.spinMax;
		spinRatioSlider.value = (float)current / max;
		spinText.text = string.Format("{0}/{1}", current, max);
		_lastCurrent = current;
		if (current >= max)
		{
			fillRemainTimeText.text = "";
			_needUpdate = false;
		}
		else
		{
			_nextFillDateTime = CurrencyData.instance.spinRechargeTime;
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

		if (_lastCurrent != CurrencyData.instance.spin)
		{
			RefreshSpin();
			_needRefresh = false;
		}
	}
	#endregion

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
}