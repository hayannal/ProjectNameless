using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterCanvas : CharacterShowCanvasBase
{
	public static CharacterCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject innerMenuPrefab;

	void Awake()
	{
		instance = this;
	}

	Transform _menuTransform;
	void Start()
	{
		_menuTransform = Instantiate<GameObject>(innerMenuPrefab).transform;
	}

	void OnEnable()
	{
		if (_menuTransform != null)
			_menuTransform.gameObject.SetActive(true);

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		// forceShow상태가 false인 창들은 스택안에 있는채로 다른 창에 가려져있다 라는 의미이므로
		// OnEnable처리중에 일부를 건너뛰어(여기선 저 아래 RefreshGrid 함수)
		// 마지막 정보가 그대로 남아있는채로 다시 보여줘야한다.(마치 어디론가 이동시켜놓고 있다가 다시 보여주는거처럼)
		if (restore)
			return;

		SetInfoCameraMode(true, BattleInstanceManager.instance.playerActor.actorId);
		ShowCanvasPlayerActor(BattleInstanceManager.instance.playerActor.actorId);
		MainCanvas.instance.OnEnterCharacterMenu(true);
		//_playerActor.RefreshWingHide();
		//RefreshGrid(true);
	}

	void OnDisable()
	{
		_menuTransform.gameObject.SetActive(false);

		if (StackCanvas.Pop(gameObject))
			return;

		// 인포창 같은거에 stacked 되어서 disable 상태중에 Home키를 누르면 InfoCamera 모드를 복구해야한다.
		// 이걸 위해 OnPop Action으로 감싸고 Push할때 넣어둔다.
		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;
		if (_playerActor == null || _playerActor.gameObject == null)
			return;

		if (_playerActor != null)
		{
			_playerActor.gameObject.SetActive(false);
			_playerActor = null;
		}
		SetInfoCameraMode(false, "");
		MainCanvas.instance.OnEnterCharacterMenu(false);
		//_playerActor.RefreshWingHide();
	}

	public void OnClickDetailButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("CharacterDetailCanvas", null);
	}
}
