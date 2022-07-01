using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

	public void OnRecvCashShopData(Dictionary<string, string> titleData, Dictionary<string, UserDataRecord> userReadOnlyData)
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
}