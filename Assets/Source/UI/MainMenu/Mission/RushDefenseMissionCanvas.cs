using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RushDefenseMissionCanvas : MonoBehaviour
{
	public static RushDefenseMissionCanvas instance;

	public GameObject rushDefenseMissionPrefab;

	public GameObject selectPositionTextObject;

	public GameObject toggleContentItemPrefab;
	public RectTransform toggleRootRectTransform;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		toggleContentItemPrefab.SetActive(false);

		RefreshCharacterButton();
	}

	Transform _groundTransform;
	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		if (_groundTransform == null)
			_groundTransform = BattleInstanceManager.instance.GetCachedObject(rushDefenseMissionPrefab, StageManager.instance.GetSafeWorldOffset(), Quaternion.identity).transform;
		else
			_groundTransform.gameObject.SetActive(true);

		if (MainCanvas.instance != null)
		{
			MainCanvas.instance.OnClickCloseButton();
			// HideState를 풀어놔야 카메라 쉐이크가 동작한다.
			MainCanvas.instance.OnEnterCharacterMenu(false, true);
			MainCanvas.instance.challengeButtonObject.SetActive(false);
			MainCanvas.instance.inputRectObject.SetActive(false);
			MainCanvas.instance.FadeOutQuestInfoGroup(0.0f, 0.1f, false, true);
		}
	}

	void OnDisable()
	{
		// 씬을 종료하고 새 씬을 구축하러 나가는 로직으로 구현되어있기 때문에
		// 하단 라인들로 넘어갈 이유가 없다. 그러니 여기서 리턴시킨다.
		return;

		//MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;
		if (CustomFollowCamera.instance == null || CameraFovController.instance == null || MainCanvas.instance == null)
			return;
		if (CustomFollowCamera.instance.gameObject == null)
			return;

		//MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	public void OnClickBackButton()
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_BackToLobby"), UIString.instance.GetString("GameUI_BackToLobbyDescription"), () => {
			SceneManager.LoadScene(0);
		});
	}

	List<CharacterToggleButton> _listCharacterToggleButton = new List<CharacterToggleButton>();
	void RefreshCharacterButton()
	{
		List<string> listSelectedActorId = RushDefenseEnterCanvas.instance.listSelectedActorId;

		GameObject newPlayerObject = Instantiate<GameObject>(toggleContentItemPrefab, toggleRootRectTransform);
		CharacterToggleButton characterToggleButton = newPlayerObject.GetComponent<CharacterToggleButton>();
		characterToggleButton.RefreshInfo(CharacterData.s_PlayerActorId);
		newPlayerObject.SetActive(true);
		_listCharacterToggleButton.Add(characterToggleButton);

		for (int i = 0; i < listSelectedActorId.Count; ++i)
		{
			GameObject newObject = Instantiate<GameObject>(toggleContentItemPrefab, toggleRootRectTransform);
			CharacterToggleButton newToggleButton = newObject.GetComponent<CharacterToggleButton>();
			newToggleButton.RefreshInfo(listSelectedActorId[i]);
			newObject.SetActive(true);
			_listCharacterToggleButton.Add(newToggleButton);
		}

		// 항상 게임을 처음 켤땐 0번탭을 보게 해준다.
		OnValueChangedToggle(CharacterData.s_PlayerActorId);
	}

	public string currentSelectedActorId { get; set; }
	public void OnValueChangedToggle(string actorId)
	{
		currentSelectedActorId = actorId;
		for (int i = 0; i < _listCharacterToggleButton.Count; ++i)
			_listCharacterToggleButton[i].OnSelect(_listCharacterToggleButton[i].actorId == actorId);
	}

	int _spawnedCount = 0;
	public void OnSpawnActor()
	{
		for (int i = 0; i < _listCharacterToggleButton.Count; ++i)
		{
			if (_listCharacterToggleButton[i].actorId == currentSelectedActorId)
			{
				_spawnedCount += 1;
				_listCharacterToggleButton[i].gameObject.SetActive(false);
				break;
			}
		}

		if (_spawnedCount == _listCharacterToggleButton.Count)
		{
			// 모두 스폰한거다. 이제 게임을 시작.
			RushDefenseMissionGround.instance.OnFinishSelect();
			return;
		}

		// 다음 항목을 선택해둔다.
		for (int i = 0; i < _listCharacterToggleButton.Count; ++i)
		{
			if (_listCharacterToggleButton[i].gameObject.activeSelf)
			{
				OnValueChangedToggle(_listCharacterToggleButton[i].actorId);
				break;
			}
		}
	}
}