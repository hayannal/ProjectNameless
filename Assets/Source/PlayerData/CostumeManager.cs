using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class CostumeManager : MonoBehaviour
{
	public static CostumeManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("CostumeManager")).AddComponent<CostumeManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static CostumeManager _instance = null;

	public ObscuredString selectedCostumeId { get; set; }
	public ObscuredInt cachedValue { get; set; }

	public static string GetAddressByCostumeId(string costumeId)
	{
		CostumeTableData costumeTableData = TableDataManager.instance.FindCostumeTableData(costumeId);
		if (costumeTableData == null)
			return "";
		return costumeTableData.prefabAddress;
	}

	List<ObscuredString> _listCostumeId = new List<ObscuredString>();
	public void OnRecvCostumeInventory(List<ItemInstance> userInventory, Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		ClearInventory();

		// list
		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId.StartsWith("Costume_") == false)
				continue;

			CostumeTableData costumeTableData = TableDataManager.instance.FindCostumeTableData(userInventory[i].ItemId);
			if (costumeTableData == null)
				continue;
			_listCostumeId.Add(userInventory[i].ItemId);
		}

		selectedCostumeId = "";
		if (userReadOnlyData.ContainsKey("selectedCostumeId"))
		{
			string costumeId = userReadOnlyData["selectedCostumeId"].Value;
			if (string.IsNullOrEmpty(costumeId) == false && Contains(costumeId))
				selectedCostumeId = costumeId;
			else
			{
				selectedCostumeId = "";
				//PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidMainCharacter);
			}
		}

		// status
		RefreshCachedStatus();
	}

	public void ClearInventory()
	{
		_listCostumeId.Clear();

		// status
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{
		cachedValue = 0;

		// spell level status
		for (int i = 0; i < _listCostumeId.Count; ++i)
		{
			CostumeTableData costumeTableData = TableDataManager.instance.FindCostumeTableData(_listCostumeId[i]);
			if (costumeTableData == null)
				continue;
			cachedValue += costumeTableData.atk;
		}
	}

	public void OnChangedStatus()
	{
		RefreshCachedStatus();
		PlayerData.instance.OnChangedStatus();
	}


	public bool Contains(string costumeId)
	{
		if (_listCostumeId.Contains(costumeId))
			return true;
		return false;
	}

	public void OnRecvPurchase(string costumeId)
	{
		if (_listCostumeId.Contains(costumeId) == false)
			_listCostumeId.Add(costumeId);
		OnChangedStatus();
	}

	public string GetCurrentPlayerPrefabAddress()
	{
		if (string.IsNullOrEmpty(selectedCostumeId) == false)
		{
			CostumeTableData costumeTableData = TableDataManager.instance.FindCostumeTableData(selectedCostumeId);
			if (costumeTableData != null)
				return costumeTableData.prefabAddress;
		}
		return CharacterData.GetAddressByActorId(CharacterData.s_PlayerActorId);
	}


	#region Player Costume Prafab
	bool _wait;
	public void ChangeCostume()
	{
		if (_wait)
			return;

		_wait = true;
		AddressableAssetLoadManager.GetAddressableGameObject(GetCurrentPlayerPrefabAddress(), "", OnLoadedCostume);
	}

	void OnLoadedCostume(GameObject prefab)
	{
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(prefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(prefab);
#endif

		_wait = false;
		PlayerActor playerActor = newObject.GetComponent<PlayerActor>();
		if (playerActor == null)
			return;
		playerActor.playerAI.enabled = false;
		playerActor.baseCharacterController.enabled = false;
		playerActor.enabled = false;
		// 캐싱할게 아니기 때문에 등록절차가 필요없다.
		//BattleInstanceManager.instance.AddCanvasPlayerActor(playerActor, _idWithCostume);

		// 애니메이터 오브젝트 교체. 본 메시 다 교체하는거다.
		if (BattleInstanceManager.instance.playerActor == null)
			return;
		if (playerActor == null)
			return;

		playerActor.cachedTransform.position = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		Animator animator = playerActor.GetComponentInChildren<Animator>();
		BattleInstanceManager.instance.playerActor.actionController.animator.gameObject.SetActive(false);
		animator.transform.parent = BattleInstanceManager.instance.playerActor.actionController.transform;
		BattleInstanceManager.instance.playerActor.actionController.PreInitializeComponent();
		playerActor.gameObject.SetActive(false);
	}
	#endregion
}