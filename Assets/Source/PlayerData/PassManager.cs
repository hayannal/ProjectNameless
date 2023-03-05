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

	public ObscuredInt cachedValue { get; set; }

	Dictionary<string, int> _dicPassAttackInfo;
	public void OnRecvPassData(Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

		_dicPassAttackInfo = null;
		if (titleData.ContainsKey("passAtk"))
			_dicPassAttackInfo = serializer.DeserializeObject<Dictionary<string, int>>(titleData["passAtk"]);

		// setting flag
		RefreshFlag();

		// status
		RefreshCachedStatus();
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

		// 먼저 영구적 패스 적용

		// 이후 기간제 패스 적용
		// 이미 앞부분에서 다른 매니저들 호출이 끝났을테니 여기선 가져와서 적용만 하면 된다.
		if (PetManager.instance.IsPetPass())
			cachedValue += GetPassAttackValue("petpass");

		if (CharacterManager.instance.IsTeamPass())
			cachedValue += GetPassAttackValue("teampass");
	}

	int GetPassAttackValue(string key)
	{
		if (_dicPassAttackInfo.ContainsKey(key))
			return _dicPassAttackInfo[key];
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
}