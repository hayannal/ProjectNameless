using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.DataModels;

public class PlayerData : MonoBehaviour
{
	public static PlayerData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("PlayerData")).AddComponent<PlayerData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static PlayerData _instance = null;

	public bool loginned { get; private set; }
	public bool newlyCreated { get; private set; }
	public bool clientOnly { get; private set; }

#if UNITY_IOS
	// 심사빌드인지 체크해두는 변수
	public bool reviewVersion { get; set; }
#endif

	public ObscuredInt highestClearStage { get; set; }
	public ObscuredInt selectedStage { get; set; }
	public int currentRewardStage
	{
		get
		{
			return Mathf.Max(highestClearStage, selectedStage);
		}
	}

	#region Player Property
	public static int SubLevelCount = 3;
	List<ObscuredInt> _listSubLevel = new List<ObscuredInt>();
	public List<ObscuredInt> listSubLevel { get { return _listSubLevel; } }
	public ObscuredInt playerLevel { get; set; }
	#endregion

	// 이용약관 확인용 변수. 값이 있으면 기록된거로 간주하고 true로 해둔다.
	public ObscuredBool termsConfirmed { get; set; }

	// 네트워크 오류로 인해 씬을 재시작할때는 타이틀 떠서 진입하듯 초기 프로세스들을 검사해야한다.
	public bool checkRestartScene { get; set; }

	public void OnNewlyCreatedPlayer()
	{
		highestClearStage = 0;
		selectedStage = 1;
		termsConfirmed = false;

		// newlyCreated는 새로 생성된 계정에서만 true일거고 재접하거나 로그아웃 할때 false로 돌아와서 유지될거다.
		newlyCreated = true;
		loginned = true;

		Debug.Log("OnNewlyCreatedPlayer Called!");
	}

	public void ResetData()
	{
		// 이게 가장 중요. 다른 것들은 받을때 알아서 다 비우고 다시 셋팅한다.
		loginned = false;
		newlyCreated = false;

		/*
		lobbyDownloadState = false;
		*/

		// OnRecvPlayerData 함수들 두번 받아도 아무 문제없게 짜두면 여기서 딱히 할일은 없을거다.
		// 두번 받는거 뿐만 아니라 모든 변수를 다 덮어서 기록하는지도 확인하면 완벽하다.(건너뛰면 이전값이 남을테니 위험)
		//
		// 대신 진입시에 앱구동처럼 처리하기 위해 재시작 플래그를 여기서 걸어둔다.
		checkRestartScene = true;
	}

	public void OnRecvPlayerStatistics(List<StatisticValue> playerStatistics)
	{
		// 통계는 없을 수 있는 값이니 초기화는 필수
		highestClearStage = 0;

		// 레벨은 1로 시작하는게 맞다.
		playerLevel = 1;
		if (_listSubLevel.Count == 0)
		{
			for (int i = 0; i < SubLevelCount; ++i)
				_listSubLevel.Add(0);
		}
		for (int i = 0; i < _listSubLevel.Count; ++i)
			_listSubLevel[i] = 0;

		// confirm
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			switch (playerStatistics[i].StatisticName)
			{
				//case "highestPlayChapter": highestPlayChapter = playerStatistics[i].Value; break;
				case "highestClearStage": highestClearStage = playerStatistics[i].Value; break;
				case "playerLevel": playerLevel = playerStatistics[i].Value; break;
				case "subLevel0": _listSubLevel[0] = playerStatistics[i].Value; break;
				case "subLevel1": _listSubLevel[1] = playerStatistics[i].Value; break;
				case "subLevel2": _listSubLevel[2] = playerStatistics[i].Value; break;
				//case "highestValue": highestValue = playerStatistics[i].Value; break;
				//case "nodClLv": nodeWarClearLevel = playerStatistics[i].Value; break;
				//case "chaosFragment": chaosFragmentCount = playerStatistics[i].Value; break;
				//case "chtRnkSus": cheatRankSus = playerStatistics[i].Value; break;
			}
		}
	}

	public void OnRecvPlayerData(Dictionary<string, UserDataRecord> userData, Dictionary<string, UserDataRecord> userReadOnlyData, List<CharacterResult> characterList, PlayerProfileModel playerProfile)
	{
		// 값이 존재하지 않다면 최고 클리어 스테이지값인 0 보다 1 큰 값일테니 1로 초기화 해둬야한다.
		selectedStage = 1;
		if (userReadOnlyData.ContainsKey("selectedStage"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["selectedStage"].Value, out intValue))
				selectedStage = intValue;
			if (selectedStage > (highestClearStage + 1))
			{
				selectedStage = highestClearStage + 1;
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidSelectedChapter);
			}
		}

		/*
		if (userData.ContainsKey("mainCharacterId"))
		{
			string actorId = userData["mainCharacterId"].Value;
			bool find = false;
			for (int i = 0; i < characterList.Count; ++i)
			{
				if (characterList[i].CharacterName == actorId)
				{
					find = true;
					break;
				}
			}
			if (find)
				_mainCharacterId = actorId;
			else
			{
				_mainCharacterId = "Actor0201";
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidMainCharacter);
			}
		}

		if (userData.ContainsKey("selectedChapter"))
		{
			int intValue = 0;
			if (int.TryParse(userData["selectedChapter"].Value, out intValue))
				selectedChapter = intValue;
			if (selectedChapter > highestPlayChapter)
			{
				selectedChapter = highestPlayChapter;
				PlayFabApiManager.instance.RequestIncCliSus(ClientSuspect.eClientSuspectCode.InvalidSelectedChapter);
			}
		}

		// 만약 디비에 정보가 없을 수 있다면(나중에 추가됐거나 하는 이유 등등) 이렇게 직접 초기화 하는게 안전하다.
		// 이 SHcha값은 항상 들어있을테지만 샘플로 이렇게 초기화 하는 형태를 보여주기 위해 남겨둔다.
		chaosMode = false;
		if (userData.ContainsKey("SHcha"))
		{
			int intValue = 0;
			if (int.TryParse(userData["SHcha"].Value, out intValue))
				chaosMode = (intValue == 1);
		}
		*/

		termsConfirmed = false;
		if (userReadOnlyData.ContainsKey("termsDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["termsDat"].Value) == false)
				termsConfirmed = true;
		}

		/*
		ContentsData.instance.OnRecvContentsData(userData, userReadOnlyData);

		displayName = "";
		if (string.IsNullOrEmpty(playerProfile.DisplayName) == false)
			displayName = playerProfile.DisplayName;
		*/

		newlyCreated = false;
		loginned = true;
	}

	public void OnSubLevelUp(int subLevelIndex)
	{
		if (subLevelIndex < _listSubLevel.Count)
			_listSubLevel[subLevelIndex] += 1;

		// 캐릭터 데이터가 변경되면 이걸 사용하는 PlayerActor의 ActorStatus도 새로 스탯을 계산해야한다.
		PlayerActor playerActor = BattleInstanceManager.instance.playerActor;
		if (playerActor != null)
			playerActor.actorStatus.InitializeActorStatus();
	}

	public void OnLevelUp()
	{
		playerLevel += 1;
		for (int i = 0; i < _listSubLevel.Count; ++i)
			_listSubLevel[i] = 0;

		// 캐릭터 데이터가 변경되면 이걸 사용하는 PlayerActor의 ActorStatus도 새로 스탯을 계산해야한다.
		PlayerActor playerActor = BattleInstanceManager.instance.playerActor;
		if (playerActor != null)
			playerActor.actorStatus.InitializeActorStatus();
	}
}