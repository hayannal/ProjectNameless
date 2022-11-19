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
	public Transform maxLevelButtonTransform;
	public Text maxLevelText;
	public Text maxLevelUpCostText;
	public GameObject maxReachedTextObject;
	public GameObject blinkObject;

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

		SetInfoCameraMode(true, basePetId);
		if (string.IsNullOrEmpty(basePetId) == false)
			ShowCanvasPetActor(basePetId, null);
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
				if (x.cachedPetTableData.star > y.cachedPetTableData.star) return -1;
				else if (x.cachedPetTableData.star < y.cachedPetTableData.star) return 1;
				if (x.cachedPetTableData.orderIndex > y.cachedPetTableData.orderIndex) return 1;
				else if (x.cachedPetTableData.orderIndex < y.cachedPetTableData.orderIndex) return -1;
				return 0;
			});

			for (int i = 0; i < _listTempPetData.Count; ++i)
			{
				PetCanvasListItem petCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
				petCanvasListItem.Initialize(_listTempPetData[i].cachedPetTableData, _listTempPetData[i].count, _listTempPetData[i].mainStatusValue, OnClickListItem);
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
			PetCanvasListItem petCanvasListItem = _container.GetCachedItem(contentItemPrefab, noGainContentRootRectTransform);
			petCanvasListItem.Initialize(_listTempTableData[i], 0, _listTempTableData[i].accumulatedAtk, OnClickListItem);
			_listPetCanvasListItem.Add(petCanvasListItem);
		}
	}

	public void OnClickMaxLevelTextButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("PetUI_PetMaxLevelMore"), 300, maxLevelButtonTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickListItem(string petId)
	{
		ShowCanvasPetActor(petId, () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("PetInfoCanvas", null);
		});
	}
}