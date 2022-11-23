using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class CurrencyData : MonoBehaviour
{
	public static CurrencyData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("CurrencyData")).AddComponent<CurrencyData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static CurrencyData _instance = null;

	public enum eCurrencyType
	{
		Gold,
		Diamond,
		Spin,
	}

	public static int s_MaxGold = 999999999;

	public static string GoldCode() { return "GO"; }
	public static string DiamondCode() { return "DI"; }
	public static string EnergyCode() { return "EN"; }

	public ObscuredInt gold { get; set; }
	public ObscuredInt energy { get; set; }
	public ObscuredInt energyMax { get; set; }
	public ObscuredInt dia { get; set; }			// 서버 상점에서 모아서 처리하는 기능이 없어서 free와 구매 다 합쳐서 처리하기로 한다.

	// 과금 요소. 클라이언트에 존재하면 무조건 굴려서 없애야하는거다. 인앱결제 결과를 받아놓는 저장소로 쓰인다.
	public ObscuredInt equipBoxKey { get; set; }
	public ObscuredInt legendEquipKey { get; set; }
	public ObscuredInt dailyDiaRemainCount { get; set; }

	#region Betting
	public ObscuredInt bettingCount { get; set; }
	public ObscuredInt brokenEnergy { get; set; }
	public ObscuredInt goldBoxTargetReward { get; set; }
	public ObscuredInt currentGoldBoxRoomReward { get; set; }
	public ObscuredInt goldBoxRemainTurn { get; set; }
	public ObscuredInt ticket { get; set; }
	public List<ObscuredInt> listBetInfo { get; set; }
	#endregion

	#region Event Point
	public ObscuredInt eventPoint { get; set; }
	public ObscuredString eventPointId { get; set; }
	public DateTime eventPointExpireTime { get; set; }
	public List<string> listEventPointOneTime { get; set; }
	#endregion

	public void OnRecvCurrencyData(Dictionary<string, int> userVirtualCurrency, Dictionary<string, VirtualCurrencyRechargeTime> userVirtualCurrencyRechargeTimes, Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics, Dictionary<string, string> titleData)
	{
		if (userVirtualCurrency.ContainsKey("GO"))
			gold = userVirtualCurrency["GO"];
		if (userVirtualCurrency.ContainsKey("DI"))
			dia = userVirtualCurrency["DI"];
		if (userVirtualCurrency.ContainsKey("EN"))
			energy = userVirtualCurrency["EN"];
		/*
		if (userVirtualCurrency.ContainsKey("LE"))	// 충전쿨이 길어서 현재수량만 기억해둔다.
			legendKey = userVirtualCurrency["LE"];
		if (userVirtualCurrency.ContainsKey("EQ"))
			equipBoxKey = userVirtualCurrency["EQ"];
		if (userVirtualCurrency.ContainsKey("LQ"))
			legendEquipKey = userVirtualCurrency["LQ"];
		if (userVirtualCurrency.ContainsKey("DA"))
			dailyDiaRemainCount = userVirtualCurrency["DA"];
		if (userVirtualCurrency.ContainsKey("RE"))
			returnScroll = userVirtualCurrency["RE"];
		if (userVirtualCurrency.ContainsKey("AN"))  // 충전쿨이 길어서 현재수량만 기억해둔다.
			analysisKey = userVirtualCurrency["AN"];
		*/

		if (userVirtualCurrencyRechargeTimes != null && userVirtualCurrencyRechargeTimes.ContainsKey("EN"))
		{
			energyMax = userVirtualCurrencyRechargeTimes["EN"].RechargeMax;
			if (userVirtualCurrencyRechargeTimes["EN"].SecondsToRecharge > 0 && energy < energyMax)
			{
				_rechargingEnergy = true;
				_energyRechargeTime = userVirtualCurrencyRechargeTimes["EN"].RechargeTime;
			}
			//TimeSpan timeSpan = userVirtualCurrencyRechargeTimes["EN"].RechargeTime - DateTime.UtcNow;
			//int totalSeconds = (int)timeSpan.TotalSeconds;
		}

		// 재화는 한정적인 자원이라 재화 안써도 되는건 통계써서 가져오기로 한다.
		bettingCount = 0;
		brokenEnergy = 0;
		ticket = 0;
		eventPoint = 0;
		goldBoxRemainTurn = 0;
		goldBoxTargetReward = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			switch (playerStatistics[i].StatisticName)
			{
				case "betCnt": bettingCount = playerStatistics[i].Value; break;
				case "brokenEnergy": brokenEnergy = playerStatistics[i].Value; break;
				case "ticket": ticket = playerStatistics[i].Value; break;
				case "eventPoint": eventPoint = playerStatistics[i].Value; break;
				case "goldBoxTurn": goldBoxRemainTurn = playerStatistics[i].Value; break;
				case "goldBoxValue": goldBoxTargetReward = playerStatistics[i].Value; break;
			}
		}

		// server boost list
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		listBetInfo = null;
		if (titleData.ContainsKey("gBst"))
		{
			string gachaBoostLstString = titleData["gBst"];
			if (string.IsNullOrEmpty(gachaBoostLstString) == false)
			{
				List<int> listBoost = serializer.DeserializeObject<List<int>>(gachaBoostLstString);
				if (listBoost != null && listBoost.Count > 0)
				{
					listBetInfo = new List<ObscuredInt>();
					for (int i = 0; i < listBoost.Count; ++i)
						listBetInfo.Add(listBoost[i]);
				}
			}
		}

		#region Event Point
		eventPointId = "";
		if (userReadOnlyData.ContainsKey("eventPointId"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["eventPointId"].Value) == false)
				eventPointId = userReadOnlyData["eventPointId"].Value;
		}

		if (userReadOnlyData.ContainsKey("eventPointExpDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["eventPointExpDat"].Value) == false)
			{
				DateTime expireDateTime = new DateTime();
				if (DateTime.TryParse(userReadOnlyData["eventPointExpDat"].Value, out expireDateTime))
					eventPointExpireTime = expireDateTime.ToUniversalTime();
			}
		}

		if (listEventPointOneTime == null)
			listEventPointOneTime = new List<string>();
		listEventPointOneTime.Clear();
		if (userReadOnlyData.ContainsKey("eventPointOneTimeLst"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["eventPointOneTimeLst"].Value) == false)
				listEventPointOneTime = serializer.DeserializeObject<List<string>>(userReadOnlyData["eventPointOneTimeLst"].Value);
		}
		#endregion


		// 최초로 계정을 생성해서 한번도 굴리지 않은 상태거나 아직 remainTurn이 설정되지 않은 상태라면
		// 한번도 골드박스룸에 입장하지 않은 상태일거다.
		// 이땐 FirstGoldBox로 보상을 설정해두고 사용하기로 한다.
		if (bettingCount == 0 || goldBoxRemainTurn == 0)
		{
			goldBoxTargetReward = BattleInstanceManager.instance.GetCachedGlobalConstantInt("FirstGoldBox");
		}
	}

	void Update()
	{
		UpdateRechargeEnergy();
	}

	bool _rechargingEnergy = false;
	DateTime _energyRechargeTime;
	public DateTime energyRechargeTime { get { return _energyRechargeTime; } }
	void UpdateRechargeEnergy()
	{
		// MEC쓰려다가 홈키 눌러서 내릴거 대비해서 DateTime검사로 처리한다.
		if (_rechargingEnergy == false)
			return;

		// 한번만 계산하고 넘기니 한번에 여러번 해야하는 상황에서 프레임 단위로 조금씩 밀리게 된다.
		// 어차피 싱크는 맞출테지만 그래도 이왕이면 여러번 체크하게 해둔다. 120회 정도면 24시간도 버틸만할거다.
		int loopCount = 0;
		for (int i = 0; i < 120; ++i)
		{
			if (DateTime.Compare(ServerTime.UtcNow, _energyRechargeTime) < 0)
				break;

			loopCount += 1;
			energy += 1;
			if (energy == energyMax)
			{
				_rechargingEnergy = false;
				break;
			}
			else
				_energyRechargeTime += TimeSpan.FromSeconds(BattleInstanceManager.instance.GetCachedGlobalConstantInt("TimeSecToGetOneEnergy"));
		}

		// 여러번 건너뛰었단건 홈키 같은거 눌러서 한동안 업데이트 안되다가 몰아서 업데이트 되었단 얘기다. 이럴땐 강제 UI 업데이트
		if (loopCount > 5)
		{
			if (BettingCanvas.instance != null)
				BettingCanvas.instance.RefreshSpin();
		}
	}
	
	public bool UseEnergy(int amount)
	{
		if (energy < amount)
			return false;

		bool full = (energy >= energyMax);
		energy -= amount;
		if (energy < energyMax)
		{
			if (full)
			{
				_energyRechargeTime = ServerTime.UtcNow + TimeSpan.FromSeconds(BattleInstanceManager.instance.GetCachedGlobalConstantInt("TimeSecToGetOneEnergy"));
				_rechargingEnergy = true;
			}
			else
			{
				if (OptionManager.instance.energyAlarm == 1)
				{
					// full이 아니었다면 이전에 등록되어있던 Noti를 먼저 삭제해야한다.
					// 만약 energyAlarm을 꺼둔채로 에너지를 소모했다면 취소시킬 Noti가 없을텐데 그걸 판단할 방법은 귀찮으므로 그냥 Cancel 호출하는거로 해둔다.
					CancelEnergyNotification();
				}
			}

			if (OptionManager.instance.energyAlarm == 1)
			{
				ReserveEnergyNotification();
			}
		}
		return true;
	}

	#region Notification
	const int EnergyNotificationId = 10001;
	public void ReserveEnergyNotification()
	{
		// 충전때까지의 시간을 구해서
		if (energy >= energyMax)
			return;

		int diffMinusOne = energyMax - energy - 1;
		TimeSpan remainTime = _energyRechargeTime - ServerTime.UtcNow;
		double totalSecond = remainTime.TotalSeconds + diffMinusOne * BattleInstanceManager.instance.GetCachedGlobalConstantInt("TimeSecToGetOneEnergy");
		DateTime deliveryTime = DateTime.Now.ToLocalTime() + TimeSpan.FromSeconds(totalSecond);
		MobileNotificationWrapper.instance.SendNotification(EnergyNotificationId, UIString.instance.GetString("SystemUI_EnergyFullTitle"), UIString.instance.GetString("SystemUI_EnergyFullBody"),
			deliveryTime, null, true, "my_custom_icon_id", "my_custom_large_icon_id");
	}

	public void CancelEnergyNotification()
	{
		MobileNotificationWrapper.instance.CancelPendingNotificationItem(EnergyNotificationId);
	}
	#endregion


	public void OnRecvRefillEnergy(int refillAmount, bool ignoreCanvas = false)
	{
		bool full = (energy >= energyMax);
		energy += refillAmount;

		if (full == false && OptionManager.instance.energyAlarm == 1)
			CancelEnergyNotification();

		if (energy >= energyMax)
			_rechargingEnergy = false;
		else
		{
			if (OptionManager.instance.energyAlarm == 1)
			{
				ReserveEnergyNotification();
			}
		}

		if (ignoreCanvas)
			return;

		if (GachaInfoCanvas.instance != null && GachaInfoCanvas.instance.gameObject.activeSelf)
			GachaInfoCanvas.instance.RefreshEnergy();
		else if (MainCanvas.instance != null)
			MainCanvas.instance.RefreshGachaAlarmObject();
	}

	// 공용 보상 처리때문에 추가하는 함수
	public void OnRecvProductReward(string type, string value, int count)
	{
		switch (type)
		{
			case "cu":
				switch (value)
				{
					case "GO": gold += count; break;
					case "EN": OnRecvRefillEnergy(count); break;
				}
				break;
			case "it":
				// 어차피 받아야하는 곳에서 처리하고 있을텐데 이런 공용 처리가 필요할지 모르겠다.
				// 필요해지면 추가하기로 한다.
				//CashShopData.instance.OnRecvCashItem(type, value, count);
				break;
		}
	}

	#region Event Point
	public void OnRecvStartEventPoint(string eventPointId, bool oneTime, string eventPointExpireTimeString)
	{
		this.eventPointId = eventPointId;
		this.eventPoint = 0;

		DateTime eventPointExpireTime = new DateTime();
		if (DateTime.TryParse(eventPointExpireTimeString, out eventPointExpireTime))
			this.eventPointExpireTime = eventPointExpireTime.ToUniversalTime();

		if (oneTime)
		{
			if (listEventPointOneTime.Contains(eventPointId) == false)
				listEventPointOneTime.Add(eventPointId);
		}
	}

	class RandomEventPointTypeInfo
	{
		public EventPointTypeTableData eventPointTypeTableData;
		public float sumWeight;
	}
	List<RandomEventPointTypeInfo> _listEventPointTypeInfo = null;
	public string GetNextRandomEventPointId()
	{
		if (eventPointId == "")
			return "fr";

		if (_listEventPointTypeInfo == null)
			_listEventPointTypeInfo = new List<RandomEventPointTypeInfo>();
		_listEventPointTypeInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.eventPointTypeTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.eventPointTypeTable.dataArray[i].eventWeight;
			if (weight <= 0.0f)
				continue;

			DateTime startDateTime = new DateTime(TableDataManager.instance.eventPointTypeTable.dataArray[i].startYear, TableDataManager.instance.eventPointTypeTable.dataArray[i].startMonth, TableDataManager.instance.eventPointTypeTable.dataArray[i].startDay);
			if (ServerTime.UtcNow < startDateTime)
				continue;
			DateTime endDateTime = new DateTime(TableDataManager.instance.eventPointTypeTable.dataArray[i].endYear, TableDataManager.instance.eventPointTypeTable.dataArray[i].endMonth, TableDataManager.instance.eventPointTypeTable.dataArray[i].endDay);
			if (ServerTime.UtcNow > endDateTime)
				continue;

			if (TableDataManager.instance.eventPointTypeTable.dataArray[i].oneTime)
			{
				if (listEventPointOneTime.Contains(TableDataManager.instance.eventPointTypeTable.dataArray[i].eventPointId))
					continue;
			}

			sumWeight += weight;
			RandomEventPointTypeInfo newInfo = new RandomEventPointTypeInfo();
			newInfo.eventPointTypeTableData = TableDataManager.instance.eventPointTypeTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listEventPointTypeInfo.Add(newInfo);
		}

		if (_listEventPointTypeInfo.Count == 0)
			return "";

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listEventPointTypeInfo[_listEventPointTypeInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listEventPointTypeInfo.Count; ++i)
		{
			if (random <= _listEventPointTypeInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listEventPointTypeInfo[index].eventPointTypeTableData.eventPointId;
	}
	#endregion


	#region Max Gold
	public bool CheckMaxGold()
	{
		if (gold >= s_MaxGold)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("SystemUI_GoldLimit", s_MaxGold), 2.0f);
			return true;
		}
		return false;
	}
	#endregion
}