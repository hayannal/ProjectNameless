using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterBoxShowCanvas : CharacterShowCanvasBase
{
	public static CharacterBoxShowCanvas instance;

	public Text characterNameText;
	public Text characterDescText;
	public GameObject effectPrefab;

	GameObject _effectObject;
	Vector3 _playerPrevPosition;
	Quaternion _playerPrevRotation;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		// 처음엔 Stack으로 구현하려고 했는데 랜덤박스나 가차나 창이 비활성화 될때 처리할게 훨씬 더 많아진다.
		// 그래서 그냥 알파로 랜덤박스 가려둔채로 위에다가 올리기로 한다.
		//bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);	
		//if (restore)
		//	return;

		// CharacterShowCanvasBase 클래스를 기반으로 만들어져있는데 이게 사실은 CharacterListCanvas에 맞춰서 만들어진건데
		// CharacterListCanvas뿐만 아니라 ExperienceCanvas까지 다 엮여있어서 이제와서 고치기가 너무 어렵다.
		// 그래서 CharacterShowCanvasBase를 수정하지 않는 선에서 어떤식으로 호출되어도 캐릭터가 잘 보여지도록 처리해보기로 한다.
		// 
		// 우선 현재 캐릭터 기반으로 카메라 모드를 변경시키고
		SetInfoCameraMode(true, BattleInstanceManager.instance.playerActor.actorId);

		// 그리고 아예 null로 바꿔서 곧바로 OnDisable이 호출되더라도-그럴일은 없겠지만
		// 잘 복구되도록 한다.
		_playerActor = null;
	}

	void OnDisable()
	{
		//if (StackCanvas.Pop(gameObject))
		//	return;

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

		SetInfoCameraMode(false, "");

		if (_playerActor != null)
		{
			_playerActor.RefreshWingHide();
			_playerActor.gameObject.SetActive(false);
			_playerActor = null;
		}
		if (_effectObject != null)
		{
			_effectObject.SetActive(false);
			_effectObject = null;
		}
	}

	Action _okAction;
	int _showIndex;
	List<string> _listId = new List<string>();
	public void ShowCanvas(List<string> listNewCharacterId, List<string> listTrpCharacterId, Action okAction)
	{
		// 연속해서 호출될 수 있으므로 미리 꺼놔야한다.
		if (_effectObject != null)
			_effectObject.SetActive(false);
		if (_playerActor != null)
			_playerActor.gameObject.SetActive(false);

		_okAction = okAction;

		_showIndex = 0;
		_listId.Clear();
		for (int i = 0; i < listNewCharacterId.Count; ++i)
			_listId.Add(listNewCharacterId[i]);
		for (int i = 0; i < listTrpCharacterId.Count; ++i)
			_listId.Add(listTrpCharacterId[i]);

		RefreshShowInfo();
	}

	void RefreshShowInfo()
	{
		if (_showIndex < _listId.Count)
		{
			string actorId = _listId[_showIndex];
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
			if (actorTableData == null)
				return;

			characterNameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
			characterDescText.SetLocalizedText(UIString.instance.GetString(actorTableData.descId));

			if (_effectObject != null)
				_effectObject.SetActive(false);
			if (_playerActor != null)
			{
				_playerActor.gameObject.SetActive(false);
				_playerActor = null;
			}

			ShowCanvasPlayerActor(actorId, () =>
			{
				OnAfterLoaded();
			});
			++_showIndex;
		}
		else if (_showIndex == _listId.Count)
		{

		}
	}
	
	void OnAfterLoaded()
	{
		_effectObject = BattleInstanceManager.instance.GetCachedObject(effectPrefab, _rootOffsetPosition, Quaternion.identity, null);
	}
	
	public void OnClickConfirmButton()
	{
		if (_showIndex < _listId.Count)
		{
			RefreshShowInfo();
			return;
		}

		if (_okAction != null)
			_okAction();
		gameObject.SetActive(false);
	}
}