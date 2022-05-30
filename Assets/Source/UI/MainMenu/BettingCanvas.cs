using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

	public Text betText;

	public Slider spinRatioSlider;
	public Text spinText;
	public Text fillRemainTimeText;

	public class CustomItemContainer : CachedItemHave<BettingCanvasListItem>
	{
	}
	CustomItemContainer[] _containerList;

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
	}

	void OnEnable()
	{
		MainCanvas.instance.OnEnterCharacterMenu(true);

		// 한번 셋팅된 상태에서 창을 다시 켤때는 굳이 리셋할 필요 없으니 냅두는게 맞다.
		//InitializeSlot();

		// refresh
		RefreshBet();
		RefreshSpin();
	}

	void OnDisable()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;
		if (MainCanvas.instance == null)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	void Update()
	{
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
		if (CurrencyData.instance.spin == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughSpin"), 2.0f);
			return;
		}
		//PlayFabApiManager.instance.RequestBetting(, () =>
		//{
		//	OnRecvSpinSlot();
		//});
	}

	void OnRecvSpinSlot()
	{
		currencySmallInfo.RefreshInfo();
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

		int targetSlot = -1;
		bool fixedResult = false;
		if (fixedResult)
		{
			targetSlot = 3;
		}
		else
		{
			// 그게 아니라면 랜덤하게
			targetSlot = Random.Range(0, (int)eSlotImage.Amount);
		}
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
		targetSlot = Random.Range(0, (int)eSlotImage.Amount);
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
		targetSlot = Random.Range(0, (int)eSlotImage.Amount);
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
		//}

		// 

		/*
		// 배경 페이드
		DOTween.To(() => canvasGroup.alpha, x => canvasGroup.alpha = x, 0.0f, 0.3f).SetEase(Ease.Linear);
		yield return Timing.WaitForSeconds(0.15f);

		// 대상 캐릭터 아이콘 가운데로 이동
		targetCharacterListItem.cachedRectTransform.DOAnchorPos(new Vector2(0.0f, _defaultAnchoredPosition.y), 0.6f);
		yield return Timing.WaitForSeconds(0.6f);

		// 새로운 결과 팝업창이 나오고
		messageTextObject.SetActive(false);
		exitObject.SetActive(false);
		resultGroupObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		float tweenDelay = 0.5f;
		yield return Timing.WaitForSeconds(0.2f);

		// pp 늘어나는 연출
		_ppChangeSpeed = -_addValue / ppChangeTime;
		_floatCurrentPp = _addValue;
		_updatePpText = true;
		yield return Timing.WaitForSeconds(ppChangeTime);
		ppValueTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(tweenDelay);

		*/

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

		inputLockObject.SetActive(false);
		backKeyButton.interactable = true;
	}


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
	int _currentBetIndex;
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
			_currentBetIndex = 0;
		}


		betText.text = string.Format("BET X{0}", _listBetValue[_currentBetIndex]);
	}

	public void OnClickBetButton()
	{
		++_currentBetIndex;
		if (_currentBetIndex >= _listBetValue.Count)
			_currentBetIndex = 0;

		RefreshBet();
	}
}