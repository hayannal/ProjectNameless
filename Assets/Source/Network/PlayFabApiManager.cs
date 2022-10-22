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
		if (CheckVersion(loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.PlayerStatistics, out needCheckResourceVersion) == false)
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
		MissionData.instance.OnRecvMissionData(loginResult.InfoResultPayload.UserReadOnlyData, loginResult.InfoResultPayload.PlayerStatistics);
		RankingData.instance.OnRecvRankingData(loginResult.InfoResultPayload.TitleData);

		/*
		DailyShopData.instance.OnRecvShopData(loginResult.InfoResultPayload.TitleData, loginResult.InfoResultPayload.UserReadOnlyData);		
		QuestData.instance.OnRecvQuestData(loginResult.InfoResultPayload.UserReadOnlyData);
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
	bool CheckVersion(Dictionary<string, string> titleData, List<StatisticValue> playerStatistics, out bool needCheckResourceVersion)
	{
		needCheckResourceVersion = false;

		// 이 시점에서는 아직 PlayerData를 구축하기 전이니 이렇게 직접 체크한다.
		// highestPlayChapter로 체크해야 기기를 바꾸든 앱을 재설치 하든 데이터를 삭제하든 모든 상황에 대응할 수 있다.
		// 현재 계정 상태에 따라 다운로드 진행을 결정하는 것.
		/*
		int highestPlayChapter = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "highestPlayChapter")
			{
				highestPlayChapter = playerStatistics[i].Value;
				break;
			}
		}

		// 튜토리얼을 마치지 않았다면 앱 업뎃이든 번들패치든 할 필요 없다. 바로 리턴.
		if (highestPlayChapter == 0)
			return true;
		*/

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
		SpellManager.instance.OnRecvSpellInventory(_loginResult.InfoResultPayload.UserInventory, _loginResult.InfoResultPayload.UserData, _loginResult.InfoResultPayload.UserReadOnlyData, _loginResult.InfoResultPayload.PlayerStatistics);
		CostumeManager.instance.OnRecvCostumeInventory(_loginResult.InfoResultPayload.UserInventory, _loginResult.InfoResultPayload.PlayerStatistics);
		/*
		TimeSpaceData.instance.OnRecvEquipInventory(_loginResult.InfoResultPayload.UserInventory, _loginResult.InfoResultPayload.UserData, _loginResult.InfoResultPayload.UserReadOnlyData);
		*/
		PlayerData.instance.OnRecvPlayerData(_loginResult.InfoResultPayload.UserData, _loginResult.InfoResultPayload.UserReadOnlyData, _loginResult.InfoResultPayload.CharacterList, _loginResult.InfoResultPayload.PlayerProfile);
		PlayerData.instance.OnRecvCharacterList(_loginResult.InfoResultPayload.CharacterList, _dicCharacterStatisticsResult);
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

	public void RequestEndBoss(int selectedStage, Action successCallback)
	{
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "EndBoss",
			FunctionParameter = new { Flg = (string)_serverEnterKeyForBoss, SeLv = selectedStage },
			GeneratePlayStreamEvent = true,
		};
		Action action = () =>
		{
			PlayFabClientAPI.ExecuteCloudScript(request, (success) =>
			{
				PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
				jsonResult.TryGetValue("retErr", out object retErr);
				bool failure = ((retErr.ToString()) == "1");
				_serverEnterKeyForBoss = "";
				if (!failure)
				{
					RetrySendManager.instance.OnSuccess();

					// 성공시 처리
					int maxStage = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxStage");
					if (selectedStage <= maxStage)
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
	public void RequestGacha(int useEnergy, int resultGold, int resultEnergy, int resultBrokenEnergy, int resultEventPoint, int reserveRoomType, bool refreshTurn, int newTurn, int newGold, Action<bool> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		// hardcode ev6
		bool checkPayback = CashShopData.instance.IsShowEvent("ev6");

		int intRefreshTurn = refreshTurn ? 1 : 0;
		string input = string.Format("{0}_{1}_{2}_{3}_{4}_{5}_{6}_{7}_{8}_{9}", CurrencyData.instance.bettingCount + 1, useEnergy, resultGold, resultEnergy, resultBrokenEnergy, resultEventPoint, reserveRoomType, intRefreshTurn, newTurn, "azirjwlm");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Gacha",
			FunctionParameter = new { Cnt = CurrencyData.instance.bettingCount + 1, Bet = useEnergy, AddGo = resultGold, AddEn = resultEnergy, AddBrEn = resultBrokenEnergy, AddEv = resultEventPoint, ResRoomTp = reserveRoomType, RefreshTurn = intRefreshTurn, NewTurn = newTurn, NewGold = newGold, Cp = checkPayback ? 1 : 0, Cs = checkSum },
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
				CurrencyData.instance.brokenEnergy = Math.Min(CurrencyData.instance.brokenEnergy + resultBrokenEnergy, BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBrokenEnergy"));
				CurrencyData.instance.eventPoint += resultEventPoint;

				if (resultBrokenEnergy > 0)
					MainCanvas.instance.RefreshCashButton();

				if (useEnergy == resultEnergy)
				{
				}
				else if (useEnergy > resultEnergy)
					CurrencyData.instance.UseEnergy(useEnergy - resultEnergy);
				else if (useEnergy < resultEnergy)
					CurrencyData.instance.OnRecvRefillEnergy(resultEnergy - useEnergy, true);

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

	public void RequestReceiveMailPresent(string id, int receiveDay, string type, int addDia, int addGold, int addEnergy, Action<bool> successCallback)
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
	public void RequestOpenCashEvent(string openEventId, string eventSub, int givenTime, int coolTime, Action successCallback)
	{
		string input = string.Format("{0}_{1}_{2}_{3}_{4}", openEventId, eventSub, givenTime, coolTime, "ldruqzvm");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenCashEvent",
			FunctionParameter = new { EvId = openEventId, EvSub = eventSub, GiTim = givenTime, CoTim = coolTime, Cs = checkSum },
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
				CashShopData.instance.OnOpenCashEvent(openEventId, eventSub);

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

	public void RequestAnalysis(Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		// 쌓아둔 게이지를 초로 환산해서 누적할 준비를 한다.
		// 최초에 2분 30초 돌리자마자 쌓으면 150 쌓게될거다.
		AnalysisData.instance.PrepareAnalysis();

		// 이 패킷 역시 Invasion 했던거처럼 다양하게 보낸다. 오리진 재화 등등
		int addExp = AnalysisData.instance.cachedExpSecond;
		int currentExp = AnalysisData.instance.analysisExp;
		int resultGold = AnalysisData.instance.cachedResultGold;

		string checkSum = "";
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		checkSum = CheckSum(string.Format("{0}_{1}_{2}_{3}", addExp, currentExp, resultGold, "xzdliroa"));

		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "Analysis",
			FunctionParameter = new { Xp = addExp, CurXp = currentExp, ReGo = resultGold, Cs = checkSum },
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


				// 재화
				CurrencyData.instance.gold += resultGold;

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
	public void RequestCompleteGuideQuest(int currentGuideQuestIndex, string rewardType, int key, int addDia, int addGold, int addEnergy, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		// 퀘완료를 보내기전에 다음번에 받을 퀘의 진행상태를 체크
		int nextInitialProceedingCount = GuideQuestData.instance.CheckNextInitialProceedingCount();

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", currentGuideQuestIndex, rewardType, nextInitialProceedingCount, key, "witpnvfwk");
		string infoCheckSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "CompleteGuideQuest",
			FunctionParameter = new { Tp = rewardType, Np = nextInitialProceedingCount, InfCs = infoCheckSum },
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

				/*
				jsonResult.TryGetValue("adChrIdPay", out object adChrIdPayload);
				if (characterBox)
				{
					List<DropManager.CharacterPpRequest> listPpInfo = DropManager.instance.GetPowerPointInfo();
					int addBalancePp = DropManager.instance.GetLobbyBalancePpAmount();
					List<string> listGrantInfo = DropManager.instance.GetGrantCharacterInfo();
					List<DropManager.CharacterTrpRequest> listTrpInfo = DropManager.instance.GetTranscendPointInfo();

					++PlayerData.instance.questCharacterBoxOpenCount;
					if ((listTrpInfo.Count + listGrantInfo.Count) == 0)
						PlayerData.instance.notStreakCharCount += 2;
					else
						PlayerData.instance.notStreakCharCount = 0;

					// update
					PlayerData.instance.OnRecvUpdateCharacterStatistics(listPpInfo, listTrpInfo, addBalancePp);
					PlayerData.instance.OnRecvGrantCharacterList(adChrIdPayload);
				}

				jsonResult.TryGetValue("itmRet", out object itmRet);
				*/
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

	public void RequestConsumeBrokenEnergy(Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);
		
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "ConsumeBrokenEnergy",
			FunctionParameter = new { Con = 1 },
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
				CurrencyData.instance.brokenEnergy = 0;
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

				CashShopData.instance.ConsumeFlag(CashShopData.eCashConsumeFlagType.SevenSlot0 + buttonIndex);
				MissionData.instance.OnRecvPurchasedCashSlot(buttonIndex);
				
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
	#endregion


	#region SevenDays
	public void RequestStartSevenDays(int newGroupId, int givenTime, int coolTime, Action successCallback, Action failureCallback)
	{
		string input = string.Format("{0}_{1}_{2}_{3}", newGroupId, givenTime, coolTime, "vmpqalxj");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "StartSevenDays",
			FunctionParameter = new { SdGrpId = newGroupId, GiTim = givenTime, CoTim = coolTime, Cs = checkSum },
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

	public void RequestGetSevenDaysReward(SevenDaysRewardTableData sevenDaysRewardTableData, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}_{4}", MissionData.instance.sevenDaysId, sevenDaysRewardTableData.day, sevenDaysRewardTableData.num, sevenDaysRewardTableData.key, "qizolrms");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetSevenDaysReward",
			FunctionParameter = new { SdGrpId = (int)MissionData.instance.sevenDaysId, Day = sevenDaysRewardTableData.day, Num = sevenDaysRewardTableData.num, InfCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.OnRecvProductReward(sevenDaysRewardTableData.rewardType, sevenDaysRewardTableData.rewardValue, sevenDaysRewardTableData.rewardCount);
				MissionData.instance.sevenDaysSumPoint += sevenDaysRewardTableData.sumPoint;
				MissionData.instance.OnRecvGetSevenDaysReward(sevenDaysRewardTableData.day, sevenDaysRewardTableData.num);

				if (successCallback != null) successCallback.Invoke();
			}
		}, (error) =>
		{
			HandleCommonError(error);
		});
	}

	public void RequestGetSevenDaysSumReward(SevenSumTableData sevenSumTableData, Action successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string input = string.Format("{0}_{1}_{2}_{3}", MissionData.instance.sevenDaysId, sevenSumTableData.count, sevenSumTableData.key, "jfskeimz");
		string checkSum = CheckSum(input);
		PlayFabClientAPI.ExecuteCloudScript(new ExecuteCloudScriptRequest()
		{
			FunctionName = "GetSevenDaysSumReward",
			FunctionParameter = new { SdGrpId = (int)MissionData.instance.sevenDaysId, Cnt = sevenSumTableData.count, InfCs = checkSum },
			GeneratePlayStreamEvent = true,
		}, (success) =>
		{
			PlayFab.Json.JsonObject jsonResult = (PlayFab.Json.JsonObject)success.FunctionResult;
			jsonResult.TryGetValue("retErr", out object retErr);
			bool failure = ((retErr.ToString()) == "1");
			if (!failure)
			{
				WaitingNetworkCanvas.Show(false);

				CurrencyData.instance.OnRecvProductReward(sevenSumTableData.rewardType, sevenSumTableData.rewardValue, sevenSumTableData.rewardCount);
				MissionData.instance.OnRecvGetSevenDaysSumReward(sevenSumTableData.count);

				if (successCallback != null) successCallback.Invoke();
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
	#endregion


	#region Spell
	public void RequestSpellBox(List<string> listSpellId, Action<bool, string> successCallback)
	{
		WaitingNetworkCanvas.Show(true);

		string checkSum = "";
		List<ItemGrantRequest> listItemGrantRequest = SpellManager.instance.GenerateGrantInfo(listSpellId, ref checkSum);
		ExecuteCloudScriptRequest request = new ExecuteCloudScriptRequest()
		{
			FunctionName = "OpenSpellBox",
			FunctionParameter = new { Lst = listItemGrantRequest, LstCs = checkSum },
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
				
				jsonResult.TryGetValue("itmRet", out object itmRet);

				if (successCallback != null) successCallback.Invoke(failure, (string)itmRet);
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
			FunctionParameter = new { ItmId = (string)spellData.uniqueId, T = targetLevel, Cs = checkSum },
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