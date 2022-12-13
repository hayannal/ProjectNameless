using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class RankingData : MonoBehaviour
{
	public static RankingData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("RankingData")).AddComponent<RankingData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static RankingData _instance = null;

	List<string> _listRankingDelInfo;

	public class DisplayRankingInfo
	{
		public string playFabId;
		public string displayName;
		public int value;
		public int ranking;
		public int orderIndex;
	}
	List<DisplayRankingInfo> _listDisplayStageRankingInfo = new List<DisplayRankingInfo>();
	public List<DisplayRankingInfo> listDisplayStageRankingInfo { get { return _listDisplayStageRankingInfo; } }

	#region BattlePower
	public ObscuredInt recordBattlePowerIndex { get; set; }
	public ObscuredInt highestBattlePower { get; set; }
	#endregion

	public DateTime rankingRefreshTime { get; private set; }

	void Update()
	{
		UpdateRefreshRankingData();
	}

	public void OnRecvRankingData(Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

		_listRankingDelInfo = null;
		if (titleData.ContainsKey("rnkBan"))
			_listRankingDelInfo = serializer.DeserializeObject<List<string>>(titleData["rnkBan"]);

		#region BattePower
		recordBattlePowerIndex = 0;
		if (userReadOnlyData.ContainsKey("recordPowerIdx"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["recordPowerIdx"].Value, out intValue))
				recordBattlePowerIndex = intValue;
		}

		highestBattlePower = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "highestBattlePower")
			{
				highestBattlePower = playerStatistics[i].Value;
				break;
			}
		}
		#endregion

		rankingRefreshTime = ServerTime.UtcNow + TimeSpan.FromSeconds(2);
	}

	List<PlayerLeaderboardEntry> _listCheatRankSusEntry;
	void UpdateRefreshRankingData()
	{
		if (DateTime.Compare(ServerTime.UtcNow, rankingRefreshTime) < 0)
			return;

		rankingRefreshTime = ServerTime.UtcNow + TimeSpan.FromMinutes(5);

		// WaitNetwork 없이 패킷 보내서 응답이 오면 갱신해둔다.
		PlayFabApiManager.instance.RequestGetStageRanking((rankLeaderboard, cheatLeaderboard) =>
		{
			// 다른 랭킹에서도 써야하니 저장해놓기로 한다.
			_listCheatRankSusEntry = cheatLeaderboard;

			// 스테이지 랭킹은 기존 구조대로 5분마다 받아와서 갱신시켜둔다.
			RecreateRankingData(rankLeaderboard, _listDisplayStageRankingInfo);
		});
	}

	void RecreateRankingData(List<PlayerLeaderboardEntry> listPlayerLeaderboardEntry, List<DisplayRankingInfo> listDisplayRankingInfo)
	{
		// 공용 함수로 만들어서 사용하기로 한다.

		// 100개씩 받을 수 있기 때문에 0 ~ 99 그룹과 100 ~ 199 그룹으로 받아서 합쳐놨을텐데 어느거가 앞에 있을진 모르니 리스트 구축하면서 찾아볼 것.
		listDisplayRankingInfo.Clear();

		// 얼마나 빠질지 추가될지 모르니 우선 다 넣고 정렬 돌리는게 맞는거 같다.
		for (int i = 0; i < listPlayerLeaderboardEntry.Count; ++i)
		{
			if (_listRankingDelInfo != null && _listRankingDelInfo.Contains(listPlayerLeaderboardEntry[i].PlayFabId))
				continue;

			bool cheatRankSus = false;
			if (_listCheatRankSusEntry != null)
			{
				for (int j = 0; j < _listCheatRankSusEntry.Count; ++j)
				{
					if (_listCheatRankSusEntry[j].StatValue > 0 && _listCheatRankSusEntry[j].PlayFabId == listPlayerLeaderboardEntry[i].PlayFabId)
					{
						cheatRankSus = true;
						break;
					}
				}
			}
			if (cheatRankSus)
				continue;

			DisplayRankingInfo info = new DisplayRankingInfo();
			info.playFabId = listPlayerLeaderboardEntry[i].PlayFabId;
			info.displayName = listPlayerLeaderboardEntry[i].Profile.DisplayName;
			if (string.IsNullOrEmpty(info.displayName)) info.displayName = string.Format("Nameless_{0}", info.playFabId.Substring(0, 5));
			info.value = listPlayerLeaderboardEntry[i].StatValue;
			info.orderIndex = i;
			listDisplayRankingInfo.Add(info);
		}

		// 정렬
		listDisplayRankingInfo.Sort(delegate (DisplayRankingInfo x, DisplayRankingInfo y)
		{
			if (x.value > y.value) return -1;
			else if (x.value < y.value) return 1;
			if (x.orderIndex < y.orderIndex) return -1;
			else if (x.orderIndex > y.orderIndex) return 1;
			return 0;
		});

		if (listDisplayRankingInfo.Count > 100)
			listDisplayRankingInfo.RemoveRange(100, listDisplayRankingInfo.Count - 100);

		// ranking을 매겨둔다.
		int ranking = 1;
		int duplicateCount = 0;
		int lastValue = 0;
		for (int i = 0; i < listDisplayRankingInfo.Count; ++i)
		{
			if (i == 0)
			{
				listDisplayRankingInfo[i].ranking = ranking;
				lastValue = listDisplayRankingInfo[i].value;
				continue;
			}

			if (lastValue == listDisplayRankingInfo[i].value)
			{
				listDisplayRankingInfo[i].ranking = ranking;
				++duplicateCount;
			}
			else
			{
				ranking = ranking + duplicateCount + 1;
				listDisplayRankingInfo[i].ranking = ranking;
				duplicateCount = 0;
				lastValue = listDisplayRankingInfo[i].value;
			}
		}
	}

	#region BattlePower
	// 미리 받아두기엔 너무 많아져서 요청시 받는 구조로 바꾼다.
	// 두번으로 나눠받아야하니 이렇게 처리한다.
	int _leaderboardPowerIndex = 0;
	List<PlayerLeaderboardEntry> _listResultLeaderboardPower;
	public DateTime _lastBattlePowerDateTime;
	List<DisplayRankingInfo> _listDisplayPowerRankingInfo = new List<DisplayRankingInfo>();
	public List<DisplayRankingInfo> listDisplayPowerRankingInfo { get { return _listDisplayPowerRankingInfo; } }
	public void RequestBattlePowerRankingData(Action successCallback)
	{
		if (_listDisplayPowerRankingInfo.Count > 0 && ServerTime.UtcNow < _lastBattlePowerDateTime + TimeSpan.FromMinutes(5))
		{
			if (successCallback != null) successCallback.Invoke();
			return;
		}

		_leaderboardPowerIndex = 0;
		_powerRankSuccessCallback = successCallback;

		PlayFabApiManager.instance.RequestGetRanking("highestBattlePower", (leaderboard) =>
		{
			if (_leaderboardPowerIndex == 0)
			{
				if (_listResultLeaderboardPower == null)
					_listResultLeaderboardPower = new List<PlayerLeaderboardEntry>();
				_listResultLeaderboardPower.Clear();

				_listResultLeaderboardPower.AddRange(leaderboard);
				++_leaderboardPowerIndex;
			}
			else if (_leaderboardPowerIndex == 1)
			{
				_listResultLeaderboardPower.AddRange(leaderboard);
				++_leaderboardPowerIndex;

				CheckRecvLeaderboardPower();
			}
			else if (_leaderboardPowerIndex == 2)
			{
				// something wrong
			}
		});

		_lastBattlePowerDateTime = ServerTime.UtcNow;
	}

	Action _powerRankSuccessCallback;
	void CheckRecvLeaderboardPower()
	{
		if (_leaderboardPowerIndex == 2)
		{
			RecreateRankingData(_listResultLeaderboardPower, _listDisplayPowerRankingInfo);
			if (_powerRankSuccessCallback != null) _powerRankSuccessCallback.Invoke();
		}
	}
	#endregion
}