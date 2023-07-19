//#define USE_TITLE_PLAYER_ENTITY
//#define USE_CHARACTER_ENTITY

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.DataModels;
using ClientSuspect;

#region GrantItem
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
#endregion

#region RevokeItem
public class RevokeInventoryItemRequest
{
	public string ItemInstanceId;
}
#endregion

public class PlayFabApiManager : MonoBehaviour
{
	public static PlayFabApiManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("PlayFabApiManager")).AddComponent<PlayFabApiManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static PlayFabApiManager _instance = null;

	public string playFabId { get { return _playFabId; } }
	ObscuredString _playFabId;
#if USE_TITLE_PLAYER_ENTITY
	PlayFab.DataModels.EntityKey _titlePlayerEntityKey;
#endif

	void Update()
	{
		UpdateCliSusQueue();
		UpdateServerUtc();
	}

	// 네트워크 함수의 특징인데
	// 로그인이나 로그인 직후 받는 플레이어 데이터(인벤부터 캐릭터 리스트 등등) 등에는
	// 보통 UI의 인풋-아웃풋 처리로 되는게 아니라서 콜백이 필요없지만
	// UI에서 진행되는 요청들(캐릭변경, 강화, 장착 등등)에는 거의 대부분 콜백이 필요하게 된다.
	// 
	// 이거와 비슷하게
	// 몇몇 항목들은 재전송이 필요하지만(메인 캐릭터 교체, 인게임 결과 반영)
	// 재화를 소비하는 항목들은 재전송하기엔 두번 재화가 나가서 위험할때가 많다.(구매, 하트소모 등등)
	// 그래서 RetrySendManager는 모든 항목에 붙이는 대신 필요한 곳에만 적용하기로 한다.

	#region Time Record
	Dictionary<string, float> _dicTimeRecord = new Dictionary<string, float>();
	public void StartTimeRecord(string recordId)
	{
		if (_dicTimeRecord.ContainsKey(recordId))
			_dicTimeRecord[recordId] = Time.time;
		else
			_dicTimeRecord.Add(recordId, Time.time);
	}

	public void EndTimeRecord(string recordId)
	{
		if (_dicTimeRecord.ContainsKey(recordId) == false)
			return;

		float deltaTime = Time.time - _dicTimeRecord[recordId];
		Debug.LogFormat("Packet Delay - {0} : {1:0.###}", recordId, deltaTime);
	}
	#endregion

	void HandleCommonError(PlayFabError error)
	{
		Debug.LogError(error.GenerateErrorReport());

		WaitingNetworkCanvas.Show(false);

		switch (error.Error)
		{
			case PlayFabErrorCode.InsufficientFunds:
				PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
				{
					FunctionName = "IncCliSus",
					FunctionParameter = new { Er = (int)error.Error, Pa1 = 0, Pa2 = 0 },
					GeneratePlayStreamEvent = true
				}, null, null);
				break;
			case PlayFabErrorCode.NotAuthenticated:
				OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_LoseSession"), () =>
				{
					// NotAuthenticated 뜨면 다시 로그인해야만 한다.
					PlayerData.instance.ResetData();
					SceneManager.LoadScene(0);
				});
				return;
		}

		if (error.Error == PlayFabErrorCode.ServiceUnavailable || error.Error == PlayFabErrorCode.DownstreamServiceUnavailable || error.Error == PlayFabErrorCode.APIRequestLimitExceeded || error.HttpCode == 400)
		{
			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("SystemUI_DisconnectServer"), () =>
			{
				// 모든 정보를 다시 받아야하기 때문에 로그인부터 하는게 맞다.
				PlayerData.instance.ResetData();
				SceneManager.LoadScene(0);
			}, 100);
		}
	}

	// PlayFabError가 아닌 상황에서도 튕겨내기 위한 함수
	public void HandleCommonError()
	{
		WaitingNetworkCanvas.Show(false);

		OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("SystemUI_DisconnectServer"), () =>
		{
			PlayerData.instance.ResetData();
			SceneManager.LoadScene(0);
		}, 100);
	}

	bool enableCliSusQueue { get; set; }
	int lastSendFrameCount { get; set; }
	public void RequestIncCliSus(eClientSuspectCode clientSuspectCode, bool sendBattleInfo = false, int param2 = 0)
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "IncCliSus",
			FunctionParameter = new { Er = (int)clientSuspectCode, Pa1 = 0, Pa2 = param2 },
			GeneratePlayStreamEvent = true
		}, null, (errorCallback) =>
		{
			HandleCliSusError(errorCallback, clientSuspectCode);
		});

		if (enableCliSusQueue && !sendBattleInfo)
			lastSendFrameCount = Time.frameCount;
	}

	void HandleCliSusError(PlayFabError errorCallback, eClientSuspectCode clientSuspectCode)
	{
		switch (clientSuspectCode)
		{
			case eClientSuspectCode.OneShotKillBoss:
				HandleCommonError(errorCallback);
				break;
		}
	}

	struct sCliSusInfo
	{
		public int code;
		public int param1;
		public int param2;
	}
	Queue<sCliSusInfo> _queueCliSusInfo;
	const float SendCliSusQueueDelay = 0.333f;
	float _cliSusSendRemainTime;
	void UpdateCliSusQueue()
	{
		if (_queueCliSusInfo == null)
			return;
		if (_queueCliSusInfo.Count == 0)
			return;

		_cliSusSendRemainTime -= Time.deltaTime;
		if (_cliSusSendRemainTime < 0.0f)
		{
			sCliSusInfo info = _queueCliSusInfo.Dequeue();

			PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
			{
				FunctionName = "IncCliSus",
				FunctionParameter = new { Er = info.code, Pa1 = info.param1, Pa2 = info.param2 },
				GeneratePlayStreamEvent = true
			}, null, (errorCallback) =>
			{
				HandleCliSusError(errorCallback, (eClientSuspectCode)info.code);
			});

			_cliSusSendRemainTime += SendCliSusQueueDelay;
		}
	}

	void ClearCliSusQueue()
	{
		if (_queueCliSusInfo == null)
			return;
		_queueCliSusInfo.Clear();
	}

	public static string CheckSum(string input)
	{
		int chk = 0x68319547;
		int length = input.Length;
		for (int i = 0; i < length; ++i)
		{
			chk += (Convert.ToInt32((int)input[i]) * (i + 1));
		}
		return Convert.ToString((chk & 0xffffffff), 16);
	}

	#region Login with PlayerData, Entity Objects
	// 자주 사용되는 걸 UserData로 보냈더니 15초당 10개 제한에 걸려서 위험하기도 해서
	// 차라리 Entity Objects를 사용하기로 한다.
	// 그런데 Entity Objects는 로그인의 추가 데이터로 받을 수 없기 때문에
	// 로그인 즉시 요청함수를 날릴거고
	// 어차피 이때 추가로 날리는 겸 해서 캐릭터 커스텀 데이터도 같이 받는게 좋을거 같아서
	// 각종 필요한 모든 Entity Objects들을 요청해두기로 한다.
	int _requestCountForGetPlayerData = 0;
	LoginResult _loginResult;
	List<StatisticValue> _additional1PlayerStatistics;
	List<StatisticValue> _additional2PlayerStatistics;
#if USE_TITLE_PLAYER_ENTITY
	GetObjectsResponse _titlePlayerEntityObject;
#endif
	Dictionary<string, GetCharacterStatisticsResult> _dicCharacterStatisticsResult = new Dictionary<string, GetCharacterStatisticsResult>();
#if USE_CHARACTER_ENTITY
	List<ObjectResult> _listCharacterEntityObject = new List<ObjectResult>();
#endif
	public void OnRecvLoginResult(LoginResult loginResult)
	{
		_playFabId = loginResult.PlayFabId;
		_loginResult = loginResult;

#if USE_TITLE_PLAYER_ENTITY
		_titlePlayerEntityKey = new PlayFab.DataModels.EntityKey { Id = loginResult.EntityToken.Entity.Id, Type = loginResult.EntityToken.Entity.Type };
#endif

		// 로그인 처리를 진행하기 전에 서버상태라던가 버전정보를 확인한다.
		if (CheckServerMaintenance(loginResult.InfoResultPayload.TitleData))
			return;

		bool needCheckResourceVersion = false;
		if (CheckVersion(loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.UserReadOnlyData, out needCheckResourceVersion) == false)
			return;

		// 리소스 체크를 해야하는 상황에서만 번들 체크를 한다.
		if (needCheckResourceVersion)
		{
			DownloadManager.instance.CheckDownloadProcess();
		}
		else
			OnLogin();
	}

	public void OnLogin()
	{
		LoginResult loginResult = _loginResult;

		ApplyGlobalTable(loginResult.InfoResultPayload.TitleData);
		AuthManager.instance.OnRecvAccountInfo(loginResult.InfoResultPayload.AccountInfo);
		CurrencyData.instance.OnRecvCurrencyData(loginResult.InfoResultPayload.UserVirtualCurrency, loginResult.InfoResultPayload.UserVirtualCurrencyRechargeTimes, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics, loginResult.InfoResultPayload.TitleData);
		MailData.instance.OnRecvMailData(loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics, loginResult.NewlyCreated);
		SupportData.instance.OnRecvSupportData(loginResult.InfoResultPayload.UserReadOnlyData);
		CashShopData.instance.OnRecvCashShopData(loginResult.InfoResultPayload.UserInventory, loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		PlayerData.instance.OnRecvServerTableData(loginResult.InfoResultPayload.TitleData);
		AnalysisData.instance.OnRecvAnalysisData(loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		GuideQuestData.instance.OnRecvGuideQuestData(loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		SubQuestData.instance.OnRecvQuestData(loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		MissionData.instance.OnRecvMissionData(loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		SubMissionData.instance.OnRecvSubMissionData(loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		FestivalData.instance.OnRecvFestivalData(loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		AttendanceData.instance.OnRecvAttendanceData(loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		RankingData.instance.OnRecvRankingData(loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);

		// PlayerData 만 다 받고 처리하고 다른 인벤이나 스펠은 여기서 처리한다.
		SpellManager.instance.OnRecvSpellInventory(loginResult.InfoResultPayload.UserInventory, loginResult.InfoResultPayload.UserData, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		CostumeManager.instance.OnRecvCostumeInventory(loginResult.InfoResultPayload.UserInventory, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		CharacterManager.instance.OnRecvCharacterInventory(loginResult.InfoResultPayload.UserInventory, loginResult.InfoResultPayload.UserData, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		PetManager.instance.OnRecvPetInventory(loginResult.InfoResultPayload.UserInventory, loginResult.InfoResultPayload.UserData, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		EquipManager.instance.OnRecvEquipInventory(loginResult.InfoResultPayload.UserInventory, loginResult.InfoResultPayload.UserData, loginResult.InfoResultPayload.UserReadOnlyData);
		PassManager.instance.OnRecvPassData(loginResult.InfoResultPayload.UserInventory, loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);

		/*
		DailyShopData.instance.OnRecvShopData(loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.UserReadOnlyData);		
		PlayerData.instance.OnRecvLevelPackageResetInfo(loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.NewlyCreated);
		CumulativeEventData.instance.OnRecvCumulativeEventData(loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics, loginResult.NewlyCreated);
		*/

		if (loginResult.NewlyCreated)
		{
			// 이때도 서버 utcTime을 받아와야하긴 하는데 서버응답 기다리는거 없이 백그라운드에서 진행하기로 한다.
			_waitOnlyServerUtc = true;
			_getServerUtcSendTime = Time.time;
			PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
			{
				FunctionName = "GetServerUtc",
			}, OnGetServerUtc, OnRecvPlayerDataFailure);

			// 처음 만든 계정이면 어차피 읽어올게 없다.
			// 전작과 달리 서버에서 rules도 안쓸테니까 초기화 후 씬 구성하면 될거다.
			PlayerData.instance.OnNewlyCreatedPlayer();

			// 더이상 쓰이는 곳 없다.
			_loginResult = null;
			return;
		}

		_loginResult = loginResult;
		_dicCharacterStatisticsResult.Clear();
#if USE_CHARACTER_ENTITY
		_listCharacterEntityObject.Clear();
#endif

		StartTimeRecord("PlayerData");

#if USE_TITLE_PLAYER_ENTITY
		// 최초 생성 이후부터는 로그인 했을때 값이 들어있을테니 읽어와서 처리한다.
		// limit을 보다보니 Player Entity쓰는거보다 UserData갱신하는게 훨씬 더 편하고 개별로 다 조각조각 내도 되서 안쓰기로 한다.
		GetObjectsRequest getObjectsRequest = new GetObjectsRequest { Entity = _titlePlayerEntityKey };
		PlayFabDataAPI.GetObjects(getObjectsRequest, OnGetObjects, OnRecvPlayerDataFailure);
		++_requestCountForGetPlayerData;
#endif

		// 이것 저것 더 요청할 수 있다. 지금 필요한건 캐릭터마다의 엔티티다. 이게 있어야 캐릭터의 파워레벨을 받을 수 있다.
		for (int i = 0; i < loginResult.InfoResultPayload.CharacterList.Count; ++i)
		{
			string characterId = loginResult.InfoResultPayload.CharacterList[i].CharacterId;
			GetCharacterStatisticsRequest getCharacterStatisticsRequest = new GetCharacterStatisticsRequest { CharacterId = characterId };
			PlayFabClientAPI.GetCharacterStatistics(getCharacterStatisticsRequest, OnGetCharacterStatistics, OnRecvPlayerDataFailure);
			++_requestCountForGetPlayerData;
#if USE_CHARACTER_ENTITY
			GetObjectsRequest getCharacterEntityRequest = new GetObjectsRequest { Entity = new PlayFab.DataModels.EntityKey { Id = characterId, Type = "character" } };
			PlayFabDataAPI.GetObjects(getCharacterEntityRequest, OnGetObjectsCharacter, OnRecvPlayerDataFailure);
			++_requestCountForGetPlayerData;
#endif
		}

		// 로그인할때 한번에 요청할 수 있는 통계 개수가 최대 25라서 이렇게 별도로 요청해서 담아두기로 한다.
		List<string> playerStatisticNames = new List<string>();
		for (int i = 0; i < 25; ++i)
			playerStatisticNames.Add(string.Format("zzHeart_{0}", TableDataManager.instance.petTable.dataArray[i].petId));
		PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest()
		{
			StatisticNames = playerStatisticNames,
		}, OnGetAdditional1PlayerStatistics, OnRecvPlayerDataFailure);
		++_requestCountForGetPlayerData;
		playerStatisticNames.Clear();
		for (int i = 25; i < TableDataManager.instance.petTable.dataArray.Length; ++i)
			playerStatisticNames.Add(string.Format("zzHeart_{0}", TableDataManager.instance.petTable.dataArray[i].petId));
		PlayFabClientAPI.GetPlayerStatistics(new GetPlayerStatisticsRequest()
		{
			StatisticNames = playerStatisticNames,
		}, OnGetAdditional2PlayerStatistics, OnRecvPlayerDataFailure);
		++_requestCountForGetPlayerData;

		// 서버의 utcTime도 받아두기로 한다. 그래서 클라가 가지고 있는 utcTime과 차이를 구해놓고 이후 클라의 utcNow를 얻을때 보정에 쓰도록 한다.
		_getServerUtcSendTime = Time.time;
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetServerUtc",
			GeneratePlayStreamEvent = true,
		}, OnGetServerUtc, OnRecvPlayerDataFailure);
		++_requestCountForGetPlayerData;
	}

	void ApplyGlobalTable(Dictionary<string, string> titleData)
	{
		// 이미 이 시점에서 TableDataManager의 내용물은 어드레서블로 받은 데이터를 로드한 상태일거다.
		// 그러니 로그인 즉시 덮어씌우면 서버값을 사용하게 될거다.
		if (titleData.ContainsKey("int"))
		{
			string tableDataString = titleData["int"];
			if (string.IsNullOrEmpty(tableDataString) == false)
			{
				var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
				Dictionary<string, int> globalConstantIntServerTable = serializer.DeserializeObject<Dictionary<string, int>>(tableDataString);
				Dictionary<string, int>.Enumerator e = globalConstantIntServerTable.GetEnumerator();
				GlobalConstantIntTable table = TableDataManager.instance.globalConstantIntTable;
				while (e.MoveNext())
				{
					string key = e.Current.Key;
					for (int i = 0; i < table.dataArray.Length; ++i)
					{
						if (table.dataArray[i].id == key)
						{
							table.dataArray[i].value = e.Current.Value;
							break;
						}
					}
				}
			}
		}
	}

#if USE_TITLE_PLAYER_ENTITY
	void OnGetObjects(GetObjectsResponse result)
	{
		_titlePlayerEntityObject = result;
		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}
#endif

	void OnGetCharacterStatistics(GetCharacterStatisticsResult result)
	{
		string characterId = "";
		GetCharacterStatisticsRequest getCharacterStatisticsRequest = result.Request as GetCharacterStatisticsRequest;
		if (getCharacterStatisticsRequest != null)
			characterId = getCharacterStatisticsRequest.CharacterId;

		if (string.IsNullOrEmpty(characterId) == false)
			_dicCharacterStatisticsResult.Add(characterId, result);

		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}

	void OnGetAdditional1PlayerStatistics(GetPlayerStatisticsResult result)
	{
		_additional1PlayerStatistics = result.Statistics;

		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}

	void OnGetAdditional2PlayerStatistics(GetPlayerStatisticsResult result)
	{
		_additional2PlayerStatistics = result.Statistics;

		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}

#if USE_CHARACTER_ENTITY
	void OnGetObjectsCharacter(GetObjectsResponse result)
	{
		Dictionary<string, ObjectResult>.Enumerator e = result.Objects.GetEnumerator();
		ObjectResult objectResult = null;
		while (e.MoveNext())
		{
			// 분명 첫번째 EntityObjects에 Actor0201 처럼 캐릭 본인의 아이디로 된 key value가 들어있을거다. "Actor"스트링으로 검사해서 추출해낸다.
			if (e.Current.Key.Contains("Actor"))
			{
				objectResult = e.Current.Value;
				break;
			}
		}

		if (objectResult != null)
			_listCharacterEntityObject.Add(objectResult);

		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}
#endif

	bool _waitOnlyServerUtc = false;
	float _getServerUtcSendTime;
	TimeSpan _timeSpanForServerUtc;
	void OnGetServerUtc(ExecuteCloudScriptResult success)
	{
		PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
		jsonResult.TryGetValue("date", out object date);
		jsonResult.TryGetValue("ms", out object ms);

		DateTime serverUtcTime = new DateTime();
		if (DateTime.TryParse((string)date, out serverUtcTime))
		{
			double millisecond = 0.0;
			double.TryParse(ms.ToString(), out millisecond);
			serverUtcTime = serverUtcTime.AddMilliseconds(millisecond);

			DateTime universalTime = serverUtcTime.ToUniversalTime();
			// 클라 시간을 변경했으면 DateTime.UtcNow도 달라지기 때문에 그냥 믿으면 안된다. 서버 타임이랑 비교해서 차이값을 기록해둔다.
			// DateTime.UtcNow에다가 offset을 더해서 예측하는 방식이므로 universalTime에서 DateTime.UtcNow를 빼서 기록해둔다.
			// 정확하게는 클라가 고친 시간 오프셋값에다가 서버에서 클라까지 오는 패킷 딜레이까지 포함된 시간이다.
			_timeSpanForServerUtc = DateTime.UtcNow - universalTime;
			_serverUtcRefreshTime = GetServerUtcTime() + TimeSpan.FromMinutes(ServerRefreshFastDelay);

			// for latency
			// 원래는 latency 보정용으로 하려고 했는데, 패킷의 가는 시간이 길고 오는 시간이 짧아지면
			// 클라가 생각하는 서버가 실제 서버타임보다 빨라질 수 있다.
			// 이 경우 요청하지 말아야하는데 요청하는 경우가 생기므로 sus를 믿을 수 없게 된다. 그러니 아예 보정처리는 하지 않기로 한다.
			//_timeSpanForServerUtc += TimeSpan.FromSeconds((Time.time - _getServerUtcSendTime) * 0.5f);
		}
		if (_waitOnlyServerUtc)
		{
			_waitOnlyServerUtc = false;
			return;
		}

		--_requestCountForGetPlayerData;
		CheckCompleteRecvPlayerData();
	}

	public DateTime GetServerUtcTime()
	{
		return DateTime.UtcNow - _timeSpanForServerUtc;
	}

	#region Refresh ServerUtc
	// 위에서 이어지는 내용이긴 한데
	// _timeSpanForServerUtc 값이 실상은 서버에서 클라까지 오는 패킷 딜레이를 포함하다보니 로그인때 하필 이 오차가 크게 저장될 경우
	// 이후 GetServerUtcTime() 값을 비교해서 서버에 요청할때 시간이 틀어질 수 있다는걸 의미했다.
	// 그래서 이 오차를 최대한 줄이기 위해 중간중간 패킷을 다시 보내서 서버와의 오차가 가장 적어지도록 갱신하기로 한다.
	// 초반 10회는 2분 간격으로 보내고 그 이후부터는 5분 간격으로 보낸다.
	// 이건 계정 전환을 해도 리셋할 필요가 없으니 그냥 앱이 가동되고 나서부터 제일 오차가 적은 값을 사용하면 된다.
	DateTime _serverUtcRefreshTime;
	const int ServerRefreshFastDelay = 1;
	const int ServerRefreshDelay = 5;
	int _serverUtcRefreshFastRemainCount = 10;
	void UpdateServerUtc()
	{
		if (PlayerData.instance.loginned == false)
			return;

		if (DateTime.Compare(GetServerUtcTime(), _serverUtcRefreshTime) < 0)
			return;

		int minutes = ServerRefreshDelay;
		if (_serverUtcRefreshFastRemainCount > 0)
		{
			minutes = ServerRefreshFastDelay;
			--_serverUtcRefreshFastRemainCount;
		}
		_serverUtcRefreshTime = GetServerUtcTime() + TimeSpan.FromMinutes(minutes);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetServerUtc",
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("date", out object date);
			jsonResult.TryGetValue("ms", out object ms);

			DateTime serverUtcTime = new DateTime();
			if (DateTime.TryParse((string)date, out serverUtcTime))
			{
				double millisecond = 0.0;
				double.TryParse(ms.ToString(), out millisecond);
				serverUtcTime = serverUtcTime.AddMilliseconds(millisecond);

				DateTime universalTime = serverUtcTime.ToUniversalTime();

				// 위의 파싱은 로그인때 했던거와 같지만 갱신할때는 이전에 저장된거와 비교해서 패킷 딜레이가 더 짧아질때만 적용하면 된다.
				// 패킷 딜레이가 짧아질수록 TimeSpan값이 커지기 때문에 아래와 같이 클때 덮으면 된다.
				// 클라가 타임을 변조해서 느리게 하든 빠르게 하든 동일하다.
				TimeSpan timeSpanForServerUtc = DateTime.UtcNow - universalTime;
				if (timeSpanForServerUtc < _timeSpanForServerUtc)
				{
					_timeSpanForServerUtc = timeSpanForServerUtc;
					Debug.LogFormat("ServerUtc TimeSpan : {0}", _timeSpanForServerUtc.TotalMilliseconds);
				}
			}
		}, (error) =>
		{
			// 주기적으로 보내는거라 에러 핸들링 하면 안된다.
			//HandleCommonError(error);
		});
	}
	#endregion

	bool CheckServerMaintenance(Dictionary<string, string> titleData)
	{
		if (titleData.ContainsKey("down") && string.IsNullOrEmpty(titleData["down"]) == false)
		{
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			Dictionary<string, string> dicInfo = serializer.DeserializeObject<Dictionary<string, string>>(titleData["down"]);
			if (dicInfo.ContainsKey("0") && dicInfo["0"] == "1" && dicInfo.Count >= 3)
			{
				DateTime startTime = new DateTime();
				DateTime endTime = new DateTime();
				if (DateTime.TryParse(dicInfo["1"], out startTime) && DateTime.TryParse(dicInfo["2"], out endTime))
				{
					DateTime universalStartTime = startTime.ToUniversalTime();
					DateTime universalEndTime = endTime.ToUniversalTime();
					if (universalStartTime < ServerTime.UtcNow && ServerTime.UtcNow < universalEndTime)
					{
						DateTime localStartTime = startTime.ToLocalTime();
						DateTime localEndTime = endTime.ToLocalTime();
						string startArgment = string.Format("{0:00}:{1:00}", localStartTime.Hour, localStartTime.Minute);
						string endArgment = string.Format("{0:00}:{1:00}", localEndTime.Hour, localEndTime.Minute);
						StartCoroutine(AuthManager.instance.RestartProcess(null, false, "SystemUI_ServerDown", startArgment, endArgment));
						return true;
					}
				}
			}
		}
		return false;
	}

	int _marketCanvasShowCount = 0;
	bool CheckVersion(Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData, out bool needCheckResourceVersion)
	{
		needCheckResourceVersion = false;

		// 리소스 체크 전에 빌드 버전부터 확인해본다. 이래야 리소스 다운로드 전 버전에서도 새 빌드가 나왔는지 확인할 수 있다.
		// 빌드번호를 서버에 적혀있는 빌드번호와 비교해야한다.
		BuildVersionInfo versionInfo = null;
#if UNITY_ANDROID
		versionInfo = Resources.Load<BuildVersionInfo>("Build/BuildVersionInfo_Android");
#elif UNITY_IOS
		versionInfo = Resources.Load<BuildVersionInfo>("Build/BuildVersionInfo_iOS");
#endif
		Debug.LogFormat("Build Version _.{0}.{1}", versionInfo.updateVersion, versionInfo.addressableVersion);
		if (titleData.ContainsKey(versionInfo.serverKeyName) && string.IsNullOrEmpty(titleData[versionInfo.serverKeyName]) == false)
		{
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			int serverVersion = 0;
			int.TryParse(titleData[versionInfo.serverKeyName], out serverVersion);
			if (versionInfo.updateVersion < serverVersion)
			{
				bool useOkBigCanvas = false;
				string updateMessageId = "SystemUI_NeedUpdate";
				if (_marketCanvasShowCount > 0)
				{
					useOkBigCanvas = true;
#if UNITY_ANDROID
					updateMessageId = "SystemUI_NeedUpdateRetryAnd";
#elif UNITY_IOS
					updateMessageId = "SystemUI_NeedUpdateRetryiOS";
#endif
				}

				// 업데이트가 있음을 알려야한다.
				StartCoroutine(AuthManager.instance.RestartProcess(() =>
				{
#if UNITY_EDITOR
					// 에디터에서는 마켓창 열필요 없으니 종료한다.
					UnityEditor.EditorApplication.isPlaying = false;
#endif
					if (Application.platform == RuntimePlatform.IPhonePlayer)
					{
						Application.OpenURL(titleData["iosUrl"]);
					}
					else if (Application.platform == RuntimePlatform.Android)
					{
						Application.OpenURL("market://details?id=" + Application.identifier);
					}
				}, useOkBigCanvas, updateMessageId));

				// 횟수를 세서 업데이트 못하고 되돌아오면 다른 안내메세지를 띄우기로 한다.
				++_marketCanvasShowCount;
				return false;
			}
#if UNITY_IOS
			else if (versionInfo.updateVersion > serverVersion)
			{
				// 심사버전을 명시하는 항목이 있다면 해당 값에서 뽑아와서 처리하고
				bool review = false;
				string reviewKeyName = string.Format("{0}_review", versionInfo.serverKeyName);
				if (titleData.ContainsKey(reviewKeyName))
				{
					if (string.IsNullOrEmpty(titleData[reviewKeyName]) == false)
					{
						int reviewVersion = 0;
						int.TryParse(titleData[reviewKeyName], out reviewVersion);
						if (reviewVersion == versionInfo.updateVersion)
							review = true;
					}
				}
				else
				{
					// 아니면 버전큰걸 심사로 처리한다.
					review = true;
				}

				// 심사버전으로 체크가 되면 뻑나지 말라고 메일과 이벤트가 돌지 않게 되는데
				// 만약 메일과 이벤트에 별다른 추가 구현상황이 없어서 뻑날게 없는 상황이면 딱히 리뷰버전 체크 안하고 넘겨도 되긴 하다.
				// 그러니 평소에 안써도 되는 상황이라면 upVrIph_review를 0으로 냅둬도 상관없을거다.
				if (review)
					PlayerData.instance.reviewVersion = true;
			}
#endif
		}

		// 이후 리소스 패치 체크.
		// 이 시점에서는 아직 PlayerData를 구축하기 전이니 이렇게 직접 체크한다.
		bool downloadConfirmed = false;
		if (userReadOnlyData.ContainsKey("downloadConfirm"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["downloadConfirm"].Value) == false)
				downloadConfirmed = true;
		}

		// 다운로드 체크하기 전이라면 패스
		if (downloadConfirmed == false)
			return true;

		// 빌드 업데이트 확인이 끝나면 리소스 체크를 해야하는데,
		// 리소스 버전은 빌드번호와 달리 직접 서버에 적어두고 비교하는 형태가 아니다.
		// 어드레서블을 사용하기 때문에 GetDownloadSizeAsync 호출해서 있으면 패치할게 있는거고 0이면 패치할게 없는거다.
		// 당연히 Async구조이기 때문에 코루틴으로 바꿔서 대기해야한다.
		//int resourceNumber = 0;
		//int.TryParse(split[2], out resourceNumber);
		needCheckResourceVersion = true;

		return true;
	}

	void OnRecvPlayerDataFailure(PlayFabError error)
	{
		// 정말 이상하게도 갑자기 PlayFab로그인은 되는데 그 뒤 모든 패킷을 보내봐도(클라이언트측 함수나 클라우드 스크립트 둘다) 503 에러를 뱉어냈다.
		// 이때는 PlayFab 서버가 동작 안하는 상태라 종료시간같은걸 정할수도 없으니 전용 스트링 하나 만들어서 보여주기로 한다.
		string stringId = "SystemUI_DisconnectServer";
		if (error.Error == PlayFabErrorCode.ServiceUnavailable)
			stringId = "SystemUI_Error503";

		// 로그인이 성공한 이상 실패할거 같진 않지만 그래도 혹시 모르니 해둔다.
		Debug.LogError(error.GenerateErrorReport());
		StartCoroutine(AuthManager.instance.RestartProcess(null, false, stringId));
	}

	void CheckCompleteRecvPlayerData()
	{
		if (_requestCountForGetPlayerData > 0)
			return;

		EndTimeRecord("PlayerData");

		// LoginResult도 받았고 추가로 요청했던 Entity Objects도 전부 받았다. 진짜 로드를 시작하자.
#if USE_TITLE_PLAYER_ENTITY
		PlayerDataEntity1 entity1Object = null;
		if (_titlePlayerEntityObject.Objects.ContainsKey("PlayerData"))
		{
			ObjectResult playerDataObjectResult = _titlePlayerEntityObject.Objects["PlayerData"];
			entity1Object = JsonUtility.FromJson<PlayerDataEntity1>(playerDataObjectResult.DataObject.ToString());
		}
#endif

		// 혹시 다 못보냈더라도 어쩔 수 없다. 이전에 로그인 했던 계정의 정보를 보낼순 없다.
		ClearCliSusQueue();
		enableCliSusQueue = true;
		PlayerData.instance.OnRecvPlayerStatistics(_loginResult.InfoResultPayload.PlayerStatistics);
		PetManager.instance.OnRecvAdditionalStatistics(_additional1PlayerStatistics, _additional2PlayerStatistics);
		/*
		TimeSpaceData.instance.OnRecvEquipInventory(_loginResult.InfoResultPayload.UserInventory, _loginResult.InfoResultPayload.UserData, _loginResult.InfoResultPayload.UserReadOnlyData);
		*/
		PlayerData.instance.OnRecvPlayerData(_loginResult.InfoResultPayload.UserData, _loginResult.InfoResultPayload.UserReadOnlyData, _loginResult.InfoResultPayload.CharacterList, _loginResult.InfoResultPayload.PlayerProfile);
		/*
		MercenaryData.instance.OnRecvMercenaryData(_loginResult.InfoResultPayload.TitleData, false);
		*/
		enableCliSusQueue = false;

		_loginResult = null;
#if USE_TITLE_PLAYER_ENTITY
		_titlePlayerEntityObject = null;
#endif
		_dicCharacterStatisticsResult.Clear();
#if USE_CHARACTER_ENTITY
		_listCharacterEntityObject.Clear();
#endif
	}
	#endregion

	public void RequestNetwork(Action successCallback)
	{
		GetUserDataRequest request = new GetUserDataRequest() { Keys = new List<string> { "mainCharacterId" } };
		Action action = () =>
		{
			PlayFabClientAPI.GetUserData(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	public void RequestNetworkOnce(Action successCallback, Action failureCallback, bool showWaitingNetworkCanvas)
	{
		if (showWaitingNetworkCanvas)
			WaitingNetworkCanvas.Show(true);

		GetUserDataRequest request = new GetUserDataRequest() { Keys = new List<string> { "mainCharacterId" } };
		PlayFabClientAPI.GetUserData(request, (success) =>
		{
			if (showWaitingNetworkCanvas)
				WaitingNetworkCanvas.Show(false);

			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			if (showWaitingNetworkCanvas)
				WaitingNetworkCanvas.Show(false);

			HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
	}

	public void RequestGetTitleData(List<string> keys, Action<Dictionary<string, string>> successCallback)
	{
		PlayFabClientAPI.GetTitleData(new GetTitleDataRequest()
		{
			Keys = keys
		}, (success) =>
		{
			if (successCallback != null) successCallback.Invoke(success.Data);
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}


	#region GrantItem
	List<ItemGrantRequest> _listGrantRequest = new List<ItemGrantRequest>();
	public List<ItemGrantRequest> GenerateGrantInfo(List<string> listItemId, ref string checkSum, string initDataType = "")
	{
		_listGrantRequest.Clear();

		for (int i = 0; i < listItemId.Count; ++i)
		{
			ItemGrantRequest info = new ItemGrantRequest();
			info.ItemId = listItemId[i];

			if (initDataType == "spell")
			{
				// 최초로 만들어질때만 Data 적용되고 이미 만들어진 아이템에는 적용되지 않으므로 기본값을 설정하면 된다.
				info.Data = new Dictionary<string, string>();
				info.Data.Add(SpellData.KeyLevel, "1");
			}
			else if (initDataType == "character" && listItemId[i].Contains("pp") == false)
			{
				info.Data = new Dictionary<string, string>();
				info.Data.Add(CharacterData.KeyLevel, "1");
				info.Data.Add(CharacterData.KeyTranscend, "0");
			}

			_listGrantRequest.Add(info);
		}

		if (_listGrantRequest.Count > 0)
		{
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			string jsonItemGrants = serializer.SerializeObject(_listGrantRequest);
			checkSum = PlayFabApiManager.CheckSum(jsonItemGrants);
		}

		// 임시 리스트를 가지고 있을 필요 없으니 클리어
		_listGrantItemId.Clear();

		return _listGrantRequest;
	}

	List<string> _listGrantItemId = new List<string>();
	public List<ItemGrantRequest> GenerateGrantRequestInfo(List<ObscuredString> listItemId, ref string checkSum, string initDataType = "")
	{
		_listGrantRequest.Clear();
		if (listItemId == null || listItemId.Count == 0)
			return _listGrantRequest;

		_listGrantItemId.Clear();
		for (int i = 0; i < listItemId.Count; ++i)
			_listGrantItemId.Add(listItemId[i]);
		return GenerateGrantInfo(_listGrantItemId, ref checkSum, initDataType);
	}

	public List<ItemGrantRequest> GenerateGrantRequestInfo(string itemId, ref string checkSum, string initDataType)
	{
		_listGrantItemId.Clear();
		_listGrantItemId.Add(itemId);
		return GenerateGrantInfo(_listGrantItemId, ref checkSum, initDataType);
	}

	public List<ItemInstance> DeserializeItemGrantResult(string jsonItemGrantResults)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		GrantItemsToUsersResult result = serializer.DeserializeObject<GrantItemsToUsersResult>(jsonItemGrantResults);
		return result.ItemGrantResults;
	}

	string ItemId2InitDataType(string rewardValue)
	{
		if (rewardValue.StartsWith("Spell_"))
			return "spell";
		else if (rewardValue.StartsWith("Actor"))
			return "character";
		else if (rewardValue.StartsWith("Pet_"))
			return "pet";
		else if (rewardValue.StartsWith("Equip"))
			return "equip";
		return "";
	}
	#endregion

	#region Revoke
	List<RevokeInventoryItemRequest> _listRevokeInventoryItemRequest = new List<RevokeInventoryItemRequest>();
	public List<RevokeInventoryItemRequest> GenerateRevokeInfo(List<EquipData> listRevokeEquipData, ref string checkSum)
	{
		_listRevokeInventoryItemRequest.Clear();

		for (int i = 0; i < listRevokeEquipData.Count; ++i)
		{
			RevokeInventoryItemRequest info = new RevokeInventoryItemRequest();
			info.ItemInstanceId = listRevokeEquipData[i].uniqueId;
			_listRevokeInventoryItemRequest.Add(info);
		}

		if (_listRevokeInventoryItemRequest.Count > 0)
		{
			var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
			string jsonRevokeInventory = serializer.SerializeObject(_listRevokeInventoryItemRequest);
			checkSum = PlayFabApiManager.CheckSum(jsonRevokeInventory);
		}

		return _listRevokeInventoryItemRequest;
	}
	#endregion


	#region Stage Boss
	ObscuredString _serverEnterKeyForBoss;
	public void RequestEnterBoss(Action<bool> successCallback, Action failureCallback)
	{
		int selectedStage = PlayerData.instance.selectedStage;
		string input = string.Format("{0}_{1}", selectedStage, "vqzcatwlq");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "EnterBoss",
			FunctionParameter = new { Enter = 1, SeLv = selectedStage, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			_serverEnterKeyForBoss = failure ? "" : resultString;
			if (successCallback != null) successCallback.Invoke(failure);
		}, (error) =>
		{
			HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
	}

	public void RequestCancelBoss()
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CancelBoss",
			GeneratePlayStreamEvent = true,
		}, null, null);
	}

	public void RequestEndBoss(int selectedStage, int currentFloor, Action successCallback)
	{
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndBoss",
			FunctionParameter = new { Flg = (string)_serverEnterKeyForBoss, SeLv = selectedStage, CuFl = currentFloor },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
				jsonResult.TryGetValue("retErr", out object retErr);
				bool failure = ((retErr.ToString()) == "1");
				if (!failure)
				{
					RetrySendManager.instance.OnSuccess();

					jsonResult.TryGetValue("nextFlg", out object nextFlg);
					_serverEnterKeyForBoss = (string)nextFlg;

					// 성공시 처리
					int maxStage = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxStage");

					// 점프로 뛰는건지 체크
					if (selectedStage != currentFloor)
					{
						int diff = currentFloor - selectedStage;
						if (diff <= (BattleInstanceManager.instance.GetCachedGlobalConstantInt("FastClearJumpStep") - 1))
						{
							selectedStage = currentFloor;
							PlayerData.instance.selectedStage = selectedStage;
						}
					}

					// 클리어는 MaxStage - 1 까지 할 수 있다.
					if (selectedStage <= (maxStage - 1))
						PlayerData.instance.highestClearStage = selectedStage;

					int nextStage = selectedStage + 1;
					if (nextStage <= maxStage)
						PlayerData.instance.selectedStage = nextStage;
				}
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true, true);
	}
	#endregion

	#region Player Character
	public void RequestSubLevelUp(int price, bool salePrice, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", price, salePrice ? 1 : 0, "izerdjqa");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "PlayerSubLevelUp",
			FunctionParameter = new { Pr = price, Sa = salePrice ? 1 : 0, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.gold -= price;
				PlayerData.instance.OnSubLevelUp();
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestLevelUp(int price, bool salePrice, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", price, salePrice ? 1 : 0, "qizlrnmo");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "PlayerLevelUp",
			FunctionParameter = new { Pr = price, Sa = salePrice ? 1 : 0, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.gold -= price;
				PlayerData.instance.OnLevelUp();
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestPressLevelUp(int prevLevel, int prevSubLevel, int prevGold, int level, int subLevel, int gold, int levelUpCount, int subLevelUpCount, bool salePrice, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}_{7}_{8}_{9}", prevLevel, prevSubLevel, prevGold, level, subLevel, gold, levelUpCount, subLevelUpCount, salePrice ? 1 : 0, "qizlrnmo");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "PlayerPressLevelUp",
			FunctionParameter = new { PvLv = prevLevel, PvSub = prevSubLevel, PvGo = prevGold, Lv = level, Sub = subLevel, Go = gold, LvCnt = levelUpCount, SubCnt = subLevelUpCount, Sa = salePrice ? 1 : 0, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Betting
	public void RequestBetting(int useSpin, int resultGold, int resultDiamond, int resultEnergy, int resultTicket, int resultEventPoint, int reserveRoomType, bool refreshTurn, int newTurn, int newGold, Action<bool> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		int intRefreshTurn = refreshTurn ? 1 : 0;
		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}_{7}_{8}_{9}_{10}", CurrencyData.instance.bettingCount + 1, useSpin, resultGold, resultDiamond, resultEnergy, resultTicket, resultEventPoint, reserveRoomType, intRefreshTurn, newTurn, "azirjwlm");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Betting",
			FunctionParameter = new { Cnt = CurrencyData.instance.bettingCount + 1, Bet = useSpin, AddGo = resultGold, AddDi = resultDiamond, AddEn = resultEnergy, AddTi = resultTicket, AddEv = resultEventPoint, ResRoomTp = reserveRoomType, RefreshTurn = intRefreshTurn, NewTurn = newTurn, NewGold = newGold, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			jsonResult.TryGetValue("roomFlg", out object roomFlg);
			jsonResult.TryGetValue("refreshTurn", out object serverRefreshTurn);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.bettingCount += 1;

				CurrencyData.instance.gold += resultGold;
				CurrencyData.instance.dia += resultDiamond;
				CurrencyData.instance.ticket += resultTicket;
				CurrencyData.instance.eventPoint += resultEventPoint;

				if (useSpin == resultEnergy)
				{
				}
				else if (useSpin > resultEnergy)
					CurrencyData.instance.UseEnergy(useSpin - resultEnergy);
				else if (useSpin < resultEnergy)
					CurrencyData.instance.OnRecvRefillEnergy(resultEnergy - useSpin);

				_serverEnterKeyForRoom = (reserveRoomType != 0) ? roomFlg.ToString() : "";

				bool refreshTurnComplete = false;
				if (refreshTurn && serverRefreshTurn.ToString() == "1")
				{
					CurrencyData.instance.goldBoxRemainTurn = newTurn;
					CurrencyData.instance.goldBoxTargetReward = newGold;
					refreshTurnComplete = true;
				}
				else
				{
					if (CurrencyData.instance.goldBoxRemainTurn > 1)
						CurrencyData.instance.goldBoxRemainTurn -= 1;
				}

				if (successCallback != null) successCallback.Invoke(refreshTurnComplete);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestEndBettingRoom(int resultGold, Action successCallback)
	{
		string input = string.Format("{0}_{1}", resultGold, "lirqzmak");
		string checkSum = CheckSum(input);

		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndBettingRoom",
			FunctionParameter = new { Flg = (string)_serverEnterKeyForRoom, AddGo = resultGold, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				string resultString = (string)success.FunctionResult;
				bool failure = (resultString == "1");
				_serverEnterKeyForRoom = "";
				if (!failure)
				{
					RetrySendManager.instance.OnSuccess();

					// 성공시 처리
					CurrencyData.instance.gold += resultGold;

					if (successCallback != null) successCallback.Invoke();
				}
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true, true);
	}
	#endregion

	#region Gacha
	public void RequestGacha(int useEnergy, int resultGold, int resultDia, int resultEnergy, int resultBrokenEnergy, int resultEventPoint, List<ObscuredString> listEventItemId, int resultAddSpellGacha, int resultAddCharacterGacha, int resultAddEquipGacha, int reserveRoomType, bool refreshTurn, int newTurn, int newGold, int newGoldGrade, int eventPointRewardCount, int eventPointRewardCompleteCount, Action<bool> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		// hardcode ev6
		bool checkPayback = CashShopData.instance.IsShowEvent("ev6");

		int intRefreshTurn = refreshTurn ? 1 : 0;
		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}_{7}_{8}_{9}_{10}_{11}_{12}_{13}_{14}_{15}", CurrencyData.instance.bettingCount + 1, useEnergy, resultGold, resultDia, resultEnergy, resultBrokenEnergy, resultEventPoint, resultAddSpellGacha, resultAddCharacterGacha, resultAddEquipGacha, reserveRoomType, intRefreshTurn, newTurn, eventPointRewardCount, eventPointRewardCompleteCount, "azirjwlm");
		string checkSum = CheckSum(input);
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listEventItemId, ref checkSum2);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Gacha",
			FunctionParameter = new { Cnt = CurrencyData.instance.bettingCount + 1, Bet = useEnergy, AddGo = resultGold, AddDi = resultDia, AddEn = resultEnergy, AddBrEn = resultBrokenEnergy, AddEv = resultEventPoint, Lst = listItemGrantRequest, LstCs = checkSum2, AddSpGa = resultAddSpellGacha, AddChGa = resultAddCharacterGacha, AddEqGa = resultAddEquipGacha, ResRoomTp = reserveRoomType, RefreshTurn = intRefreshTurn, NewTurn = newTurn, NewGold = newGold, NewGoldGrade = newGoldGrade, Cp = checkPayback ? 1 : 0, EpRc = eventPointRewardCount, EpRcc = eventPointRewardCompleteCount, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			jsonResult.TryGetValue("roomFlg", out object roomFlg);
			jsonResult.TryGetValue("refreshTurn", out object serverRefreshTurn);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.bettingCount += 1;

				CurrencyData.instance.gold += resultGold;
				CurrencyData.instance.dia += resultDia;

				if (resultBrokenEnergy > 0)
				{
					int maxBrokenEnergy = CashShopData.instance.GetMaxBrokenEnergy();
					CurrencyData.instance.brokenEnergy = Math.Min(CurrencyData.instance.brokenEnergy + resultBrokenEnergy, maxBrokenEnergy);
					if (CurrencyData.instance.brokenEnergy >= maxBrokenEnergy)
						CashShopData.instance.brokenEnergyMaxReached = true;
					MainCanvas.instance.RefreshCashButton();
				}

				CurrencyData.instance.eventPoint += resultEventPoint;

				if (useEnergy == resultEnergy)
				{
				}
				else if (useEnergy > resultEnergy)
					CurrencyData.instance.UseEnergy(useEnergy - resultEnergy);
				else if (useEnergy < resultEnergy)
					CurrencyData.instance.OnRecvRefillEnergy(resultEnergy - useEnergy, true);

				if (listEventItemId.Count > 0 && listItemGrantRequest.Count > 0)
				{
					// instanceId 파싱이 필요한 스펠이나 장비 캐릭같았다면 이렇게 파싱하겠지만
					//jsonResult.TryGetValue("itmRet", out object itmRet);
					// 컨슘 아이템으로 한정되어있기 때문에 이렇게 Consume 전용으로 호출해본다.
					for (int i = 0; i < listEventItemId.Count; ++i)
						CashShopData.instance.OnRecvConsumeItem(listEventItemId[i], 1);
				}
				if (resultAddSpellGacha > 0)
					CashShopData.instance.OnRecvConsumeItem("Cash_sSpellGacha", resultAddSpellGacha);
				if (resultAddCharacterGacha > 0)
					CashShopData.instance.OnRecvConsumeItem("Cash_sCharacterGacha", resultAddCharacterGacha);
				if (resultAddEquipGacha > 0)
					CashShopData.instance.OnRecvConsumeItem("Cash_sEquipGacha", resultAddEquipGacha);

				_serverEnterKeyForRoom = (reserveRoomType != 0) ? roomFlg.ToString() : "";

				bool refreshTurnComplete = false;
				if (refreshTurn && serverRefreshTurn.ToString() == "1")
				{
					CurrencyData.instance.goldBoxRemainTurn = newTurn;
					CurrencyData.instance.goldBoxTargetReward = newGold;
					CurrencyData.instance.goldBoxTargetGrade = newGoldGrade;
					refreshTurnComplete = true;
				}
				else
				{
					if (CurrencyData.instance.goldBoxRemainTurn > 1)
						CurrencyData.instance.goldBoxRemainTurn -= 1;
				}

				if (checkPayback)
				{
					jsonResult.TryGetValue("applyPayback", out object applyPayback);
					if ((applyPayback.ToString()) == "1")
						CashShopData.instance.energyUseForPayback += useEnergy;
				}

				if (successCallback != null) successCallback.Invoke(refreshTurnComplete);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	ObscuredString _serverEnterKeyForRoom;
	public void RequestEndGachaRoom(int resultGold, Action successCallback)
	{
		string input = string.Format("{0}_{1}", resultGold, "lirqzmak");
		string checkSum = CheckSum(input);

		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndGachaRoom",
			FunctionParameter = new { Flg = (string)_serverEnterKeyForRoom, AddGo = resultGold, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				string resultString = (string)success.FunctionResult;
				bool failure = (resultString == "1");
				_serverEnterKeyForRoom = "";
				if (!failure)
				{
					RetrySendManager.instance.OnSuccess();

					// 성공시 처리
					CurrencyData.instance.gold += resultGold;

					if (successCallback != null) successCallback.Invoke();
				}
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true, true);
	}
	#endregion


	#region Mail
	public void RequestRefreshMailList(int mailTableDataCount, string osCode, int clientVersion, Action<bool, bool, bool, string, string> successCallback)
	{
		string input = string.Format("{0}_{1}_{2}", osCode, clientVersion, "qalzpocv");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "RefreshMail",
			FunctionParameter = new { Mtc = mailTableDataCount, Os = osCode, CltVer = clientVersion, Cs = checkSum },
			GeneratePlayStreamEvent = true
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("del", out object del);
			jsonResult.TryGetValue("add", out object add);
			jsonResult.TryGetValue("mod", out object mod);
			jsonResult.TryGetValue("dat", out object jsonDateTime);
			jsonResult.TryGetValue("mtd", out object jsonMailTable);
			bool deleted = ((del.ToString()) == "1");
			bool added = ((add.ToString()) == "1");
			bool modified = ((mod.ToString()) == "1");
			if (successCallback != null) successCallback.Invoke(deleted, added, modified, (string)jsonDateTime, (string)jsonMailTable);
		}, (error) =>
		{
			// 5분마다 주기적으로 보내는거라 에러 핸들링 하면 안된다.
			//HandleCommonError(error);
		});
	}

	public void RequestReceiveMailPresent(string id, int receiveDay, string type, int addDia, int addGold, int addEnergy, int addTicket, Action<bool> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetMail",
			FunctionParameter = new { Id = id, Dy = receiveDay, Tp = type },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				bool result = MailData.instance.OnRecvGetMail(id, receiveDay, type);
				if (result)
				{
					WaitingNetworkCanvas.Show(false);

					CurrencyData.instance.dia += addDia;
					CurrencyData.instance.gold += addGold;
					if (addEnergy > 0)
						CurrencyData.instance.OnRecvRefillEnergy(addEnergy);
					if (addTicket > 0)
						CurrencyData.instance.OnRecvRefillTicket(addTicket);

					if (successCallback != null) successCallback.Invoke(failure);
				}
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Account Delete
	public void RequestDeleteAccount(bool cancel, Action successCallback)
	{
		if (cancel == false)
			WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "DeleteAccount",
			FunctionParameter = new { Cancel = cancel ? 1 : 0 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			if (cancel == false)
				WaitingNetworkCanvas.Show(false);
			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion

	#region Terms
	public void RequestConfirmTerms(Action successCallback)
	{
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "Terms",
			FunctionParameter = new { Terms = 1 },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				PlayerData.instance.termsConfirmed = true;
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}
	#endregion

	#region Download Confirm
	public void RequestConfirmDownload(Action successCallback)
	{
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConfirmDownload",
			FunctionParameter = new { Down = 1 },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				PlayerData.instance.downloadConfirmed = true;
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	public void RequestConfirmDownloadReward(int addEnergy, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConfirmDownloadReward",
			FunctionParameter = new { Down = 2 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.OnRecvRefillEnergy(addEnergy);
				PlayerData.instance.downloadRewarded = true;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion

	#region Tutorial Step
	public void RequestCompleteTutorialStep(int step)
	{
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "CompleteTutorialStep",
			FunctionParameter = new { Step = step },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, false);
	}
	#endregion

	#region Open Canvas Event
	public void RequestCompleteOpenCanvasEvent(int index)
	{
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "CompleteOpenCanvasEvent",
			FunctionParameter = new { Idx = index },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				PlayerData.instance.OnCompleteShowCanvasEvent(index);
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, false);
	}
	#endregion

	#region Support
	public void RequestRefreshInquiryList(Action<string> successCallback)
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "RefreshInquiry",
			FunctionParameter = new { Inq = 1 },
			GeneratePlayStreamEvent = true
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("dat", out object jsonInquiryData);
			if (successCallback != null) successCallback.Invoke((string)jsonInquiryData);
		}, (error) =>
		{
			// 5분마다 주기적으로 보내는거라 에러 핸들링 하면 안된다.
			//HandleCommonError(error);
		});
	}

	public void ReqeustWriteInquiry(string body, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Inquiry",
			FunctionParameter = new { Body = body },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				SupportData.instance.OnRecvWriteInquiry(body);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region CashEvent
	public void RequestOpenCashEvent(string openEventId, string eventSub, string generatedParameter, int givenTime, int coolTime, Action successCallback)
	{
		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", openEventId, eventSub, generatedParameter, givenTime, coolTime, "ldruqzvm");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenCashEvent",
			FunctionParameter = new { EvId = openEventId, EvSub = eventSub, GePa = generatedParameter, GiTim = givenTime, CoTim = coolTime, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				jsonResult.TryGetValue("date", out object date);

				// 성공시에는 서버에서 방금 기록한 유효기간 만료 시간이 날아온다.
				CashShopData.instance.OnRecvOpenCashEvent(openEventId, (string)date);

				jsonResult.TryGetValue("cool", out object useCool);
				if ((useCool.ToString()) == "1")
				{
					jsonResult.TryGetValue("cdate", out object cdate);
					CashShopData.instance.OnRecvCoolTimeCashEvent(openEventId, (string)cdate);
				}

				// 추가로 해야할 설정들이 있는지 확인
				CashShopData.instance.OnOpenCashEvent(openEventId, eventSub, generatedParameter);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestCloseCashEvent(string closeEventId, Action successCallback)
	{
		string input = string.Format("{0}_{1}", closeEventId, "ldruqzvn");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CloseCashEvent",
			FunctionParameter = new { EvId = closeEventId, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				CashShopData.instance.OnRecvCloseCashEvent(closeEventId);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion

	#region EventPoint
	public void RequestStartEventPoint(string startEventPointId, int limitHour, bool oneTime, bool completeRefresh, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}", startEventPointId, limitHour, oneTime ? 1 : 0, "qrzlmvix");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "StartEventPoint",
			FunctionParameter = new { EpntId = startEventPointId, LiHr = limitHour, OnTim = oneTime ? 1 : 0, CoRe = completeRefresh ? 1 : 0, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				jsonResult.TryGetValue("date", out object date);

				// 성공시에는 서버에서 방금 기록한 유효기간 만료 시간이 날아온다.
				CurrencyData.instance.OnRecvStartEventPoint(startEventPointId, oneTime, (string)date);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Analysis
	public void RequestStartAnalysis(Action successCallback, Action failureCallback)
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "StartAnalysis",
			FunctionParameter = new { Inf = 1 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				jsonResult.TryGetValue("date", out object date);
				AnalysisData.instance.OnRecvAnalysisStartInfo((string)date);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			//HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
	}

	public void RequestAnalysis(int addExp, int useBoost, int resultGold, int resultDia, int resultEnergy, List<ObscuredString> listEventItemId, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		// 이 패킷 역시 Invasion 했던거처럼 다양하게 보낸다. 오리진 재화 등등
		int currentExp = AnalysisData.instance.analysisExp;

		string checkSum = "";
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		checkSum = CheckSum(string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}", addExp, currentExp, useBoost, resultGold, resultDia, resultEnergy, "xzdliroa"));
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listEventItemId, ref checkSum2);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Analysis",
			FunctionParameter = new { Xp = addExp, CurXp = currentExp, UseBoost = useBoost, AddGo = resultGold, AddDi = resultDia, AddEn = resultEnergy, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				// 레벨업이 있다면 먼저 레벨업을 적용시키고나서
				AnalysisData.instance.AddExp(addExp);

				// 부스트 삭제
				if (useBoost > 0)
					AnalysisData.instance.boostRemainTime -= useBoost;

				// 재화
				CurrencyData.instance.gold += resultGold;
				CurrencyData.instance.dia += resultDia;
				CurrencyData.instance.OnRecvRefillEnergy(resultEnergy, true);

				if (listEventItemId.Count > 0 && listItemGrantRequest.Count > 0)
				{
					// RequestGacha 처리했던거럼 똑같이 컨슘만 있을거라 이렇게 처리한다.
					for (int i = 0; i < listEventItemId.Count; ++i)
						CashShopData.instance.OnRecvConsumeItem(listEventItemId[i], 1);
				}

				// 시간을 셋팅해야 새 레벨에 맞는 CompleteTime으로 갱신이 제대로 된다.
				// 성공시에만 date파싱을 한다.
				jsonResult.TryGetValue("date", out object date);
				AnalysisData.instance.OnRecvAnalysisStartInfo((string)date);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestLevelUpAnalysis(int currentLevel, int targetLevel, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		int currentExp = AnalysisData.instance.analysisExp;
		string input = string.Format("{0}_{1}_{2}_{3}", currentLevel, targetLevel, currentExp, "leuzvjqa");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "LevelUpAnalysis",
			FunctionParameter = new { CurXp = currentExp, Cur = currentLevel, Ta = targetLevel, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				AnalysisData.instance.OnLevelUp(targetLevel);
				CurrencyData.instance.dia -= price;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Guide Quest
	public void RequestGuideQuestProceedingCount(int currentGuideQuestIndex, int addCount, int expectCount, int key, Action successCallback)
	{
		string input = string.Format("{0}_{1}_{2}_{3}_{4}", currentGuideQuestIndex, addCount, expectCount, key, "zxiozlmqj");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GuideQuestProceedingCount",
			FunctionParameter = new { Add = addCount, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (failure)
				HandleCommonError();
			else
			{
				GuideQuestData.instance.currentGuideQuestProceedingCount += addCount;
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	public void RequestCompleteGuideQuest(int currentGuideQuestIndex, string rewardType, int key, int addDia, int addGold, int addEnergy, int addTicket, List<ObscuredString> listEventItemId, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		// 퀘완료를 보내기전에 다음번에 받을 퀘의 진행상태를 체크
		int nextInitialProceedingCount = GuideQuestData.instance.CheckNextInitialProceedingCount();

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", currentGuideQuestIndex, rewardType, nextInitialProceedingCount, key, "witpnvfwk");
		string infoCheckSum = CheckSum(input);
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listEventItemId, ref checkSum2);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CompleteGuideQuest",
			FunctionParameter = new { Tp = rewardType, Np = nextInitialProceedingCount, InfCs = infoCheckSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				GuideQuestData.instance.currentGuideQuestIndex += 1;
				GuideQuestData.instance.currentGuideQuestProceedingCount = nextInitialProceedingCount;

				CurrencyData.instance.dia += addDia;
				CurrencyData.instance.gold += addGold;
				if (addEnergy > 0)
					CurrencyData.instance.OnRecvRefillEnergy(addEnergy);
				if (addTicket > 0)
					CurrencyData.instance.OnRecvRefillTicket(addTicket);

				if (listEventItemId.Count > 0 && listItemGrantRequest.Count > 0)
				{
					// RequestGacha 처리했던거처럼 컨슘 아이템으로 한정되어있기 때문에 이렇게 Consume 전용으로 호출해본다.
					for (int i = 0; i < listEventItemId.Count; ++i)
						CashShopData.instance.OnRecvConsumeItem(listEventItemId[i], 1);
				}

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Quest
	public void RequestRegisterQuestList(List<SubQuestData.QuestInfo> listQuestInfoForSend, bool proceeding, Action successCallback)
	{
		string checkSum = "";
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string jsonListQst = serializer.SerializeObject(listQuestInfoForSend);
		checkSum = CheckSum(string.Format("{0}_{1}_{2}", jsonListQst, proceeding ? 1 : 0, "cibpqxrh"));

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SetQuestList",
			FunctionParameter = new { Lst = listQuestInfoForSend, Proc = proceeding ? 1 : 0, LstCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (failure)
				HandleCommonError();
			else
			{
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	public void RequestSelectQuest(int questIdx, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SelectQuest",
			FunctionParameter = new { QstIdx = questIdx },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				SubQuestData.instance.currentQuestIndex = questIdx;
				SubQuestData.instance.currentQuestStep = SubQuestData.eQuestStep.Proceeding;
				SubQuestData.instance.currentQuestProceedingCount = 0;
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	public void RequestQuestProceedingCount(int addCount, Action successCallback)
	{
		string input = string.Format("{0}_{1}", addCount, "ckwqizmn");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "QuestProceedingCount",
			FunctionParameter = new { Add = addCount, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (failure)
				HandleCommonError();
			else
			{
				SubQuestData.instance.currentQuestProceedingCount += addCount;
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	public void RequestCompleteQuest(bool doubleClaim, int diaCount, int addEnergy, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CompleteQuest",
			FunctionParameter = new { Dbl = doubleClaim ? 1 : 0 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				SubQuestData.instance.currentQuestStep = SubQuestData.eQuestStep.Select;
				SubQuestData.instance.currentQuestIndex = 0;
				SubQuestData.instance.currentQuestProceedingCount = 0;
				SubQuestData.instance.todayQuestRewardedCount += 1;
				if (doubleClaim)
					CurrencyData.instance.dia -= diaCount;
				CurrencyData.instance.OnRecvRefillEnergy(addEnergy);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Purchase Validate
#if UNITY_ANDROID
	public void RequestValidatePurchase(string isoCurrencyCode, uint price, string receiptJson, string signature, Action successCallback, Action<PlayFabError> failureCallback)
	{
		PlayFabClientAPI.ValidateGooglePlayPurchase(new ValidateGooglePlayPurchaseRequest()
		{
			CurrencyCode = isoCurrencyCode,
			PurchasePrice = price,
			ReceiptJson = receiptJson,
			Signature = signature
#elif UNITY_IOS
	public void RequestValidatePurchase(string isoCurrencyCode, int price, string receiptData, Action successCallback, Action<PlayFabError> failureCallback)
	{
		PlayFabClientAPI.ValidateIOSReceipt(new ValidateIOSReceiptRequest()
		{
			CurrencyCode = isoCurrencyCode,
			PurchasePrice = price,
			ReceiptData = receiptData
#endif
		}, (success) =>
		{
			PlayerData.instance.vtd += (int)price;
			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke(error);
		});
	}
	#endregion

	#region CashShop
	public void RequestGetLevelPassReward(int level, int rewardEnergy, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", level, rewardEnergy, "qizlrewm");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetLevelPassReward",
			FunctionParameter = new { SeLv = level, SeRw = rewardEnergy, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.OnRecvLevelPassReward(level);
				CurrencyData.instance.OnRecvRefillEnergy(rewardEnergy);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestGetEnergyPaybackReward(int use, int rewardPayback, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string cashEventId = EnergyPaybackCanvas.instance.cashEventId;
		string input = string.Format("{0}_{1}_{2}_{3}", cashEventId, use, rewardPayback, "azixpmlr");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetEnergyPaybackReward",
			FunctionParameter = new { EvId = cashEventId, SeUs = use, SeRw = rewardPayback, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.OnRecvEnergyPaybackReward(use);
				CurrencyData.instance.OnRecvRefillEnergy(rewardPayback);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestStartBrokenEnergyExpire(int givenTime, Action successCallback, Action failureCallback)
	{
		string input = string.Format("{0}_{1}", givenTime, "riezqpsa");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "StartBrokenEnergyExpire",
			FunctionParameter = new { GiTim = givenTime, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				jsonResult.TryGetValue("date", out object date);

				// 성공시에는 서버에서 방금 기록한 유효기간 만료 시간이 날아온다.
				CashShopData.instance.brokenEnergyExpireStarted = true;
				CashShopData.instance.OnRecvStartBrokenEnergyExpire((string)date);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			//HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
	}

	public void RequestNextStepBrokenEnergy(int currentLevel, int nextLevel, Action successCallback, Action failureCallback)
	{
		string input = string.Format("{0}_{1}_{2}", currentLevel, nextLevel, "zreqalpn");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "NextStepBrokenEnergy",
			FunctionParameter = new { CuLv = currentLevel, NeLv = nextLevel, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				CashShopData.instance.brokenEnergyExpireStarted = false;
				CashShopData.instance.brokenEnergyLevel = nextLevel;
				CurrencyData.instance.brokenEnergy = 0;
				if (MainCanvas.instance != null)
					MainCanvas.instance.RefreshCashButton();

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			//HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
	}

	public void RequestConsumeBrokenEnergy(int currentLevel, int nextLevel, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", currentLevel, nextLevel, "xreqbipm");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeBrokenEnergy",
			FunctionParameter = new { CuLv = currentLevel, NeLv = nextLevel, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.BrokenEnergy);
				CurrencyData.instance.OnRecvRefillEnergy(CurrencyData.instance.brokenEnergy);
				CashShopData.instance.brokenEnergyExpireStarted = false;
				CashShopData.instance.brokenEnergyLevel = nextLevel;
				CurrencyData.instance.brokenEnergy = 0;
				if (MainCanvas.instance != null)
					MainCanvas.instance.RefreshCashButton();

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestGetContinuousProduct(string cashEventId, ShopProductTableData shopProductTableData, int contiNum, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", cashEventId, shopProductTableData.productId, contiNum, shopProductTableData.key, "zqilrkxm");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetContinuousProduct",
			FunctionParameter = new { EvId = cashEventId, SpId = shopProductTableData.productId, ContiNum = contiNum, InfCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.AddContinuousProductStep(cashEventId, 1);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeContinuousNext(string cashEventId, bool cashStep, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeContiNext",
			FunctionParameter = new { EvId = cashEventId, Cash = cashStep ? 1 : 0 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				if (cashEventId == "ev4")
					CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.Ev4ContiNext);
				CashShopData.instance.AddContinuousProductStep(cashEventId, 1);
				MainCanvas.instance.RefreshCashButton();

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestGetOnePlusTwoProduct(string cashEventId, ShopProductTableData shopProductTableData, int index, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", cashEventId, shopProductTableData.productId, index, shopProductTableData.key, "zqilrkxc");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetOnePlusTwoProduct",
			FunctionParameter = new { EvId = cashEventId, SpId = shopProductTableData.productId, Idx = index, InfCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.OnRecvOnePlusTwoReward(cashEventId, index);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeOnePlusTwoCash(string cashEventId, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeOnePlusTwoCash",
			FunctionParameter = new { EvId = cashEventId },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				if (cashEventId == "ev5")
					CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.Ev5OnePlTwoCash);
				CashShopData.instance.OnRecvOnePlusTwoReward(cashEventId, 0);
				if (cashEventId == "ev5")
					MainCanvas.instance.RefreshOnePlusTwo1AlarmObject();

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeSevenSlot(int buttonIndex, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeSevenSlot",
			FunctionParameter = new { Idx = buttonIndex },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.SevenSlot1 + buttonIndex);
				MissionData.instance.OnRecvPurchasedCashSlot(buttonIndex);
				
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeSevenTotal(Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		int count = CashShopData.instance.GetConsumeCount(CashShopData.eCashConsumeCountType.SevenTotal);
		string input = string.Format("{0}_{1}", count, "qirsmnzo");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeSevenTotal",
			FunctionParameter = new { Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeCount(CashShopData.eCashConsumeCountType.SevenTotal, count);
				MissionData.instance.sevenDaysSumPoint += count * SevenTotalCanvas.PointPerConsumeItem;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeFestivalSlot(int buttonIndex, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeFestivalSlot",
			FunctionParameter = new { Idx = buttonIndex },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.FestivalSlot1 + buttonIndex);
				FestivalData.instance.OnRecvPurchasedCashSlot(buttonIndex);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeFestivalTotal(Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		int count = CashShopData.instance.GetConsumeCount(CashShopData.eCashConsumeCountType.FestivalTotal);
		string input = string.Format("{0}_{1}", count, "risdmozq");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeFestivalTotal",
			FunctionParameter = new { Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeCount(CashShopData.eCashConsumeCountType.FestivalTotal, count);
				FestivalData.instance.festivalSumPoint += count * FestivalTotalCanvas.PointPerConsumeItem;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeAnalysisBoost(Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		int count = CashShopData.instance.GetConsumeCount(CashShopData.eCashConsumeCountType.AnalysisBoost);
		string input = string.Format("{0}_{1}", count, "orzwamlp");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeAnalysisBoost",
			FunctionParameter = new { Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeCount(CashShopData.eCashConsumeCountType.AnalysisBoost, count);
				AnalysisData.instance.boostRemainTime += count * 24 * 3600;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestReceiveDailyDiamond(int addDia, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ReceiveDailyDiamond",
			FunctionParameter = new { Di = addDia },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeCount(CashShopData.eCashItemCountType.DailyDiamond, 1);
				CurrencyData.instance.dia += addDia;

				jsonResult.TryGetValue("date", out object date);

				// 성공시에는 서버에서 방금 기록한 마지막 수령 시간이 날아온다.
				CashShopData.instance.OnRecvDailyDiamondInfo((string)date);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestUpdateStageClearPackageList(string jsonStageClearPackageList, Action successCallback)
	{
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "UpdateStageClearPackageList",
			FunctionParameter = new { Lst = jsonStageClearPackageList },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, false);
	}

	public void RequestFirstPurchaseReward(ShopProductTableData shopProductTableData, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", shopProductTableData.productId, shopProductTableData.key, "ewpnskaz");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "FirstPurchaseReward",
			FunctionParameter = new { SpId = shopProductTableData.productId, InfCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.firstPurchaseRewarded = true;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeAcquiredSpell(string selectedId, int count, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", selectedId, count, "ecosaplq");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeAcquiredSpell",
			FunctionParameter = new { ItmId = selectedId, Cnt = count, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.AcquiredSpell);
				SpellManager.instance.OnRecvPurchaseItem(selectedId, count);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeUnacquiredSpell(string selectedId, int count, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", selectedId, count, "xcodapmq");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeUnacquiredSpell",
			FunctionParameter = new { ItmId = selectedId, Cnt = count, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.UnacquiredSpell);
				SpellManager.instance.OnRecvPurchaseItem(selectedId, count);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeAcquiredCharacter(string selectedId, int count, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", selectedId, count, "qxpieraz");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeAcquiredCharacter",
			FunctionParameter = new { ItmId = selectedId, Cnt = count, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.AcquiredCompanion);
				CharacterManager.instance.OnRecvPurchaseItem(selectedId, count);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeAcquiredCharacterPp(string selectedId, int count, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", selectedId, count, "iorqznsk");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeAcquiredCharacterPp",
			FunctionParameter = new { ItmId = selectedId, Cnt = count, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.AcquiredCompanionPp);
				CharacterManager.instance.OnRecvPurchaseItem(string.Format("{0}pp", selectedId), count);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeUnacquiredCharacter(string selectedId, int count, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", selectedId, count, "rexpklqm");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeUnacquiredCharacter",
			FunctionParameter = new { ItmId = selectedId, Cnt = count, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.UnacquiredCompanion);
				CharacterManager.instance.OnRecvPurchaseItem(selectedId, count);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeRelayPackage(Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeRelayPackage",
			FunctionParameter = new { Cur = (int)CashShopData.instance.relayPackagePurchasedNum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.RelayPackage);
				CashShopData.instance.relayPackagePurchasedNum += 1;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestGetFreePackage(FreePackageTableData freePackageTableData, ShopProductTableData shopProductTableData, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", freePackageTableData.type, freePackageTableData.conValue, shopProductTableData.productId, shopProductTableData.key, "sfzmplqw");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetFreePackage",
			FunctionParameter = new { Tp = freePackageTableData.type, Con = freePackageTableData.conValue, SpId = shopProductTableData.productId, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion

	#region Costume
	public void RequestPurchaseCostumeByGold(string costumeId, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", costumeId, price, "qizlrmnd");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "PurchaseCostumeByGold",
			FunctionParameter = new { CosId = costumeId, Pr = price, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.gold -= price;

				jsonResult.TryGetValue("id", out object id);
				CostumeManager.instance.OnRecvPurchase((string)id);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestSelectCostume(string costumeId, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}", costumeId, "qizlrxja");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SelectCostume",
			FunctionParameter = new { CosId = costumeId, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CostumeManager.instance.selectedCostumeId = costumeId;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region SevenDays
	public void RequestStartSevenDays(int newGroupId, int givenTime, int coolTime, Action successCallback, Action failureCallback)
	{
		List<int> listInitialCount = MissionData.instance.GetInitialCount();
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string jsonInitialCounts = serializer.SerializeObject(listInitialCount);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", newGroupId, givenTime, coolTime, jsonInitialCounts, "vmpqalxj");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "StartSevenDays",
			FunctionParameter = new { SdGrpId = newGroupId, GiTim = givenTime, CoTim = coolTime, CntLst = listInitialCount, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				jsonResult.TryGetValue("date", out object date);

				// 성공시에는 서버에서 방금 기록한 유효기간 만료 시간이 날아온다.
				MissionData.instance.OnRecvSevenDaysStartInfo((string)date);
				MissionData.instance.OnRecvInitialCountList(listInitialCount);

				jsonResult.TryGetValue("cool", out object useCool);
				if ((useCool.ToString()) == "1")
				{
					jsonResult.TryGetValue("cdate", out object cdate);
					MissionData.instance.OnRecvSevenDaysCoolTimeInfo((string)cdate);
				}

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			//HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
	}

	public void RequestSevenDaysProceedingCount(int type, int addCount, int expectCount, Action successCallback)
	{
		string input = string.Format("{0}_{1}_{2}_{3}", type, addCount, expectCount, "wxiopljz");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SevenDaysProceedingCount",
			FunctionParameter = new { Tp = type, Add = addCount, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (failure)
				HandleCommonError();
			else
			{
				MissionData.instance.SetProceedingCount(type, addCount, expectCount);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestGetSevenDaysReward(SevenDaysRewardTableData sevenDaysRewardTableData, Action<string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", MissionData.instance.sevenDaysId, sevenDaysRewardTableData.day, sevenDaysRewardTableData.num, sevenDaysRewardTableData.key, "qizolrms");
		string checkSum = CheckSum(input);

		ExecuteCloudScriptRequest request = null;
		if (sevenDaysRewardTableData.rewardType == "cu")
		{
			request = new ExecuteCloudScriptRequest()
			{
				FunctionName = "GetSevenDaysReward",
				FunctionParameter = new { SdGrpId = (int)MissionData.instance.sevenDaysId, Day = sevenDaysRewardTableData.day, Num = sevenDaysRewardTableData.num, InfCs = checkSum },
				GeneratePlayStreamEvent = true,
			};
		}
		else if (sevenDaysRewardTableData.rewardType == "it")
		{
			List<string> listItemId = new List<string>();
			for (int i = 0; i < sevenDaysRewardTableData.rewardCount; ++i)
				listItemId.Add(sevenDaysRewardTableData.rewardValue);
			string checkSum2 = "";
			List<ItemGrantRequest> listItemGrantRequest = GenerateGrantInfo(listItemId, ref checkSum2, ItemId2InitDataType(sevenDaysRewardTableData.rewardValue));
			request = new ExecuteCloudScriptRequest()
			{
				FunctionName = "GetSevenDaysReward",
				FunctionParameter = new { SdGrpId = (int)MissionData.instance.sevenDaysId, Day = sevenDaysRewardTableData.day, Num = sevenDaysRewardTableData.num, InfCs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
				GeneratePlayStreamEvent = true,
			};
		}
		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				MissionData.instance.sevenDaysSumPoint += sevenDaysRewardTableData.sumPoint;
				MissionData.instance.OnRecvGetSevenDaysReward(sevenDaysRewardTableData.day, sevenDaysRewardTableData.num);

				CurrencyData.instance.OnRecvProductReward(sevenDaysRewardTableData.rewardType, sevenDaysRewardTableData.rewardValue, sevenDaysRewardTableData.rewardCount);

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestGetSevenDaysSumReward(SevenSumTableData sevenSumTableData, Action<string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}", MissionData.instance.sevenDaysId, sevenSumTableData.count, sevenSumTableData.key, "jfskeimz");
		string checkSum = CheckSum(input);

		ExecuteCloudScriptRequest request = null;
		if (sevenSumTableData.rewardType == "cu")
		{
			request = new ExecuteCloudScriptRequest()
			{
				FunctionName = "GetSevenDaysSumReward",
				FunctionParameter = new { SdGrpId = (int)MissionData.instance.sevenDaysId, Cnt = sevenSumTableData.count, InfCs = checkSum },
				GeneratePlayStreamEvent = true,
			};
		}
		else if(sevenSumTableData.rewardType == "it")
		{
			List<string> listItemId = new List<string>();
			for (int i = 0; i < sevenSumTableData.rewardCount; ++i)
				listItemId.Add(sevenSumTableData.rewardValue);
			string checkSum2 = "";
			List<ItemGrantRequest> listItemGrantRequest = GenerateGrantInfo(listItemId, ref checkSum2, ItemId2InitDataType(sevenSumTableData.rewardValue));
			request = new ExecuteCloudScriptRequest()
			{
				FunctionName = "GetSevenDaysSumReward",
				FunctionParameter = new { SdGrpId = (int)MissionData.instance.sevenDaysId, Cnt = sevenSumTableData.count, InfCs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
				GeneratePlayStreamEvent = true,
			};
		}
		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				MissionData.instance.OnRecvGetSevenDaysSumReward(sevenSumTableData.count);

				CurrencyData.instance.OnRecvProductReward(sevenSumTableData.rewardType, sevenSumTableData.rewardValue, sevenSumTableData.rewardCount);

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Festival
	public void RequestStartFestival(int newGroupId, int collectGivenTime, int exchangeGivenTime, int coolTime, Action successCallback, Action failureCallback)
	{
		string input = string.Format("{0}_{1}_{2}_{3}_{4}", newGroupId, collectGivenTime, exchangeGivenTime, coolTime, "vmpqdfax");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "StartFestival",
			FunctionParameter = new { FsGrpId = newGroupId, GiTim = collectGivenTime, GiTim2 = exchangeGivenTime, CoTim = coolTime, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				jsonResult.TryGetValue("date", out object date);

				// 성공시에는 서버에서 방금 기록한 유효기간 만료 시간이 날아온다.
				FestivalData.instance.OnRecvFestivalStartInfo((string)date);

				jsonResult.TryGetValue("date2", out object date2);
				FestivalData.instance.OnRecvFestivalStart2Info((string)date2);

				jsonResult.TryGetValue("cool", out object useCool);
				if ((useCool.ToString()) == "1")
				{
					jsonResult.TryGetValue("cdate", out object cdate);
					FestivalData.instance.OnRecvFestivalCoolTimeInfo((string)cdate);
				}

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			//HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
	}

	public void RequestFestivalProceedingCount(int type, int addCount, int expectCount, Action successCallback)
	{
		string input = string.Format("{0}_{1}_{2}_{3}", type, addCount, expectCount, "rfsouimz");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "FestivalProceedingCount",
			FunctionParameter = new { Tp = type, Add = addCount, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (failure)
				HandleCommonError();
			else
			{
				FestivalData.instance.SetProceedingCount(type, addCount, expectCount);
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestGetFestivalCollect(FestivalCollectTableData festivalCollectTableData, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}", FestivalData.instance.festivalId, festivalCollectTableData.num, festivalCollectTableData.key, "vdrwpljz");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetFestivalCollect",
			FunctionParameter = new { FsGrpId = (int)FestivalData.instance.festivalId, Num = festivalCollectTableData.num, InfCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				FestivalData.instance.festivalSumPoint += festivalCollectTableData.festivalPoint;
				FestivalData.instance.OnRecvGetFestivalCollect(festivalCollectTableData.num);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestFestivalExchange(FestivalExchangeTableData festivalExchangeTableData, int baseCount, Action<string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", FestivalData.instance.festivalId, festivalExchangeTableData.num, baseCount, festivalExchangeTableData.key, "rzcepikn");
		string checkSum = CheckSum(input);

		ExecuteCloudScriptRequest request = null;
		if (festivalExchangeTableData.rewardType == "cu")
		{
			request = new ExecuteCloudScriptRequest()
			{
				FunctionName = "FestivalExchange",
				FunctionParameter = new { FsGrpId = (int)FestivalData.instance.festivalId, Num = festivalExchangeTableData.num, Cnt = baseCount, Cs = checkSum },
				GeneratePlayStreamEvent = true,
			};
		}
		else if (festivalExchangeTableData.rewardType == "it")
		{
			List<string> listItemId = new List<string>();
			for (int i = 0; i < baseCount; ++i)
			{
				for (int j = 0; j < festivalExchangeTableData.rewardCount; ++j)
					listItemId.Add(festivalExchangeTableData.rewardValue);
			}
			string checkSum2 = "";
			List<ItemGrantRequest> listItemGrantRequest = GenerateGrantInfo(listItemId, ref checkSum2, ItemId2InitDataType(festivalExchangeTableData.rewardValue));
			request = new ExecuteCloudScriptRequest()
			{
				FunctionName = "FestivalExchange",
				FunctionParameter = new { FsGrpId = (int)FestivalData.instance.festivalId, Num = festivalExchangeTableData.num, Cnt = baseCount, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
				GeneratePlayStreamEvent = true,
			};
		}

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				FestivalData.instance.festivalSumPoint -= festivalExchangeTableData.neededCount * baseCount;
				FestivalData.instance.OnRecvFestivalExchange(festivalExchangeTableData.num, baseCount);

				CurrencyData.instance.OnRecvProductReward(festivalExchangeTableData.rewardType, festivalExchangeTableData.rewardValue, festivalExchangeTableData.rewardCount);

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Ranking
	public void RequestRegisterName(string name, Action successCallback, Action<PlayFabError> failureCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest()
		{
			DisplayName = name,
		}, (success) =>
		{
			WaitingNetworkCanvas.Show(false);

			PlayerData.instance.displayName = name;
			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			WaitingNetworkCanvas.Show(false);

			if (error.Error == PlayFabErrorCode.InvalidParams || error.Error == PlayFabErrorCode.NameNotAvailable)
			{
				if (failureCallback != null) failureCallback.Invoke(error);
				return;
			}
			HandleCommonError(error);
		});
	}

	public void RequestGetStageRanking(Action<List<PlayerLeaderboardEntry>, List<PlayerLeaderboardEntry>> successCallback)
	{
		// 두번으로 나눠받아야하니 이렇게 처리한다.
		_leaderboardStageIndex = 0;
		_leaderboardStageCheatIndex = 0;
		_leaderboardStageSuccessCallback = successCallback;

		PlayerProfileViewConstraints playerProfileViewConstraints = new PlayerProfileViewConstraints();
		playerProfileViewConstraints.ShowDisplayName = true;
		playerProfileViewConstraints.ShowLocations = true;

		PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest()
		{
			MaxResultsCount = 100,
			ProfileConstraints = playerProfileViewConstraints,
			StartPosition = 0,
			StatisticName = "highestClearStage",
		}, (success) =>
		{
			OnRecvGetLeaderboard(success.Leaderboard);
		}, (error) =>
		{
			// wait 캔버스 없이 하는거니 에러처리 하지 않기로 한다.
			//HandleCommonError(error);
		});

		PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest()
		{
			MaxResultsCount = 100,
			ProfileConstraints = playerProfileViewConstraints,
			StartPosition = 100,
			StatisticName = "highestClearStage",
		}, (success) =>
		{
			OnRecvGetLeaderboard(success.Leaderboard);
		}, (error) =>
		{
			//HandleCommonError(error);
		});

		PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest()
		{
			MaxResultsCount = 100,
			ProfileConstraints = playerProfileViewConstraints,
			StartPosition = 0,
			StatisticName = "chtRnkSus",
		}, (success) =>
		{
			OnRecvGetCheatLeaderboard(success.Leaderboard);
		}, (error) =>
		{
			//HandleCommonError(error);
		});
	}

	int _leaderboardStageIndex = 0;
	Action<List<PlayerLeaderboardEntry>, List<PlayerLeaderboardEntry>> _leaderboardStageSuccessCallback;
	List<PlayerLeaderboardEntry> _listResultLeaderboardStage;
	void OnRecvGetLeaderboard(List<PlayerLeaderboardEntry> leaderboard)
	{
		if (_leaderboardStageIndex == 0)
		{
			if (_listResultLeaderboardStage == null)
				_listResultLeaderboardStage = new List<PlayerLeaderboardEntry>();
			_listResultLeaderboardStage.Clear();

			_listResultLeaderboardStage.AddRange(leaderboard);
			++_leaderboardStageIndex;
		}
		else if (_leaderboardStageIndex == 1)
		{
			_listResultLeaderboardStage.AddRange(leaderboard);
			++_leaderboardStageIndex;

			CheckRecvLeaderboard();
		}
		else if (_leaderboardStageIndex == 2)
		{
			// something wrong
		}
	}

	int _leaderboardStageCheatIndex = 0;
	List<PlayerLeaderboardEntry> _listCheatLeaderboardStage;
	void OnRecvGetCheatLeaderboard(List<PlayerLeaderboardEntry> leaderboard)
	{
		if (_listCheatLeaderboardStage == null)
			_listCheatLeaderboardStage = new List<PlayerLeaderboardEntry>();
		_listCheatLeaderboardStage.Clear();
		_listCheatLeaderboardStage.AddRange(leaderboard);
		++_leaderboardStageCheatIndex;

		CheckRecvLeaderboard();
	}

	void CheckRecvLeaderboard()
	{
		if (_leaderboardStageCheatIndex == 1 && _leaderboardStageIndex == 2)
		{
			if (_leaderboardStageSuccessCallback != null)
				_leaderboardStageSuccessCallback.Invoke(_listResultLeaderboardStage, _listCheatLeaderboardStage);
		}
	}

	public void RequestGetRanking(string statisticName, Action<List<PlayerLeaderboardEntry>> successCallback)
	{
		PlayerProfileViewConstraints playerProfileViewConstraints = new PlayerProfileViewConstraints();
		playerProfileViewConstraints.ShowDisplayName = true;
		playerProfileViewConstraints.ShowLocations = true;

		PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest()
		{
			MaxResultsCount = 100,
			ProfileConstraints = playerProfileViewConstraints,
			StartPosition = 0,
			StatisticName = statisticName,
		}, (success) =>
		{
			if (successCallback != null) successCallback.Invoke(success.Leaderboard);
		}, (error) =>
		{
			HandleCommonError(error);
		});

		PlayFabClientAPI.GetLeaderboard(new GetLeaderboardRequest()
		{
			MaxResultsCount = 100,
			ProfileConstraints = playerProfileViewConstraints,
			StartPosition = 100,
			StatisticName = statisticName,
		}, (success) =>
		{
			if (successCallback != null) successCallback.Invoke(success.Leaderboard);
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestRecordBattlePower(int power, Action successCallback)
	{
		string input = string.Format("{0}_{1}_{2}", (int)RankingData.instance.recordBattlePowerIndex, power, "efcpujen");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "RecordBattlePower",
			FunctionParameter = new { Pow = power, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				RankingData.instance.recordBattlePowerIndex += 1;
				if (RankingData.instance.recordBattlePowerIndex == 100000000) RankingData.instance.recordBattlePowerIndex = 0;
				RankingData.instance.highestBattlePower = power;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			//HandleCommonError(error);
		});
	}
	#endregion


	#region Spell
	public void RequestOpenSpellBox(List<ObscuredString> listSpellId, int baseCount, int price, bool more, Action<string> successCallback)
	{
		// RandomBoxScreenCanvas에서 컨트롤할거니 여기서는 하지 않는다.
		//WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}", baseCount, price, more ? 1 : 0, "ewomvjsa");
		string checkSum = CheckSum(input);
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listSpellId, ref checkSum2, "spell");
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenSpellBox",
			FunctionParameter = new { BasCnt = baseCount, Pr = price, More = more ? 1 : 0, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				//WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.dia -= price;
				
				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeSpellGacha(List<ObscuredString> listSpellId, int fixedStar, Action<string> successCallback)
	{
		// RandomBoxScreenCanvas에서 컨트롤할거니 여기서는 하지 않는다.
		//WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", listSpellId.Count, fixedStar, "fosrqpmx");
		string checkSum = CheckSum(input);
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listSpellId, ref checkSum2, "spell");
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeSpellGacha",
			FunctionParameter = new { Star = fixedStar, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				//WaitingNetworkCanvas.Show(false);

				switch (fixedStar)
				{
					case 3: CashShopData.instance.ConsumeCount(CashShopData.eCashConsumeCountType.Spell3Gacha, listSpellId.Count); break;
					case 4: CashShopData.instance.ConsumeCount(CashShopData.eCashConsumeCountType.Spell4Gacha, listSpellId.Count); break;
					case 5: CashShopData.instance.ConsumeCount(CashShopData.eCashConsumeCountType.Spell5Gacha, listSpellId.Count); break;
					default: CashShopData.instance.ConsumeCount(CashShopData.eCashConsumeCountType.SpellGacha, listSpellId.Count); break;
				}

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestLevelUpSpell(SpellData spellData, int targetLevel, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}", (string)spellData.spellId, spellData.count, targetLevel, "zlireplm");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "LevelUpSpell",
			FunctionParameter = new { ItmId = (string)spellData.spellId, T = targetLevel, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				spellData.OnLevelUp(targetLevel);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestLevelUpTotalSpell(int targetLevel, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", targetLevel, price, "xlireplq");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "LevelUpTotalSpell",
			FunctionParameter = new { T = targetLevel, Pr = price, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.gold -= price;
				SpellManager.instance.OnLevelUpTotalSpell(targetLevel);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestSpellPressLevelUp(SpellData spellData, int prevSpellLevel, int prevGold, int spellLevel, int gold, int levelUpCount, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}_{7}_{8}", (string)spellData.spellId, spellData.cachedSkillTableData.grade, spellData.cachedSkillTableData.star, prevSpellLevel, prevGold, spellLevel, gold, levelUpCount, "zlireplx");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SpellPressLevelUp",
			FunctionParameter = new { ItmId = (string)spellData.spellId, grade = spellData.cachedSkillTableData.grade, star = spellData.cachedSkillTableData.star, PvLv = prevSpellLevel, PvGo = prevGold, Lv = spellLevel, Go = gold, LvCnt = levelUpCount, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);
				
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestTotalSpellPressLevelUp(int prevTotalSpellLevel, int prevGold, int totalSpellLevel, int gold, int levelUpCount, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", prevTotalSpellLevel, prevGold, totalSpellLevel, gold, levelUpCount, "xliseplz");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "TotalSpellPressLevelUp",
			FunctionParameter = new { PvLv = prevTotalSpellLevel, PvGo = prevGold, Lv = totalSpellLevel, Go = gold, LvCnt = levelUpCount, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Character
	public void RequestOpenCharacterBox(List<ObscuredString> listActorId, int baseCount, int price, Action<string> successCallback)
	{
		// RandomBoxScreenCanvas에서 컨트롤할거니 여기서는 하지 않는다.
		//WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", baseCount, price, "vnxjalpr");
		string checkSum = CheckSum(input);
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listActorId, ref checkSum2, "character");
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenCharacterBox",
			FunctionParameter = new { BasCnt = baseCount, Pr = price, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				//WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.dia -= price;

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestOpenPickUpCharacterBox(List<ObscuredString> listActorId, int baseCount, int price, int notStreakCountResult, Action<string> successCallback)
	{
		// RandomBoxScreenCanvas에서 컨트롤할거니 여기서는 하지 않는다.
		//WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}", baseCount, price, notStreakCountResult, "xvjwapqm");
		string checkSum = CheckSum(input);
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listActorId, ref checkSum2, "character");
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenPickUpCharacterBox",
			FunctionParameter = new { BasCnt = baseCount, Pr = price, StrCnt = notStreakCountResult, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				//WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.dia -= price;

				jsonResult.TryGetValue("date", out object date);
				CashShopData.instance.OnRecvPickUpCharacterCount((string)date, notStreakCountResult);

				jsonResult.TryGetValue("itmRet", out object itmRet);
				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeCharacterGacha(List<ObscuredString> listActorId, Action<string> successCallback)
	{
		// RandomBoxScreenCanvas에서 컨트롤할거니 여기서는 하지 않는다.
		//WaitingNetworkCanvas.Show(true);

		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listActorId, ref checkSum2, "character");
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeCharacterGacha",
			FunctionParameter = new { Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				//WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeCount(CashShopData.eCashConsumeCountType.CharacterGacha, CashShopData.instance.GetConsumeCount(CashShopData.eCashConsumeCountType.CharacterGacha));

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestSelectTeamPosition(string actorId, int positionIndex, int prevSwapIndex, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}", actorId, positionIndex, prevSwapIndex, "redsmnap");
		string checkSum = CheckSum(input);
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "SelectTeamPosition",
			FunctionParameter = new { ItmId = actorId, Pos = positionIndex, PrevSwap = prevSwapIndex, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				if (prevSwapIndex != -1)
					CharacterManager.instance.listTeamPositionId[prevSwapIndex] = CharacterManager.instance.listTeamPositionId[positionIndex];
				CharacterManager.instance.listTeamPositionId[positionIndex] = actorId;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestCharacterPressLevelUp(CharacterData characterData, int prevCharacterLevel, int prevGold, int characterLevel, int gold, int levelUpCount, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}_{7}", (string)characterData.actorId, characterData.cachedActorTableData.grade, prevCharacterLevel, prevGold, characterLevel, gold, levelUpCount, "zlireplx");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CharacterPressLevelUp",
			FunctionParameter = new { ItmId = (string)characterData.actorId, grade = characterData.cachedActorTableData.grade, PvLv = prevCharacterLevel, PvGo = prevGold, Lv = characterLevel, Go = gold, LvCnt = levelUpCount, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestCharacterTranscend(CharacterData characterData, int targetLevel, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", (string)characterData.actorId, characterData.count, characterData.cachedActorTableData.grade, targetLevel, price, "rxcjklap");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CharacterTranscend",
			FunctionParameter = new { ItmId = (string)characterData.actorId, grade = characterData.cachedActorTableData.grade, T = targetLevel, Pr = price, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.gold -= price;

				characterData.OnTranscendLevelUp(targetLevel);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Pet
	public void RequestGetFirstPet(string petId, Action<string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}", petId, "frklzpqi");
		string checkSum = CheckSum(input);
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetFirstPet",
			FunctionParameter = new { ItmId = petId, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				PetManager.instance.activePetId = petId;
				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestSearchPetList(List<ObscuredString> listSearchPetId, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		List<string> listId = new List<string>();
		for (int i = 0; i < listSearchPetId.Count; ++i)
			listId.Add(listSearchPetId[i]);

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		string jsonSearchLst = serializer.SerializeObject(listId);

		string input = string.Format("{0}_{1}", jsonSearchLst, "rqpjfers");
		string checkSum = CheckSum(input);
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "SearchPet",
			FunctionParameter = new { SrchLst = listId, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				PetManager.instance.SetSearchInfo(listSearchPetId);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestSelectActivePet(string petId, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}", petId, "sjoperwl");
		string checkSum = CheckSum(input);
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "SelectActivePet",
			FunctionParameter = new { ItmId = petId, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				PetManager.instance.activePetId = petId;
				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestEndPet(int captureToolIndex, List<ObscuredString> listGainPetId, Action<string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}", captureToolIndex, "qrioxkjm");
		string checkSum = CheckSum(input);
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listGainPetId, ref checkSum2, "pet");
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndPet",
			FunctionParameter = new { CapIdx = captureToolIndex, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				int useTicket = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyPet");
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.UseTicket, useTicket);
				CurrencyData.instance.UseTicket(useTicket);
				PetManager.instance.dailySearchCount += 1;
				PetManager.instance.GetInProgressSearchIdList().Clear();

				switch (captureToolIndex)
				{
					case 1: CashShopData.instance.ConsumeCount(CashShopData.eCashItemCountType.CaptureBetter, 1); break;
					case 2: CashShopData.instance.ConsumeCount(CashShopData.eCashItemCountType.CaptureBest, 1); break;
				}

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestHeartPet(PetData petData, int targetHeart, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", (string)petData.petId, petData.count, petData.heart, targetHeart, "xmrlpoqs");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "HeartPet",
			FunctionParameter = new { ItmId = (string)petData.petId, T = targetHeart, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				int diff = targetHeart - petData.heart;
				PetManager.instance.dailyHeartCount += diff;

				petData.OnHeartPlus(targetHeart);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestPetMaxCount(PetData petData, int targetLevel, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", (string)petData.petId, petData.count, petData.cachedPetTableData.star, targetLevel, price, "reouzalw");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "PetMaxCount",
			FunctionParameter = new { ItmId = (string)petData.petId, star = petData.cachedPetTableData.star, T = targetLevel, Pr = price, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.gold -= price;

				petData.OnMaxLevelUp(targetLevel);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestStartPetSale(string startPetSaleId, int givenTime, int coolTime, Action successCallback)
	{
		string input = string.Format("{0}_{1}_{2}_{3}", startPetSaleId, givenTime, coolTime, "rdlipasc");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "StartPetSale",
			FunctionParameter = new { PtsId = startPetSaleId, GiTim = givenTime, CoTim = coolTime, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				jsonResult.TryGetValue("date", out object date);
				PetManager.instance.OnRecvStartPetSale(startPetSaleId, (string)date);

				jsonResult.TryGetValue("cdate", out object cdate);
				PetManager.instance.OnRecvCoolTimePetSale((string)cdate);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumePetSale(List<ObscuredString> listGainPetId, Action<string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listGainPetId, ref checkSum2, "pet");
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumePetSale",
			FunctionParameter = new { Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.PetSale);

				// Now로 바꿔서 더이상 되돌아올 수 없게 한다. 재접하면 DB갱신되어있을테니 알아서 과거로 처리될거다.
				PetManager.instance.petSaleExpireTime = ServerTime.UtcNow;

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumePetPass(Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		int givenTime = BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetPassGivenTime");
		string input = string.Format("{0}_{1}", givenTime, "wopnzalx");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumePetPass",
			FunctionParameter = new { GiTim = givenTime, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.PetPass);

				jsonResult.TryGetValue("date", out object date);
				PetManager.instance.OnRecvPetPessExpireInfo((string)date);
				PassManager.instance.OnChangedStatus();

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeTeamPass(Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		int givenTime = BattleInstanceManager.instance.GetCachedGlobalConstantInt("TeamPassGivenTime");
		string input = string.Format("{0}_{1}", givenTime, "xpvzqkjs");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeTeamPass",
			FunctionParameter = new { GiTim = givenTime, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.TeamPass);

				jsonResult.TryGetValue("date", out object date);
				CharacterManager.instance.OnRecvTeamPessExpireInfo((string)date);
				PassManager.instance.OnChangedStatus();

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Equip
	public void RequestOpenEquipBox(List<ObscuredString> listEquipId, int baseCount, int price, Action<string> successCallback)
	{
		// RandomBoxScreenCanvas에서 컨트롤할거니 여기서는 하지 않는다.
		//WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", baseCount, price, "diowaxpq");
		string checkSum = CheckSum(input);
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listEquipId, ref checkSum2, "equip");
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenEquipBox",
			FunctionParameter = new { BasCnt = baseCount, Pr = price, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				//WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.dia -= price;

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestOpenPickUpEquipBox(List<ObscuredString> listEquipId, int baseCount, int price, int notStreakCount1Result, int notStreakCount2Result, Action<string> successCallback)
	{
		// RandomBoxScreenCanvas에서 컨트롤할거니 여기서는 하지 않는다.
		//WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", baseCount, price, notStreakCount1Result, notStreakCount2Result, "erplsnqz");
		string checkSum = CheckSum(input);
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listEquipId, ref checkSum2, "equip");
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenPickUpEquipBox",
			FunctionParameter = new { BasCnt = baseCount, Pr = price, StrCnt1 = notStreakCount1Result, StrCnt2 = notStreakCount2Result, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				//WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.dia -= price;

				jsonResult.TryGetValue("date", out object date);
				CashShopData.instance.OnRecvPickUpEquipCount((string)date, notStreakCount1Result, notStreakCount2Result);

				jsonResult.TryGetValue("itmRet", out object itmRet);
				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeEquipGacha(List<ObscuredString> listEquipId, Action<string> successCallback)
	{
		// RandomBoxScreenCanvas에서 컨트롤할거니 여기서는 하지 않는다.
		//WaitingNetworkCanvas.Show(true);

		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listEquipId, ref checkSum2, "equip");
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeEquipGacha",
			FunctionParameter = new { Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				//WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeCount(CashShopData.eCashConsumeCountType.EquipGacha, CashShopData.instance.GetConsumeCount(CashShopData.eCashConsumeCountType.EquipGacha));

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeEquipTypeGacha(List<ObscuredString> listEquipId, int equipGrade, int equipRarity, int equipType, CashShopData.eCashConsumeCountType equipTypeGachaConsumeType, Action<string> successCallback)
	{
		// RandomBoxScreenCanvas에서 컨트롤할거니 여기서는 하지 않는다.
		//WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", listEquipId.Count, equipGrade, equipRarity, equipType, "wplsfmzq");
		string checkSum = CheckSum(input);
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listEquipId, ref checkSum2, "equip");
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeEquipTypeGacha",
			FunctionParameter = new { Gr = equipGrade, Ra = equipRarity, Tp = equipType, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		};

		PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				//WaitingNetworkCanvas.Show(false);

				CashShopData.instance.ConsumeCount(equipTypeGachaConsumeType, CashShopData.instance.GetConsumeCount(equipTypeGachaConsumeType));

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestEquipList(List<EquipData> listEquipData, List<string> listUniqueId, List<int> listEquipPos, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", (string)listEquipData[0].equipId, listEquipPos[0], "kerqplum");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "EquipPosLst",
			FunctionParameter = new { IdLst = listUniqueId, PosLst = listEquipPos, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				for (int i = 0; i < listEquipData.Count; ++i)
					EquipManager.instance.OnEquip(listEquipData[i]);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestEquip(EquipData equipData, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", (string)equipData.equipId, equipData.cachedEquipTableData.equipType, "kszqproi");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "EquipPos",
			FunctionParameter = new { Id = (string)equipData.uniqueId, Pos = equipData.cachedEquipTableData.equipType, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				EquipManager.instance.OnEquip(equipData);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestUnequip(EquipData equipData, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}", (string)equipData.equipId, equipData.cachedEquipTableData.equipType, "kszqprox");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "UnequipPos",
			FunctionParameter = new { Id = (string)equipData.uniqueId, Pos = equipData.cachedEquipTableData.equipType, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				EquipManager.instance.OnUnequip(equipData);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestLockEquip(EquipData equipData, bool lockState, Action successCallback)
	{
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "LockEquip",
			FunctionParameter = new { Id = (string)equipData.uniqueId, Lck = lockState ? 1 : 0 },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				RetrySendManager.instance.OnSuccess();
				equipData.SetLock(lockState);
				if (successCallback != null) successCallback.Invoke();
			}, (error) =>
			{
				RetrySendManager.instance.OnFailure();
			});
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	public void RequestAutoCompositeEquip(List<ObscuredString> listEquipId, List<EquipData> listMaterialEquipData, Action<string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string checkSum = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listEquipId, ref checkSum, "equip");
		string checkSum2 = "";
		List<RevokeInventoryItemRequest> listRevokeRequest = GenerateRevokeInfo(listMaterialEquipData, ref checkSum2);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "AutoCompositeEquip",
			FunctionParameter = new { Lst = listItemGrantRequest, LstCs = checkSum, RvLst = listRevokeRequest, RvLstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				EquipManager.instance.OnRevokeInventory(listMaterialEquipData);

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestCompositeEquip(EquipData enhanceEquipData, bool equipped, int equipType, List<ObscuredString> listEquipId, List<EquipData> listMaterialEquipData, Action<string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string enhanceUniqueId = "";
		string enhanceEquipId = "";
		int nextEnhanceLevel = 0;
		if (enhanceEquipData != null)
		{
			enhanceUniqueId = enhanceEquipData.uniqueId;
			enhanceEquipId = enhanceEquipData.equipId;
			nextEnhanceLevel = enhanceEquipData.enhanceLevel + 1;
		}

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}", enhanceEquipId, listEquipId.Count, listMaterialEquipData.Count, nextEnhanceLevel, equipped ? 1 : 0, equipType, "mrqzlpas");
		string checkSum = CheckSum(input);
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listEquipId, ref checkSum2, "equip");
		string checkSum3 = "";
		List<RevokeInventoryItemRequest> listRevokeRequest = GenerateRevokeInfo(listMaterialEquipData, ref checkSum3);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CompositeEquip",
			FunctionParameter = new { EhId = enhanceUniqueId, Eq = equipped ? 1 : 0, Pos = equipType, T = nextEnhanceLevel, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2, RvLst = listRevokeRequest, RvLstCs = checkSum3 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				EquipManager.instance.OnRevokeInventory(listMaterialEquipData);

				if (enhanceEquipData != null)
					enhanceEquipData.OnEnhance(enhanceEquipData.enhanceLevel + 1);

				jsonResult.TryGetValue("itmRet", out object itmRet);

				string newEquipUniqueId = "";
				if (listEquipId.Count == 1 && equipped && enhanceEquipData == null)
				{
					// 새로 얻은 아이템을 장착시켜야한다.
					// 지금 함수 구조에서는 EquipData를 얻을 방법이 없기 때문에 instanceId를 기억해뒀다가 장착하는 형태로 해본다.
					List<ItemInstance> listItemInstance = PlayFabApiManager.instance.DeserializeItemGrantResult((string)itmRet);
					if (listItemInstance != null && listItemInstance.Count == 1)
						newEquipUniqueId = listItemInstance[0].ItemInstanceId;
				}

				if (successCallback != null) successCallback.Invoke((string)itmRet);

				// 위 콜백에서 인벤토리에 추가되어있을거다.
				if (listEquipId.Count == 1 && equipped && enhanceEquipData == null && string.IsNullOrEmpty(newEquipUniqueId) == false)
				{
					EquipData newEquipData = EquipManager.instance.FindEquipData(newEquipUniqueId, (EquipManager.eEquipSlotType)equipType);
					if (newEquipData != null)
						EquipManager.instance.OnEquip(newEquipData);
				}
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestEquipListByPurchase(Action successCallback)
	{
		// Equip 구매로 인해 인벤토리를 갱신하기 위해 만든 함수다.
		// 이게 갱신이 되어야 얻은 무기를 합성하거나 장착할 수 있어서 실패하면 에러로 넘긴다.
		PlayFabClientAPI.GetUserInventory(new GetUserInventoryRequest(), (success) =>
		{
			EquipManager.instance.OnRecvRefreshEquipInventory(success.Inventory);
			if (successCallback != null) successCallback.Invoke();
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Sub Mission
	public void RequestFortuneWheel(int reward, int useTicket, bool consume, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", (int)SubMissionData.instance.fortuneWheelDailyCount, reward, useTicket, consume ? 1 : 0, "rqoiurzs");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "FortuneWheel",
			FunctionParameter = new { AddGo = reward, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				SubMissionData.instance.fortuneWheelDailyCount += 1;
				CurrencyData.instance.gold += reward;
				if (useTicket > 0)
				{
					GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.FreeFortuneWheel);
					GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.UseTicket, useTicket);
					CurrencyData.instance.UseTicket(useTicket);
				}
				if (consume)
					CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.FortuneWheel);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestConsumeFortuneWheel(int reward, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeFortuneWheel",
			FunctionParameter = new { AddGo = reward },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				if (SubMissionData.instance.fortuneWheelDailyCount == 1)
					SubMissionData.instance.fortuneWheelDailyCount += 1;
				CurrencyData.instance.gold += reward;
				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.FortuneWheel);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestSelectRushDefenseMission(int selectedDifficulty)
	{
		string input = string.Format("{0}_{1}", selectedDifficulty, "ormzajpd");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SelectRushDefense",
			FunctionParameter = new { Sel = selectedDifficulty, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				SubMissionData.instance.rushDefenseSelectedLevel = selectedDifficulty;
			}
		}, (error) =>
		{
			//HandleCommonError(error);
		});
	}

	public void RequestEndRushDefenseMission(bool firstClear, int selectedDifficulty, int reward, int useTicket, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", (int)SubMissionData.instance.rushDefenseDailyCount, reward, useTicket, selectedDifficulty, firstClear ? 1 : 0, "ormzajpw");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndRushDefense",
			FunctionParameter = new { Sel = selectedDifficulty, Fir = firstClear ? 1 : 0, AddEn = reward, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				SubMissionData.instance.rushDefenseDailyCount += 1;
				CurrencyData.instance.OnRecvRefillEnergy(reward);
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.ClearRushDefense);
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.UseTicket, useTicket);
				CurrencyData.instance.UseTicket(useTicket);

				if (firstClear)
				{
					SubMissionData.instance.rushDefenseClearLevel = selectedDifficulty;

					int nextLevel = selectedDifficulty + 1;
					if (nextLevel > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxRushDefense"))
						nextLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxRushDefense");
					SubMissionData.instance.rushDefenseSelectedLevel = nextLevel;
				}
				else
					SubMissionData.instance.rushDefenseSelectedLevel = selectedDifficulty;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestSelectBossDefenseMission(int selectedDifficulty)
	{
		string input = string.Format("{0}_{1}", selectedDifficulty, "xprwalms");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SelectBossDefense",
			FunctionParameter = new { Sel = selectedDifficulty, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				SubMissionData.instance.bossDefenseSelectedLevel = selectedDifficulty;
			}
		}, (error) =>
		{
			//HandleCommonError(error);
		});
	}

	public void RequestEndBossDefenseMission(bool firstClear, int selectedDifficulty, int reward, int useTicket, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", (int)SubMissionData.instance.bossDefenseDailyCount, reward, useTicket, selectedDifficulty, firstClear ? 1 : 0, "xprwalmz");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndBossDefense",
			FunctionParameter = new { Sel = selectedDifficulty, Fir = firstClear ? 1 : 0, AddDi = reward, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				SubMissionData.instance.bossDefenseDailyCount += 1;
				CurrencyData.instance.dia += reward;
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.ClearBossDefense);
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.UseTicket, useTicket);
				CurrencyData.instance.UseTicket(useTicket);

				if (firstClear)
				{
					SubMissionData.instance.bossDefenseClearLevel = selectedDifficulty;

					int nextLevel = selectedDifficulty + 1;
					if (nextLevel > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossDefense"))
						nextLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossDefense");
					SubMissionData.instance.bossDefenseSelectedLevel = nextLevel;
				}
				else
					SubMissionData.instance.bossDefenseSelectedLevel = selectedDifficulty;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestSelectGoldDefenseMission(int selectedDifficulty)
	{
		string input = string.Format("{0}_{1}", selectedDifficulty, "kslrmzqa");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "SelectGoldDefense",
			FunctionParameter = new { Sel = selectedDifficulty, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				SubMissionData.instance.goldDefenseSelectedLevel = selectedDifficulty;
			}
		}, (error) =>
		{
			//HandleCommonError(error);
		});
	}

	public void RequestEndGoldDefenseMission(bool firstClear, int selectedDifficulty, int reward, int useTicket, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", (int)SubMissionData.instance.goldDefenseDailyCount, reward, useTicket, selectedDifficulty, firstClear ? 1 : 0, "lramqxpk");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndGoldDefense",
			FunctionParameter = new { Sel = selectedDifficulty, Fir = firstClear ? 1 : 0, AddGo = reward, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				SubMissionData.instance.goldDefenseDailyCount += 1;
				CurrencyData.instance.gold += reward;
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.ClearGoldDefense);
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.UseTicket, useTicket);
				CurrencyData.instance.UseTicket(useTicket);

				if (firstClear)
				{
					SubMissionData.instance.goldDefenseClearLevel = selectedDifficulty;

					int nextLevel = selectedDifficulty + 1;
					if (nextLevel > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxGoldDefense"))
						nextLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxGoldDefense");
					SubMissionData.instance.goldDefenseSelectedLevel = nextLevel;
				}
				else
					SubMissionData.instance.goldDefenseSelectedLevel = selectedDifficulty;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestEndRobotDefenseMission(int killCount, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", (int)SubMissionData.instance.robotDefenseDailyCount, (int)SubMissionData.instance.robotDefenseClearLevel, (int)SubMissionData.instance.robotDefenseRepeatClearCount, (int)SubMissionData.instance.robotDefenseKillCount, killCount, "irzqljma");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndRobotDefense",
			FunctionParameter = new { Kil = killCount, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				SubMissionData.instance.robotDefenseDailyCount += 1;
				SubMissionData.instance.robotDefenseKillCount += killCount;

				// 서버쪽에서도 알아서 한시간 넣을거다.
				SubMissionData.instance.robotDefenseCoolExpireTime = ServerTime.UtcNow + TimeSpan.FromHours(1);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestRobotDefenseStepUp(int useKillCount, List<ObscuredString> listEventItemId, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string checkSum = "";
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		checkSum = CheckSum(string.Format("{0}_{1}_{2}_{3}_{4}", useKillCount, (int)SubMissionData.instance.robotDefenseClearLevel, (int)SubMissionData.instance.robotDefenseRepeatClearCount, (int)SubMissionData.instance.robotDefenseKillCount, "rosqzalm"));
		string checkSum2 = "";
		List<ItemGrantRequest> listItemGrantRequest = GenerateGrantRequestInfo(listEventItemId, ref checkSum2);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "StepUpRobotDefense",
			FunctionParameter = new { UseKil = useKillCount, ClLv = (int)SubMissionData.instance.robotDefenseClearLevel, RpCnt = (int)SubMissionData.instance.robotDefenseRepeatClearCount, Cs = checkSum, Lst = listItemGrantRequest, LstCs = checkSum2 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				SubMissionData.instance.StepUpRobotDefense(useKillCount);
				
				if (listEventItemId.Count > 0 && listItemGrantRequest.Count > 0)
				{
					// RequestGacha 처리했던거럼 똑같이 컨슘만 있을거라 이렇게 처리한다.
					for (int i = 0; i < listEventItemId.Count; ++i)
						CashShopData.instance.OnRecvConsumeItem(listEventItemId[i], 1);
				}

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestLevelUpRobotDefenseCount(int targetLevel, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}", (int)SubMissionData.instance.robotDefenseDroneCountLevel, targetLevel, price, "mrqzalpu");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "LevelUpRobotDefenseCount",
			FunctionParameter = new { T = targetLevel, Pr = price, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				SubMissionData.instance.robotDefenseDroneCountLevel = targetLevel;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestLevelUpRobotDefenseAttack(int targetLevel, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}", (int)SubMissionData.instance.robotDefenseDroneAttackLevel, targetLevel, price, "rwqmplaz");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "LevelUpRobotDefenseAttack",
			FunctionParameter = new { T = targetLevel, Pr = price, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				SubMissionData.instance.OnLevelUpRobotDefenseAtkLevel(targetLevel);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region BossBattle
	// BossBattle역시 입장시마다 랜덤으로 된 숫자키를 하나 받는다.
	ObscuredString _serverEnterKeyForBossBattle;
	public void RequestEnterBossBattle(int selectedDifficulty, Action<bool> successCallback, Action failureCallback)
	{
		string input = string.Format("{0}_{1}", selectedDifficulty, "eimzkrnx");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "EnterBossBattle",
			FunctionParameter = new { Enter = 1, SeLv = selectedDifficulty, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			_serverEnterKeyForBossBattle = failure ? "" : resultString;
			if (successCallback != null) successCallback.Invoke(failure);
		}, (error) =>
		{
			HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
	}

	public void RequestCancelBossBattle()
	{
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CancelBossBattle",
			GeneratePlayStreamEvent = true,
		}, null, null);
	}

	public void RequestEndBossBattle(bool clear, int nextBossId, int playLevel, int useTicket, int useTicketForQuest, int addGold, int addDia, int addEnergy, Action<bool, int, string> successCallback)
	{
		string input = string.Format("{0}_{1}_{2}_{3}_{4}", (string)_serverEnterKeyForBossBattle, clear ? 1 : 0, nextBossId, playLevel, "rezslmnq");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndBossBattle",
			FunctionParameter = new { Flg = (string)_serverEnterKeyForBossBattle, Cl = (clear ? 1 : 0), Nb = nextBossId, PlLv = playLevel, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			_serverEnterKeyForBossBattle = "";
			if (!failure)
			{
				if (clear)
				{
					int addPoint = playLevel;
					if (MissionListCanvas.IsAlarmBossBattle())
						addPoint *= BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleDailyBonusTimes");
					SubMissionData.instance.bossBattlePoint += addPoint;
					SubMissionData.instance.bossBattleDailyCount += 1;
					GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.ClearBossBattle);
				}
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.UseTicket, useTicketForQuest);
				if (useTicket > 0)
					CurrencyData.instance.UseTicket(useTicket);

				CurrencyData.instance.dia += addDia;
				CurrencyData.instance.gold += addGold;
				if (addEnergy > 0)
					CurrencyData.instance.OnRecvRefillEnergy(addEnergy);

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke(clear, nextBossId, (string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestRefreshBoss(int nextBossId, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);
		string input = string.Format("{0}_{1}", nextBossId, "vkalqwmi");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "RefreshBoss",
			FunctionParameter = new { Nb = nextBossId, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				SubMissionData.instance.bossBattleId = nextBossId;

				// 클라이언트에서 먼저 삭제한 다음
				CurrencyData.instance.gold -= price;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestCallKingBoss(Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CallKingBoss",
			FunctionParameter = new { Call = 1 },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				int nextKingId = SubMissionData.instance.bossBattleClearId + 1;
				if (nextKingId <= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattle"))
					SubMissionData.instance.bossBattleId = nextKingId;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion

	#region Point Shop
	public void RequestBuyPointShopItem(int typeId, int index, int price, int rewardAmount, int key, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", typeId, index, rewardAmount, key, SubMissionData.instance.bossBattlePoint, "rplamqzs");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "BuyPointShopItem",
			FunctionParameter = new { TypeId = typeId, Idx = index, Pr = price, Rwd = rewardAmount, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			WaitingNetworkCanvas.Show(false);

			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				switch (typeId)
				{
					case 1: CurrencyData.instance.gold += rewardAmount; break;
					case 2: CurrencyData.instance.dia += rewardAmount; break;
					case 3: CurrencyData.instance.OnRecvRefillEnergy(rewardAmount); break;
				}
				SubMissionData.instance.bossBattlePoint -= price;

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestLevelUpPointShopAttack(int targetLevel, int price, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}", (int)SubMissionData.instance.bossBattleAttackLevel, targetLevel, price, "vrojbqse");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "LevelUpPointShopAttack",
			FunctionParameter = new { T = targetLevel, Pr = price, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			string resultString = (string)success.FunctionResult;
			bool failure = (resultString == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				SubMissionData.instance.bossBattlePoint -= price;
				SubMissionData.instance.OnLevelUpPointShopAttack(targetLevel);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion


	#region Attendance
	public void RequestStartAttendance(string startAttendanceId, int givenTime, bool oneTime, bool completeRefresh, Action successCallback, Action failureCallback)
	{
		string input = string.Format("{0}_{1}_{2}_{3}", startAttendanceId, givenTime, oneTime ? 1 : 0, "dmnapoqz");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "StartAttendance",
			FunctionParameter = new { AttId = startAttendanceId, GiTim = givenTime, OnTim = oneTime ? 1 : 0, CoRe = completeRefresh ? 1 : 0, Cs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				jsonResult.TryGetValue("date", out object date);

				// 성공시에는 서버에서 방금 기록한 유효기간 만료 시간이 날아온다.
				AttendanceData.instance.OnRecvAttendanceExpireInfo(startAttendanceId, oneTime, (string)date);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			//HandleCommonError(error);
			if (failureCallback != null) failureCallback.Invoke();
		});
	}

	public void RequestGetAttendanceReward(string rewardType, int key, int addDia, int addGold, int addEnergy, int earlyBonus, Action<string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", AttendanceData.instance.attendanceId, rewardType, earlyBonus, key, "orapzvqb");
		string infoCheckSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetAttendanceReward",
			FunctionParameter = new { Tp = rewardType, Early = earlyBonus, InfCs = infoCheckSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				// 성공시에는 서버에서 방금 기록한 마지막 수령 시간이 날아온다.
				jsonResult.TryGetValue("date", out object date);
				AttendanceData.instance.OnRecvRepeatLoginInfo((string)date);
				AttendanceData.instance.rewardReceiveCount += 1;

				CurrencyData.instance.dia += addDia;
				CurrencyData.instance.gold += addGold;

				AttendanceData.instance.earlyBonusDays = earlyBonus;
				if (earlyBonus > 0)
				{
					addEnergy += (BattleInstanceManager.instance.GetCachedGlobalConstantInt("AttendanceEarlyEnergy") * earlyBonus);

					// 마지막 earlyBonus를 받는 타이밍에는 이벤트를 빠르게 종료시키기 위해 조정된 expireTime이 날아온다.
					jsonResult.TryGetValue("adjustdate", out object adjustdate);
					AttendanceData.instance.OnRecvAttendanceExpireInfo((string)adjustdate);
				}
				if (addEnergy > 0)
					CurrencyData.instance.OnRecvRefillEnergy(addEnergy);

				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke((string)itmRet);
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}
	#endregion



	#region Sample
	// Sample 1. 콜백도 없고 재전송도 없을땐 이렇게 간단하게 처리
	public void RequestPlayerProfile()
	{
		GetPlayerProfileRequest request = new GetPlayerProfileRequest() { PlayFabId = playFabId };
		PlayFabClientAPI.GetPlayerProfile(request, OnGetPlayerProfileSuccess, OnGetPlayerProfileFailure);
	}

	void OnGetPlayerProfileSuccess(GetPlayerProfileResult result)
	{
	}

	void OnGetPlayerProfileFailure(PlayFabError error)
	{
	}

	// Sample 2. UI에서는 callback 필요할테니 이런식으로 처리한다.
	// 게다가 메인 캐릭터 설정은 재화를 소모하는 요청이 아니기 때문에 Retry도 적용할 수 있다.
	public void RequestChangeMainCharacter(string mainCharacterId, Action successCallback, Action failureCallback = null)
	{
		// 직접 Send하는 대신 RetrySendManager에게 맡긴다.
		GetPlayerProfileRequest request = new GetPlayerProfileRequest() { PlayFabId = playFabId };
		Action action = () =>
		{
			PlayFabClientAPI.GetPlayerProfile(request, OnChangeMainCharacterSuccess, OnChangeMainCharacterFailure);
		};
		RetrySendManager.instance.RequestAction(action, true);
	}

	void OnChangeMainCharacterSuccess(GetPlayerProfileResult result)
	{
		RetrySendManager.instance.OnSuccess();

		// 나머지 처리
		//
	}

	void OnChangeMainCharacterFailure(PlayFabError error)
	{
		// 이때만 재전송 할건가? 고민했었는데
		//error.Error = PlayFabErrorCode.ServiceUnavailable;
		//error.HttpCode = 400;
		// 어차피 Retry를 해도 되는 패킷이라고 한 이상 꼭 제한을 걸필요는 없을거 같았다. 우선은 어떤 실패를 해도 재시도 하는거로 처리
		RetrySendManager.instance.OnFailure();
	}



	// Sample 3. PlayFab에서 제공하는 함수의 리턴값이 필요한 경우에는 Sample 2.와는 조금 다르게 결과값을 넘겨줘야한다.
	// 아무래도 이게 제일 비중이 많을거 같은데
	// 어차피 이렇게 짤거라면 UI쪽에서 직접 PlayFab함수를 호출해서 처리하는게 더 깔끔한거 아닌가.
	// Retry도 필요없을테고.. 이건 UI처리하는 부분 생길때 다시 고민해보자.
	public void RequestNeedReturn(string mainCharacterId, Action<GetPlayerProfileResult> successCallback)
	{

	}
	#endregion
}