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
		SevenTotal = 3,
		SevenSlot0 = 4,
		SevenSlot1 = 5,
		SevenSlot2 = 6,
		SevenSlot3 = 7,

		Amount,
	}
	List<ObscuredBool> _listCashConsumeFlag = new List<ObscuredBool>();
	List<string> _listCashConsumeFlagKey = new List<string> { "Cash_sBrokenEnergy", "Cash_sEv4ContiNext", "Cash_sEv5OnePlTwoCash", "Cash_sSevenTotal", "Cash_sSevenSlot0", "Cash_sSevenSlot1", "Cash_sSevenSlot2", "Cash_sSevenSlot3" };

	public enum eCashCountType
	{
		DailyGold = 0,

		Amount,
	}
	List<ObscuredInt> _listCashCount = new List<ObscuredInt>();
	List<string> _listCashCountKey = new List<string> { "Cash_cDailyGold" };

	// 레벨패스에서 받았음을 기억해두는 변수인데 어차피 받을때마다 서버검증 하기때문에 Obscured 안쓰고 그냥 사용하기로 한다.
	List<int> _listLevelPassReward;
	// 구조가 거의 비슷해서 그대로 비슷하게 구현해본다.
	List<int> _listEnergyPaybackReward;

	#region EventPoint
	public enum eEventStartCondition
	{
		ByCode = 0,
		Login = 1,
		BossStageFailed = 2,
		SpinZero = 3,
		OnApplicationPause = 4,
	}
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
		_listCashCount.Clear();
		for (int i = 0; i < (int)eCashCountType.Amount; ++i)
			_listCashCount.Add(0);

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

			for (int j = 0; j < _listCashCountKey.Count; ++j)
			{
				if (_listCashCountKey[j] == userInventory[i].ItemId)
				{
					_listCashCount[j] = (userInventory[i].RemainingUses != null) ? (int)userInventory[i].RemainingUses : 0;
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

			// 아직 이벤트가 다 완성된게 아니니 우선 이거로 막아둔다.
			//if ((eEventStartCondition)TableDataManager.instance.eventTypeTable.dataArray[i].triggerCondition != eEventStartCondition.SpinZero)
			//	continue;

			PlayFabApiManager.instance.RequestOpenCashEvent(TableDataManager.instance.eventTypeTable.dataArray[i].id, TableDataManager.instance.eventTypeTable.dataArray[i].eventSub, TableDataManager.instance.eventTypeTable.dataArray[i].givenTime, TableDataManager.instance.eventTypeTable.dataArray[i].coolTime, () =>
			{
				if (MainCanvas.instance != null && MainCanvas.instance.gameObject.activeSelf)
					MainCanvas.instance.ShowCashEvent(TableDataManager.instance.eventTypeTable.dataArray[i].id, true, true);
			});

			// 이벤트는 중복해서 열리지 않게 하기 위해 한개라도 열었으면 break 시키는게 맞을거 같다.
			break;
		}
	}

	public void OnOpenCashEvent(string openEventId, string eventSub)
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
			}
		}
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


	#region Pending Product
	public bool CheckPendingProduct()
	{
		// 초기화가 안되어도 캐시샵이 열리는 구조로 바꾸면서 체크
		if (CodelessIAPStoreListener.initializationComplete == false)
			return false;

		if (IAPListenerWrapper.instance.pendingProduct == null)
			return false;

		Product pendingProduct = IAPListenerWrapper.instance.pendingProduct;
		Debug.LogFormat("Check IAPListener pending product id : {0}", pendingProduct.definition.id);
		//Debug.LogFormat("IAPListener failed product storeSpecificId : {0}", failedProduct.definition.storeSpecificId);

		// 완료되지 않은 구매상품의 아이디에 따라 뭘 진행시킬지 판단해야한다.
		if (pendingProduct.definition.id == "bigboost")
		{
			BigBoostEventCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id == "levelpass")
		{
			LevelPassCanvas.ExternalRetryPurchase(pendingProduct);
		}
		else if (pendingProduct.definition.id == "brokenenergy")
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


		return true;
	}

	public bool CheckUncomsumeProduct()
	{
		// 위 펜딩 상품이 없을때는 DB에서 소모시켜야할 아이템이 남아있는지 확인해서 처리해주면 된다.
		for (int i = 0; i < (int)eCashConsumeFlagType.Amount; ++i)
		{
			if (IsPurchasedFlag((eCashConsumeFlagType)i) == false)
				continue;

			bool process = false;
			string itemName = "";
			switch ((eCashConsumeFlagType)i)
			{
				case eCashConsumeFlagType.BrokenEnergy:
					ConsumeItemTableData consumeItemTableData = TableDataManager.instance.FindConsumeItemTableData(_listCashConsumeFlagKey[i]);
					if (consumeItemTableData != null)
						itemName = UIString.instance.GetString(consumeItemTableData.name);
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
				case eCashConsumeFlagType.SevenSlot0:
					PlayFabApiManager.instance.RequestConsumeSevenSlot(0, null);
					break;
				case eCashConsumeFlagType.SevenSlot1:
					PlayFabApiManager.instance.RequestConsumeSevenSlot(1, null);
					break;
				case eCashConsumeFlagType.SevenSlot2:
					PlayFabApiManager.instance.RequestConsumeSevenSlot(2, null);
					break;
				case eCashConsumeFlagType.SevenSlot3:
					PlayFabApiManager.instance.RequestConsumeSevenSlot(3, null);
					break;
			}

			if (process == false)
				continue;

			OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress", itemName), () =>
			{
				BrokenEnergyCanvas.ConsumeProduct();
			}, -1, true);
			return true;
		}

		return false;
	}
	#endregion
}