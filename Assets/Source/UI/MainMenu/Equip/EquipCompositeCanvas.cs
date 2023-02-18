using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;
using MEC;

public class EquipCompositeCanvas : EquipShowCanvasBase
{
	public static EquipCompositeCanvas instance;

	public enum eCompositeMaterialType
	{
		SameEquip = 1,
		SameEquipType = 2,
		AnyEquipType = 3,
	}

	public Sprite[] equipTypeSpriteList;
	public Sprite equipIconSprite;

	public CurrencySmallInfo currencySmallInfo;

	public Button backKeyButton;
	public GameObject inputLockObject;
	public GameObject standbyEffectPrefab;
	public GameObject successEffectPrefab;

	public Text messageText;
	public EquipCanvasListItem selectedEquipCanvasListItem;

	public GameObject material1RootObject;
	public GameObject material2RootObject;
	public GameObject resultRootObject;
	public EquipCanvasListItem material1EquipCanvasListItem;
	public EquipCanvasListItem material2EquipCanvasListItem;
	public EquipCanvasListItem resultEquipCanvasListItem;

	public EquipListStatusInfo materialSmallStatusInfo;

	public Image compositeButtonImage;
	public Text compositeButtonText;
	public RectTransform autoAlarmRootTransform;

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

		if (restore)
			return;

		SetInfoCameraMode(true);

		ResetSelect();
		EquipInfoGround.instance.ResetEquipObject();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

		SetInfoCameraMode(false);

		materialSmallStatusInfo.gameObject.SetActive(false);
		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].ShowAlarm(false);
	}

	float _materialSmallStatusInfoShowRemainTime;
	void Update()
	{
		if (_materialSmallStatusInfoShowRemainTime > 0.0f)
		{
			_materialSmallStatusInfoShowRemainTime -= Time.deltaTime;
			if (_materialSmallStatusInfoShowRemainTime <= 0.0f)
			{
				_materialSmallStatusInfoShowRemainTime = 0.0f;
				materialSmallStatusInfo.gameObject.SetActive(false);
			}
		}
	}


	List<EquipCanvasListItem> _listEquipCanvasListItem = new List<EquipCanvasListItem>();
	List<EquipData> _listCurrentEquipData = new List<EquipData>();
	bool _availableAutoComposite = false;
	void RefreshGrid()
	{
		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
		{
			_listEquipCanvasListItem[i].ShowAlarm(false);
			_listEquipCanvasListItem[i].gameObject.SetActive(false);
		}
		_listEquipCanvasListItem.Clear();

		_listCurrentEquipData.Clear();
		List<EquipData> listEquipData = null;
		for (int i = 0; i < (int)EquipManager.eEquipSlotType.Amount; ++i)
		{
			listEquipData = EquipManager.instance.GetEquipListByType((EquipManager.eEquipSlotType)i);
			for (int j = 0; j < listEquipData.Count; ++j)
			{
				//if (EquipManager.instance.IsEquipped(listEquipData[j]))
				//	continue;
				//if (listEquipData[j].isLock)
				//	continue;
				_listCurrentEquipData.Add(listEquipData[j]);
			}
		}
		emptyEquipObject.SetActive(_listCurrentEquipData.Count == 0);

		_listCurrentEquipData.Sort(delegate (EquipData x, EquipData y)
		{
			//if (x.newEquip && y.newEquip == false) return -1;
			//else if (x.newEquip == false && y.newEquip) return 1;
			if (x.cachedEquipTableData != null && y.cachedEquipTableData != null)
			{
				if (x.cachedEquipTableData.grade > y.cachedEquipTableData.grade) return -1;
				else if (x.cachedEquipTableData.grade < y.cachedEquipTableData.grade) return 1;
				if (x.cachedEquipTableData.rarity > y.cachedEquipTableData.rarity) return -1;
				else if (x.cachedEquipTableData.rarity < y.cachedEquipTableData.rarity) return 1;
				if (x.cachedEquipTableData.equipType < y.cachedEquipTableData.equipType) return -1;
				else if (x.cachedEquipTableData.equipType > y.cachedEquipTableData.equipType) return 1;
				if (x.enhanceLevel > y.enhanceLevel) return -1;
				else if (x.enhanceLevel < y.enhanceLevel) return 1;
				if (x.mainStatusValue > y.mainStatusValue) return -1;
				else if (x.mainStatusValue < y.mainStatusValue) return 1;
			}
			return 0;
		});

		_availableAutoComposite = false;
		for (int i = 0; i < _listCurrentEquipData.Count; ++i)
		{
			EquipCanvasListItem equipCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipCanvasListItem.Initialize(_listCurrentEquipData[i], OnClickListItem);
			if (EquipManager.instance.IsEquipped(_listCurrentEquipData[i]))
				equipCanvasListItem.equippedText.gameObject.SetActive(true);
			#region Composite Usable
			if (EquipManager.instance.IsCompositeAvailable(_listCurrentEquipData[i], _listCurrentEquipData))
			{
				equipCanvasListItem.ShowAlarm(true);
				if (_listCurrentEquipData[i].cachedEquipTableData.grade <= 2)
					_availableAutoComposite = true;
			}
			#endregion
			_listEquipCanvasListItem.Add(equipCanvasListItem);
		}

		if (_availableAutoComposite)
			AlarmObject.Show(autoAlarmRootTransform);
		else
			AlarmObject.Hide(autoAlarmRootTransform);
	}

	void RefreshMaterialGrid()
	{
		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
		{
			if (_listEquipCanvasListItem[i].equipData == _selectedEquipData)
				continue;
			if (EquipManager.instance.IsValidMaterial(_selectedEquipData, _listEquipCanvasListItem[i].equipData))
				continue;
			_listEquipCanvasListItem[i].blackObject.SetActive(true);
		}
	}

	bool _selectMaterialMode;
	EquipData _selectedEquipData;
	public void OnClickListItem(EquipData equipData)
	{
		// 선택한 메인을 다시 눌렀을땐 취소
		if (_selectedEquipData != null && _selectedEquipData == equipData)
		{
			ResetSelect();
			return;
		}

		// 재료 선택모드
		if (_selectMaterialMode && equipData != null)
		{
			OnMultiSelectListItem(equipData);
			return;
		}

		// 최대 강화상태에 도달한 장비면
		if (_selectedEquipData == null && equipData != null && EquipManager.instance.IsMaxGradeEnhance(equipData))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_MaxEnhanceReached"), 2.0f);
			return;
		}

		_selectedEquipData = equipData;

		string selectedUniqueId = "";
		if (_selectedEquipData != null)
			selectedUniqueId = _selectedEquipData.uniqueId;

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].ShowSelectObject(_listEquipCanvasListItem[i].equipData.uniqueId == selectedUniqueId);

		if (_selectedEquipData == null)
			return;

		ShowSmallEquipInfo(equipData);
		//RefreshDiffStatusInfo(equipData);

		selectedEquipCanvasListItem.Initialize(equipData, null);
		if (EquipManager.instance.IsEquipped(equipData))
			selectedEquipCanvasListItem.equippedText.gameObject.SetActive(true);
		selectedEquipCanvasListItem.gameObject.SetActive(true);

		messageText.SetLocalizedText(UIString.instance.GetString("EquipUI_SelectMaterial"));

		if (EquipManager.GetEnhanceLevelMaxByGrade(equipData.cachedEquipTableData.grade) == equipData.enhanceLevel)
		{
			EquipTableData nextGradeEquipTableData = EquipManager.GetNextGradeEquipTableData(equipData.cachedEquipTableData);
			if (nextGradeEquipTableData != null)
				resultEquipCanvasListItem.Initialize(nextGradeEquipTableData);
		}
		else
			resultEquipCanvasListItem.Initialize(equipData.cachedEquipTableData, equipData.enhanceLevel + 1);
		resultEquipCanvasListItem.lockImage.gameObject.SetActive(selectedEquipCanvasListItem.lockImage.gameObject.activeSelf);
		resultRootObject.SetActive(true);

		// 현재 선택된 장비에 따라 재료로 선택할 수 있는 수량과 조건이 정해져있으니 그거대로 그리드에 표시해야한다.
		_selectMaterialMode = true;
		RefreshMaterialGrid();

		EquipCompositeTableData equipCompositeTableData = TableDataManager.instance.FindEquipCompositeTableData(_selectedEquipData.cachedEquipTableData.rarity, _selectedEquipData.cachedEquipTableData.grade, _selectedEquipData.enhanceLevel);
		if (equipCompositeTableData == null)
			return;

		_selectableCountMax = equipCompositeTableData.count;
		RefreshMaterialSlot(_selectedEquipData, material1EquipCanvasListItem, equipCompositeTableData);
		RefreshMaterialSlot(_selectedEquipData, material2EquipCanvasListItem, equipCompositeTableData);
		material1RootObject.SetActive(_selectableCountMax >= 1);
		material2RootObject.SetActive(_selectableCountMax >= 2);
	}

	void RefreshMaterialSlot(EquipData selectedEquipData, EquipCanvasListItem equipCanvasListItem, EquipCompositeTableData equipCompositeTableData)
	{
		switch ((eCompositeMaterialType)equipCompositeTableData.materialType)
		{
			case eCompositeMaterialType.SameEquip:
				equipCanvasListItem.Initialize(selectedEquipData.cachedEquipTableData);
				break;
			case eCompositeMaterialType.SameEquipType:
				equipCanvasListItem.Initialize(selectedEquipData.cachedEquipTableData);
				equipCanvasListItem.equipIconImage.sprite = equipTypeSpriteList[selectedEquipData.cachedEquipTableData.equipType];
				break;
			case eCompositeMaterialType.AnyEquipType:
				equipCanvasListItem.Initialize(selectedEquipData.cachedEquipTableData);
				equipCanvasListItem.equipIconImage.sprite = equipIconSprite;
				break;
		}
		equipCanvasListItem.InitializeGrade(equipCompositeTableData.materialGrade, false);
		EquipCanvasListItem.RefreshRarity(equipCompositeTableData.materialRarity, equipCanvasListItem.rarityText, equipCanvasListItem.rarityGradient);
		equipCanvasListItem.gameObject.SetActive(true);
		equipCanvasListItem.blackObject.SetActive(true);
	}

	void ResetSelect()
	{
		messageText.SetLocalizedText(UIString.instance.GetString("EquipUI_SelectComposite"));
		selectedEquipCanvasListItem.gameObject.SetActive(false);
		material1RootObject.SetActive(false);
		material2RootObject.SetActive(false);
		resultRootObject.SetActive(false);
		OnClickListItem(null);

		_selectMaterialMode = false;
		_listMultiSelectUniqueId.Clear();
		_listMultiSelectEquipData.Clear();
		RefreshGrid();

		bool disablePrice = true;
		compositeButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		compositeButtonText.color = !disablePrice ? Color.white : ColorUtil.halfGray;
	}

	void ShowSmallEquipInfo(EquipData equipData)
	{
		if (equipData == null)
			return;

		materialSmallStatusInfo.RefreshInfo(equipData, false);
		materialSmallStatusInfo.detailShowButton.gameObject.SetActive(false);
		materialSmallStatusInfo.lockButton.gameObject.SetActive(false);
		materialSmallStatusInfo.unlockButton.gameObject.SetActive(false);
		materialSmallStatusInfo.equipButtonObject.gameObject.SetActive(false);
		materialSmallStatusInfo.unequipButtonObject.gameObject.SetActive(false);
		materialSmallStatusInfo.gameObject.SetActive(false);
		materialSmallStatusInfo.gameObject.SetActive(true);
		_materialSmallStatusInfoShowRemainTime = 2.0f;
	}

	List<string> _listMultiSelectUniqueId = new List<string>();
	List<EquipData> _listMultiSelectEquipData = new List<EquipData>();
	public List<EquipData> listMultiSelectEquipData { get { return _listMultiSelectEquipData; } }
	int _selectableCountMax;
	public void OnMultiSelectListItem(EquipData equipData)
	{
		bool contains = _listMultiSelectUniqueId.Contains(equipData.uniqueId);
		if (contains == false && _listMultiSelectEquipData.Count >= _selectableCountMax)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_CannotSelectMore"), 1.5f);
			return;
		}

		// 재료조건에 안맞으면 어두운 색으로 되어있을테니 그냥 리턴해도 된다.
		if (EquipManager.instance.IsValidMaterial(_selectedEquipData, equipData) == false)
			return;

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
		{
			if (_listEquipCanvasListItem[i].equipData.uniqueId == equipData.uniqueId)
				_listEquipCanvasListItem[i].ShowSelectObject(!contains);
		}
		if (contains)
		{
			_listMultiSelectUniqueId.Remove(equipData.uniqueId);
			_listMultiSelectEquipData.Remove(equipData);
		}
		else
		{
			_listMultiSelectUniqueId.Add(equipData.uniqueId);
			_listMultiSelectEquipData.Add(equipData);

			ShowSmallEquipInfo(equipData);
		}

		//RefreshCountText();
		OnMultiSelectMaterial();
	}

	void OnMultiSelectMaterial()
	{
		bool disablePrice = (_selectableCountMax != _listMultiSelectEquipData.Count);
		compositeButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		compositeButtonText.color = !disablePrice ? Color.white : ColorUtil.halfGray;

		EquipCompositeTableData equipCompositeTableData = TableDataManager.instance.FindEquipCompositeTableData(_selectedEquipData.cachedEquipTableData.rarity, _selectedEquipData.cachedEquipTableData.grade, _selectedEquipData.enhanceLevel);
		if (equipCompositeTableData == null)
			return;

		if (_listMultiSelectEquipData.Count >= 1)
			material1EquipCanvasListItem.Initialize(_listMultiSelectEquipData[0], null);
		else
			RefreshMaterialSlot(_selectedEquipData, material1EquipCanvasListItem, equipCompositeTableData);

		if (_listMultiSelectEquipData.Count >= 2)
			material2EquipCanvasListItem.Initialize(_listMultiSelectEquipData[1], null);
		else
			RefreshMaterialSlot(_selectedEquipData, material2EquipCanvasListItem, equipCompositeTableData);
	}

	public void OnClickSelectedEquipButton()
	{
		if (selectedEquipCanvasListItem.gameObject.activeSelf)
		{
			ResetSelect();
			return;
		}
	}

	//public void OnClickMaterial1EquipButton()
	//{
	//	if (material1EquipCanvasListItem.gameObject.activeSelf)
	//	{
	//		ResetSelect();
	//		return;
	//	}
	//}
	//public void OnClickMaterial2EquipButton()
	//{
	//}

	List<ObscuredString> _listNewEquipId = new List<ObscuredString>();
	public void OnClickAutoCompositeButton()
	{
		if (_availableAutoComposite == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_NoComposite"), 2.0f);
			return;
		}

		_listNewEquipId.Clear();
		_listMultiSelectEquipData.Clear();
		EquipManager.instance.CollectAutoComposite(_listNewEquipId, _listMultiSelectEquipData);

		_prevCombatValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		PlayFabApiManager.instance.RequestAutoCompositeEquip(_listNewEquipId, _listMultiSelectEquipData, OnRecvAutoResult);
	}

	void OnRecvAutoResult(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = EquipManager.instance.OnRecvItemGrantResult(itemGrantString, 0);
		if (listItemInstance == null)
			return;

		Timing.RunCoroutine(CompositeProcess(listItemInstance));
	}

	public void OnClickCompositeButton()
	{
		if (_selectedEquipData == null)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_SelectComposite"), 2.0f);
			return;
		}

		if (_listMultiSelectEquipData.Count < _selectableCountMax)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("EquipUI_SelectMaterial"), 2.0f);
			return;
		}

		// EnhanceLevel이 올라가는건지 새 아이템이 되는건지에 따라 인자가 달라져야한다.
		// 그리고 장착된 아이템이냐 아니냐에 따라서도 인자가 달라져야한다.
		EquipData enhanceEquipData = null;
		_listNewEquipId.Clear();
		if (EquipManager.GetEnhanceLevelMaxByGrade(_selectedEquipData.cachedEquipTableData.grade) == _selectedEquipData.enhanceLevel)
		{
			_listMultiSelectEquipData.Add(_selectedEquipData);

			EquipTableData nextGradeEquipTableData = EquipManager.GetNextGradeEquipTableData(_selectedEquipData.cachedEquipTableData);
			if (nextGradeEquipTableData != null)
				_listNewEquipId.Add(nextGradeEquipTableData.equipId);
		}
		else
			enhanceEquipData = _selectedEquipData;

		_equippedComposite = EquipManager.instance.IsEquipped(_selectedEquipData);
		_prevCombatValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		PlayFabApiManager.instance.RequestCompositeEquip(enhanceEquipData, _equippedComposite, _selectedEquipData.cachedEquipTableData.equipType,
			_listNewEquipId, _listMultiSelectEquipData, OnRecvResult);
	}

	bool _equippedComposite = false;
	void OnRecvResult(string itemGrantString)
	{
		// Auto때와 달리 itemGrant가 없을 수 있으니
		List<ItemInstance> listItemInstance = null;
		if (itemGrantString == "")
			listItemInstance = new List<ItemInstance>();
		else
			listItemInstance = EquipManager.instance.OnRecvItemGrantResult(itemGrantString, 0);
		if (listItemInstance == null)
			return;

		//GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.EquipGacha, _count);

		Timing.RunCoroutine(CompositeProcess(listItemInstance));
	}

	bool _processed = false;
	float _prevCombatValue = 0.0f;
	IEnumerator<float> CompositeProcess(List<ItemInstance> listItemInstance)
	{
		_processed = true;

		inputLockObject.SetActive(true);
		backKeyButton.interactable = false;

		// 새로운 아이템으로 변환인지 아니면 그냥 enhanceLevel증가인지 확인
		bool enhanceLevel = false;
		if (_listNewEquipId.Count == 0 && listItemInstance.Count == 0 && _selectedEquipData != null)
			enhanceLevel = true;

		// 인풋 막은 상태에서 3D 오브젝트부터 제단에 설치
		if (_selectedEquipData != null)
		{
			EquipInfoGround.instance.CreateEquipObject(_selectedEquipData, true);
			while (EquipInfoGround.instance.IsShowEquippedObject() == false)
				yield return Timing.WaitForOneFrame;
			yield return Timing.WaitForSeconds(0.5f);
		}

		EquipInfoGround.instance.ScaleDownGradeParticle(true);

		// 선이펙트
		BattleInstanceManager.instance.GetCachedObject(standbyEffectPrefab, rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(1.75f);

		BattleInstanceManager.instance.GetCachedObject(successEffectPrefab, rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(1.5f);

		EquipInfoGround.instance.ScaleDownGradeParticle(false);

		// 결과창 보이는 타이밍에 하단 그리드도 다시 갱신하는게 좋을거 같다.
		RefreshGrid();

		// 결과창
		UIInstanceManager.instance.ShowCanvasAsync("EquipCompositeResultCanvas", () =>
		{
			if (enhanceLevel && _selectedEquipData != null)
				EquipCompositeResultCanvas.instance.RefreshResult(_selectedEquipData, OnCloseResultCanvas);
			else
				EquipCompositeResultCanvas.instance.RefreshResult(listItemInstance, OnCloseResultCanvas);
		});

		if (_equippedComposite && _selectedEquipData != null)
		{
			// 밖에 있는 시공간 제단을 업데이트 해줘야한다.
			int positionIndex = _selectedEquipData.cachedEquipTableData.equipType;
			EquipGround.instance.equipAltarList[positionIndex].RefreshEquipObject();

			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(_prevCombatValue, nextValue);
			});
		}

		inputLockObject.SetActive(false);
		backKeyButton.interactable = true;

		_processed = false;
	}

	void OnCloseResultCanvas()
	{
		// 3D 오브젝트는 이때 비활성화 시켜본다.
		ResetSelect();
		EquipInfoGround.instance.ResetEquipObject();
		_equippedComposite = false;
	}
}