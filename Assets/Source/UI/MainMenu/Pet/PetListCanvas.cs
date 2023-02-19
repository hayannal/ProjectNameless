using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
using MEC;

public class PetListCanvas : PetShowCanvasBase
{
	public static PetListCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject todayHeartRootObject;
	public Text todayHeartRemainText;
	public Text todayHeartRemainCountText;
	public RectTransform alarmRootTransform;
	public Text todayHeartResetRemainTimeText;

	public Transform separateLineTransform;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;
	public RectTransform noGainContentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<PetCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	public class NoGainCustomItemContainer : CachedItemHave<PetCanvasListItem>
	{
	}
	NoGainCustomItemContainer _noGainContainer = new NoGainCustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	void OnEnable()
	{
		RefreshHeart();

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (restore)
			return;

		// leftCharacter rightCharacter 있는지 확인 후 없으면
		// 인벤에 있는 캐릭터라도 가져와서 기본값으로 설정한다.
		string basePetId = PetManager.instance.activePetId;
		if (string.IsNullOrEmpty(basePetId) && PetManager.instance.listPetData.Count > 0)
			basePetId = PetManager.instance.listPetData[0].petId;

		int count = 0;
		PetData petData = PetManager.instance.GetPetData(basePetId);
		if (petData != null)
			count = petData.count;

		SetInfoCameraMode(true, basePetId);
		if (string.IsNullOrEmpty(basePetId) == false)
			ShowCanvasPetActor(basePetId, count, null);
		_petActor = BattleInstanceManager.instance.GetCachedCanvasPetActor(basePetId);

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
		if (_petActor == null || _petActor.gameObject == null)
			return;

		SetInfoCameraMode(false, "");
		MainCanvas.instance.OnEnterCharacterMenu(false);

		if (_petActor != null)
		{
			_petActor.gameObject.SetActive(false);
			_petActor = null;
		}
		DisableAdditionalObjectList();
	}

	void Update()
	{
		UpdateAdditionalObject();
		UpdateHeartResetRemainTime();
	}


	public static int GetTodayRemainHeart()
	{
		int maxCount = BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetHeartCount");
		int remainCount = maxCount - PetManager.instance.dailyHeartCount;
		return remainCount;
	}

	public static bool CheckTodayHeart()
	{
		if (GetTodayRemainHeart() > 0)
			return true;
		return false;
	}

	void RefreshHeart()
	{
		int remainCount = GetTodayRemainHeart();
		todayHeartRemainCountText.text = remainCount.ToString();

		if (remainCount > 0)
			AlarmObject.Show(alarmRootTransform);
		else
			AlarmObject.Hide(alarmRootTransform);
	}

	List<PetData> _listTempPetData = new List<PetData>();
	List<PetTableData> _listTempTableData = new List<PetTableData>();
	List<PetCanvasListItem> _listPetCanvasListItem = new List<PetCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listPetCanvasListItem.Count; ++i)
			_listPetCanvasListItem[i].gameObject.SetActive(false);
		_listPetCanvasListItem.Clear();

		_listTempPetData.Clear();
		_listTempTableData.Clear();
		separateLineTransform.gameObject.SetActive(false);
		int noGainCount = 0;
		for (int i = 0; i < TableDataManager.instance.petTable.dataArray.Length; ++i)
		{
			if (PetManager.instance.ContainsPet(TableDataManager.instance.petTable.dataArray[i].petId) == false)
			{
				++noGainCount;
				continue;
			}
			_listTempPetData.Add(PetManager.instance.GetPetData(TableDataManager.instance.petTable.dataArray[i].petId));
		}

		if (_listTempPetData.Count > 0)
		{
			_listTempPetData.Sort(delegate (PetData x, PetData y)
			{
				if (x.heart > y.heart) return -1;
				else if (x.heart < y.heart) return 1;
				if (x.cachedPetTableData.star > y.cachedPetTableData.star) return -1;
				else if (x.cachedPetTableData.star < y.cachedPetTableData.star) return 1;
				if (x.cachedPetTableData.orderIndex > y.cachedPetTableData.orderIndex) return 1;
				else if (x.cachedPetTableData.orderIndex < y.cachedPetTableData.orderIndex) return -1;
				return 0;
			});

			for (int i = 0; i < _listTempPetData.Count; ++i)
			{
				PetCanvasListItem petCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
				petCanvasListItem.Initialize(_listTempPetData[i].cachedPetTableData, _listTempPetData[i].count, _listTempPetData[i].step, _listTempPetData[i].heart, _listTempPetData[i].mainStatusValue, OnClickListItem);
				_listPetCanvasListItem.Add(petCanvasListItem);
			}
		}

		if (noGainCount == 0)
			return;

		separateLineTransform.gameObject.SetActive(true);

		for (int i = 0; i < TableDataManager.instance.petTable.dataArray.Length; ++i)
		{
			if (PetManager.instance.ContainsPet(TableDataManager.instance.petTable.dataArray[i].petId))
				continue;
			_listTempTableData.Add(TableDataManager.instance.petTable.dataArray[i]);
		}

		_listTempTableData.Sort(delegate (PetTableData x, PetTableData y)
		{
			if (x.orderIndex > y.orderIndex) return 1;
			else if (x.orderIndex < y.orderIndex) return -1;
			return 0;
		});

		for (int i = 0; i < _listTempTableData.Count; ++i)
		{
			PetCanvasListItem petCanvasListItem = _noGainContainer.GetCachedItem(contentItemPrefab, noGainContentRootRectTransform);
			petCanvasListItem.Initialize(_listTempTableData[i], 0, 0, 0, _listTempTableData[i].accumulatedAtk, OnClickListItem);
			_listPetCanvasListItem.Add(petCanvasListItem);
		}
	}

	public string selectedPetId { get; private set; }
	public void OnClickListItem(string petId, int count)
	{
		selectedPetId = petId;
		ShowCanvasPetActor(petId, count, () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("PetInfoCanvas", null);
		});
	}


	int _lastRemainTimeSecond = -1;
	void UpdateHeartResetRemainTime()
	{
		if (PetManager.instance.dailyHeartCount == 0)
		{
			todayHeartResetRemainTimeText.text = "";
			_lastRemainTimeSecond = -1;
			return;
		}

		if (ServerTime.UtcNow < PlayerData.instance.dayRefreshTime)
		{
			System.TimeSpan remainTime = PlayerData.instance.dayRefreshTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				todayHeartResetRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			todayHeartResetRemainTimeText.text = "";
		}
	}
}