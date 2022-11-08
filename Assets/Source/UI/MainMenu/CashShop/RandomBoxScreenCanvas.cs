using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

	public SpellBoxResultCanvas spellBoxResultCanvas;

	public GameObject objectRoot;
	public Animator openReadyAnimator;
	public Animator openAnimator;
	public GameObject boxOpenEffectObject;
	public Image boxImage;
	public Sprite defaultBoxSprite;
	public Coffee.UIExtensions.UIEffect addColorEffect;

	public GameObject exitObject;

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
	}

	void OnEnable()
	{
		//MainCanvas.instance.OnEnterCharacterMenu(true);

		if (CashShopCanvas.instance != null && CashShopCanvas.instance.gameObject.activeSelf)
		{
			StackCanvas.Push(gameObject);
		}
		else if (GachaCanvas.instance != null && GachaCanvas.instance.gameObject.activeSelf)
		{
			StackCanvas.Push(gameObject);
		}

		objectRoot.SetActive(false);
		Timing.RunCoroutine(OpenDropProcess());
	}

	void OnDisable()
	{
		if (CashShopCanvas.instance != null && CashShopCanvas.instance.gameObject.activeSelf == false && StackCanvas.IsInStack(CashShopCanvas.instance.gameObject))
		{
			StackCanvas.Pop(gameObject);
		}
		else if (GachaCanvas.instance != null && GachaCanvas.instance.gameObject.activeSelf == false && StackCanvas.IsInStack(GachaCanvas.instance.gameObject))
		{
			StackCanvas.Pop(gameObject);
		}

		openAnimator.enabled = false;
		openReadyAnimator.enabled = false;
		boxOpenEffectObject.SetActive(false);
		boxImage.sprite = defaultBoxSprite;
		addColorEffect.colorFactor = 0.0f;
		boxImage.color = Color.white;
		exitObject.SetActive(false);
		_recvResult = false;

		if (_closeAction != null)
		{
			_closeAction();
			_closeAction = null;
		}
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

	void ShowResult()
	{
		switch (_boxType)
		{
			case eBoxType.Spell: spellBoxResultCanvas.ShowResult(_listItemInstance); spellBoxResultCanvas.gameObject.SetActive(true); break;
		}
	}

	System.Action _closeAction;
	public void SetCloseCallback(Action closeCallback)
	{
		_closeAction = closeCallback;
	}
}