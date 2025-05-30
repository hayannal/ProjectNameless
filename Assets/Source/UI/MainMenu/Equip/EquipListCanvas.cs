﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipListCanvas : EquipShowCanvasBase
{
	public static EquipListCanvas instance;

	public EquipTypeButton[] equipTypeButtonList;
	public EquipListStatusInfo diffStatusInfo;
	public EquipListStatusInfo equippedStatusInfo;
	public GameObject reopenButtonObject;
	public GameObject reopenEquippedStatusInfoTextObject;
	bool _closeEquippedStatusInfoByUser;
	public GameObject detailButtonObject;

	public Text countText;
	public GameObject emptyEquipObject;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<EquipCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

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

		// 여기서 equippedStatusInfo 하이드 시키지 않는거처럼 reopenButtonObject도 그냥 놔둬야한다. 이래야 돋보기 보고 나올때 마지막 상태가 유지된다.
		//reopenButtonObject.SetActive(false);
		reopenEquippedStatusInfoTextObject.gameObject.SetActive(false);

		// CharacterListCanvas 와 비슷한 구조다.
		if (restore)
			return;

		SetInfoCameraMode(true);

		// 캐릭터리스트와 달리 장비종류별로 Grid가 달라질 수 있어서 외부에서 RefreshInfo 함수을 통해서 처리하기로 한다.
		//RefreshGrid(true);
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		if (StackCanvas.Pop(gameObject))
			return;

		// CharacterListCanvas 와 비슷한 구조다.
		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

		SetInfoCameraMode(false);

		// 장비의 new표시를 전부 사라지게 하는 곳은 9탭 나갈때다.
		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].ShowAlarm(false);
		EquipManager.instance.ResetNewEquip();
	}

	public void RefreshInfo(int positionIndex)
	{
		_forceRefresh = true;
		OnSelectEquipType(positionIndex);
		_forceRefresh = false;

		// 외부에서 제단 인디케이터를 클릭해서 들어온거니 장착이 되어있다면 자동으로 정보창을 보여준다.
		EquipData equipData = EquipManager.instance.GetEquippedDataByType((EquipManager.eEquipSlotType)positionIndex);
		if (equipData != null)
			RefreshEquippedStatusInfo(equipData);
		_closeEquippedStatusInfoByUser = false;
	}

	#region EquipTypeButton
	bool _forceRefresh = false;
	public void OnSelectEquipType(int positionIndex)
	{
		if (_forceRefresh == false &&_currentEquipType == (EquipManager.eEquipSlotType)positionIndex)
			return;

		for (int i = 0; i < equipTypeButtonList.Length; ++i)
			equipTypeButtonList[i].selected = (equipTypeButtonList[i].positionIndex == positionIndex);

		_currentEquipType = (EquipManager.eEquipSlotType)positionIndex;
		RefreshGrid(true, true);
		RefreshEquippedObject();

		// 탭바뀔땐 비교창 하이드
		diffStatusInfo.gameObject.SetActive(false);
		// 탭이 바뀔때 장착된 아이템이 있고 유저가 닫기버튼을 직접 누르지 않은 상태라면
		if (_closeEquippedStatusInfoByUser == false)
		{
			EquipData equipData = EquipManager.instance.GetEquippedDataByType((EquipManager.eEquipSlotType)positionIndex);
			if (equipData != null)
				RefreshEquippedStatusInfo(equipData);
			else
			{
				equippedStatusInfo.gameObject.SetActive(false);
				reopenButtonObject.SetActive(false);
			}
		}
		else
		{
			EquipData equipData = EquipManager.instance.GetEquippedDataByType((EquipManager.eEquipSlotType)positionIndex);
			reopenButtonObject.SetActive(equipData != null);
		}
	}
	#endregion

	void RefreshEquippedObject(bool playEquipAnimation = false)
	{
		// 빠르게 탭을 바꾸다보면 로딩중에 취소되고 다음 템을 로드할수도 있을거다.
		EquipData equipData = EquipManager.instance.GetEquippedDataByType(_currentEquipType);
		if (equipData == null)
		{
			EquipInfoGround.instance.ResetEquipObject();
			return;
		}

		EquipInfoGround.instance.CreateEquipObject(equipData, playEquipAnimation);
	}

	List<EquipCanvasListItem> _listEquipCanvasListItem = new List<EquipCanvasListItem>();
	List<EquipData> _listCurrentEquipData = new List<EquipData>();
	EquipManager.eEquipSlotType _currentEquipType;
	public EquipManager.eEquipSlotType currentEquipType { get { return _currentEquipType; } }
	public void RefreshGrid(bool refreshInventory, bool resetSelected)
	{
		// 강화나 옵션탭에서 장비를 업하면서 재료로 소모했다면 인벤토리는 리프레쉬 하되 현재 선택된건 유지해야한다.
		// 이럴때 대비해서 몇가지 예외처리 해둔다.
		if (refreshInventory && resetSelected == false)
		{
			if (equippedStatusInfo.gameObject.activeSelf)
				equippedStatusInfo.RefreshStatus();
		}

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
		{
			_listEquipCanvasListItem[i].ShowAlarm(false);
			_listEquipCanvasListItem[i].gameObject.SetActive(false);
		}
		_listEquipCanvasListItem.Clear();

		if (refreshInventory)
		{
			_listCurrentEquipData.Clear();
			List<EquipData> listEquipData = EquipManager.instance.GetEquipListByType(_currentEquipType);
			for (int i = 0; i < listEquipData.Count; ++i)
			{
				if (EquipManager.instance.IsEquipped(listEquipData[i]))
					continue;
				_listCurrentEquipData.Add(listEquipData[i]);
			}

			// 장착된 템을 인벤에서 못찾는다면 아마도 강화이전을 통해 장착된걸 재료로 써버려서 삭제가 되었을 경우일거다.
			// 이럴땐 새로 장착된 템이 있는지 보고 있으면 갱신하고 없으면 닫기로 한다.
			if (equippedStatusInfo.gameObject.activeSelf)
			{
				EquipData equipData = EquipManager.instance.GetEquippedDataByType(_currentEquipType);
				if (equipData != null)
					RefreshEquippedStatusInfo(equipData);
				else
				{
					equippedStatusInfo.gameObject.SetActive(false);
					reopenButtonObject.SetActive(false);
				}
			}

			// 인벤토리를 리프레쉬 하는데 열려있는 정보창의 equipData가 삭제되었다면 템을 삭제한 후 리프레쉬 한걸거다. 이땐 정보창을 강제로 닫아준다.
			if (diffStatusInfo.gameObject.activeSelf && _listCurrentEquipData.Contains(diffStatusInfo.equipData) == false)
			{
				diffStatusInfo.gameObject.SetActive(false);
				RefreshEquippedObject();
			}
			if (_selectedEquipData != null && _listCurrentEquipData.Contains(_selectedEquipData) == false)
				_selectedEquipData = null;

			countText.text = string.Format(EquipManager.instance.IsInventoryVisualMax() ? "<color=#FF2200>{0}</color> / {1}" : "{0} / {1}", EquipManager.instance.inventoryItemCount, EquipManager.InventoryVisualMax);
		}
		if (_listCurrentEquipData.Count == 0)
		{
			emptyEquipObject.SetActive(true);
			return;
		}
		emptyEquipObject.SetActive(false);

		_listCurrentEquipData.Sort(delegate (EquipData x, EquipData y)
		{
			if (x.newEquip && y.newEquip == false) return -1;
			else if (x.newEquip == false && y.newEquip) return 1;
			if (x.cachedEquipTableData != null && y.cachedEquipTableData != null)
			{
				if (x.cachedEquipLevelTableData.grade > y.cachedEquipLevelTableData.grade) return -1;
				else if (x.cachedEquipLevelTableData.grade < y.cachedEquipLevelTableData.grade) return 1;
				if (x.cachedEquipTableData.rarity > y.cachedEquipTableData.rarity) return -1;
				else if (x.cachedEquipTableData.rarity < y.cachedEquipTableData.rarity) return 1;
				if (x.enhanceLevel > y.enhanceLevel) return -1;
				else if (x.enhanceLevel < y.enhanceLevel) return 1;
				if (x.mainStatusValue > y.mainStatusValue) return -1;
				else if (x.mainStatusValue < y.mainStatusValue) return 1;
				int stringCompare = string.Compare(x.cachedEquipTableData.equipGroup, y.cachedEquipTableData.equipGroup);
				if (stringCompare < 0) return -1;
				else if (stringCompare > 0) return 1;
			}
			return 0;
		});

		for (int i = 0; i < _listCurrentEquipData.Count; ++i)
		{
			EquipCanvasListItem equipCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipCanvasListItem.Initialize(_listCurrentEquipData[i], OnClickListItem);
			if (_listCurrentEquipData[i].newEquip) equipCanvasListItem.ShowAlarm(true);
			_listEquipCanvasListItem.Add(equipCanvasListItem);
		}

		if (resetSelected)
			OnClickListItem(null);
		else
			OnClickListItem(_selectedEquipData);
	}

	EquipData _selectedEquipData;
	public void OnClickListItem(EquipData equipData)
	{
		_selectedEquipData = equipData;

		string selectedUniqueId = "";
		if (_selectedEquipData != null)
			selectedUniqueId = _selectedEquipData.uniqueId;

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].ShowSelectObject(_listEquipCanvasListItem[i].equipData.uniqueId == selectedUniqueId);

		if (_selectedEquipData == null)
			return;

		RefreshDiffStatusInfo(equipData);
	}

	void RefreshDiffStatusInfo(EquipData equipData)
	{
		if (diffStatusInfo.gameObject.activeSelf)
			diffStatusInfo.gameObject.SetActive(false);
		diffStatusInfo.RefreshInfo(equipData, false);
		diffStatusInfo.gameObject.SetActive(true);
		detailButtonObject.gameObject.SetActive(false);
	}

	void RefreshEquippedStatusInfo(EquipData equipData)
	{
		if (equippedStatusInfo.gameObject.activeSelf)
			equippedStatusInfo.gameObject.SetActive(false);
		equippedStatusInfo.RefreshInfo(equipData, true);
		equippedStatusInfo.gameObject.SetActive(true);
		reopenButtonObject.SetActive(false);
	}

	public void OnCloseDiffStatusInfo()
	{
		OnClickListItem(null);

		EquipData equipData = EquipManager.instance.GetEquippedDataByType(_currentEquipType);
		if (equipData == null)
			return;
		if (EquipInfoGround.instance.IsShowEquippedObject() == false)
			return;
		detailButtonObject.gameObject.SetActive(true);
	}

	public void OnCloseEquippedStatusInfo()
	{
		reopenButtonObject.SetActive(true);
		reopenEquippedStatusInfoTextObject.SetActive(true);
		_closeEquippedStatusInfoByUser = true;
	}

	public void RefreshSelectedItem()
	{
		if (_selectedEquipData == null)
			return;

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
		{
			if (_listEquipCanvasListItem[i].equipData.uniqueId == _selectedEquipData.uniqueId)
				_listEquipCanvasListItem[i].RefreshStatus();
		}
	}

	public void OnEquip(EquipData equipData)
	{
		if (_selectedEquipData != equipData)
		{
			// 선택하지 않은걸 장착할 수 있나?
			return;
		}

		// 장착시 내려오는 애니 적용
		RefreshEquippedObject(true);
		RefreshGrid(true, true);

		// 모든 비교창을 닫는다.
		diffStatusInfo.gameObject.SetActive(false);
		equippedStatusInfo.gameObject.SetActive(false);
		reopenButtonObject.SetActive(true);

		// 밖에 있는 시공간 제단을 업데이트 해줘야한다.
		int positionIndex = equipData.cachedEquipTableData.equipType;
		EquipGround.instance.equipAltarList[positionIndex].RefreshEquipObject();

		// 신규 장비를 장착하면 탭에 있던 new표시도 리프레쉬
		equipData.newEquip = false;
		equipTypeButtonList[positionIndex].RefreshAlarmObject();
		equipTypeButtonList[positionIndex].RefreshEquipGrade();
	}

	public void OnUnequip(EquipData equipData)
	{
		RefreshGrid(true, true);
		RefreshEquippedObject();

		// 모든 비교창을 닫는다.
		diffStatusInfo.gameObject.SetActive(false);
		equippedStatusInfo.gameObject.SetActive(false);
		reopenButtonObject.SetActive(false);

		// 밖에 있는 시공간 제단을 업데이트 해줘야한다.
		int positionIndex = equipData.cachedEquipTableData.equipType;
		EquipGround.instance.equipAltarList[positionIndex].RefreshEquipObject();

		// 장비 해제하면 equipTypeButton에도 알려야한다.
		equipTypeButtonList[positionIndex].RefreshEquipGrade();
	}

	

	public void OnClickEquippedInfoButton()
	{
		EquipData equipData = EquipManager.instance.GetEquippedDataByType(_currentEquipType);
		if (equipData == null)
			return;

		if (equippedStatusInfo.gameObject.activeSelf)
			return;

		reopenButtonObject.SetActive(false);
		reopenEquippedStatusInfoTextObject.gameObject.SetActive(false);
		RefreshEquippedStatusInfo(equipData);
		SoundManager.instance.PlaySFX("GridOn");

		// 장비 오브젝트를 탭해서 켜면 플래그를 초기화시킨다.
		_closeEquippedStatusInfoByUser = false;
	}

	public void OnClickDetailButton()
	{
		// 현재 보여지고 있는 장착된 템이라서 카메라만 옮겨주면 될거다.
		UIInstanceManager.instance.ShowCanvasAsync("EquipInfoDetailCanvas", null);
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
		//StackCanvas.Back();
	}

	public void OnClickHomeButton()
	{
		// 현재 상태에 따라
		LobbyCanvas.Home();
	}
}