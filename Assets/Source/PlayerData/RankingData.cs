using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

	public DateTime rankingRefreshTime { get; private set; }

	void Update()
	{
		UpdateRefreshRankingData();
	}

	public void OnRecvRankingData(Dictionary<string, string> titleData)
	{
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

		_listRankingDelInfo = null;
		if (titleData.ContainsKey("rnkBan"))
			_listRankingDelInfo = serializer.DeserializeObject<List<string>>(titleData["rnkBan"]);

		rankingRefreshTime = ServerTime.UtcNow + TimeSpan.FromSeconds(3);
	}

	void UpdateRefreshRankingData()
	{
		if (DateTime.Compare(ServerTime.UtcNow, rankingRefreshTime) < 0)
			return;

		rankingRefreshTime = ServerTime.UtcNow + TimeSpan.FromMinutes(5);

		// WaitNetwork 없이 패킷 보내서 응답이 오면 갱신해둔다.
		PlayFabApiManager.instance.RequestGetStageRanking((rankLeaderboard, cheatLeaderboard) =>
		{
			RecreateStageRankingData(rankLeaderboard, cheatLeaderboard);
		});
	}

	void RecreateStageRankingData(List<PlayerLeaderboardEntry> listPlayerLeaderboardEntry, List<PlayerLeaderboardEntry> listCheatRankSusEntry)
	{
		// 100개씩 받을 수 있기 때문에 0 ~ 99 그룹과 100 ~ 199 그룹으로 받아서 합쳐놨을텐데 어느거가 앞에 있을진 모르니 리스트 구축하면서 찾아볼 것.
		_listDisplayStageRankingInfo.Clear();

		// 얼마나 빠질지 추가될지 모르니 우선 다 넣고 정렬 돌리는게 맞는거 같다.
		for (int i = 0; i < listPlayerLeaderboardEntry.Count; ++i)
		{
			if (_listRankingDelInfo != null && _listRankingDelInfo.Contains(listPlayerLeaderboardEntry[i].PlayFabId))
				continue;

			bool cheatRankSus = false;
			if (listCheatRankSusEntry != null)
			{
				for (int j = 0; j < listCheatRankSusEntry.Count; ++j)
				{
					if (listCheatRankSusEntry[j].StatValue > 0 && listCheatRankSusEntry[j].PlayFabId == listPlayerLeaderboardEntry[i].PlayFabId)
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
			_listDisplayStageRankingInfo.Add(info);
		}

		// 정렬
		_listDisplayStageRankingInfo.Sort(delegate (DisplayRankingInfo x, DisplayRankingInfo y)
		{
			if (x.value > y.value) return -1;
			else if (x.value < y.value) return 1;
			if (x.orderIndex < y.orderIndex) return -1;
			else if (x.orderIndex > y.orderIndex) return 1;
			return 0;
		});

		if (_listDisplayStageRankingInfo.Count > 100)
			_listDisplayStageRankingInfo.RemoveRange(100, _listDisplayStageRankingInfo.Count - 100);

		// ranking을 매겨둔다.
		int ranking = 1;
		int duplicateCount = 0;
		int lastValue = 0;
		for (int i = 0; i < _listDisplayStageRankingInfo.Count; ++i)
		{
			if (i == 0)
			{
				_listDisplayStageRankingInfo[i].ranking = ranking;
				lastValue = _listDisplayStageRankingInfo[i].value;
				continue;
			}

			if (lastValue == _listDisplayStageRankingInfo[i].value)
			{
				_listDisplayStageRankingInfo[i].ranking = ranking;
				++duplicateCount;
			}
			else
			{
				ranking = ranking + duplicateCount + 1;
				_listDisplayStageRankingInfo[i].ranking = ranking;
				duplicateCount = 0;
				lastValue = _listDisplayStageRankingInfo[i].value;
			}
		}
	}
}