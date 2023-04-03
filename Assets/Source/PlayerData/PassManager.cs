using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

public class PassManager : MonoBehaviour
{
	public static PassManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("PassManager")).AddComponent<PassManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static PassManager _instance = null;

	List<ObscuredString> _listItemAtk = new List<ObscuredString>();

	public ObscuredInt cachedValue { get; set; }

	Dictionary<string, int> _dicPassAttackInfo;
	Dictionary<string, int> _dicItemAttackInfo;
	public void OnRecvPassData(List<ItemInstance> userInventory, Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

		_dicPassAttackInfo = null;
		if (titleData.ContainsKey("passAtk"))
			_dicPassAttackInfo = serializer.DeserializeObject<Dictionary<string, int>>(titleData["passAtk"]);

		_dicItemAttackInfo = null;
		if (titleData.ContainsKey("itemAtk"))
			_dicItemAttackInfo = serializer.DeserializeObject<Dictionary<string, int>>(titleData["itemAtk"]);


		// 다른 인벤토리와 달리 별다른 정보가 필요없으므로 아이디만 저장해도 된다.
		_listItemAtk.Clear();
		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId.StartsWith("RelayAtk_") == false && userInventory[i].ItemId.StartsWith("FreeLevelAtk_") == false && userInventory[i].ItemId.StartsWith("FreeStageAtk_") == false)
				continue;

			if (_listItemAtk.Contains(userInventory[i].ItemId) == false)
				_listItemAtk.Add(userInventory[i].ItemId);
		}

		// setting flag
		RefreshFlag();

		// status
		RefreshCachedStatus();
	}

	public static string ShopProductId2ItemId(string shopProductId)
	{
		string[] split = shopProductId.Split('_');
		if (split.Length != 2)
			return "";

		string nameId = "";
		switch (split[0])
		{
			case "relay": nameId = "RelayAtk"; break;
			case "freelevel": nameId = "FreeLevelAtk"; break;
			case "freestage": nameId = "FreeStageAtk"; break;
			default: return "";
		}
		int number = 0;
		int.TryParse(split[1], out number);
		return string.Format("{0}_{1:00}", nameId, number);
	}

	void RefreshFlag()
	{
		// 이 매니저는 마지막에 호출되기 때문에 다른 매니저에 있는 결과값을 가져와서 처리해도 괜찮다.
		_petPassActived = PetManager.instance.IsPetPass();
		_teamPassActived = CharacterManager.instance.IsTeamPass();
	}

	void RefreshCachedStatus()
	{
		cachedValue = 0;

		// 먼저 보유한 아이템에 대해서 영구적 패스 적용. 중복해서 가지고 있더라도 1회만 적용되게 해둔다.
		Dictionary<string, int>.Enumerator e = _dicItemAttackInfo.GetEnumerator();
		while (e.MoveNext())
		{
			if (_listItemAtk.Contains(e.Current.Key))
				cachedValue += e.Current.Value;
		}

		// 이후 기간제 패스 적용
		// 이미 앞부분에서 다른 매니저들 호출이 끝났을테니 여기선 가져와서 적용만 하면 된다.
		if (PetManager.instance.IsPetPass())
			cachedValue += GetPassAttackValue("petpass");

		if (CharacterManager.instance.IsTeamPass())
			cachedValue += GetPassAttackValue("teampass");
	}

	public int GetPassAttackValue(string key)
	{
		if (_dicPassAttackInfo.ContainsKey(key))
			return _dicPassAttackInfo[key];
		return 0;
	}

	public int GetItemAttackValue(string key)
	{
		if (_dicItemAttackInfo.ContainsKey(key))
			return _dicItemAttackInfo[key];
		return 0;
	}

	void Update()
	{
		UpdatePetPassRemainTime();
		UpdateTeamPassRemainTime();
	}

	bool _petPassActived;
	void UpdatePetPassRemainTime()
	{
		if (_petPassActived == false)
			return;

		if (ServerTime.UtcNow < PetManager.instance.petPassExpireTime)
		{
		}
		else
		{
			_petPassActived = false;
			OnChangedStatus();
		}
	}

	bool _teamPassActived;
	void UpdateTeamPassRemainTime()
	{
		if (_teamPassActived == false)
			return;

		if (ServerTime.UtcNow < CharacterManager.instance.teamPassExpireTime)
		{
		}
		else
		{
			_teamPassActived = false;
			OnChangedStatus();
		}
	}



	public void OnChangedStatus()
	{
		// 패스 중 변경이 일어난거다.
		RefreshFlag();

		// 전체 패스 재계산 후
		RefreshCachedStatus();
		PlayerData.instance.OnChangedStatus();
	}


	#region Grant
	public void OnRecvItemGrantResult(string itemId)
	{
		if (_listItemAtk.Contains(itemId))
			return;

		_listItemAtk.Add(itemId);
		OnChangedStatus();
	}
	#endregion
}