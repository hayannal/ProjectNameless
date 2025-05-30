﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;
using ActorStatusDefine;

public class EquipManager : MonoBehaviour
{
	public static EquipManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("EquipManager")).AddComponent<EquipManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static EquipManager _instance = null;

	public enum eEquipSlotType
	{
		Axe,
		Dagger,
		Bow,
		Staff,
		Hammer,
		Sword,
		Gun,
		Shield,
		TwoHanded,

		Amount,
	}

	public const int InventoryVisualMax = 500;

	public static int GetEnhanceLevelMaxByGrade(int grade)
	{
		EquipGradeTableData equipGradeTableData = TableDataManager.instance.FindEquipGradeTableData(grade);
		if (equipGradeTableData == null)
			return 0;
		return equipGradeTableData.compositeLevelMax;
	}

	public static EquipLevelTableData GetNextGradeEquipLevelTableData(EquipLevelTableData equipLevelTableData)
	{
		EquipLevelTableData nextGradeEquipLevelTableData = TableDataManager.instance.FindEquipLevelTableDataByGrade(equipLevelTableData.grade + 1, equipLevelTableData.equipGroup);
		if (nextGradeEquipLevelTableData == null)
			return null;
		return nextGradeEquipLevelTableData;
	}

	Dictionary<string, EquipTableData> _dicEquipTableData = new Dictionary<string, EquipTableData>();
	public EquipTableData GetCachedEquipTableData(string equipGroup)
	{
		if (_dicEquipTableData.ContainsKey(equipGroup))
			return _dicEquipTableData[equipGroup];

		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(equipGroup);
		if (equipTableData == null)
			return null;

		_dicEquipTableData.Add(equipGroup, equipTableData);
		return equipTableData;
	}

	public ObscuredInt cachedValue { get; set; }
	EquipStatusList _cachedEquipStatusList = new EquipStatusList();
	public EquipStatusList cachedEquipStatusList { get { return _cachedEquipStatusList; } }

	#region First Equip Bonus
	public bool firstEquipBonusApplied { get; set; }
	const string firstEquipBonusId = "Equip035102";
	#endregion

	// 하나의 리스트로 관리하려고 하다가 아무리봐도 타입별 리스트로 관리하는게 이득이라 바꿔둔다.
	//List<EquipData> _listEquipData = new List<EquipData>();
	//public List<EquipData> listEquipData { get { return _listEquipData; } }
	List<List<EquipData>> _listEquipData = new List<List<EquipData>>();
	Dictionary<int, EquipData> _dicEquippedData = new Dictionary<int, EquipData>();

	public void OnRecvEquipInventory(List<ItemInstance> userInventory, Dictionary<string, UserDataRecord> userData, Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		ClearInventory();

		// list
		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId.StartsWith("Equip") == false)
				continue;

			EquipLevelTableData equipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(userInventory[i].ItemId);
			if (equipLevelTableData == null)
				continue;

			EquipData newEquipData = new EquipData();
			newEquipData.uniqueId = userInventory[i].ItemInstanceId;
			newEquipData.equipId = userInventory[i].ItemId;
			newEquipData.Initialize(userInventory[i].CustomData);
			_listEquipData[newEquipData.cachedEquipTableData.equipType].Add(newEquipData);
		}

		// dictionary
		int invalidEquipSlotIndex = -1;
		_dicEquippedData.Clear();
		for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
		{
			string key = string.Format("eqPo{0}", i);
			if (userReadOnlyData.ContainsKey(key))
			{
				string uniqueId = userReadOnlyData[key].Value;
				if (string.IsNullOrEmpty(uniqueId))
					continue;
				EquipData equipData = FindEquipData(uniqueId, (eEquipSlotType)i);
				if (equipData == null)
				{
					invalidEquipSlotIndex = i;
					continue;
				}
				if (equipData.cachedEquipTableData.equipType != i)
				{
					// 슬롯에 맞지않는 아이템이 장착되어있다. 이것도 잘못된 케이스다.
					invalidEquipSlotIndex = i;
					continue;
				}
				_dicEquippedData.Add(i, equipData);
			}
		}
		if (invalidEquipSlotIndex != -1)
		{
			PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidEquipType, false, invalidEquipSlotIndex);
		}

		// status
		RefreshCachedStatus();

		#region First Equip Bonus
		firstEquipBonusApplied = false;
		if (userReadOnlyData.ContainsKey("firstEquipBonusApplied"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["firstEquipBonusApplied"].Value) == false)
				firstEquipBonusApplied = true;
		}
		#endregion

		/*
		// reconstruct
		reconstructPoint = 0;
		if (userReadOnlyData.ContainsKey("recon"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["recon"].Value, out intValue))
				reconstructPoint = intValue;
		}
		*/
	}

	public void ClearInventory()
	{
		if (_listEquipData.Count == 0)
		{
			for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
			{
				List<EquipData> listEquipData = new List<EquipData>();
				_listEquipData.Add(listEquipData);
			}
		}
		for (int i = 0; i < _listEquipData.Count; ++i)
			_listEquipData[i].Clear();
		_dicEquippedData.Clear();

		// status
		RefreshCachedStatus();
	}

	public static bool ContainsEquip(ShopProductTableData shopProductTableData)
	{
		if ((shopProductTableData.rewardType1 == "it" && shopProductTableData.rewardValue1.StartsWith("Equip")) ||
			(shopProductTableData.rewardType2 == "it" && shopProductTableData.rewardValue2.StartsWith("Equip")) ||
			(shopProductTableData.rewardType3 == "it" && shopProductTableData.rewardValue3.StartsWith("Equip")) ||
			(shopProductTableData.rewardType4 == "it" && shopProductTableData.rewardValue4.StartsWith("Equip")) ||
			(shopProductTableData.rewardType5 == "it" && shopProductTableData.rewardValue5.StartsWith("Equip")))
			return true;

		return false;
	}

	public void OnRecvRefreshEquipInventory(List<ItemInstance> userInventory)
	{
		for (int i = 0; i < _listEquipData.Count; ++i)
			_listEquipData[i].Clear();

		// 기존꺼를 찾아서 옮겨도 되는건데 인벤토리가 많아질수록 찾는 시간이 더 오래걸릴거 같아서 그냥 재구축하기로 한다.
		// 합성창 같은거 열어두지 않는이상 EquipData를 캐싱하고 있는 곳은 없을거라서 새로 구축해도 상관없을거 같다.
		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId.StartsWith("Equip") == false)
				continue;

			EquipLevelTableData equipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(userInventory[i].ItemId);
			if (equipLevelTableData == null)
				continue;

			EquipData newEquipData = new EquipData();
			newEquipData.uniqueId = userInventory[i].ItemInstanceId;
			newEquipData.equipId = userInventory[i].ItemId;
			newEquipData.Initialize(userInventory[i].CustomData);
			_listEquipData[newEquipData.cachedEquipTableData.equipType].Add(newEquipData);
		}

		// dictionary refresh
		Dictionary<int, EquipData> dicNewEquippedData = new Dictionary<int, EquipData>();
		Dictionary<int, EquipData>.Enumerator e = _dicEquippedData.GetEnumerator();
		while (e.MoveNext())
		{
			EquipData equipData = e.Current.Value;
			if (equipData == null)
				continue;

			EquipData newEquipData = FindEquipData(equipData.uniqueId, (eEquipSlotType)equipData.cachedEquipTableData.equipType);
			if (newEquipData == null)
				continue;
			if (newEquipData.cachedEquipTableData.equipType != e.Current.Key)
				continue;
			dicNewEquippedData.Add(e.Current.Key, newEquipData);
		}
		_dicEquippedData = dicNewEquippedData;

		// status
		RefreshCachedStatus();
	}

	public void LateInitialize()
	{
		Timing.RunCoroutine(LoadProcess());
	}

	IEnumerator<float> LoadProcess()
	{
		// 아무래도 로비 진입 후 시공간 들어갈때 너무 렉이 심해서 장착중인 장비의 프리팹은 미리 로딩해두기로 한다.
		// 한번에 하나씩만 로드하기 위해 플래그를 건다.
		// 근데 이래도 오래 걸리는건 여전한데 아웃라인을 동적으로 생성하는데서 온다.
		for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
		{
			EquipData equipData = EquipManager.instance.GetEquippedDataByType((EquipManager.eEquipSlotType)i);
			if (equipData == null)
				continue;

			bool waitLoad = true;
			AddressableAssetLoadManager.GetAddressableGameObject(equipData.cachedEquipTableData.prefabAddress, "Equip", (prefab) =>
			{
				waitLoad = false;
			});
			while (waitLoad == true)
				yield return Timing.WaitForOneFrame;
#if !UNITY_EDITOR
			Debug.LogFormat("TimeSpaceData Load Finish. Index : {0} / FrameCount : {1}", i, Time.frameCount);
#endif
		}
	}

	void RefreshCachedStatus()
	{
		cachedValue = 0;
		for (int i = 0; i < _cachedEquipStatusList.valueList.Length; ++i)
			_cachedEquipStatusList.valueList[i] = 0.0f;

		Dictionary<int, EquipData>.Enumerator e = _dicEquippedData.GetEnumerator();
		while (e.MoveNext())
		{
			EquipData equipData = e.Current.Value;
			if (equipData == null)
				continue;

			// 이제 모든 템은 기본값이 Attack이다.
			cachedValue += equipData.mainStatusValue;

			// 서브옵으로 붙는 것도 다 한군데 모아놔야한다.
			for (int i = 0; i < _cachedEquipStatusList.valueList.Length; ++i)
				_cachedEquipStatusList.valueList[i] += equipData.equipStatusList.valueList[i];
		}
	}

	public void OnChangedStatus()
	{
		// 장착되어있는 장비 중 하나가 변경된거다. 해당 장비는 장착 혹은 탈착 혹은 속성 변경으로 인한 데이터 갱신이 완료된 상태일테니
		// 전체 장비 재계산 후
		RefreshCachedStatus();
		PlayerData.instance.OnChangedStatus();
	}



	public EquipData FindEquipData(string uniqueId, eEquipSlotType equipSlotType)
	{
		List<EquipData> listEquipData = GetEquipListByType(equipSlotType);
		for (int i = 0; i < listEquipData.Count; ++i)
		{
			if (listEquipData[i].uniqueId == uniqueId)
				return listEquipData[i];
		}
		return null;
	}

	public List<EquipData> GetEquipListByType(eEquipSlotType equipSlotType)
	{
		return _listEquipData[(int)equipSlotType];
	}

	public bool IsEquipped(EquipData equipData)
	{
		bool equipped = false;
		int equipType = equipData.cachedEquipTableData.equipType;
		if (_dicEquippedData.ContainsKey(equipType))
		{
			if (_dicEquippedData[equipType].uniqueId == equipData.uniqueId)
				equipped = true;
		}
		return equipped;
	}

	public EquipData GetEquippedDataByType(eEquipSlotType equipSlotType)
	{
		if (_dicEquippedData.ContainsKey((int)equipSlotType))
			return _dicEquippedData[(int)equipSlotType];
		return null;
	}

	public bool IsExistEquipByType(eEquipSlotType equipSlotType)
	{
		List<EquipData> listEquipData = GetEquipListByType(equipSlotType);
		return listEquipData.Count > 0;
	}

	public void PreloadEquipIcon()
	{
		for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
		{
			List<EquipData> listEquipData = GetEquipListByType((eEquipSlotType)i);
			for (int j = 0; j < listEquipData.Count; ++j)
				AddressableAssetLoadManager.GetAddressableSprite(listEquipData[j].cachedEquipTableData.shotAddress, "Icon", null);
		}
	}

	public bool IsInventoryVisualMax()
	{
		return (inventoryItemCount >= InventoryVisualMax);
	}

	public int inventoryItemCount
	{
		get
		{
			int count = 0;
			for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
			{
				List<EquipData> listEquipData = GetEquipListByType((eEquipSlotType)i);
				count += listEquipData.Count;
			}
			return count;
		}
	}

	#region Packet
	public void OnEquip(EquipData equipData)
	{
		int equipType = equipData.cachedEquipTableData.equipType;
		if (_dicEquippedData.ContainsKey(equipType))
			_dicEquippedData[equipType] = equipData;
		else
			_dicEquippedData.Add(equipType, equipData);

		OnChangedStatus();
	}

	public void OnUnequip(EquipData equipData)
	{
		int equipType = equipData.cachedEquipTableData.equipType;
		if (_dicEquippedData.ContainsKey(equipType))
			_dicEquippedData.Remove(equipType);

		OnChangedStatus();
	}

	public void OnRevokeInventory(List<EquipData> listRevokeEquipData, bool checkEquipped = false)
	{
		bool unequip = false;
		for (int i = 0; i < listRevokeEquipData.Count; ++i)
		{
			eEquipSlotType equipType = (eEquipSlotType)listRevokeEquipData[i].cachedEquipTableData.equipType;
			if (checkEquipped && IsEquipped(listRevokeEquipData[i]))
			{
				_dicEquippedData.Remove((int)equipType);
				unequip = true;
			}

			List<EquipData> listEquipData = GetEquipListByType(equipType);
			if (listEquipData.Contains(listRevokeEquipData[i]))
			{
				listEquipData.Remove(listRevokeEquipData[i]);
			}
			else
			{
				Debug.LogErrorFormat("Revoke Inventory Error. Not found Equip : {0}", listRevokeEquipData[i].uniqueId);
			}
		}

		// 장착된걸 지웠을땐 바로 스탯을 재계산한다.
		if (unequip)
			OnChangedStatus();
	}
	#endregion


	#region Composite
	List<EquipData> _listCurrentEquipData = new List<EquipData>();
	public void CollectAutoComposite(List<ObscuredString> listNewEquipId, List<EquipData> listMaterialEquipData)
	{
		for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
		{
			_listCurrentEquipData.Clear();

			List<EquipData> listEquipData = GetEquipListByType((eEquipSlotType)i);
			for (int j = 0; j < listEquipData.Count; ++j)
			{
				// 자동 융합에서는 락 걸려있으면 템이 바뀌는거니까 락을 유지시켜줄 수가 없다. 그러니 융합 대상에서 제외하기로 한다.
				if (listEquipData[j].isLock)
					continue;
				if (IsEquipped(listEquipData[j]))
					continue;
				if (listEquipData[j].cachedEquipLevelTableData.grade > 2)
					continue;

				_listCurrentEquipData.Add(listEquipData[j]);
			}

			_listCurrentEquipData.Sort(delegate (EquipData x, EquipData y)
			{
				if (x.cachedEquipTableData != null && y.cachedEquipTableData != null)
				{
					if (x.cachedEquipLevelTableData.grade > y.cachedEquipLevelTableData.grade) return -1;
					else if (x.cachedEquipLevelTableData.grade < y.cachedEquipLevelTableData.grade) return 1;
					if (x.cachedEquipTableData.equipType < y.cachedEquipTableData.equipType) return -1;
					else if (x.cachedEquipTableData.equipType > y.cachedEquipTableData.equipType) return 1;
					if (x.mainStatusValue > y.mainStatusValue) return -1;
					else if (x.mainStatusValue < y.mainStatusValue) return 1;
				}
				return 0;
			});

			// 위에서부터 하나씩 돌면서 결과를 수집한다.
			for (int j = 0; j < _listCurrentEquipData.Count; ++j)
			{
				if (listMaterialEquipData.Contains(_listCurrentEquipData[j]))
					continue;

				FindMaterial(_listCurrentEquipData, j, listNewEquipId, listMaterialEquipData);
			}
		}
	}

	bool FindMaterial(List<EquipData> listEquipData, int selectIndex, List<ObscuredString> listNewEquipId, List<EquipData> listMaterialEquipData)
	{
		EquipCompositeTableData equipCompositeTableData = TableDataManager.instance.FindEquipCompositeTableData(listEquipData[selectIndex].cachedEquipTableData.rarity, listEquipData[selectIndex].cachedEquipLevelTableData.grade, listEquipData[selectIndex].enhanceLevel);
		if (equipCompositeTableData == null)
			return false;

		bool findResult = false;
		int availableMaterialCount = 0;
		for (int i = 0; i < listEquipData.Count; ++i)
		{
			// 본체는 당연히 제외
			if (i == selectIndex)
				continue;

			// 이미 다른 곳의 재료로 등록된 아이템이면 패스
			if (listMaterialEquipData.Contains(listEquipData[i]))
				continue;

			// 재료 확인
			if (IsValidMaterial(listEquipData[selectIndex], listEquipData[i]) == false)
				continue;

			// 여기까지 오면 
			++availableMaterialCount;
			if (availableMaterialCount >= equipCompositeTableData.count)
			{
				findResult = true;
				break;
			}
		}

		// 만약 찾는데 실패했다면 굳이 아래로직은 실행할 필요 없다. 재료가 2개 필요한데 하나밖에 없을때도 이쪽으로 들어오게 된다.
		if (findResult == false)
			return false;

		// 찾기만 하고 끝내는게 아니라 등록까지 해야하는거라면 다시 for루프 돌면서
		int count = 0;
		for (int i = 0; i < listEquipData.Count; ++i)
		{
			if (i == selectIndex)
				continue;
			if (listMaterialEquipData.Contains(listEquipData[i]))
				continue;
			if (IsValidMaterial(listEquipData[selectIndex], listEquipData[i]) == false)
				continue;

			// 재료 리스트에 추가.
			listMaterialEquipData.Add(listEquipData[i]);
			++count;
			if (count >= equipCompositeTableData.count)
				break;
		}

		// AutoComposite 에서는 파란색템 이하만 돌리기 때문에 enhance가 없다.
		if (EquipManager.GetEnhanceLevelMaxByGrade(listEquipData[selectIndex].cachedEquipLevelTableData.grade) == listEquipData[selectIndex].enhanceLevel)
		{
			// 본체도 재료와 함께 소멸되고
			listMaterialEquipData.Add(listEquipData[selectIndex]);

			// 새로운 아이템으로 융합될거다.
			EquipLevelTableData nextGradeEquipLevelTableData = EquipManager.GetNextGradeEquipLevelTableData(listEquipData[selectIndex].cachedEquipLevelTableData);
			if (nextGradeEquipLevelTableData != null)
				listNewEquipId.Add(nextGradeEquipLevelTableData.equipId);
		}
		return findResult;
	}
	
	public bool IsValidMaterial(EquipData selectedEquipData, EquipData materialEquipData)
	{
		//if (materialEquipData.equipId == "Equip030101")
		//	Debug.Log("2222");

		// 락걸린 재료와 장착중인 재료는 재료로 사용할 수 없다.
		if (materialEquipData.isLock)
			return false;
		if (IsEquipped(materialEquipData))
			return false;

		EquipCompositeTableData equipCompositeTableData = TableDataManager.instance.FindEquipCompositeTableData(selectedEquipData.cachedEquipTableData.rarity, selectedEquipData.cachedEquipLevelTableData.grade, selectedEquipData.enhanceLevel);
		if (equipCompositeTableData == null)
			return false;

		switch ((EquipCompositeCanvas.eCompositeMaterialType)equipCompositeTableData.materialType)
		{
			case EquipCompositeCanvas.eCompositeMaterialType.SameEquip:
				// 같은 장비인지 판단하는 컬럼인 group을 비교해야한다. 
				if (selectedEquipData.cachedEquipLevelTableData.equipGroup != materialEquipData.cachedEquipLevelTableData.equipGroup)
					return false;
				break;
			case EquipCompositeCanvas.eCompositeMaterialType.SameEquipType:
				if (selectedEquipData.cachedEquipTableData.equipType != materialEquipData.cachedEquipTableData.equipType)
					return false;
				break;
			case EquipCompositeCanvas.eCompositeMaterialType.AnyEquipType:
				break;
		}

		// grade랑 rarity는 항상 검사
		if (equipCompositeTableData.materialGrade != materialEquipData.cachedEquipLevelTableData.grade)
			return false;
		if (equipCompositeTableData.materialRarity != materialEquipData.cachedEquipTableData.rarity)
			return false;

		return true;
	}

	public bool IsCompositeAvailable(EquipData selectedEquipData, List<EquipData> listEquipData)
	{
		//if (selectedEquipData.equipId == "Equip030101")
		//	Debug.Log("1111");

		// 먼저 최대치에 도달했는지부터 확인
		if (IsMaxGradeEnhance(selectedEquipData))
			return false;

		// 융합 테이블에 없는건지 확인
		EquipCompositeTableData equipCompositeTableData = TableDataManager.instance.FindEquipCompositeTableData(selectedEquipData.cachedEquipTableData.rarity, selectedEquipData.cachedEquipLevelTableData.grade, selectedEquipData.enhanceLevel);
		if (equipCompositeTableData == null)
			return false;

		// 이후 재료 확인
		int availableMaterialCount = 0;
		for (int i = 0; i < listEquipData.Count; ++i)
		{
			// 본체는 당연히 제외
			if (listEquipData[i] == selectedEquipData)
				continue;

			// 재료 확인
			if (IsValidMaterial(selectedEquipData, listEquipData[i]) == false)
				continue;

			// 여기까지 오면 
			++availableMaterialCount;
			if (availableMaterialCount >= equipCompositeTableData.count)
				return true;
		}
		return false;
	}

	public bool IsMaxGradeEnhance(EquipData selectedEquipData)
	{
		EquipGradeTableData lastEquipGradeTableData = TableDataManager.instance.equipGradeTable.dataArray[TableDataManager.instance.equipGradeTable.dataArray.Length - 1];
		if (lastEquipGradeTableData == null)
			return false;
		if (selectedEquipData.cachedEquipLevelTableData.grade == lastEquipGradeTableData.grade && selectedEquipData.enhanceLevel >= lastEquipGradeTableData.compositeLevelMax)
			return true;
		return false;
	}
	#endregion



	#region Grant
	class RandomGachaEquipGrade
	{
		public int grade;
		public int rarity;
		public float sumWeight;
	}
	List<RandomGachaEquipGrade> _listGachaEquipGrade = null;
	class RandomGachaEquipId
	{
		public string equipId;
		public float sumWeight;
	}
	List<RandomGachaEquipId> _listGachaEquipId = null;
	public string GetRandomGachaResult(bool applyPickUpEquip)
	{
		// 등급별 확률을 구해야한다.
		if (_listGachaEquipGrade == null)
			_listGachaEquipGrade = new List<RandomGachaEquipGrade>();
		_listGachaEquipGrade.Clear();

		#region PickUp Equip
		float pickUpEquipOverrideProb = 0.0f;
		if (applyPickUpEquip)
		{
			CashShopData.PickUpEquipInfo info = CashShopData.instance.GetCurrentPickUpEquipInfo();
			if (info != null && info.ov > 0.0f)
				pickUpEquipOverrideProb = info.ov;
		}
		#endregion

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.gachaEquipTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.gachaEquipTable.dataArray[i].prob;
			if (weight <= 0.0f)
				continue;

			#region PickUp Equip
			if (TableDataManager.instance.gachaEquipTable.dataArray[i].rarity == 2 && TableDataManager.instance.gachaEquipTable.dataArray[i].grade == 3 && pickUpEquipOverrideProb > 0.0f)
				weight = pickUpEquipOverrideProb;
			if (TableDataManager.instance.gachaEquipTable.dataArray[i].rarity == 0 && TableDataManager.instance.gachaEquipTable.dataArray[i].grade == 0 && pickUpEquipOverrideProb > 0.0f)
				weight = weight - pickUpEquipOverrideProb + TableDataManager.instance.gachaEquipTable.dataArray[0].prob;
			#endregion

			sumWeight += weight;
			RandomGachaEquipGrade newInfo = new RandomGachaEquipGrade();
			newInfo.grade = TableDataManager.instance.gachaEquipTable.dataArray[i].grade;
			newInfo.rarity = TableDataManager.instance.gachaEquipTable.dataArray[i].rarity;
			newInfo.sumWeight = sumWeight;
			_listGachaEquipGrade.Add(newInfo);
		}
		if (_listGachaEquipGrade.Count == 0)
			return "";

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listGachaEquipGrade[_listGachaEquipGrade.Count - 1].sumWeight);
		for (int i = 0; i < _listGachaEquipGrade.Count; ++i)
		{
			if (random <= _listGachaEquipGrade[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		#region PickUp Equip
		bool lockRarityByPickUp = false;
		if (applyPickUpEquip)
		{
			CashShopData.PickUpEquipInfo info = CashShopData.instance.GetCurrentPickUpEquipInfo();
			if (info != null)
			{
				if (tempPickUpNotStreakCount2 == (info.ssc - 1))
				{
					// 이번에 굴리는게 픽업의 최종 보너스 단계라면 강제로 rarity를 S나 SS로 고정해야한다.
					for (int i = 0; i < TableDataManager.instance.gachaEquipTable.dataArray.Length; ++i)
					{
						if (TableDataManager.instance.gachaEquipTable.dataArray[i].rarity == 2)
						{
							lockRarityByPickUp = true;
							index = i;
							break;
						}
					}
				}
				else if (tempPickUpNotStreakCount1 >= (info.sc - 1))
				{
					// 이번에 굴리는게 픽업의 최종 보너스 단계라면 강제로 rarity를 S나 SS로 고정해야한다.
					for (int i = 0; i < TableDataManager.instance.gachaEquipTable.dataArray.Length; ++i)
					{
						if (TableDataManager.instance.gachaEquipTable.dataArray[i].rarity == 1)
						{
							lockRarityByPickUp = true;
							index = i;
							break;
						}
					}
				}
			}
		}
		#endregion
		if (index == -1)
			return "";
		int selectedGrade = _listGachaEquipGrade[index].grade;
		int selectedRarity = _listGachaEquipGrade[index].rarity;

		// 등급이 결정되었으면 등급안에서 다시 굴려야한다.
		if (_listGachaEquipId == null)
			_listGachaEquipId = new List<RandomGachaEquipId>();
		_listGachaEquipId.Clear();

		#region PickUp Equip
		string pickUpEquipId = "";
		if (applyPickUpEquip)
		{
			CashShopData.PickUpEquipInfo info = CashShopData.instance.GetCurrentPickUpEquipInfo();
			if (info != null)
			{
				EquipLevelTableData pickUpEquipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(info.id);
				if (pickUpEquipLevelTableData != null)
				{
					EquipTableData pickUpEquipTableData = GetCachedEquipTableData(pickUpEquipLevelTableData.equipGroup);
					if (pickUpEquipTableData != null)
					{
						if (selectedGrade == pickUpEquipLevelTableData.grade && selectedRarity == pickUpEquipTableData.rarity)
						{
							// 픽업을 제외한 나머지의 합산값을 구해야한다.
							pickUpEquipId = pickUpEquipLevelTableData.equipId;
						}
					}
				}
			}
		}

		float pickUpEquipForceWeight = 0.0f;
		if (applyPickUpEquip && string.IsNullOrEmpty(pickUpEquipId) == false)
		{
			for (int i = 0; i < TableDataManager.instance.equipLevelTable.dataArray.Length; ++i)
			{
				if (TableDataManager.instance.equipLevelTable.dataArray[i].grade != selectedGrade)
					continue;
				EquipTableData equipTableData = GetCachedEquipTableData(TableDataManager.instance.equipLevelTable.dataArray[i].equipGroup);
				if (equipTableData == null)
					continue;
				if (equipTableData.rarity != selectedRarity)
					continue;
				if (TableDataManager.instance.equipLevelTable.dataArray[i].equipId == pickUpEquipId)
					continue;
				pickUpEquipForceWeight += equipTableData.equipGachaWeight;
			}
		}
		#endregion

		sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.equipLevelTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.equipLevelTable.dataArray[i].grade != selectedGrade)
				continue;
			EquipTableData equipTableData = GetCachedEquipTableData(TableDataManager.instance.equipLevelTable.dataArray[i].equipGroup);
			if (equipTableData == null)
				continue;
			if (equipTableData.rarity != selectedRarity)
				continue;

			float weight = equipTableData.equipGachaWeight;
			#region PickUp Equip
			if (applyPickUpEquip && TableDataManager.instance.equipLevelTable.dataArray[i].equipId == pickUpEquipId)
				weight = pickUpEquipForceWeight;
			#endregion
			sumWeight += weight;
			RandomGachaEquipId newInfo = new RandomGachaEquipId();
			newInfo.equipId = TableDataManager.instance.equipLevelTable.dataArray[i].equipId;
			newInfo.sumWeight = sumWeight;
			_listGachaEquipId.Add(newInfo);
		}
		if (_listGachaEquipId.Count == 0)
			return "";

		index = -1;
		random = UnityEngine.Random.Range(0.0f, _listGachaEquipId[_listGachaEquipId.Count - 1].sumWeight);
		for (int i = 0; i < _listGachaEquipId.Count; ++i)
		{
			if (random <= _listGachaEquipId[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		#region First Equip Bonus
		if (firstEquipBonusApplied == false)
		{
			if (lockRarityByPickUp)
			{
				// 픽업으로 인해 레어리티가 고정된 상태라면
				// 후보중에 보너스 장비가 있는지 검사 후 인덱스를 할당하고
				for (int i = 0; i < _listGachaEquipId.Count; ++i)
				{
					if (_listGachaEquipId[i].equipId == firstEquipBonusId)
					{
						// 다음번 굴림을 위해 플래그부터 걸어두고 변경. 그러니 리스트에 없으면 다음번에 적용하기로 한다.
						firstEquipBonusApplied = true;
						index = i;
						break;
					}
				}
			}
			else
			{
				// 고정되지 않은 상태라면 다음번 굴림을 위해 플래그부터 걸어두고 리턴
				firstEquipBonusApplied = true;
				return firstEquipBonusId;
			}
		}
		#endregion
		if (index == -1)
			return "";
		return _listGachaEquipId[index].equipId;
	}

	#region PickUp Equip
	public int tempPickUpNotStreakCount1 { get; private set; }
	public int tempPickUpNotStreakCount2 { get; private set; }
	#endregion
	List<ObscuredString> _listRandomObscuredId = new List<ObscuredString>();
	public List<ObscuredString> GetRandomIdList(int count, bool applyPickUpEquip = false)
	{
		#region PickUp Equip
		if (applyPickUpEquip)
		{
			tempPickUpNotStreakCount1 = CashShopData.instance.GetCurrentPickUpEquipNotStreakCount1();
			tempPickUpNotStreakCount2 = CashShopData.instance.GetCurrentPickUpEquipNotStreakCount2();
		}
		#endregion

		_listRandomObscuredId.Clear();

		for (int i = 0; i < count; ++i)
		{
			string randomEquipId = GetRandomGachaResult(applyPickUpEquip);
			_listRandomObscuredId.Add(randomEquipId);

			#region PickUp Equip
			if (applyPickUpEquip)
			{
				bool getRarity1 = false;
				bool getRarity2 = false;
				if (string.IsNullOrEmpty(randomEquipId) == false)
				{
					EquipLevelTableData equipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(randomEquipId);
					if (equipLevelTableData != null)
					{
						EquipTableData equipTableData = GetCachedEquipTableData(equipLevelTableData.equipGroup);
						if (equipTableData != null)
						{
							if (equipTableData != null && equipTableData.rarity == 1)
								getRarity1 = true;
							if (equipTableData != null && equipTableData.rarity == 2)
								getRarity2 = true;
						}
					}
				}
				if (getRarity1)
					tempPickUpNotStreakCount1 = 0;
				else
					++tempPickUpNotStreakCount1;
				if (getRarity2)
					tempPickUpNotStreakCount2 = 0;
				else
					++tempPickUpNotStreakCount2;
			}
			#endregion
		}

		return _listRandomObscuredId;
	}

	public string GetTypeGachaResult(int equipGrade, int equipRarity, int equipType)
	{
		// TypeGacha라서 등급별 확률이 고정이다.
		int selectedGrade = equipGrade;
		int selectedRarity = equipRarity;

		// 등급이 결정되었으면 등급안에서 다시 굴려야한다.
		if (_listGachaEquipId == null)
			_listGachaEquipId = new List<RandomGachaEquipId>();
		_listGachaEquipId.Clear();
		
		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.equipLevelTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.equipLevelTable.dataArray[i].grade != selectedGrade)
				continue;
			EquipTableData equipTableData = GetCachedEquipTableData(TableDataManager.instance.equipLevelTable.dataArray[i].equipGroup);
			if (equipTableData == null)
				continue;
			if (equipTableData.rarity != selectedRarity)
				continue;
			if (equipTableData.equipType != equipType)
				continue;

			float weight = equipTableData.equipGachaWeight;
			sumWeight += weight;
			RandomGachaEquipId newInfo = new RandomGachaEquipId();
			newInfo.equipId = TableDataManager.instance.equipLevelTable.dataArray[i].equipId;
			newInfo.sumWeight = sumWeight;
			_listGachaEquipId.Add(newInfo);
		}
		if (_listGachaEquipId.Count == 0)
			return "";

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listGachaEquipId[_listGachaEquipId.Count - 1].sumWeight);
		for (int i = 0; i < _listGachaEquipId.Count; ++i)
		{
			if (random <= _listGachaEquipId[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listGachaEquipId[index].equipId;
	}

	public List<ObscuredString> GetRandomIdList(int count, int equipGrade, int equipRarity, int equipType)
	{
		_listRandomObscuredId.Clear();

		for (int i = 0; i < count; ++i)
		{
			string randomEquipId = GetTypeGachaResult(equipGrade, equipRarity, equipType);
			_listRandomObscuredId.Add(randomEquipId);
		}

		return _listRandomObscuredId;
	}



	public List<ItemInstance> OnRecvItemGrantResult(string jsonItemGrantResults, int expectCount = 0)
	{
		List<ItemInstance> listItemInstance = PlayFabApiManager.instance.DeserializeItemGrantResult(jsonItemGrantResults);

		int totalCount = 0;
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			EquipLevelTableData equipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(listItemInstance[i].ItemId);
			if (equipLevelTableData == null)
				continue;

			++totalCount;
		}
		if (expectCount != 0 && totalCount != expectCount)
			return null;

		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			EquipLevelTableData equipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(listItemInstance[i].ItemId);
			if (equipLevelTableData == null)
				continue;

			EquipData newEquipData = new EquipData();
			newEquipData.uniqueId = listItemInstance[i].ItemInstanceId;
			newEquipData.equipId = listItemInstance[i].ItemId;
			newEquipData.Initialize(listItemInstance[i].CustomData);
			newEquipData.newEquip = true;
			_listEquipData[newEquipData.cachedEquipTableData.equipType].Add(newEquipData);

			// 보유 공격력이 없으니 호출할 필요가 없다.
			//OnChangedStatus();
		}

		grantNewEquip = true;

		return listItemInstance;
	}


	// 로비 포탈 검사할땐 for loop돌면서 newEquip 있는지 확인하는 거보다 플래그 하나 검사하는게 훨씬 편하다.
	public bool grantNewEquip { get; set; }
	#endregion


	#region AlarmObject
	public void ResetNewEquip()
	{
		if (grantNewEquip == false)
			return;

		for (int i = 0; i < (int)eEquipSlotType.Amount; ++i)
		{
			List<EquipData> listEquipData = GetEquipListByType((eEquipSlotType)i);
			if (listEquipData.Count == 0)
				continue;

			for (int j = 0; j < listEquipData.Count; ++j)
				listEquipData[j].newEquip = false;
		}
		grantNewEquip = false;

		// 시공간에서 마을로 돌아갈땐 항상 TimeSpacePortal이 파티클 재생때문에 꺼졌다 켜진다. 이때 알아서 알람 갱신이 되니 여기서 처리할 필요 없다.
		//if (TimeSpacePortal.instance != null && TimeSpacePortal.instance.gameObject.activeSelf)
		//	TimeSpacePortal.instance.RefreshAlarmObject();

		// 하지만 리셋 타이밍에 제단에 붙어있는 New표시는 삭제해야하므로 호출해준다.
		EquipGround.instance.RefreshAlarmObjectList();
	}
	#endregion
}