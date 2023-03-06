using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class CashShopData : MonoBehaviour
{
	public static CashShopData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("CashShopData")).AddComponent<CashShopData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static CashShopData _instance = null;

	// 상품들은 대부분은 각각의 유효기간을 가진다.
	Dictionary<string, DateTime> _dicExpireTime = new Dictionary<string, DateTime>();
	// 유효기간뿐만 아니라 한번 열린 이벤트를 바로 또다시 여는걸 방지하기 위해서 쿨타임도 가질 수 있다.
	Dictionary<string, DateTime> _dicCoolTimeExpireTime = new Dictionary<string, DateTime>();

	// 추가 정보들이 필요한 이벤트도 있다.
	Dictionary<string, int> _dicContinuousProductStep = new Dictionary<string, int>();
	Dictionary<string, List<int>> _dicOnePlusTwoReward = new Dictionary<string, List<int>>();

	// 미습득 스펠 캐릭 패키지 같은 경우엔
	// 클라이언트가 골라서 디비에 기록해두는 경우도 있다.
	public ObscuredString unacquiredSpellSelectedId { get; set; }
	public ObscuredString acquiredSpellSelectedId { get; set; }
	public ObscuredString unacquiredCharacterSelectedId { get; set; }
	public ObscuredString acquiredCharacterSelectedId { get; set; }
	public ObscuredString acquiredCharacterPpSelectedId { get; set; }

	// CF에다가 비트플래그로 구분하는건 중복 구매시 다음 아이템의 플래그가 켜질 수 있어서 안하기로 한다.
	// 이 플래그 대신 아이템에다가 접두사로 구분하는 형태로 해서 인벤토리를 검사하는 형태로 변경하기로 한다.
	public enum eCashFlagType
	{
		LevelPass = 0,
		StagePass1 = 1,

		Amount,
	}
	List<ObscuredBool> _listCashFlag = new List<ObscuredBool>();
	List<string> _listCashFlagKey = new List<string> { "Cash_bLevelPass", "Cash_bStagePass" };

	public enum eCashConsumeFlagType
	{
		BrokenEnergy = 0,
		Ev4ContiNext = 1,
		Ev5OnePlTwoCash = 2,
		SevenSlot1 = 3,
		SevenSlot2 = 4,
		SevenSlot3 = 5,
		SevenSlot4 = 6,
		PetSale = 7,
		PetPass = 8,
		FortuneWheel = 9,
		FestivalSlot1 = 10,
		FestivalSlot2 = 11,
		FestivalSlot3 = 12,
		FestivalSlot4 = 13,
		UnacquiredSpell = 14,
		AcquiredSpell = 15,
		UnacquiredCompanion = 16,
		AcquiredCompanion = 17,
		AcquiredCompanionPp = 18,
		TeamPass = 19,

		Amount,
	}
	List<ObscuredBool> _listCashConsumeFlag = new List<ObscuredBool>();
	List<string> _listCashConsumeFlagKey = new List<string> { "Cash_sBrokenEnergy", "Cash_sEv4ContiNext", "Cash_sEv5OnePlTwoCash",
		"Cash_sSevenSlot1", "Cash_sSevenSlot2", "Cash_sSevenSlot3", "Cash_sSevenSlot4", "Cash_sPetSale", "Cash_sPetPass", "Cash_sFortuneWheel",
		"Cash_sFestivalSlot1", "Cash_sFestivalSlot2", "Cash_sFestivalSlot3", "Cash_sFestivalSlot4",
		"Cash_sUnacquiredSpell", "Cash_sAcquiredSpell", "Cash_sUnacquiredCompanion", "Cash_sAcquiredCompanion", "Cash_sAcquiredCompanionPp",
		"Cash_sTeamPass"
	};

	public enum eCashConsumeCountType
	{
		SpellGacha = 0,
		CharacterGacha = 1,
		EquipGacha = 2,
		SevenTotal = 3,
		FestivalTotal = 4,
		Spell3Gacha = 5,
		Spell4Gacha = 6,
		Spell5Gacha = 7,
		AnalysisBoost = 8,

		Amount,
	}
	List<ObscuredInt> _listCashConsumeCount = new List<ObscuredInt>();
	List<string> _listCashConsumeCountKey = new List<string> { "Cash_sSpellGacha", "Cash_sCharacterGacha", "Cash_sEquipGacha", "Cash_sSevenTotal", "Cash_sFestivalTotal",
		"Cash_sSpell3Gacha", "Cash_sSpell4Gacha", "Cash_sSpell5Gacha", "Cash_sAnalysisBoost"
	};

	public enum eCashItemCountType
	{
		DailyDiamond = 0,
		CaptureBetter = 1,
		CaptureBest = 2,

		Amount,
	}
	List<ObscuredInt> _listCashItemCount = new List<ObscuredInt>();
	List<string> _listCashItemCountKey = new List<string> { "Item_cDailyGem", "Item_cCaptureBetter", "Item_cCaptureBest" };

	// 레벨패스에서 받았음을 기억해두는 변수인데 어차피 받을때마다 서버검증 하기때문에 Obscured 안쓰고 그냥 사용하기로 한다.
	List<int> _listLevelPassReward;
	// 구조가 거의 비슷해서 그대로 비슷하게 구현해본다.
	List<int> _listEnergyPaybackReward;

	// 스테이지 클리어 패키지 리스트
	List<int> _listStageClearPackage;

	// 브로큰 에너지 레벨
	ObscuredInt _brokenEnergyLevel;
	public int brokenEnergyLevel { get { return _brokenEnergyLevel; } set { _brokenEnergyLevel = value; } }
	// 브로큰 에너지는 항상 Expire 되는게 아니다. 특정 조건이 되면 발동된다.
	public ObscuredBool brokenEnergyExpireStarted { get; set; }
	public DateTime brokenEnergyExpireTime { get; set; }

	// 첫구매 보상 받았는지 확인용
	public ObscuredBool firstPurchaseRewarded { get; set; }

	#region EventPoint
	public enum eEventStartCondition
	{
		ByCode = 0,
		Login = 1,
		BossStageFailed = 2,
		SpinZero = 3,
		OnCloseMainMenu = 4,
	}
	#endregion

	#region PickUp Event
	public class PickUpCharacterInfo
	{
		public int sy;
		public int sm;
		public int sd;
		public int ey;
		public int em;
		public int ed;
		public string id;
		public int bc;	// bonus count

		public int count;
		public int price;
	}
	List<PickUpCharacterInfo> _listPickUpCharacterInfo;
	ObscuredInt _pickUpCharacterNotStreakCount;
	DateTime _pickUpCharacterCountDateTime;

	public class PickUpEquipInfo
	{
		public int sy;
		public int sm;
		public int sd;
		public int ey;
		public int em;
		public int ed;
		public string id;
		public int sc;  // s rarity bonus count;
		public int ssc;	// ss rarity bonus count;

		public int count;
		public int price;
		public float ov;	// override 0.02
	}
	List<PickUpEquipInfo> _listPickUpEquipInfo;
	ObscuredInt _pickUpEquipNotStreakCount1;
	ObscuredInt _pickUpEquipNotStreakCount2;
	DateTime _pickUpEquipCountDateTime;
	#endregion

	public void OnRecvCashShopData(List<ItemInstance> userInventory, Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		/*
		// 아직 언픽스드를 쓸지 안쓸지 모르니
		// PlayerData.ResetData 호출되면 다시 여기로 들어올테니 플래그들 초기화 시켜놓는다.
		_checkedUnfixedItemInfo = false;
		*/

		_dicExpireTime.Clear();
		_dicCoolTimeExpireTime.Clear();
		_dicContinuousProductStep.Clear();
		_dicOnePlusTwoReward.Clear();
		unacquiredSpellSelectedId = "";
		acquiredSpellSelectedId = "";
		unacquiredCharacterSelectedId = "";
		acquiredCharacterSelectedId = "";
		acquiredCharacterPpSelectedId = "";

		// 이벤트는 여러개 있고 각각의 유효기간이 있으니 테이블 돌면서
		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		for (int i = 0; i < TableDataManager.instance.eventTypeTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.eventTypeTable.dataArray[i].givenTime > 0)
			{
				string key = string.Format("{0}ExpDat", TableDataManager.instance.eventTypeTable.dataArray[i].id);
				if (userReadOnlyData.ContainsKey(key))
				{
					if (string.IsNullOrEmpty(userReadOnlyData[key].Value) == false)
					{
						DateTime expireDateTime = new DateTime();
						if (DateTime.TryParse(userReadOnlyData[key].Value, out expireDateTime))
						{
							DateTime universalTime = expireDateTime.ToUniversalTime();
							_dicExpireTime.Add(TableDataManager.instance.eventTypeTable.dataArray[i].id, universalTime);
						}
					}
				}
			}

			if (TableDataManager.instance.eventTypeTable.dataArray[i].coolTime > 0)
			{
				string key = string.Format("{0}CoolExpDat", TableDataManager.instance.eventTypeTable.dataArray[i].id);
				if (userReadOnlyData.ContainsKey(key))
				{
					if (string.IsNullOrEmpty(userReadOnlyData[key].Value) == false)
					{
						DateTime coolExpireDateTime = new DateTime();
						if (DateTime.TryParse(userReadOnlyData[key].Value, out coolExpireDateTime))
						{
							DateTime universalTime = coolExpireDateTime.ToUniversalTime();
							_dicCoolTimeExpireTime.Add(TableDataManager.instance.eventTypeTable.dataArray[i].id, universalTime);
						}
					}
				}
			}

			// 추가 정보를 얻기 위해서 eventSub를 검사해본다.
			if (string.IsNullOrEmpty(TableDataManager.instance.eventTypeTable.dataArray[i].eventSub) == false)
			{
				string key = "";
				switch (TableDataManager.instance.eventTypeTable.dataArray[i].eventSub)
				{
					case "oneofthree":
						break;
					case "conti":
						key = string.Format("{0}ContiNum", TableDataManager.instance.eventTypeTable.dataArray[i].id);
						if (userReadOnlyData.ContainsKey(key))
						{
							if (string.IsNullOrEmpty(userReadOnlyData[key].Value) == false)
							{
								int intValue = 0;
								if (int.TryParse(userReadOnlyData[key].Value, out intValue))
									_dicContinuousProductStep.Add(TableDataManager.instance.eventTypeTable.dataArray[i].id, intValue);
							}
						}
						break;
					case "oneplustwo":
						key = string.Format("{0}OnePlTwoLst", TableDataManager.instance.eventTypeTable.dataArray[i].id);
						if (userReadOnlyData.ContainsKey(key))
						{
							if (string.IsNullOrEmpty(userReadOnlyData[key].Value) == false)
							{
								List<int> listOnePlusTwoReward = serializer.DeserializeObject<List<int>>(userReadOnlyData[key].Value);
								_dicOnePlusTwoReward.Add(TableDataManager.instance.eventTypeTable.dataArray[i].id, listOnePlusTwoReward);
							}
						}
						break;

						#region Sub Condition
					case "unacquiredspell":
						key = "evUnacquiredSpell";
						if (userReadOnlyData.ContainsKey(key))
						{
							if (string.IsNullOrEmpty(userReadOnlyData[key].Value) == false)
								unacquiredSpellSelectedId = userReadOnlyData[key].Value;
						}
						break;
					case "acquiredspell":
						key = "evAcquiredSpell";
						if (userReadOnlyData.ContainsKey(key))
						{
							if (string.IsNullOrEmpty(userReadOnlyData[key].Value) == false)
								acquiredSpellSelectedId = userReadOnlyData[key].Value;
						}
						break;
					case "unacquiredcompanion":
						key = "evUnacquiredCompanion";
						if (userReadOnlyData.ContainsKey(key))
						{
							if (string.IsNullOrEmpty(userReadOnlyData[key].Value) == false)
								unacquiredCharacterSelectedId = userReadOnlyData[key].Value;
						}
						break;
					case "acquiredcompanion":
						key = "evAcquiredCompanion";
						if (userReadOnlyData.ContainsKey(key))
						{
							if (string.IsNullOrEmpty(userReadOnlyData[key].Value) == false)
								acquiredCharacterSelectedId = userReadOnlyData[key].Value;
						}
						break;
					case "acquiredcompanionpp":
						key = "evAcquiredCompanionPp";
						if (userReadOnlyData.ContainsKey(key))
						{
							if (string.IsNullOrEmpty(userReadOnlyData[key].Value) == false)
								acquiredCharacterPpSelectedId = userReadOnlyData[key].Value;
						}
						break;
						#endregion
				}
			}
		}

		// 이번 캐시상품의 핵심이 되는 플래그다.
		_listCashFlag.Clear();
		for (int i = 0; i < (int)eCashFlagType.Amount; ++i)
			_listCashFlag.Add(false);
		_listCashConsumeFlag.Clear();
		for (int i = 0; i < (int)eCashConsumeFlagType.Amount; ++i)
			_listCashConsumeFlag.Add(false);
		_listCashConsumeCount.Clear();
		for (int i = 0; i < (int)eCashConsumeCountType.Amount; ++i)
			_listCashConsumeCount.Add(0);
		_listCashItemCount.Clear();
		for (int i = 0; i < (int)eCashItemCountType.Amount; ++i)
			_listCashItemCount.Add(0);

		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId.StartsWith("Cash_") == false)
				continue;

			for (int j = 0; j < _listCashFlagKey.Count; ++j)
			{
				if (_listCashFlagKey[j] == userInventory[i].ItemId)
				{
					_listCashFlag[j] = true;
					break;
				}
			}

			for (int j = 0; j < _listCashConsumeFlagKey.Count; ++j)
			{
				if (_listCashConsumeFlagKey[j] == userInventory[i].ItemId)
				{
					_listCashConsumeFlag[j] = true;
					break;
				}
			}

			for (int j = 0; j < _listCashConsumeCountKey.Count; ++j)
			{
				if (_listCashConsumeCountKey[j] == userInventory[i].ItemId)
				{
					_listCashConsumeCount[j] = (userInventory[i].RemainingUses != null) ? (int)userInventory[i].RemainingUses : 0;
					break;
				}
			}

			for (int j = 0; j < _listCashItemCountKey.Count; ++j)
			{
				if (_listCashItemCountKey[j] == userInventory[i].ItemId)
				{
					_listCashItemCount[j] = (userInventory[i].RemainingUses != null) ? (int)userInventory[i].RemainingUses : 0;
					break;
				}
			}
		}

		for (int i = 0; i < userInventory.Count; ++i)
		{
			if (userInventory[i].ItemId.StartsWith("Item_") == false)
				continue;

			for (int j = 0; j < _listCashItemCountKey.Count; ++j)
			{
				if (_listCashItemCountKey[j] == userInventory[i].ItemId)
				{
					_listCashItemCount[j] = (userInventory[i].RemainingUses != null) ? (int)userInventory[i].RemainingUses : 0;
					break;
				}
			}
		}

		_listLevelPassReward = null;
		if (userReadOnlyData.ContainsKey("lvPssLst"))
		{
			string lvPssLstString = userReadOnlyData["lvPssLst"].Value;
			if (string.IsNullOrEmpty(lvPssLstString) == false)
				_listLevelPassReward = serializer.DeserializeObject<List<int>>(lvPssLstString);
		}
		levelPassAlarmStateForNoPass = (IsPurchasedFlag(eCashFlagType.LevelPass) == false);

		_listEnergyPaybackReward = null;
		if (userReadOnlyData.ContainsKey("enPbkLst"))
		{
			string enPbkLstString = userReadOnlyData["enPbkLst"].Value;
			if (string.IsNullOrEmpty(enPbkLstString) == false)
				_listEnergyPaybackReward = serializer.DeserializeObject<List<int>>(enPbkLstString);
		}

		energyUseForPayback = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "energyUseForPayback")
			{
				energyUseForPayback = playerStatistics[i].Value;
				break;
			}
		}

		dailyDiamondReceived = false;
		if (userReadOnlyData.ContainsKey("lasDaiDiaDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasDaiDiaDat"].Value) == false)
				OnRecvDailyDiamondInfo(userReadOnlyData["lasDaiDiaDat"].Value);
		}

		_listStageClearPackage = null;
		if (userReadOnlyData.ContainsKey("stgClrPckLst"))
		{
			string stgClrPckLstString = userReadOnlyData["stgClrPckLst"].Value;
			if (string.IsNullOrEmpty(stgClrPckLstString) == false)
				_listStageClearPackage = serializer.DeserializeObject<List<int>>(stgClrPckLstString);
		}

		firstPurchaseRewarded = false;
		if (userReadOnlyData.ContainsKey("firstPurchaseReward"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["firstPurchaseReward"].Value) == false)
				firstPurchaseRewarded = true;
		}

		_brokenEnergyLevel = 1;
		if (userReadOnlyData.ContainsKey("brokenEnergyLevel"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["brokenEnergyLevel"].Value, out intValue))
				_brokenEnergyLevel = intValue;
			if (_brokenEnergyLevel > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBrokenStep"))
				_brokenEnergyLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBrokenStep");
		}

		brokenEnergyExpireStarted = false;
		if (userReadOnlyData.ContainsKey("brokenEnergyExpireStarted"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["brokenEnergyExpireStarted"].Value) == false)
			{
				if (userReadOnlyData["brokenEnergyExpireStarted"].Value == "1")
				{
					if (userReadOnlyData.ContainsKey("brokenEnergyExpDat"))
					{
						if (string.IsNullOrEmpty(userReadOnlyData["brokenEnergyExpDat"].Value) == false)
						{
							DateTime expireDateTime = new DateTime();
							if (DateTime.TryParse(userReadOnlyData["brokenEnergyExpDat"].Value, out expireDateTime))
							{
								brokenEnergyExpireTime = expireDateTime.ToUniversalTime();
								brokenEnergyExpireStarted = true;

								// 플레이 중에 갑자기 확 바뀌는건 좀 그러니 로그인 시점에서만 expireTime 지났는지 확인해서 다음 레벨로 바꾸기로 해본다.
								if (ServerTime.UtcNow > brokenEnergyExpireTime)
									brokenEnergyNeedNextStep = true;
							}
						}
					}
				}
			}
		}

		#region PickUp Event
		_listPickUpCharacterInfo = null;
		if (titleData.ContainsKey("pickUpChar"))
			_listPickUpCharacterInfo = serializer.DeserializeObject<List<PickUpCharacterInfo>>(titleData["pickUpChar"]);

		_listPickUpEquipInfo = null;
		if (titleData.ContainsKey("pickUpEquip"))
			_listPickUpEquipInfo = serializer.DeserializeObject<List<PickUpEquipInfo>>(titleData["pickUpEquip"]);

		if (userReadOnlyData.ContainsKey("pickUpCharCntDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["pickUpCharCntDat"].Value) == false)
			{
				DateTime expireDateTime = new DateTime();
				if (DateTime.TryParse(userReadOnlyData["pickUpCharCntDat"].Value, out expireDateTime))
					_pickUpCharacterCountDateTime = expireDateTime.ToUniversalTime();
			}
		}

		_pickUpCharacterNotStreakCount = 0;
		if (userReadOnlyData.ContainsKey("pickUpCharCnt"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["pickUpCharCnt"].Value) == false)
			{
				int intValue = 0;
				if (int.TryParse(userReadOnlyData["pickUpCharCnt"].Value, out intValue))
					_pickUpCharacterNotStreakCount = intValue;
			}
		}

		if (userReadOnlyData.ContainsKey("pickUpEquipCntDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["pickUpEquipCntDat"].Value) == false)
			{
				DateTime expireDateTime = new DateTime();
				if (DateTime.TryParse(userReadOnlyData["pickUpEquipCntDat"].Value, out expireDateTime))
					_pickUpEquipCountDateTime = expireDateTime.ToUniversalTime();
			}
		}

		_pickUpEquipNotStreakCount1 = 0;
		if (userReadOnlyData.ContainsKey("pickUpEquipCnt1"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["pickUpEquipCnt1"].Value) == false)
			{
				int intValue = 0;
				if (int.TryParse(userReadOnlyData["pickUpEquipCnt1"].Value, out intValue))
					_pickUpEquipNotStreakCount1 = intValue;
			}
		}

		_pickUpEquipNotStreakCount2 = 0;
		if (userReadOnlyData.ContainsKey("pickUpEquipCnt2"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["pickUpEquipCnt2"].Value) == false)
			{
				int intValue = 0;
				if (int.TryParse(userReadOnlyData["pickUpEquipCnt2"].Value, out intValue))
					_pickUpEquipNotStreakCount2 = intValue;
			}
		}
		#endregion

		/*
		// 일일 무료 아이템 수령기록 데이터. 마지막 오픈 시간을 받는건 일퀘 때와 비슷한 구조다. 상점 슬롯과 별개로 처리된다.
		if (userReadOnlyData.ContainsKey("lasFreDat"))
		{
			if (string.IsNullOrEmpty(userReadOnlyData["lasFreDat"].Value) == false)
				OnRecvDailyFreeItemInfo(userReadOnlyData["lasFreDat"].Value);
		}
		*/

		/*
		// 일일 상점 항목별 구매기록 데이터. 각각의 여부는 따로 관리되며 리셋 타이밍은 하나를 공유해서 쓴다.
		if (_listShopSlotPurchased == null)
			_listShopSlotPurchased = new List<ObscuredBool>();
		_listShopSlotPurchased.Clear();
		for (int i = 0; i <= ShopSlotMax; ++i)
			_listShopSlotPurchased.Add(false);

		for (int i = 0; i <= ShopSlotMax; ++i)
		{
			string key = string.Format("lasShpDat{0}", i);
			if (userReadOnlyData.ContainsKey(key))
			{
				if (string.IsNullOrEmpty(userReadOnlyData[key].Value) == false)
					OnRecvDailyShopSlotInfo(userReadOnlyData[key].Value, i);
			}
		}

		// 구매기록은 정시에 갱신된다.
		dailyShopSlotPurchasedResetTime = new DateTime(ServerTime.UtcNow.Year, ServerTime.UtcNow.Month, ServerTime.UtcNow.Day) + TimeSpan.FromDays(1);
		*/
	}

	void Update()
	{
		UpdateBrokenEnergy();
	}

	public bool IsPurchasedFlag(eCashFlagType cashFlagType)
	{
		if ((int)cashFlagType < _listCashFlag.Count)
		{
			return _listCashFlag[(int)cashFlagType];
		}
		return false;
	}

	public void PurchaseFlag(eCashFlagType cashFlagType)
	{
		if ((int)cashFlagType < _listCashFlag.Count)
			_listCashFlag[(int)cashFlagType] = true;
	}

	#region CashConsumeFlag
	public bool IsPurchasedFlag(eCashConsumeFlagType cashConsumeFlagType)
	{
		if ((int)cashConsumeFlagType < _listCashConsumeFlag.Count)
		{
			return _listCashConsumeFlag[(int)cashConsumeFlagType];
		}
		return false;
	}

	public void PurchaseFlag(eCashConsumeFlagType cashConsumeFlagType)
	{
		if ((int)cashConsumeFlagType < _listCashConsumeFlag.Count)
			_listCashConsumeFlag[(int)cashConsumeFlagType] = true;
	}

	public void ConsumeFlag(eCashConsumeFlagType cashConsumeFlagType)
	{
		if ((int)cashConsumeFlagType < _listCashConsumeFlag.Count)
			_listCashConsumeFlag[(int)cashConsumeFlagType] = false;
	}

	public string GetConsumeId(eCashConsumeFlagType cashConsumeFlagType)
	{
		if ((int)cashConsumeFlagType < _listCashConsumeFlagKey.Count)
			return _listCashConsumeFlagKey[(int)cashConsumeFlagType];
		return "";
	}
	#endregion

	#region CashConsumeCount
	public int GetConsumeCount(eCashConsumeCountType cashConsumeCountType)
	{
		if ((int)cashConsumeCountType < _listCashConsumeCount.Count)
		{
			return _listCashConsumeCount[(int)cashConsumeCountType];
		}
		return 0;
	}

	public void PurchaseCount(eCashConsumeCountType cashConsumeCountType, int count)
	{
		if ((int)cashConsumeCountType < _listCashConsumeCount.Count)
			_listCashConsumeCount[(int)cashConsumeCountType] += count;
	}

	public void ConsumeCount(eCashConsumeCountType cashConsumeCountType, int count)
	{
		if ((int)cashConsumeCountType < _listCashConsumeCount.Count)
			_listCashConsumeCount[(int)cashConsumeCountType] -= count;
	}
	#endregion

	#region Cash Item
	public int GetCashItemCount(eCashItemCountType cashItemCountType)
	{
		if ((int)cashItemCountType < _listCashItemCount.Count)
		{
			return _listCashItemCount[(int)cashItemCountType];
		}
		return 0;
	}

	public void PurchaseCount(eCashItemCountType cashItemCountType, int count)
	{
		if ((int)cashItemCountType < _listCashItemCount.Count)
			_listCashItemCount[(int)cashItemCountType] += count;
	}

	public void ConsumeCount(eCashItemCountType cashItemCountType, int count)
	{
		if ((int)cashItemCountType < _listCashItemCount.Count)
			_listCashItemCount[(int)cashItemCountType] -= count;
	}
	#endregion

	public void OnRecvConsumeItem(string value, int count)
	{
		switch (value)
		{
			case "Cash_sSpellGacha":
				PurchaseCount(eCashConsumeCountType.SpellGacha, count);
				break;
			case "Cash_sCharacterGacha":
				PurchaseCount(eCashConsumeCountType.CharacterGacha, count);
				break;
			case "Cash_sEquipGacha":
				PurchaseCount(eCashConsumeCountType.EquipGacha, count);
				break;
			case "Cash_sSevenTotal":
				PurchaseCount(eCashConsumeCountType.SevenTotal, count);
				// SevenTotal은 구매 즉시 바로 소모시켜서 통계값으로 적용시키면 된다.
				PlayFabApiManager.instance.RequestConsumeSevenTotal(null);
				break;
			case "Cash_sFestivalTotal":
				PurchaseCount(eCashConsumeCountType.FestivalTotal, count);
				PlayFabApiManager.instance.RequestConsumeFestivalTotal(null);
				break;
			case "Cash_sSpell3Gacha":
				PurchaseCount(eCashConsumeCountType.Spell3Gacha, count);
				break;
			case "Cash_sSpell4Gacha":
				PurchaseCount(eCashConsumeCountType.Spell4Gacha, count);
				break;
			case "Cash_sSpell5Gacha":
				PurchaseCount(eCashConsumeCountType.Spell5Gacha, count);
				break;
			case "Cash_sAnalysisBoost":
				PurchaseCount(eCashConsumeCountType.AnalysisBoost, count);
				break;
		}
	}



	public void OnRecvOpenCashEvent(string openEventId, string cashEventExpireTimeString)
	{
		DateTime cashEventExpireTime = new DateTime();
		if (DateTime.TryParse(cashEventExpireTimeString, out cashEventExpireTime))
		{
			DateTime universalTime = cashEventExpireTime.ToUniversalTime();
			if (_dicExpireTime.ContainsKey(openEventId))
				_dicExpireTime[openEventId] = universalTime;
			else
				_dicExpireTime.Add(openEventId, universalTime);
		}
	}

	public void OnRecvCloseCashEvent(string closeEventId)
	{
		_dicExpireTime.Remove(closeEventId);
	}

	public bool IsShowEvent(string eventId)
	{
		if (string.IsNullOrEmpty(eventId))
			return false;
		if (_dicExpireTime.ContainsKey(eventId) && ServerTime.UtcNow < _dicExpireTime[eventId])
			return true;
		return false;
	}

	public void OnRecvCoolTimeCashEvent(string openEventId, string cashEventCoolTimeExpireTimeString)
	{
		DateTime cashEventCoolTimeExpireTime = new DateTime();
		if (DateTime.TryParse(cashEventCoolTimeExpireTimeString, out cashEventCoolTimeExpireTime))
		{
			DateTime universalTime = cashEventCoolTimeExpireTime.ToUniversalTime();
			if (_dicCoolTimeExpireTime.ContainsKey(openEventId))
				_dicCoolTimeExpireTime[openEventId] = universalTime;
			else
				_dicCoolTimeExpireTime.Add(openEventId, universalTime);
		}
	}

	public bool IsCoolTimeEvent(string eventId)
	{
		if (_dicCoolTimeExpireTime.ContainsKey(eventId) && ServerTime.UtcNow < _dicCoolTimeExpireTime[eventId])
			return true;
		return false;
	}

	DateTime _emptyDateTime = new DateTime();
	public DateTime GetExpireDateTime(string eventId)
	{
		if (_dicExpireTime.ContainsKey(eventId))
			return _dicExpireTime[eventId];
		return _emptyDateTime;
	}

	#region Event Start
	public void CheckStartEvent(eEventStartCondition eventStartCondition)
	{
		if (PlayerData.instance.downloadConfirmed == false)
			return;

		for (int i = 0; i < TableDataManager.instance.eventTypeTable.dataArray.Length; ++i)
		{
			if ((eEventStartCondition)TableDataManager.instance.eventTypeTable.dataArray[i].triggerCondition != eventStartCondition && (eEventStartCondition)TableDataManager.instance.eventTypeTable.dataArray[i].subTriggerCondition != eventStartCondition)
				continue;
			if (IsShowEvent(TableDataManager.instance.eventTypeTable.dataArray[i].id))
				continue;
			if (IsCoolTimeEvent(TableDataManager.instance.eventTypeTable.dataArray[i].id))
				continue;

			if (TableDataManager.instance.eventTypeTable.dataArray[i].startYear != 0 && TableDataManager.instance.eventTypeTable.dataArray[i].endYear != 0)
			{
				DateTime startDateTime = new DateTime(TableDataManager.instance.eventTypeTable.dataArray[i].startYear, TableDataManager.instance.eventTypeTable.dataArray[i].startMonth, TableDataManager.instance.eventTypeTable.dataArray[i].startDay);
				if (ServerTime.UtcNow < startDateTime)
					continue;
				DateTime endDateTime = new DateTime(TableDataManager.instance.eventTypeTable.dataArray[i].endYear, TableDataManager.instance.eventTypeTable.dataArray[i].endMonth, TableDataManager.instance.eventTypeTable.dataArray[i].endDay);
				if (ServerTime.UtcNow > endDateTime)
					continue;
			}

			if (TableDataManager.instance.eventTypeTable.dataArray[i].prob < 1.0f)
			{
				if (UnityEngine.Random.value > TableDataManager.instance.eventTypeTable.dataArray[i].prob)
					continue;
			}

			string generatedParameter = "";
			if (CheckSubCondition(TableDataManager.instance.eventTypeTable.dataArray[i].eventSub, ref generatedParameter) == false)
			{
				// 
				continue;
			}

			// 아직 이벤트가 다 완성된게 아니니 우선 이거로 막아둔다.
			//if ((eEventStartCondition)TableDataManager.instance.eventTypeTable.dataArray[i].triggerCondition != eEventStartCondition.SpinZero)
			//	continue;

			PlayFabApiManager.instance.RequestOpenCashEvent(TableDataManager.instance.eventTypeTable.dataArray[i].id, TableDataManager.instance.eventTypeTable.dataArray[i].eventSub, generatedParameter, TableDataManager.instance.eventTypeTable.dataArray[i].givenTime, TableDataManager.instance.eventTypeTable.dataArray[i].coolTime, () =>
			{
				if (MainCanvas.instance != null && MainCanvas.instance.gameObject.activeSelf)
					MainCanvas.instance.ShowCashEvent(TableDataManager.instance.eventTypeTable.dataArray[i].id, true, true);
			});

			// 이벤트는 중복해서 열리지 않게 하기 위해 한개라도 열었으면 break 시키는게 맞을거 같다.
			break;
		}
	}

	public void OnOpenCashEvent(string openEventId, string eventSub, string generatedParameter)
	{
		if (string.IsNullOrEmpty(eventSub) == false)
		{
			switch (eventSub)
			{
				case "conti":
					ResetContinuousProductStep(openEventId);
					break;
				case "energypayback":
					energyUseForPayback = 0;
					_listEnergyPaybackReward = null;
					break;
				case "oneplustwo":
					ResetOnePlusTwoReward(openEventId);
					break;
				case "unacquiredspell": unacquiredSpellSelectedId = generatedParameter; break;
				case "acquiredspell": acquiredSpellSelectedId = generatedParameter; break;
				case "unacquiredcompanion": unacquiredCharacterSelectedId = generatedParameter; break;
				case "acquiredcompanion": acquiredCharacterSelectedId = generatedParameter; break;
				case "acquiredcompanionpp": acquiredCharacterPpSelectedId = generatedParameter; break;
			}
		}
	}

	bool CheckSubCondition(string eventSub, ref string generatedParameter)
	{
		// 일부 이벤트들은 없는 캐릭터 아이디를 클라에서 선택해서 서버로 보내는 등의 추가 절차가 필요했다.
		// 이 로직을 구현하기 위해 이렇게 generatedParameter 라는 걸 추가해서 전달하기로 한다.
		switch (eventSub)
		{
			// hardcode ev13
			case "unacquiredspell":
				// 없는 스펠을 골라내서 소량 파는 이벤트
				// 습득한 스킬이 중간에 획득될 경우 이벤트를 종료하는 로직도 필요하다.
				if (SpellManager.instance.GetSpellKindsCount() <= BattleInstanceManager.instance.GetCachedGlobalConstantInt("Ev13CountLimit"))
					return false;
				if (IsPurchasedFlag(eCashConsumeFlagType.UnacquiredSpell))
					return false;
				string selectedNewSpellId = SpellManager.instance.PickOneAcquiredSpellId(false);
				if (string.IsNullOrEmpty(selectedNewSpellId))
					return false;
				generatedParameter = selectedNewSpellId;
				return true;
			// hardcode ev14
			case "acquiredspell":
				// 있는 스펠을 골라내서 대량으로 파는 이벤트
				// 습득한 스펠이 5개 이하일땐 활성화 되지 않는다.
				if (SpellManager.instance.GetSpellKindsCount() <= BattleInstanceManager.instance.GetCachedGlobalConstantInt("Ev14CountLimit"))
					return false;
				// 펫 세일과 마찬가지로 컨슘이 남아있다면 구매 복원을 완료할때까지 시작하지 않는게 맞다.
				if (IsPurchasedFlag(eCashConsumeFlagType.AcquiredSpell))
					return false;

				// 적절한 스펠을 골라낸다.
				string selectedSpellId = SpellManager.instance.PickOneAcquiredSpellId(true);
				if (string.IsNullOrEmpty(selectedSpellId))
					return false;
				generatedParameter = selectedSpellId;
				return true;
			// hardcode ev15
			case "unacquiredcompanion":
				if (CharacterManager.instance.listCharacterData.Count <= BattleInstanceManager.instance.GetCachedGlobalConstantInt("Ev15CountLimit"))
					return false;
				if (IsPurchasedFlag(eCashConsumeFlagType.UnacquiredCompanion))
					return false;
				string selectedNewActorId = CharacterManager.instance.PickOneAcquiredActorId(AcquiredCharacterSaleCanvas.eAcquiredType.UnacquiredCharacter);
				if (string.IsNullOrEmpty(selectedNewActorId))
					return false;
				generatedParameter = selectedNewActorId;
				return true;
			// hardcode ev16
			case "acquiredcompanion":
				if (CharacterManager.instance.listCharacterData.Count <= BattleInstanceManager.instance.GetCachedGlobalConstantInt("Ev16CountLimit"))
					return false;
				if (IsPurchasedFlag(eCashConsumeFlagType.AcquiredCompanion))
					return false;
				string selectedActorId = CharacterManager.instance.PickOneAcquiredActorId(AcquiredCharacterSaleCanvas.eAcquiredType.AcquiredCharacter);
				if (string.IsNullOrEmpty(selectedActorId))
					return false;
				generatedParameter = selectedActorId;
				return true;
			// hardcode ev17
			case "acquiredcompanionpp":
				if (CharacterManager.instance.listCharacterData.Count <= BattleInstanceManager.instance.GetCachedGlobalConstantInt("Ev17CountLimit"))
					return false;
				if (IsPurchasedFlag(eCashConsumeFlagType.AcquiredCompanionPp))
					return false;
				string selectedActorPpId = CharacterManager.instance.PickOneAcquiredActorId(AcquiredCharacterSaleCanvas.eAcquiredType.AcquiredCharacterPp);
				if (string.IsNullOrEmpty(selectedActorPpId))
					return false;
				generatedParameter = selectedActorPpId;
				return true;
		}

		// 특별한 조건 체크가 없다면 항상 true
		return true;
	}
	#endregion

	#region Level Pass
	public bool IsGetLevelPassReward(int level)
	{
		if (_listLevelPassReward == null)
			return false;

		return _listLevelPassReward.Contains(level);
	}

	public List<int> OnRecvLevelPassReward(int level)
	{
		if (_listLevelPassReward == null)
			_listLevelPassReward = new List<int>();
		if (_listLevelPassReward.Contains(level) == false)
			_listLevelPassReward.Add(level);
		return _listLevelPassReward;
	}
	public bool levelPassAlarmStateForNoPass { get; set; }
	#endregion

	#region Broken Energy
	public int GetMaxBrokenEnergy()
	{
		BrokenEnergyTableData brokenEnergyTableData = TableDataManager.instance.FindBrokenEnergyTableData(_brokenEnergyLevel);
		if (brokenEnergyTableData != null)
			return brokenEnergyTableData.maxEnergy;
		return 0;
	}

	public void OnRecvStartBrokenEnergyExpire(string brokenEnergyExpireTimeString)
	{
		DateTime brokenEnergyExpireTime = new DateTime();
		if (DateTime.TryParse(brokenEnergyExpireTimeString, out brokenEnergyExpireTime))
			this.brokenEnergyExpireTime = brokenEnergyExpireTime.ToUniversalTime();
	}

	void UpdateBrokenEnergy()
	{
		UpdateStartBrokenEnergyExpire();
		UpdateEndBrokenEnergyExpire();
	}

	// 이게 켜지면 패킷을 보내서 ExpireDateTime을 활성화 해야한다.
	public bool brokenEnergyMaxReached { get; set; }
	bool _waitResponse;
	void UpdateStartBrokenEnergyExpire()
	{
		if (_waitResponse)
			return;
		if (brokenEnergyMaxReached == false)
			return;
		if (brokenEnergyExpireStarted)
			return;

		_waitResponse = true;
		PlayFabApiManager.instance.RequestStartBrokenEnergyExpire(BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBrokenGivenTime"), () =>
		{
			brokenEnergyMaxReached = false;
			_waitResponse = false;
		}, () =>
		{
			_waitResponse = false;
		});
	}

	public bool brokenEnergyNeedNextStep { get; set; }
	void UpdateEndBrokenEnergyExpire()
	{
		if (_waitResponse)
			return;
		if (BrokenEnergyCanvas.instance != null && BrokenEnergyCanvas.instance.gameObject.activeSelf)
			return;
		if (brokenEnergyNeedNextStep == false)
			return;
		if (brokenEnergyExpireStarted == false)
			return;

		_waitResponse = true;
		int nextLevel = brokenEnergyLevel + 1;
		if (nextLevel > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBrokenStep"))
			nextLevel = 1;
		PlayFabApiManager.instance.RequestNextStepBrokenEnergy(brokenEnergyLevel, nextLevel, () =>
		{
			brokenEnergyNeedNextStep = false;
			_waitResponse = false;
		}, () =>
		{
			_waitResponse = false;
		});
	}
	#endregion

	#region Energy Payback
	public bool IsGetEnergyPaybackReward(int use)
	{
		if (_listEnergyPaybackReward == null)
			return false;

		return _listEnergyPaybackReward.Contains(use);
	}

	public List<int> OnRecvEnergyPaybackReward(int use)
	{
		if (_listEnergyPaybackReward == null)
			_listEnergyPaybackReward = new List<int>();
		if (_listEnergyPaybackReward.Contains(use) == false)
			_listEnergyPaybackReward.Add(use);
		return _listEnergyPaybackReward;
	}
	public ObscuredInt energyUseForPayback { get; set; }
	#endregion

	#region Continuous Product
	public int GetContinuousProductStep(string cashEventId)
	{
		if (_dicContinuousProductStep.ContainsKey(cashEventId))
			return _dicContinuousProductStep[cashEventId];
		return 0;
	}

	public void AddContinuousProductStep(string cashEventId, int add)
	{
		if (_dicContinuousProductStep.ContainsKey(cashEventId))
			_dicContinuousProductStep[cashEventId] += add;
		else
			_dicContinuousProductStep.Add(cashEventId, add);
	}

	public void ResetContinuousProductStep(string cashEventId)
	{
		if (_dicContinuousProductStep.ContainsKey(cashEventId))
			_dicContinuousProductStep.Remove(cashEventId);
	}
	#endregion

	#region OnePlusTwo
	public bool IsGetOnePlusTwoReward(string cashEventId, int index)
	{
		if (_dicOnePlusTwoReward == null)
			return false;

		if (_dicOnePlusTwoReward.ContainsKey(cashEventId) == false)
			return false;

		List<int> listReward = _dicOnePlusTwoReward[cashEventId];
		return listReward.Contains(index);
	}

	public List<int> OnRecvOnePlusTwoReward(string cashEventId, int index)
	{
		if (_dicOnePlusTwoReward.ContainsKey(cashEventId))
		{
			List<int> listReward = _dicOnePlusTwoReward[cashEventId];
			if (listReward.Contains(index) == false)
				listReward.Add(index);
			return listReward;
		}
		else
		{
			List<int> listReward = new List<int>();
			listReward.Add(index);
			_dicOnePlusTwoReward.Add(cashEventId, listReward);
			return listReward;
		}
	}

	public void ResetOnePlusTwoReward(string cashEventId)
	{
		if (_dicOnePlusTwoReward.ContainsKey(cashEventId))
			_dicOnePlusTwoReward.Remove(cashEventId);
	}
	#endregion

	#region DailyDiamomd
	public ObscuredBool dailyDiamondReceived { get; set; }

	void OnRecvDailyDiamondInfo(DateTime lastDailyPackageOpenTime)
	{
		if (ServerTime.UtcNow.Year == lastDailyPackageOpenTime.Year && ServerTime.UtcNow.Month == lastDailyPackageOpenTime.Month && ServerTime.UtcNow.Day == lastDailyPackageOpenTime.Day)
			dailyDiamondReceived = true;
		else
			dailyDiamondReceived = false;
	}

	public void OnRecvDailyDiamondInfo(string lastDailyDiamondOpenTimeString)
	{
		DateTime lastDailyDiamondOpenTime = new DateTime();
		if (DateTime.TryParse(lastDailyDiamondOpenTimeString, out lastDailyDiamondOpenTime))
		{
			DateTime universalTime = lastDailyDiamondOpenTime.ToUniversalTime();
			OnRecvDailyDiamondInfo(universalTime);
		}
	}
	#endregion

	#region Stage Clear Package
	public bool IsPurchasedStageClearPackage(int stage)
	{
		if (_listStageClearPackage == null)
			return false;

		return _listStageClearPackage.Contains(stage);
	}

	public List<int> OnRecvStageClearPackage(int stage)
	{
		if (_listStageClearPackage == null)
			_listStageClearPackage = new List<int>();
		if (_listStageClearPackage.Contains(stage) == false)
			_listStageClearPackage.Add(stage);
		return _listStageClearPackage;
	}
	#endregion

	#region PickUp Event
	public PickUpCharacterInfo GetCurrentPickUpCharacterInfo()
	{
		for (int i = 0; i < _listPickUpCharacterInfo.Count; ++i)
		{
			DateTime startDateTime = new DateTime(_listPickUpCharacterInfo[i].sy, _listPickUpCharacterInfo[i].sm, _listPickUpCharacterInfo[i].sd);
			DateTime endDateTime = new DateTime(_listPickUpCharacterInfo[i].ey, _listPickUpCharacterInfo[i].em, _listPickUpCharacterInfo[i].ed);
			if (startDateTime <= ServerTime.UtcNow && ServerTime.UtcNow <= endDateTime)
				return _listPickUpCharacterInfo[i];
		}
		return null;
	}

	public PickUpEquipInfo GetCurrentPickUpEquipInfo()
	{
		for (int i = 0; i < _listPickUpEquipInfo.Count; ++i)
		{
			DateTime startDateTime = new DateTime(_listPickUpEquipInfo[i].sy, _listPickUpEquipInfo[i].sm, _listPickUpEquipInfo[i].sd);
			DateTime endDateTime = new DateTime(_listPickUpEquipInfo[i].ey, _listPickUpEquipInfo[i].em, _listPickUpEquipInfo[i].ed);
			if (startDateTime <= ServerTime.UtcNow && ServerTime.UtcNow <= endDateTime)
				return _listPickUpEquipInfo[i];
		}
		return null;
	}

	public int GetCurrentPickUpCharacterNotStreakCount()
	{
		PickUpCharacterInfo info = GetCurrentPickUpCharacterInfo();
		if (info == null)
			return 0;

		DateTime startDateTime = new DateTime(info.sy, info.sm, info.sd);
		DateTime endDateTime = new DateTime(info.ey, info.em, info.ed);
		if (startDateTime <= _pickUpCharacterCountDateTime && _pickUpCharacterCountDateTime <= endDateTime)
			return _pickUpCharacterNotStreakCount;
		return 0;
	}

	public int GetCurrentPickUpEquipNotStreakCount1()
	{
		PickUpEquipInfo info = GetCurrentPickUpEquipInfo();
		if (info == null)
			return 0;

		DateTime startDateTime = new DateTime(info.sy, info.sm, info.sd);
		DateTime endDateTime = new DateTime(info.ey, info.em, info.ed);
		if (startDateTime <= _pickUpEquipCountDateTime && _pickUpEquipCountDateTime <= endDateTime)
			return _pickUpEquipNotStreakCount1;
		return 0;
	}

	public int GetCurrentPickUpEquipNotStreakCount2()
	{
		PickUpEquipInfo info = GetCurrentPickUpEquipInfo();
		if (info == null)
			return 0;

		DateTime startDateTime = new DateTime(info.sy, info.sm, info.sd);
		DateTime endDateTime = new DateTime(info.ey, info.em, info.ed);
		if (startDateTime <= _pickUpEquipCountDateTime && _pickUpEquipCountDateTime <= endDateTime)
			return _pickUpEquipNotStreakCount2;
		return 0;
	}

	public void OnRecvPickUpCharacterCount(string countDateTimeString, int notStreakCount)
	{
		DateTime countDateTime = new DateTime();
		if (DateTime.TryParse(countDateTimeString, out countDateTime))
			_pickUpCharacterCountDateTime = countDateTime.ToUniversalTime();

		_pickUpCharacterNotStreakCount = notStreakCount;
	}

	public void OnRecvPickUpEquipCount(string countDateTimeString, int notStreakCount1, int notStreakCount2)
	{
		DateTime countDateTime = new DateTime();
		if (DateTime.TryParse(countDateTimeString, out countDateTime))
			_pickUpEquipCountDateTime = countDateTime.ToUniversalTime();

		_pickUpEquipNotStreakCount1 = notStreakCount1;
		_pickUpEquipNotStreakCount2 = notStreakCount2;
	}
	#endregion


	#region Pending Product
	public bool CheckPendingProduct()
	{
		Debug.Log("CheckPendingProduct");

		// 초기화가 안되어도 캐시샵이 열리는 구조로 바꾸면서 체크
		if (CodelessIAPStoreListener.initializationComplete == false)
		{
			Debug.Log("not initializationComplete");
			//return false;
		}

		if (IAPListenerWrapper.instance.pendingProduct == null)
		{
			Debug.Log("no pendingProduct.");
			return false;
		}

		Product pendingProduct = IAPListenerWrapper.instance.pendingProduct;
		Debug.LogFormat("Check IAPListener pending product id : {0}", pendingProduct.definition.id);
		//Debug.LogFormat("IAPListener failed product storeSpecificId : {0}", failedProduct.definition.storeSpecificId);

		// 완료되지 않은 구매상품의 아이디에 따라 뭘 진행시킬지 판단해야한다.
		if (pendingProduct.definition.id.Contains("bigboost"))
		{
			BigBoostEventCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id == "levelpass")
		{
			LevelPassCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("brokenenergy"))
		{
			BrokenEnergyCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("_oneofthree"))
		{
			OneOfThreeCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("_conti"))
		{
			ContinuousShopProductCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("_oneplustwo"))
		{
			OnePlusTwoCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("seventotal"))
		{
			SevenTotalCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("cashshopenergy"))
		{
			CashShopEnergyListItem.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("cashshopgold"))
		{
			CashShopEnergyListItem.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("cashshopgem"))
		{
			CashShopEnergyListItem.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("costume_"))
		{
			CostumeCanvasListItem.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("petcapture_"))
		{
			BuyCaptureCanvasListItem.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("petsale_"))
		{
			PetSaleCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id == "petpass")
		{
			PetPassCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id == "fortunewheel")
		{
			FortuneWheelCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("festivalgroup"))
		{
			FestivalTotalCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("stageclear_"))
		{
			StageClearPackageBox.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("almostthere"))
		{
			AlmostThereEventCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("flashsale"))
		{
			FlashSaleEventCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("nuclearsale"))
		{
			NuclearSaleEventCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("analysisboost_"))
		{
			AnalysisBoostCanvasListItem.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("unacquiredspell"))
		{
			UnacquiredSpellSaleCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("acquiredspell"))
		{
			AcquiredSpellSaleCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("unacquiredcompanion"))
		{
			UnacquiredCharacterSaleCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("acquiredcompanionpp"))
		{
			AcquiredCharacterPpSaleCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id.Contains("acquiredcompanion"))
		{
			AcquiredCharacterSaleCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id == "teampass")
		{
			TeamPassCanvas.ExternalRetryPurchase(pendingProduct);
		}

		return true;
	}

	public bool CheckUncomsumeProduct()
	{
		// 다른 컨슘보다도 스텝이 길고 중요한 가차박스는 따로 검사한다.
		if (GetConsumeCount(eCashConsumeCountType.SpellGacha) > 0 || GetConsumeCount(eCashConsumeCountType.CharacterGacha) > 0 || GetConsumeCount(eCashConsumeCountType.EquipGacha) > 0 ||
			GetConsumeCount(eCashConsumeCountType.Spell3Gacha) > 0 || GetConsumeCount(eCashConsumeCountType.Spell4Gacha) > 0 || GetConsumeCount(eCashConsumeCountType.Spell5Gacha) > 0)
		{
			int count = GetConsumeCount(eCashConsumeCountType.SpellGacha);
			if (count > 0)
				ConsumeProductProcessor.instance.AddConsumeGacha(_listCashConsumeCountKey[(int)eCashConsumeCountType.SpellGacha], count);

			count = GetConsumeCount(eCashConsumeCountType.CharacterGacha);
			if (count > 0)
				ConsumeProductProcessor.instance.AddConsumeGacha(_listCashConsumeCountKey[(int)eCashConsumeCountType.CharacterGacha], count);

			count = GetConsumeCount(eCashConsumeCountType.EquipGacha);
			if (count > 0)
				ConsumeProductProcessor.instance.AddConsumeGacha(_listCashConsumeCountKey[(int)eCashConsumeCountType.EquipGacha], count);

			count = GetConsumeCount(eCashConsumeCountType.Spell3Gacha);
			if (count > 0)
				ConsumeProductProcessor.instance.AddConsumeGacha(_listCashConsumeCountKey[(int)eCashConsumeCountType.Spell3Gacha], count);

			count = GetConsumeCount(eCashConsumeCountType.Spell4Gacha);
			if (count > 0)
				ConsumeProductProcessor.instance.AddConsumeGacha(_listCashConsumeCountKey[(int)eCashConsumeCountType.Spell4Gacha], count);

			count = GetConsumeCount(eCashConsumeCountType.Spell5Gacha);
			if (count > 0)
				ConsumeProductProcessor.instance.AddConsumeGacha(_listCashConsumeCountKey[(int)eCashConsumeCountType.Spell5Gacha], count);

			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneConsumeProgress"), () =>
			{
				ConsumeProductProcessor.instance.ProcessConsume();
			}, -1, true);
			return true;
		}

		// 위 펜딩 상품이 없을때는 DB에서 소모시켜야할 아이템이 남아있는지 확인해서 처리해주면 된다.
		for (int i = 0; i < (int)eCashConsumeFlagType.Amount; ++i)
		{
			if (IsPurchasedFlag((eCashConsumeFlagType)i) == false)
				continue;

			bool process = false;
			string itemName = "";
			string processMessage = "";
			switch ((eCashConsumeFlagType)i)
			{
				case eCashConsumeFlagType.BrokenEnergy:
				case eCashConsumeFlagType.PetSale:
				case eCashConsumeFlagType.UnacquiredSpell:
				case eCashConsumeFlagType.AcquiredSpell:
				case eCashConsumeFlagType.UnacquiredCompanion:
				case eCashConsumeFlagType.AcquiredCompanion:
				case eCashConsumeFlagType.AcquiredCompanionPp:
					ConsumeItemTableData consumeItemTableData = TableDataManager.instance.FindConsumeItemTableData(_listCashConsumeFlagKey[i]);
					if (consumeItemTableData != null)
					{
						itemName = UIString.instance.GetString(consumeItemTableData.name);
						processMessage = string.Format("{0}\n\n{1}", itemName, UIString.instance.GetString("ShopUI_NotDoneConsumeProgress"));
					}
					else
						processMessage = UIString.instance.GetString("ShopUI_NotDoneConsumeProgress");
					process = true;
					break;
				case eCashConsumeFlagType.Ev4ContiNext:
					// hardcode ev4
					int currentCompleteStep = GetContinuousProductStep("ev4");
					string id = string.Format("ev4_conti_{0}", currentCompleteStep + 1);
					ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData(id);
					bool cashStep = (shopProductTableData != null && shopProductTableData.free == false);
					PlayFabApiManager.instance.RequestConsumeContinuousNext("ev4", cashStep, null);
					break;
				case eCashConsumeFlagType.Ev5OnePlTwoCash:
					// hardcode ev5
					PlayFabApiManager.instance.RequestConsumeOnePlusTwoCash("ev5", null);
					break;
				case eCashConsumeFlagType.SevenSlot1:
					PlayFabApiManager.instance.RequestConsumeSevenSlot(0, null);
					break;
				case eCashConsumeFlagType.SevenSlot2:
					PlayFabApiManager.instance.RequestConsumeSevenSlot(1, null);
					break;
				case eCashConsumeFlagType.SevenSlot3:
					PlayFabApiManager.instance.RequestConsumeSevenSlot(2, null);
					break;
				case eCashConsumeFlagType.SevenSlot4:
					PlayFabApiManager.instance.RequestConsumeSevenSlot(3, null);
					break;
				case eCashConsumeFlagType.PetPass:
					PlayFabApiManager.instance.RequestConsumePetPass(null);
					break;
				case eCashConsumeFlagType.FestivalSlot1:
					PlayFabApiManager.instance.RequestConsumeFestivalSlot(0, null);
					break;
				case eCashConsumeFlagType.FestivalSlot2:
					PlayFabApiManager.instance.RequestConsumeFestivalSlot(1, null);
					break;
				case eCashConsumeFlagType.FestivalSlot3:
					PlayFabApiManager.instance.RequestConsumeFestivalSlot(2, null);
					break;
				case eCashConsumeFlagType.FestivalSlot4:
					PlayFabApiManager.instance.RequestConsumeFestivalSlot(3, null);
					break;
				case eCashConsumeFlagType.TeamPass:
					PlayFabApiManager.instance.RequestConsumeTeamPass(null);
					break;
			}

			if (process == false)
				continue;

			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), processMessage, () =>
			{
				switch ((eCashConsumeFlagType)i)
				{
					case eCashConsumeFlagType.BrokenEnergy:
						BrokenEnergyCanvas.ConsumeProduct();
						break;
					case eCashConsumeFlagType.PetSale:
						PetSaleCanvas.ConsumeProduct();
						break;
					case eCashConsumeFlagType.FortuneWheel:
						FortuneWheelCanvas.ConsumeProduct();
						break;
					case eCashConsumeFlagType.UnacquiredSpell:
						UnacquiredSpellSaleCanvas.ConsumeProduct();
						break;
					case eCashConsumeFlagType.AcquiredSpell:
						AcquiredSpellSaleCanvas.ConsumeProduct();
						break;
					case eCashConsumeFlagType.UnacquiredCompanion:
						UnacquiredCharacterSaleCanvas.ConsumeProduct();
						break;
					case eCashConsumeFlagType.AcquiredCompanion:
						AcquiredCharacterSaleCanvas.ConsumeProduct();
						break;
					case eCashConsumeFlagType.AcquiredCompanionPp:
						AcquiredCharacterPpSaleCanvas.ConsumeProduct();
						break;
				}
				
			}, -1, true);
			return true;
		}

		// Count Consume도 확인
		for (int i = 0; i < (int)eCashConsumeCountType.Amount; ++i)
		{
			if (GetConsumeCount((eCashConsumeCountType)i) == 0)
				continue;

			bool process = false;
			string itemName = "";
			switch ((eCashConsumeCountType)i)
			{
				case eCashConsumeCountType.SevenTotal:
					PlayFabApiManager.instance.RequestConsumeSevenTotal(null);
					break;
				case eCashConsumeCountType.FestivalTotal:
					PlayFabApiManager.instance.RequestConsumeFestivalTotal(null);
					break;
				case eCashConsumeCountType.AnalysisBoost:
					PlayFabApiManager.instance.RequestConsumeAnalysisBoost(null);
					break;
			}

			if (process == false)
				continue;

			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneConsumeProgress", itemName), () =>
			{
				// 컨슘은 전용으로 다루는 창이 아니기 때문에 로직으로 따로 구현해야한다.
				// 뽑기 로직을 간단하게 수행하는 형태일거다.
				//BrokenEnergyCanvas.ConsumeProduct();
				switch ((eCashConsumeCountType)i)
				{
					//case eCashConsumeCountType.SpellGacha:
					//	ConsumeSpellGacha();
					//	break;
				}

			}, -1, true);
			return true;
		}

		return false;
	}
	#endregion
}