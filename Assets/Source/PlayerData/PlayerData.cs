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
	public ObscuredInt cheatRankSus { get; set; }

	#region Player Property
	public ObscuredInt playerLevel { get; set; }
	public ObscuredInt subLevel { get; set; }
	#endregion

	// 클라가 들고있는 날짜 갱신 타임.
	public DateTime dayRefreshTime { get; private set; }

	// 서버 테이블 갱신시간. 플레이어 데이터와 상관없이 하루 단위로 받아서 갱신하는거다.
	public DateTime serverTableRefreshTime { get; private set; }

	// 이용약관 확인용 변수. 값이 있으면 기록된거로 간주하고 true로 해둔다.
	public ObscuredBool termsConfirmed { get; set; }

	// 대용량 다운로드를 허용했는지. 한번 허용하면 true로 해둔다.
	public ObscuredBool downloadConfirmed { get; set; }
	public ObscuredBool downloadRewarded { get; set; }

	// 디스플레이 네임
	public string displayName { get; set; }

	// Vtd
	public int vtd { get; set; }

	// 네트워크 오류로 인해 씬을 재시작할때는 타이틀 떠서 진입하듯 초기 프로세스들을 검사해야한다.
	public bool checkRestartScene { get; set; }

	void Update()
	{
		UpdateDayRefreshTime();
		UpdateServerTableRefreshTime();
	}

	void UpdateDayRefreshTime()
	{
		if (loginned == false)
			return;

		if (DateTime.Compare(ServerTime.UtcNow, dayRefreshTime) < 0)
			return;

		dayRefreshTime += TimeSpan.FromDays(1);

		// 날짜 변경이 되었음을 알린다.
		MissionData.instance.OnRefreshDay();
		SubMissionData.instance.OnRefreshDay();
		FestivalData.instance.OnRefreshDay();
		PetManager.instance.OnRefreshDay();
		AttendanceData.instance.OnRefreshDay();
	}

	List<string> _listTitleKey;
	void UpdateServerTableRefreshTime()
	{
		if (loginned == false)
			return;

		/*
		if (_listDailyShopSlotInfo == null)
			return;
		*/

		if (DateTime.Compare(ServerTime.UtcNow, serverTableRefreshTime) < 0)
			return;

		// 상품의 구매 여부와 상관없이 무조건 갱신해야한다.
		serverTableRefreshTime += TimeSpan.FromDays(1);

		if (_listTitleKey == null)
		{
			_listTitleKey = new List<string>();
			//_listTitleKey.Add("daShp");
			//_listTitleKey.Add("daFre");

			// 데일리 샵 관련 테이블 말고도 아래에서 추가로 받아야하는 것들도 포함시켜야한다.
			//_listTitleKey.Add("lvRst");
			//_listTitleKey.Add("mcLst");
			//_listTitleKey.Add("rnkSt");
			//_listTitleKey.Add("rnkBan");
		}

		// 패킷을 보내서 새 정보를 받아와야한다.
		PlayFabApiManager.instance.RequestGetTitleData(_listTitleKey, (dicData) =>
		{
			// 새 테이블로 갱신하면 된다.
			OnRecvServerTableData(dicData);

			// 이땐 절대 구매 내역을 초기화 하면 안된다. 이 타이밍은 날짜 갱신 5분전에 상점 리스트를 새로 받는거라 구매 내역은 실제로 날짜가 갱신되는 타이밍에 해야한다.


			// 테이블 받는 로직이 이미 구현되어있어서 레벨팩 리셋 타이머도 같이 처리해주기로 한다.
			//OnRecvLevelPackageResetInfo(dicData);

			// 머셔너리 데이터도 같이 포함
			//MercenaryData.instance.OnRecvMercenaryData(dicData, true);

			// 랭킹 예비 추가삭제 리스트도 포함
			//RankingData.instance.OnRecvRankingData(dicData);
		});
	}

	public void OnNewlyCreatedPlayer()
	{
		OnRecvPlayerStatistics(null);

		selectedStage = 1;
		cheatRankSus = 0;
		termsConfirmed = false;
		downloadConfirmed = false;
		downloadRewarded = false;
		displayName = "";
		vtd = 0;


		// 계정 시작시 패킷 오류가 생기면 복구할 방법이 없기 때문에 여기서 처리하지 않기로 하고 Update돌면서 확인하기로 한다.
		//AnalysisData.instance.OnNewlyCreatedPlayer();

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

	public void OnRecvServerTableData(Dictionary<string, string> titleData)
	{
		// 일일 상점같은 서버 테이블을 매일 받아두기 위해 만든 함수.
		// 특이한건 딱 날짜 넘어가는 타이밍에 받으면 잠깐 데이터가 틀어질 수 있기 때문에 3분전에 미리 받는거로 해둔다.
		// 이럼 다음날에 되자마자 바로 갱신하는데 쓰일 수 있다.
		// 사실 당일 데이터를 바꿔놨다면 저 3분 사이에 다른템이 나올 수 있다는건데
		// 이런식으로 당일 데이터를 바꾸는 일은 없을테니까 할 수 있는 방식이다.
		serverTableRefreshTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1) - TimeSpan.FromMinutes(3);

		// 그런데 만약 서버 리셋타임 5분도 안남기고 접속한거라면 괜히 또 받아질테니 리셋 타임과 비교해봐서 하루를 밀어둔다.
		if (DateTime.Compare(ServerTime.UtcNow, serverTableRefreshTime) < 0)
		{
		}
		else
			serverTableRefreshTime += TimeSpan.FromDays(1);

		// 날짜 변경 감지
		dayRefreshTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1);

		/*
		_listDailyShopSlotInfo = null;
		_listDailyFreeItemInfo = null;

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

		// 일일상점 데이터
		if (titleData.ContainsKey("daShp"))
			_listDailyShopSlotInfo = serializer.DeserializeObject<List<DailyShopSlotInfo>>(titleData["daShp"]);

		// 일일 무료 아이템
		if (titleData.ContainsKey("daFre"))
			_listDailyFreeItemInfo = serializer.DeserializeObject<List<DailyFreeItemInfo>>(titleData["daFre"]);
		*/
	}

	public void OnRecvPlayerStatistics(List<StatisticValue> playerStatistics)
	{
		// 통계는 없을 수 있는 값이니 초기화는 필수
		highestClearStage = 0;

		// 레벨은 1로 시작하는게 맞다.
		playerLevel = 1;
		subLevel = 0;

		if (playerStatistics == null)
			return;

		// confirm
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			switch (playerStatistics[i].StatisticName)
			{
				//case "highestPlayChapter": highestPlayChapter = playerStatistics[i].Value; break;
				case "highestClearStage": highestClearStage = playerStatistics[i].Value; break;
				case "playerLevel": playerLevel = playerStatistics[i].Value; break;
				case "playerSubLevel": subLevel = playerStatistics[i].Value; break;
				case "chtRnkSus": cheatRankSus = playerStatistics[i].Value; break;
				//case "highestValue": highestValue = playerStatistics[i].Value; break;
				//case "nodClLv": nodeWarClearLevel = playerStatistics[i].Value; break;
				//case "chaosFragment": chaosFragmentCount = playerStatistics[i].Value; break;
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

		downloadConfirmed = false;
		downloadRewarded = false;
		if (userReadOnlyData.ContainsKey("downloadConfirm"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["downloadConfirm"].Value) == false)
			{
				downloadConfirmed = true;
				if (userReadOnlyData["downloadConfirm"].Value == "2")
					downloadRewarded = true;
			}	
		}

		/*
		ContentsData.instance.OnRecvContentsData(userData, userReadOnlyData);
		*/

		displayName = "";
		if (string.IsNullOrEmpty(playerProfile.DisplayName) == false)
			displayName = playerProfile.DisplayName;

		vtd = 0;
		if (playerProfile.TotalValueToDateInUSD != null)
			vtd = (int)playerProfile.TotalValueToDateInUSD;

		if (userReadOnlyData.ContainsKey("delAccDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["delAccDat"].Value) == false)
			{
				// 통계랑 항상 쌍으로 기록하기때문에 readOnlyData 한쪽만 검사해도 무방하다
				// 여기 값이 들어있으면 
				PlayFabApiManager.instance.RequestDeleteAccount(true, null);
			}
		}

		newlyCreated = false;
		loginned = true;
	}

	public void OnSubLevelUp()
	{
		subLevel += 1;

		OnChangedStatus();
	}

	public void OnLevelUp()
	{
		playerLevel += 1;
		subLevel = 0;

		MainCanvas.instance.RefreshCashButton();
		OnChangedStatus();
	}

	public void OnChangedStatus()
	{
		// 캐릭터 데이터가 변경되면 이걸 사용하는 PlayerActor의 ActorStatus도 새로 스탯을 계산해야한다.
		PlayerActor playerActor = BattleInstanceManager.instance.playerActor;
		if (playerActor != null)
			playerActor.actorStatus.InitializeActorStatus();

		TeamManager.instance.InitializeActorStatus();

		if (StageFloorInfoCanvas.instance != null)
			StageFloorInfoCanvas.instance.RefreshCombatPower();

		RecordHighestBattlePower();
	}

	bool _waitPacket = false;
	void RecordHighestBattlePower()
	{
		if (_waitPacket)
			return;

		string stringValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower).ToString("N0");
		stringValue = stringValue.Replace(",", "");
		int intValue = 0;
		int.TryParse(stringValue, out intValue);
		if (intValue > RankingData.instance.highestBattlePower)
		{
			_waitPacket = true;
			PlayFabApiManager.instance.RequestRecordBattlePower(intValue, () =>
			{
				_waitPacket = false;
			});
		}
	}

	public bool CheckConfirmDownload()
	{
		if (downloadConfirmed)
			return true;
		UIInstanceManager.instance.ShowCanvasAsync("DownloadConfirmCanvas", null);
		return false;
	}
}