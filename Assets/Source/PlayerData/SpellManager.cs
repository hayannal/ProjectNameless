using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

// 아이템 전부에서 사용하는거라 공용으로
public class ItemGrantRequest
{
	public Dictionary<string, string> Data;
	public string ItemId;
}

public class GrantItemsToUsersResult
{
	public List<ItemInstance> ItemGrantResults;
}

public class SpellManager : MonoBehaviour
{
	public static SpellManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("SpellManager")).AddComponent<SpellManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static SpellManager _instance = null;

	List<SpellData> _listSpellData = new List<SpellData>();
	public void OnRecvSpellInventory(List<ItemInstance> userInventory, Dictionary<string, UserDataRecord> userData, Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		ClearInventory();

		// list
		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId.StartsWith("Spell_") == false)
				continue;

			SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(userInventory[i].ItemId);
			if (skillTableData == null)
				continue;

			SpellData newSpellData = new SpellData();
			newSpellData.uniqueId = userInventory[i].ItemInstanceId;
			newSpellData.spellId = userInventory[i].ItemId;
			newSpellData.Initialize((userInventory[i].RemainingUses != null) ? (int)userInventory[i].RemainingUses : 0, userInventory[i].CustomData);
			_listSpellData.Add(newSpellData);
		}
		
		// status
		RefreshCachedStatus();
	}

	public void ClearInventory()
	{
		_listSpellData.Clear();

		// status
		RefreshCachedStatus();
	}

	void RefreshCachedStatus()
	{

	}


	public int GetSpellLevel(string id)
	{
		for (int i = 0; i < _listSpellData.Count; ++i)
		{
			if (_listSpellData[i].spellId == id)
				return _listSpellData[i].level;
		}
		return 0;
	}


	#region Grant
	List<ItemGrantRequest> _listGrantRequest = new List<ItemGrantRequest>();
	public List<ItemGrantRequest> GenerateGrantInfo(List<string> listSpellId, ref string checkSum)
	{
		_listGrantRequest.Clear();

		for (int i = 0; i < listSpellId.Count; ++i)
		{
			ItemGrantRequest info = new ItemGrantRequest();
			info.ItemId = listSpellId[i];
			// 최초로 만들어질때만 Data 적용되고 이미 만들어진 아이템에는 적용되지 않으므로 기본값을 설정하면 된다.
			info.Data = new Dictionary<string, string>();
			info.Data.Add(SpellData.KeyLevel, "1");
			_listGrantRequest.Add(info);
		}

		if (_listGrantRequest.Count > 0)
		{
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			string jsonItemGrants = serializer.SerializeObject(_listGrantRequest);
			checkSum = PlayFabApiManager.CheckSum(jsonItemGrants);
		}

		// 임시 리스트를 가지고 있을 필요 없으니 클리어
		_listSpellId.Clear();

		return _listGrantRequest;
	}

	List<string> _listSpellId = new List<string>();
	public List<ItemGrantRequest> GenerateGrantRequestInfo(List<ObscuredString> listSpellId, ref string checkSum)
	{
		_listGrantRequest.Clear();
		if (listSpellId == null || listSpellId.Count == 0)
			return _listGrantRequest;

		_listSpellId.Clear();
		for (int i = 0; i < listSpellId.Count; ++i)
			_listSpellId.Add(listSpellId[i]);
		return GenerateGrantInfo(_listSpellId, ref checkSum);
	}

	public List<ItemGrantRequest> GenerateGrantRequestInfo(string spellId, ref string checkSum)
	{
		_listSpellId.Clear();
		_listSpellId.Add(spellId);
		return GenerateGrantInfo(_listSpellId, ref checkSum);
	}

	public List<ItemInstance> DeserializeItemGrantResult(string jsonItemGrantResults)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		GrantItemsToUsersResult result = serializer.DeserializeObject<GrantItemsToUsersResult>(jsonItemGrantResults);
		return result.ItemGrantResults;
	}

	// 대부분의 아이템 획득은 이걸 써서 처리하게 될거다.
	#endregion
}
