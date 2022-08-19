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

	// 결제시 CF 항목에다가 플래그로 기록해서 구매여부를 확인하기로 한다.
	// 0 이라면 아무것도 구매하지 않은 상태일거고
	// 1 이면 첫번째 항목 구매
	// 2 면 두번째 항목 구매
	// 3 이면 첫번째 두번째 둘다 구매
	// 이런식으로 플래그 조합으로 처리한다.
	public enum eCashFlagType
	{
		LevelPass = 0,
		StagePass1 = 1,

		Amount,
	}
	List<ObscuredBool> _listCashFlag = new List<ObscuredBool>();

	public enum eCashCountType
	{
		DailyGold = 0,

		Amount,
	}
	List<ObscuredInt> _listCashCount = new List<ObscuredInt>();

	// 레벨패스에서 받았음을 기억해두는 변수인데 어차피 받을때마다 서버검증 하기때문에 Obscured 안쓰고 그냥 사용하기로 한다.
	List<int> _listLevelPassReward;

	public void OnRecvCashShopData(Dictionary<string, int> userVirtualCurrency, Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData)
	{
		/*
		// 아직 언픽스드를 쓸지 안쓸지 모르니
		// PlayerData.ResetData 호출되면 다시 여기로 들어올테니 플래그들 초기화 시켜놓는다.
		_checkedUnfixedItemInfo = false;
		*/

		_dicExpireTime.Clear();

		// 이벤트는 여러개 있고 각각의 유효기간이 있으니 테이블 돌면서
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
		}

		// 이번 캐시상품의 핵심이 되는 플래그다.
		_listCashFlag.Clear();
		if (userVirtualCurrency.ContainsKey("CF"))
		{
			int cashFlagValue = userVirtualCurrency["CF"];

			// 하나로 온 int를 쪼개서 플래그 리스트로 분리해서 관리한다.
			for (int i = 0; i < (int)eCashFlagType.Amount; ++i)
			{
				int flagValue = 1 << i;
				_listCashFlag.Add((flagValue & cashFlagValue) > 0);
			}
		}

		// 이건 카운트 처리용
		if (userVirtualCurrency.ContainsKey("CC"))
		{

		}

		var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
		_listLevelPassReward = null;
		if (userReadOnlyData.ContainsKey("lvPssLst"))
		{
			string lvPssLstString = userReadOnlyData["lvPssLst"].Value;
			if (string.IsNullOrEmpty(lvPssLstString) == false)
				_listLevelPassReward = serializer.DeserializeObject<List<int>>(lvPssLstString);
		}
		levelPassAlarmStateForNoPass = (IsPurchasedFlag(eCashFlagType.LevelPass) == false);

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

	DateTime _emptyDateTime = new DateTime();
	public DateTime GetExpireDateTime(string eventId)
	{
		if (_dicExpireTime.ContainsKey(eventId))
			return _dicExpireTime[eventId];
		return _emptyDateTime;
	}

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
}