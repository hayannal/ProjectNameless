using System;
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

	public const int InventoryVisualMax = 200;
	public const int InventoryRealMax = 249;

	public ObscuredInt cachedValue { get; set; }
	EquipStatusList _cachedEquipStatusList = new EquipStatusList();
	public EquipStatusList cachedEquipStatusList { get { return _cachedEquipStatusList; } }

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

			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(userInventory[i].ItemId);
			if (equipTableData == null)
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
	#endregion




	#region Grant
	class RandomGachaEquipGrade
	{
		public int grade;
		public float sumWeight;
	}
	List<RandomGachaEquipGrade> _listGachaEquipGrade = null;
	class RandomGachaEquipId
	{
		public string equipId;
		public float sumWeight;
	}
	List<RandomGachaEquipId> _listGachaEquipId = null;
	public string GetRandomGachaResult()
	{
		// 등급별 확률을 구해야한다.
		if (_listGachaEquipGrade == null)
			_listGachaEquipGrade = new List<RandomGachaEquipGrade>();
		_listGachaEquipGrade.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.gachaEquipTable.dataArray.Length; ++i)
		{
			// 기본 prob로 돌리면 된다.
			float weight = TableDataManager.instance.gachaEquipTable.dataArray[i].prob;
			if (weight <= 0.0f)
				continue;

			sumWeight += weight;
			RandomGachaEquipGrade newInfo = new RandomGachaEquipGrade();
			newInfo.grade = TableDataManager.instance.gachaEquipTable.dataArray[i].grade;
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
		if (index == -1)
			return "";
		int selectedGrade = _listGachaEquipGrade[index].grade;

		// 등급이 결정되었으면 등급안에서 다시 굴려야한다.
		if (_listGachaEquipId == null)
			_listGachaEquipId = new List<RandomGachaEquipId>();
		_listGachaEquipId.Clear();

		sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.equipTable.dataArray[i].grade != selectedGrade)
				continue;

			sumWeight += TableDataManager.instance.equipTable.dataArray[i].equipGachaWeight;
			RandomGachaEquipId newInfo = new RandomGachaEquipId();
			newInfo.equipId = TableDataManager.instance.equipTable.dataArray[i].equipId;
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
		if (index == -1)
			return "";
		return _listGachaEquipId[index].equipId;
	}

	List<ObscuredString> _listRandomObscuredId = new List<ObscuredString>();
	public List<ObscuredString> GetRandomIdList(int count)
	{
		_listRandomObscuredId.Clear();

		for (int i = 0; i < count; ++i)
			_listRandomObscuredId.Add(GetRandomGachaResult());

		return _listRandomObscuredId;
	}



	public List<ItemInstance> OnRecvItemGrantResult(string jsonItemGrantResults, int expectCount = 0)
	{
		List<ItemInstance> listItemInstance = PlayFabApiManager.instance.DeserializeItemGrantResult(jsonItemGrantResults);

		int totalCount = 0;
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listItemInstance[i].ItemId);
			if (equipTableData == null)
				continue;

			++totalCount;
		}
		if (expectCount != 0 && totalCount != expectCount)
			return null;

		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(listItemInstance[i].ItemId);
			if (equipTableData == null)
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