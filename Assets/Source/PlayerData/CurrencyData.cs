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
	}

	public static int s_MaxGold = 999999999;

	public static string GoldCode() { return "GO"; }
	public static string DiamondCode() { return "DI"; }
	public static string EnergyCode() { return "EN"; }
	public static string TicketCode() { return "TI"; }

	public ObscuredInt gold { get; set; }
	public ObscuredInt energy { get; set; }
	public ObscuredInt energyMax { get; set; }
	public ObscuredInt dia { get; set; }			// 서버 상점에서 모아서 처리하는 기능이 없어서 free와 구매 다 합쳐서 처리하기로 한다.
	public ObscuredInt ticket { get; set; }
	public ObscuredInt ticketMax { get; set; }

	#region Betting
	public ObscuredInt bettingCount { get; set; }
	public ObscuredInt brokenEnergy { get; set; }
	public ObscuredInt goldBoxTargetReward { get; set; }
	public ObscuredInt goldBoxTargetGrade { get; set; }
	public ObscuredInt currentGoldBoxRoomReward { get; set; }
	public ObscuredInt goldBoxRemainTurn { get; set; }
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
		if (userVirtualCurrency.ContainsKey("TI"))
			ticket = userVirtualCurrency["TI"];
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

		if (userVirtualCurrencyRechargeTimes != null && userVirtualCurrencyRechargeTimes.ContainsKey("TI"))
		{
			ticketMax = userVirtualCurrencyRechargeTimes["TI"].RechargeMax;
			if (userVirtualCurrencyRechargeTimes["TI"].SecondsToRecharge > 0 && ticket < ticketMax)
			{
				_rechargingTicket = true;
				_ticketRechargeTime = userVirtualCurrencyRechargeTimes["TI"].RechargeTime;
			}
		}

		// 재화는 한정적인 자원이라 재화 안써도 되는건 통계써서 가져오기로 한다.
		bettingCount = 0;
		brokenEnergy = 0;
		eventPoint = 0;
		goldBoxRemainTurn = 0;
		goldBoxTargetReward = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			switch (playerStatistics[i].StatisticName)
			{
				case "betCnt": bettingCount = playerStatistics[i].Value; break;
				case "brokenEnergy": brokenEnergy = playerStatistics[i].Value; break;
				case "eventPoint": eventPoint = playerStatistics[i].Value; break;
				case "goldBoxTurn": goldBoxRemainTurn = playerStatistics[i].Value; break;
				case "goldBoxValue": goldBoxTargetReward = playerStatistics[i].Value; break;
			}
		}

		goldBoxTargetGrade = 0;
		if (userReadOnlyData.ContainsKey("goldBoxValueGrade"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["goldBoxValueGrade"].Value, out intValue))
				goldBoxTargetGrade = intValue;
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
			goldBoxTargetGrade = 4;
		}
	}

	void Update()
	{
		UpdateRechargeEnergy();
		UpdateRechargeTicket();
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

	#region Ticket
	bool _rechargingTicket = false;
	DateTime _ticketRechargeTime;
	public DateTime ticketRechargeTime { get { return _ticketRechargeTime; } }
	void UpdateRechargeTicket()
	{
		// MEC쓰려다가 홈키 눌러서 내릴거 대비해서 DateTime검사로 처리한다.
		if (_rechargingTicket == false)
			return;

		// 한번만 계산하고 넘기니 한번에 여러번 해야하는 상황에서 프레임 단위로 조금씩 밀리게 된다.
		// 어차피 싱크는 맞출테지만 그래도 이왕이면 여러번 체크하게 해둔다. 120회 정도면 24시간도 버틸만할거다.
		int loopCount = 0;
		for (int i = 0; i < 120; ++i)
		{
			if (DateTime.Compare(ServerTime.UtcNow, _ticketRechargeTime) < 0)
				break;

			loopCount += 1;
			ticket += 1;
			if (ticket == ticketMax)
			{
				_rechargingTicket = false;
				break;
			}
			else
				_ticketRechargeTime += TimeSpan.FromSeconds(BattleInstanceManager.instance.GetCachedGlobalConstantInt("TimeSecToGetOneTicket"));
		}

		// 여러번 건너뛰었단건 홈키 같은거 눌러서 한동안 업데이트 안되다가 몰아서 업데이트 되었단 얘기다. 이럴땐 강제 UI 업데이트
		if (loopCount > 5)
		{
			//if (MissionListCanvas.instance != null)
			//	MissionListCanvas.instance.RefreshTicket();
		}
	}

	public bool UseTicket(int amount)
	{
		if (ticket < amount)
			return false;

		bool full = (ticket >= ticketMax);
		ticket -= amount;
		if (ticket < ticketMax)
		{
			if (full)
			{
				_ticketRechargeTime = ServerTime.UtcNow + TimeSpan.FromSeconds(BattleInstanceManager.instance.GetCachedGlobalConstantInt("TimeSecToGetOneTicket"));
				_rechargingTicket = true;
			}
			else
			{
				/*
				if (OptionManager.instance.energyAlarm == 1)
				{
					// full이 아니었다면 이전에 등록되어있던 Noti를 먼저 삭제해야한다.
					// 만약 energyAlarm을 꺼둔채로 에너지를 소모했다면 취소시킬 Noti가 없을텐데 그걸 판단할 방법은 귀찮으므로 그냥 Cancel 호출하는거로 해둔다.
					CancelEnergyNotification();
				}
				*/
			}

			/*
			if (OptionManager.instance.energyAlarm == 1)
			{
				ReserveEnergyNotification();
			}
			*/
		}
		return true;
	}

	public void OnRecvRefillTicket(int refillAmount, bool ignoreCanvas = false)
	{
		bool full = (ticket >= ticketMax);
		ticket += refillAmount;

		/*
		if (full == false && OptionManager.instance.energyAlarm == 1)
			CancelEnergyNotification();
		*/

		if (ticket >= ticketMax)
			_rechargingEnergy = false;
		else
		{
			/*
			if (OptionManager.instance.energyAlarm == 1)
			{
				ReserveEnergyNotification();
			}
			*/
		}

		if (ignoreCanvas)
			return;

		if (MissionListCanvas.instance != null && MissionListCanvas.instance.gameObject.activeSelf && MissionTabCanvas.instance != null && MissionTabCanvas.instance.gameObject.activeSelf)
			MissionListCanvas.instance.RefreshTicket();
	}
	#endregion

	// 공용 보상 처리때문에 추가하는 함수
	public void OnRecvProductReward(string type, string value, int count)
	{
		switch (type)
		{
			case "cu":
				switch (value)
				{
					case "GO": gold += count; break;
					case "DI": dia += count; break;
					case "EN": OnRecvRefillEnergy(count); break;
					case "TI": OnRecvRefillTicket(count); break;
				}
				break;
			case "it":
				// 어차피 받아야하는 곳에서 처리하고 있을텐데 이런 공용 처리가 필요할지 모르겠다.
				// 필요해지면 추가하기로 한다.
				switch (value)
				{
					case "Cash_sSevenTotal":
					case "Cash_sFestivalTotal":
					case "Cash_sAnalysisBoost":
						// 캐시샵쪽에서 Consume은 전부 처리하고 있으니 넘기면 된다. SevenTotalCanvas에서 상품 구매시 이쪽으로 넘어와서 처리될거다.
						CashShopData.instance.OnRecvConsumeItem(value, count);
						break;
				}
				break;
		}
	}

	public void OnRecvProductRewardExtendGacha(ShopProductTableData shopProductTableData)
	{
		string rewardType = shopProductTableData.rewardType1;
		string rewardValue = shopProductTableData.rewardValue1;
		int rewardCount = shopProductTableData.rewardCount1;
		if (rewardType == "cu") OnRecvProductReward(rewardType, rewardValue, rewardCount);
		else if (rewardType == "it") CashShopData.instance.OnRecvConsumeItem(rewardValue, rewardCount);

		rewardType = shopProductTableData.rewardType2;
		rewardValue = shopProductTableData.rewardValue2;
		rewardCount = shopProductTableData.rewardCount2;
		if (rewardType == "cu") OnRecvProductReward(rewardType, rewardValue, rewardCount);
		else if (rewardType == "it") CashShopData.instance.OnRecvConsumeItem(rewardValue, rewardCount);

		rewardType = shopProductTableData.rewardType3;
		rewardValue = shopProductTableData.rewardValue3;
		rewardCount = shopProductTableData.rewardCount3;
		if (rewardType == "cu") OnRecvProductReward(rewardType, rewardValue, rewardCount);
		else if (rewardType == "it") CashShopData.instance.OnRecvConsumeItem(rewardValue, rewardCount);

		rewardType = shopProductTableData.rewardType4;
		rewardValue = shopProductTableData.rewardValue4;
		rewardCount = shopProductTableData.rewardCount4;
		if (rewardType == "cu") OnRecvProductReward(rewardType, rewardValue, rewardCount);
		else if (rewardType == "it") CashShopData.instance.OnRecvConsumeItem(rewardValue, rewardCount);

		rewardType = shopProductTableData.rewardType5;
		rewardValue = shopProductTableData.rewardValue5;
		rewardCount = shopProductTableData.rewardCount5;
		if (rewardType == "cu") OnRecvProductReward(rewardType, rewardValue, rewardCount);
		else if (rewardType == "it") CashShopData.instance.OnRecvConsumeItem(rewardValue, rewardCount);
	}

	// 인앱결제에서 아이템 직접 지급하기 위해 만든 함수. 결과창은 CommonReward로 통일해서 보여줄거다. 일부에선 공격력도 들어있어서 토스트로 때울때도 있다.
	public void OnRecvProductRewardExtendGachaAndItem(ShopProductTableData shopProductTableData)
	{
		string rewardType = shopProductTableData.rewardType1;
		string rewardValue = shopProductTableData.rewardValue1;
		int rewardCount = shopProductTableData.rewardCount1;
		if (rewardType == "cu") OnRecvProductReward(rewardType, rewardValue, rewardCount);
		else if (rewardType == "it")
		{
			if (rewardValue.StartsWith("Cash_s")) CashShopData.instance.OnRecvConsumeItem(rewardValue, rewardCount);
			else OnRecvPurchaseItem(rewardValue, rewardCount);
		}

		rewardType = shopProductTableData.rewardType2;
		rewardValue = shopProductTableData.rewardValue2;
		rewardCount = shopProductTableData.rewardCount2;
		if (rewardType == "cu") OnRecvProductReward(rewardType, rewardValue, rewardCount);
		else if (rewardType == "it")
		{
			if (rewardValue.StartsWith("Cash_s")) CashShopData.instance.OnRecvConsumeItem(rewardValue, rewardCount);
			else OnRecvPurchaseItem(rewardValue, rewardCount);
		}

		rewardType = shopProductTableData.rewardType3;
		rewardValue = shopProductTableData.rewardValue3;
		rewardCount = shopProductTableData.rewardCount3;
		if (rewardType == "cu") OnRecvProductReward(rewardType, rewardValue, rewardCount);
		else if (rewardType == "it")
		{
			if (rewardValue.StartsWith("Cash_s")) CashShopData.instance.OnRecvConsumeItem(rewardValue, rewardCount);
			else OnRecvPurchaseItem(rewardValue, rewardCount);
		}

		rewardType = shopProductTableData.rewardType4;
		rewardValue = shopProductTableData.rewardValue4;
		rewardCount = shopProductTableData.rewardCount4;
		if (rewardType == "cu") OnRecvProductReward(rewardType, rewardValue, rewardCount);
		else if (rewardType == "it")
		{
			if (rewardValue.StartsWith("Cash_s")) CashShopData.instance.OnRecvConsumeItem(rewardValue, rewardCount);
			else OnRecvPurchaseItem(rewardValue, rewardCount);
		}

		rewardType = shopProductTableData.rewardType5;
		rewardValue = shopProductTableData.rewardValue5;
		rewardCount = shopProductTableData.rewardCount5;
		if (rewardType == "cu") OnRecvProductReward(rewardType, rewardValue, rewardCount);
		else if (rewardType == "it")
		{
			if (rewardValue.StartsWith("Cash_s")) CashShopData.instance.OnRecvConsumeItem(rewardValue, rewardCount);
			else OnRecvPurchaseItem(rewardValue, rewardCount);
		}
	}

	void OnRecvPurchaseItem(string rewardValue, int rewardCount)
	{
		//서버에선 직접 지급할거라서 클라는 아이디만 가지고 처리해야한다.
		if (rewardValue.StartsWith("Spell_"))
		{
			SpellManager.instance.OnRecvPurchaseItem(rewardValue, rewardCount);
		}
		else if (rewardValue.StartsWith("Actor"))
		{
			// 액터는 지금 common reward icon 으로 표기는 하지 않지만 인앱결제로 제공할 가능성이 있으니 추가해두기로 한다.
			// 근데 아마 지정해서 파는건 얻을 수 없는 상황도 있기 때문에 사용하지 않을거 같다.
			CharacterManager.instance.OnRecvPurchaseItem(rewardValue, rewardCount);
		}
		else if (rewardValue.StartsWith("Pet_"))
		{
			PetManager.instance.OnRecvPurchaseItem(rewardValue, rewardCount);
		}
		else if (rewardValue.StartsWith("Equip"))
		{
			// 장비는 uniqueId가 무조건 있어야하므로 이렇게 추가하면 서버와 동기화를 할 수 없다.
			// 그래서 여기서 처리하는 대신 인벤토리 리프레쉬를 호출해서 인벤에 넣게 될거다.
			//PlayFabApiManager.instance.RequestEquipListByPurchase(null);
			Debug.LogError("Invalid Call. Equip need uniqueId.");
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