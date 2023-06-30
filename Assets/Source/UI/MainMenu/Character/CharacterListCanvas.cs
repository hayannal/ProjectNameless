using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using MEC;

public class CharacterListCanvas : CharacterShowCanvasBase
{
	public static CharacterListCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public Transform separateLineTransform;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;
	public RectTransform noGainContentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<CharacterCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	public class NoGainCustomItemContainer : CachedItemHave<CharacterCanvasListItem>
	{
	}
	NoGainCustomItemContainer _noGainContainer = new NoGainCustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		if (PlayerData.instance.openFlagShowCharacterCanvas == false)
		{
			UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
			{
				EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("TutorialUI_CharacterName"), UIString.instance.GetString("TutorialUI_CharacterDesc"), UIString.instance.GetString("TutorialUI_CharacterMore"), null, 0.785f);
			});
			PlayFabApiManager.instance.RequestCompleteOpenCanvasEvent(0);
		}

		contentItemPrefab.SetActive(false);
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (restore)
			return;

		// leftCharacter rightCharacter 있는지 확인 후 없으면
		// 인벤에 있는 캐릭터라도 가져와서 기본값으로 설정한다.
		string baseActorId = "";
		for (int i = (int)TeamManager.ePosition.Amount - 1; i >= 0; --i)
		{
			baseActorId = CharacterManager.instance.listTeamPositionId[i];
			if (string.IsNullOrEmpty(baseActorId) == false)
				break;
		}
		if (string.IsNullOrEmpty(baseActorId) && CharacterManager.instance.listCharacterData.Count > 0)
			baseActorId = CharacterManager.instance.listCharacterData[0].actorId;

		SetInfoCameraMode(true, baseActorId);
		if (string.IsNullOrEmpty(baseActorId) == false)
			ShowCanvasPlayerActor(baseActorId, null);
		_playerActor = BattleInstanceManager.instance.GetCachedCanvasPlayerActor(baseActorId);
		if (_playerActor != null)
			_playerActor.RefreshWingHide();

		MainCanvas.instance.OnEnterCharacterMenu(true);

		// grid
		RefreshGrid();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

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

		SetInfoCameraMode(false, "");
		MainCanvas.instance.OnEnterCharacterMenu(false);

		if (_playerActor != null)
		{
			_playerActor.RefreshWingHide();
			_playerActor.gameObject.SetActive(false);
			_playerActor = null;
		}

		// CharacterListCanvas의 Alarm은 수동으로 관리하니 여기서 꺼줘야한다.
		for (int i = 0; i < _listCharacterCanvasListItem.Count; ++i)
			_listCharacterCanvasListItem[i].ShowAlarm(false);
	}
	
	List<CharacterData> _listTempCharacterData = new List<CharacterData>();
	List<ActorTableData> _listTempTableData = new List<ActorTableData>();
	List<CharacterCanvasListItem> _listCharacterCanvasListItem = new List<CharacterCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listCharacterCanvasListItem.Count; ++i)
			_listCharacterCanvasListItem[i].gameObject.SetActive(false);
		_listCharacterCanvasListItem.Clear();

		_listTempCharacterData.Clear();
		_listTempTableData.Clear();
		separateLineTransform.gameObject.SetActive(false);
		int noGainCount = 0;
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_PlayerActorId)
				continue;
			if (CharacterManager.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId) == false)
			{
				++noGainCount;
				continue;
			}
			_listTempCharacterData.Add(CharacterManager.instance.GetCharacterData(TableDataManager.instance.actorTable.dataArray[i].actorId));
		}

		if (_listTempCharacterData.Count > 0)
		{
			_listTempCharacterData.Sort(delegate (CharacterData x, CharacterData y)
			{
				if (x.cachedActorTableData.grade > y.cachedActorTableData.grade) return -1;
				else if (x.cachedActorTableData.grade < y.cachedActorTableData.grade) return 1;
				if (x.transcend > y.transcend) return -1;
				else if (x.transcend < y.transcend) return 1;
				if (x.level > y.level) return -1;
				else if (x.level < y.level) return 1;
				if (x.cachedActorTableData.orderIndex > y.cachedActorTableData.orderIndex) return 1;
				else if (x.cachedActorTableData.orderIndex < y.cachedActorTableData.orderIndex) return -1;
				return 0;
			});

			for (int i = 0; i < _listTempCharacterData.Count; ++i)
			{
				CharacterCanvasListItem characterCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
				characterCanvasListItem.Initialize(_listTempCharacterData[i].actorId, _listTempCharacterData[i].level, _listTempCharacterData[i].transcend, false, 0, null, null, OnClickListItem);
				_listCharacterCanvasListItem.Add(characterCanvasListItem);
			}
		}
		RefreshAlarmList();

		if (noGainCount == 0)
			return;

		separateLineTransform.gameObject.SetActive(true);

		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_PlayerActorId)
				continue;
			if (CharacterManager.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId))
				continue;
			_listTempTableData.Add(TableDataManager.instance.actorTable.dataArray[i]);
		}

		_listTempTableData.Sort(delegate (ActorTableData x, ActorTableData y)
		{
			if (x.orderIndex > y.orderIndex) return 1;
			else if (x.orderIndex < y.orderIndex) return -1;
			return 0;
		});

		for (int i = 0; i < _listTempTableData.Count; ++i)
		{
			CharacterCanvasListItem characterCanvasListItem = _noGainContainer.GetCachedItem(contentItemPrefab, noGainContentRootRectTransform);
			characterCanvasListItem.Initialize(_listTempTableData[i].actorId, 0, 0, false, 0, null, null, OnClickListItem);
			characterCanvasListItem.ShowAlarm(false);
			_listCharacterCanvasListItem.Add(characterCanvasListItem);
		}
	}

	public string selectedActorId { get; private set; }
	public void OnClickListItem(string actorId)
	{
		selectedActorId = actorId;
		ShowCanvasPlayerActor(actorId, () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("CharacterInfoCanvas", null);
		});
	}

	// 현재 GridItem들에 대한 알람 갱신 처리. 알람 갱신 한곳으로 모은다.
	public void RefreshAlarmList()
	{
		for (int i = 0; i < _listCharacterCanvasListItem.Count; ++i)
		{
			_listCharacterCanvasListItem[i].ShowAlarm(false);
			if (i >= _listTempCharacterData.Count)
				continue;

			if (_listTempCharacterData[i].IsAlarmState())
				_listCharacterCanvasListItem[i].ShowAlarm(true);
		}
	}


	public void OnClickCanvasInfoButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("EventInfoCanvas", () =>
		{
			EventInfoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("TutorialUI_CharacterName"), UIString.instance.GetString("TutorialUI_CharacterDesc"), UIString.instance.GetString("TutorialUI_CharacterMore"), null, 0.785f, false);
		});
	}
}