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
		// �� �Ŵ����� �������� ȣ��Ǳ� ������ �ٸ� �Ŵ����� �ִ� ������� �����ͼ� ó���ص� ������.
		_petPassActived = PetManager.instance.IsPetPass();
		_teamPassActived = CharacterManager.instance.IsTeamPass();
	}

	void RefreshCachedStatus()
	{
		cachedValue = 0;

		// ���� ������ �н� ����

		// ���� �Ⱓ�� �н� ����
		// �̹� �պκп��� �ٸ� �Ŵ����� ȣ���� �������״� ���⼱ �����ͼ� ���븸 �ϸ� �ȴ�.
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
		// �н� �� ������ �Ͼ�Ŵ�.
		RefreshFlag();

		// ��ü �н� ���� ��
		RefreshCachedStatus();
		PlayerData.instance.OnChangedStatus();
	}
}