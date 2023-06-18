using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Michsky.UI.Hexart;
using PlayFab.ClientModels;
using MEC;
using DG.Tweening;

public class RandomBoxScreenCanvas : MonoBehaviour
{
	public static RandomBoxScreenCanvas instance;

	public enum eBoxType
	{
		Spell,
		Character,
		Pet,
		Equip,
	}

	public CurrencySmallInfo currencySmallInfo;

	public SpellBoxResultCanvas spellBoxResultCanvas;
	public CharacterBoxResultCanvas characterBoxResultCanvas;
	public EquipBoxResultCanvas equipBoxResultCanvas;

	public CanvasGroup rootCanvasGroup;
	public GameObject objectRoot;
	public Animator openReadyAnimator;
	public Animator openAnimator;
	public GameObject boxOpenEffectObject;
	public Image boxImage;
	public Sprite defaultBoxSprite;
	public Coffee.UIExtensions.UIEffect addColorEffect;

	public GameObject exitObject;
	public GameObject spellRetryRootObject;
	public GameObject pickUpCharacterRetryRootObject;
	public GameObject characterRetryRootObject;
	public GameObject pickUpEquipRetryRootObject;
	public GameObject equipRetryRootObject;
	public GameObject bottomInputLockObject;

	public GameObject switchGroupObject;
	public SwitchAnim alarmSwitch;

	// 각각을 규격화 하기 어려우니 서브 캔버스로 구현해야할듯.
	// x100 회 뽑기일수도 있으니 작게 나오는 그리드도 필요할거다.

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		// 각각의 그리드에는 스케일 OutBounce 가 적용되어있는데 아이템이 최초로 생성되면서 그리드가 표시되면
		// tween애니 실행중에 복제되면서 스케일 초기값이 스케일 먹여진채로 시작되게 된다.
		// 이걸 막기 위해서 자식 캔버스들을 켜둔채로 프리팹에 적용해두고 시작과 동시에 tween은 오브젝트가 꺼진채로 실행시켜두기로 한다.
		spellBoxResultCanvas.gameObject.SetActive(false);
		characterBoxResultCanvas.gameObject.SetActive(false);
		equipBoxResultCanvas.gameObject.SetActive(false);
	}

	bool _cashShopProcess = false;
	bool _consumeProcess = false;
	void OnEnable()
	{
		//MainCanvas.instance.OnEnterCharacterMenu(true);

		if (CashShopTabCanvas.instance != null && CashShopTabCanvas.instance.gameObject.activeSelf)
		{
			_cashShopProcess = true;
			StackCanvas.Push(gameObject);
		}
		else if (GachaCanvas.instance != null && GachaCanvas.instance.gameObject.activeSelf)
		{
			StackCanvas.Push(gameObject);
		}
		else if (ResearchCanvas.instance != null && ResearchCanvas.instance.gameObject.activeSelf)
		{
			StackCanvas.Push(gameObject);
		}
		else if (SevenDaysTabCanvas.instance != null && SevenDaysTabCanvas.instance.gameObject.activeSelf)
		{
			StackCanvas.Push(gameObject);
		}
		else if (FestivalTabCanvas.instance != null && FestivalTabCanvas.instance.gameObject.activeSelf)
		{
			StackCanvas.Push(gameObject);
		}
		else if (MainCanvas.instance.IsHideState() == false)
		{
			_consumeProcess = true;
			// 앱 시작시 컨슘 복구 과정이라면 이렇게 들어오게 된다.
			MainCanvas.instance.OnEnterCharacterMenu(true);
		}

		objectRoot.SetActive(false);
		Timing.RunCoroutine(OpenDropProcess());
	}

	void OnDisable()
	{
		if (CashShopTabCanvas.instance != null && CashShopTabCanvas.instance.gameObject.activeSelf == false && StackCanvas.IsInStack(CashShopTabCanvas.instance.gameObject))
		{
			_cashShopProcess = false;
			StackCanvas.Pop(gameObject);
		}
		else if (GachaCanvas.instance != null && GachaCanvas.instance.gameObject.activeSelf == false && StackCanvas.IsInStack(GachaCanvas.instance.gameObject))
		{
			StackCanvas.Pop(gameObject);
		}
		else if (ResearchCanvas.instance != null && ResearchCanvas.instance.gameObject.activeSelf == false && StackCanvas.IsInStack(ResearchCanvas.instance.gameObject))
		{
			StackCanvas.Pop(gameObject);
		}
		else if (SevenDaysTabCanvas.instance != null && SevenDaysTabCanvas.instance.gameObject.activeSelf == false && StackCanvas.IsInStack(SevenDaysTabCanvas.instance.gameObject))
		{
			StackCanvas.Pop(gameObject);
		}
		else if (FestivalTabCanvas.instance != null && FestivalTabCanvas.instance.gameObject.activeSelf == false && StackCanvas.IsInStack(FestivalTabCanvas.instance.gameObject))
		{
			StackCanvas.Pop(gameObject);
		}
		else if (_consumeProcess)
		{
			_consumeProcess = false;
			MainCanvas.instance.OnEnterCharacterMenu(false);
		}

		ResetBoxState();

		bottomInputLockObject.SetActive(false);
		spellRetryRootObject.SetActive(false);
		pickUpCharacterRetryRootObject.SetActive(false);
		characterRetryRootObject.SetActive(false);
		pickUpEquipRetryRootObject.SetActive(false);
		equipRetryRootObject.SetActive(false);
		switchGroupObject.SetActive(false);
		exitObject.SetActive(false);
		_recvResult = false;
		_retryRemainTime = 0.0f;
		s_lastPickUpState = false;

		if (_closeAction != null)
		{
			_closeAction();
			_closeAction = null;
		}
	}

	void ResetBoxState()
	{
		openAnimator.enabled = false;
		openReadyAnimator.enabled = false;
		boxOpenEffectObject.SetActive(false);
		boxImage.sprite = defaultBoxSprite;
		addColorEffect.colorFactor = 0.0f;
		boxImage.color = Color.white;
	}

	void Update()
	{
		UpdateRetry();
	}

	public void OnClickBackground()
	{
		//_waitTouch = false;
	}

	IEnumerator<float> OpenDropProcess()
	{
		yield return Timing.WaitForSeconds(0.1f);
		objectRoot.SetActive(true);

		yield return Timing.WaitForSeconds(0.9f);
		openReadyAnimator.enabled = true;
		boxOpenEffectObject.SetActive(true);

		yield return Timing.WaitForSeconds(0.5f);
		openAnimator.enabled = true;

		yield return Timing.WaitForSeconds(0.5f);
		boxImage.DOFade(0.0f, 0.15f);

		// 박스가 완전히 사라지기 전에
		yield return Timing.WaitForSeconds(0.05f);

		if (_recvResult)
		{
			// 타입에 따라 결과창을 보여주고
			ShowResult();
		}
		else
		{
			// 만약 여기까지 했는데도 패킷을 못받았으면 대기중임을 알리는 창을 켜고 기다린다.
			WaitingNetworkCanvas.Show(true);
			_tooLate = true;
		}

		yield break;
	}

	eBoxType _boxType;
	bool _recvResult;
	List<ItemInstance> _listItemInstance;
	bool _tooLate;
	public void OnRecvResult(eBoxType boxType, List<ItemInstance> listItemInstance)
	{
		currencySmallInfo.RefreshInfo();

		_recvResult = true;
		_boxType = boxType;
		_listItemInstance = listItemInstance;

		// 이미 엄청 늦은 상태에서 받는거라면 wait창 닫고 바로 결과를 보여준다.
		if (_tooLate && WaitingNetworkCanvas.IsShow())
		{
			WaitingNetworkCanvas.Show(false);
			ShowResult();
			_tooLate = false;
		}
	}

	#region Retry
	public void OnRecvRetryResult(eBoxType boxType, List<ItemInstance> listItemInstance)
	{
		// currency
		currencySmallInfo.RefreshInfo();

		// 먼저 닫아야할 결과들을 닫아둔다.
		ResetBoxState();
		switch (boxType)
		{
			case eBoxType.Spell: spellBoxResultCanvas.gameObject.SetActive(false); break;
			case eBoxType.Character: characterBoxResultCanvas.gameObject.SetActive(false); break;
			case eBoxType.Equip: equipBoxResultCanvas.gameObject.SetActive(false); break;
		}

		_recvResult = true;
		_boxType = boxType;
		_listItemInstance = listItemInstance;

		// 창이 열린채로 재시도 하는거라 기존 로직과는 달리 하단 버튼들을 비활성화 하는 처리가 필요하다
		bottomInputLockObject.SetActive(true);

		// OnEnable 하던거처럼 처리
		objectRoot.SetActive(false);
		Timing.RunCoroutine(OpenDropProcess());
	}

	float _retryRemainTime;
	void UpdateRetry()
	{
		if (_retryRemainTime > 0.0f)
		{
			_retryRemainTime -= Time.deltaTime;
			if (_retryRemainTime <= 0.0f)
			{
				_retryRemainTime = 0.0f;
				RetryGacha();
			}
		}
	}

	// 픽업인지 구분하는건 RandomBoxScreenCanvas 인스턴스 생성되기 전에 저장해놔야해서
	// 아예 static으로 저장하기로 한다.
	public static bool s_lastPickUpState { get; set; }

	int _lastPrice = 0;
	CashShopSpellSmallListItem _spellSmallListItem;
	CashShopCharacterSmallListItem _characterSmallListItem;
	CashShopEquipSmallListItem _equipSmallListItem;
	PickUpCharacterSmallListItem _pickUpCharacterSmallListItem;
	PickUpEquipSmallListItem _pickUpEquipSmallListItem;
	public void SetLastItem(CashShopSpellSmallListItem item, int price) { _spellSmallListItem = item; _lastPrice = price; s_lastPickUpState = false; }
	public void SetLastItem(CashShopCharacterSmallListItem item, int price) { _characterSmallListItem = item; _lastPrice = price; s_lastPickUpState = false; }
	public void SetLastItem(CashShopEquipSmallListItem item, int price) { _equipSmallListItem = item; _lastPrice = price; s_lastPickUpState = false; }
	public void SetLastItem(PickUpCharacterSmallListItem item, int price) { _pickUpCharacterSmallListItem = item; _lastPrice = price; s_lastPickUpState = true; }
	public void SetLastItem(PickUpEquipSmallListItem item, int price) { _pickUpEquipSmallListItem = item; _lastPrice = price; s_lastPickUpState = true; }
	void RetryGacha()
	{
		switch (_boxType)
		{
			case eBoxType.Spell:
				_spellSmallListItem.OnClickButton();
				break;
			case eBoxType.Character:
				if (s_lastPickUpState) _pickUpCharacterSmallListItem.OnClickButton();
				else _characterSmallListItem.OnClickButton();
				break;
			case eBoxType.Equip:
				if (s_lastPickUpState) _pickUpEquipSmallListItem.OnClickButton();
				else _equipSmallListItem.OnClickButton();
				break;
		}
	}
	#endregion

	#region SpellBoxShow
	List<string> _listNewSpellId = new List<string>();
	#endregion
	#region CharacterBoxShow
	List<string> _listNewCharacterId = new List<string>();
	List<string> _listTrpCharacterId = new List<string>();
	#endregion
	void ShowResult()
	{
		switch (_boxType)
		{
			case eBoxType.Spell:

				_listNewSpellId.Clear();
				// 스킬도 신규 획득창이 필요하다고 한다.
				for (int i = 0; i < _listItemInstance.Count; ++i)
				{
					if (_listItemInstance[i].UsesIncrementedBy == _listItemInstance[i].RemainingUses)
						_listNewSpellId.Add(_listItemInstance[i].ItemId);
				}
				if (_listNewSpellId.Count > 0)
				{
					UIInstanceManager.instance.ShowCanvasAsync("SpellInfoCanvas", () =>
					{
						SpellInfoCanvas.instance.SetNewInfo(_listNewSpellId, () =>
						{
							spellBoxResultCanvas.ShowResult(_listItemInstance);
							spellBoxResultCanvas.gameObject.SetActive(true);
						});
					});
				}
				else
				{
					spellBoxResultCanvas.ShowResult(_listItemInstance);
					spellBoxResultCanvas.gameObject.SetActive(true);
				}
				break;
			case eBoxType.Character:

				_listNewCharacterId.Clear();
				_listTrpCharacterId.Clear();
				// 캐릭터와 펫 장비는 풀스크린 연출창이 들어가야한다. 대신 창을 띄울 수 없는지 확인하고 띄워야한다.
				for (int i = 0; i < _listItemInstance.Count; ++i)
				{
					if (_listItemInstance[i].ItemId.Contains("pp"))
						continue;

					bool newCharacter = false;
					if (_listItemInstance[i].UsesIncrementedBy == _listItemInstance[i].RemainingUses)
					{
						_listNewCharacterId.Add(_listItemInstance[i].ItemId);
						newCharacter = true;
					}
					for (int j = 0; j < _listItemInstance[i].UsesIncrementedBy; ++j)
					{
						if (newCharacter && j == 0)
							continue;
						_listTrpCharacterId.Add(_listItemInstance[i].ItemId);
					}
				}
				if (_listNewCharacterId.Count > 0 || _listTrpCharacterId.Count > 0)
				{
					UIInstanceManager.instance.ShowCanvasAsync("CharacterBoxShowCanvas", () =>
					{
						rootCanvasGroup.alpha = 0.0f;
						CharacterBoxShowCanvas.instance.ShowCanvas(_listNewCharacterId, _listTrpCharacterId, () =>
						{
							rootCanvasGroup.alpha = 1.0f;
							characterBoxResultCanvas.ShowResult(_listItemInstance, _listNewCharacterId, _listTrpCharacterId);
							characterBoxResultCanvas.gameObject.SetActive(true);
						});
					});
				}
				else
				{
					characterBoxResultCanvas.ShowResult(_listItemInstance, _listNewCharacterId, _listTrpCharacterId);
					characterBoxResultCanvas.gameObject.SetActive(true);
				}
				break;
			case eBoxType.Equip:
				equipBoxResultCanvas.ShowResult(_listItemInstance);
				equipBoxResultCanvas.gameObject.SetActive(true);
				break;
		}
	}

	System.Action _closeAction;
	public void SetCloseCallback(Action closeCallback)
	{
		_closeAction = closeCallback;
	}

	#region Retry
	public void OnEndGachaGridProcess()
	{
		exitObject.SetActive(true);

		// 캐시샵에서 열었던거라면 추가로 굴릴 수 있게 처리해야한다.
		if (_cashShopProcess)
		{
			switch (_boxType)
			{
				case eBoxType.Spell:
					spellRetryRootObject.SetActive(true);
					break;
				case eBoxType.Character:
					if (s_lastPickUpState)
					{
						pickUpCharacterRetryRootObject.SetActive(false);
						if (CashShopCanvas.IsUsablePickUpCharacter() == false)
						{
							// 이벤트가 종료되거나 캐릭터를 다 뽑아서 굴릴 수 없다면 더이상 재굴림을 허용하지 않는다.
							bottomInputLockObject.SetActive(false);
							switchGroupObject.SetActive(false);
							return;
						}
						pickUpCharacterRetryRootObject.SetActive(true);
					}
					else
						characterRetryRootObject.SetActive(true);
					break;
				case eBoxType.Equip:
					if (EquipManager.instance.IsInventoryVisualMax())
					{
						// 인벤이 꽉차도 
						bottomInputLockObject.SetActive(false);
						switchGroupObject.SetActive(false);
						return;
					}

					if (s_lastPickUpState)
					{
						pickUpEquipRetryRootObject.SetActive(false);
						if (CashShopCanvas.IsUsablePickUpEquip() == false)
						{
							// 이벤트가 종료되면 더이상 재굴림을 허용하지 않는다.
							bottomInputLockObject.SetActive(false);
							switchGroupObject.SetActive(false);
							return;
						}
						pickUpEquipRetryRootObject.SetActive(true);
					}
					else
						equipRetryRootObject.SetActive(true);
					break;
			}

			// 캐시샵 열고 처음 굴릴때는 안보이다 나타나는거니 초기화를 해주고
			if (switchGroupObject.activeSelf == false)
			{
				bottomInputLockObject.SetActive(false);
				switchGroupObject.SetActive(true);
				if (alarmSwitch.isOn)
					alarmSwitch.AnimateSwitch();
			}
			else
			{
				// 두번째 굴릴때부터는 switch 상태가 켜져있는지 확인하고 연속가차를 실행한다.
				if (alarmSwitch.isOn)
				{
					if (CurrencyData.instance.dia < _lastPrice)
					{
						bottomInputLockObject.SetActive(false);
						alarmSwitch.AnimateSwitch();
						return;
					}

					_retryRemainTime = 0.6f;
				}
				else
					bottomInputLockObject.SetActive(false);
			}
		}
	}
	#endregion


	#region Alarm
	public void OnSwitchOnAutoGacha()
	{
		
	}

	IEnumerator<float> DelayedResetSwitch()
	{
		yield return Timing.WaitForOneFrame;
		alarmSwitch.AnimateSwitch();
	}

	public void OnSwitchOffAutoGacha()
	{
		bottomInputLockObject.SetActive(false);
	}
	#endregion
}