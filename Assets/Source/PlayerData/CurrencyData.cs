using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
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

	public static string GoldCode() { return "GO"; }
	public static string DiamondCode() { return "DI"; }
	public static string SpinCode() { return "SP"; }

	public ObscuredInt gold { get; set; }
	public ObscuredInt spin { get; set; }
	public ObscuredInt spinMax { get; set; }
	public ObscuredInt dia { get; set; }			// 서버 상점에서 모아서 처리하는 기능이 없어서 free와 구매 다 합쳐서 처리하기로 한다.

	// 과금 요소. 클라이언트에 존재하면 무조건 굴려서 없애야하는거다. 인앱결제 결과를 받아놓는 저장소로 쓰인다.
	public ObscuredInt equipBoxKey { get; set; }
	public ObscuredInt legendEquipKey { get; set; }
	public ObscuredInt dailyDiaRemainCount { get; set; }

	public void OnRecvCurrencyData(Dictionary<string, int> userVirtualCurrency, Dictionary<string, VirtualCurrencyRechargeTime> userVirtualCurrencyRechargeTimes)
	{
		if (userVirtualCurrency.ContainsKey("GO"))
			gold = userVirtualCurrency["GO"];
		if (userVirtualCurrency.ContainsKey("DI"))
			dia = userVirtualCurrency["DI"];
		if (userVirtualCurrency.ContainsKey("SP"))
			spin = userVirtualCurrency["SP"];
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

		if (userVirtualCurrencyRechargeTimes != null && userVirtualCurrencyRechargeTimes.ContainsKey("SP"))
		{
			spinMax = userVirtualCurrencyRechargeTimes["SP"].RechargeMax;
			if (userVirtualCurrencyRechargeTimes["SP"].SecondsToRecharge > 0 && spin < spinMax)
			{
				_rechargingSpin = true;
				_spinRechargeTime = userVirtualCurrencyRechargeTimes["SP"].RechargeTime;
			}
			//TimeSpan timeSpan = userVirtualCurrencyRechargeTimes["EN"].RechargeTime - DateTime.UtcNow;
			//int totalSeconds = (int)timeSpan.TotalSeconds;
		}
	}

	void Update()
	{
		UpdateRechargeEnergy();
	}

	bool _rechargingSpin = false;
	DateTime _spinRechargeTime;
	public DateTime spinRechargeTime { get { return _spinRechargeTime; } }
	void UpdateRechargeEnergy()
	{
		// MEC쓰려다가 홈키 눌러서 내릴거 대비해서 DateTime검사로 처리한다.
		if (_rechargingSpin == false)
			return;

		// 한번만 계산하고 넘기니 한번에 여러번 해야하는 상황에서 프레임 단위로 조금씩 밀리게 된다.
		// 어차피 싱크는 맞출테지만 그래도 이왕이면 여러번 체크하게 해둔다. 120회 정도면 24시간도 버틸만할거다.
		int loopCount = 0;
		for (int i = 0; i < 120; ++i)
		{
			if (DateTime.Compare(ServerTime.UtcNow, _spinRechargeTime) < 0)
				break;

			loopCount += 1;
			spin += 1;
			if (spin == spinMax)
			{
				_rechargingSpin = false;
				break;
			}
			else
				_spinRechargeTime += TimeSpan.FromSeconds(BattleInstanceManager.instance.GetCachedGlobalConstantInt("TimeSecToGetOneSpin"));
		}

		// 여러번 건너뛰었단건 홈키 같은거 눌러서 한동안 업데이트 안되다가 몰아서 업데이트 되었단 얘기다. 이럴땐 강제 UI 업데이트
		if (loopCount > 5)
		{
			if (BettingCanvas.instance != null)
				BettingCanvas.instance.RefreshSpin();
		}
	}
	
	public bool UseSpin(int amount)
	{
		if (spin < amount)
			return false;

		bool full = (spin >= spinMax);
		spin -= amount;
		if (spin < spinMax)
		{
			if (full)
			{
				_spinRechargeTime = ServerTime.UtcNow + TimeSpan.FromSeconds(BattleInstanceManager.instance.GetCachedGlobalConstantInt("TimeSecToGetOneSpin"));
				_rechargingSpin = true;
			}
			else
			{
				if (OptionManager.instance.energyAlarm == 1)
				{
					// full이 아니었다면 이전에 등록되어있던 Noti를 먼저 삭제해야한다.
					// 만약 energyAlarm을 꺼둔채로 에너지를 소모했다면 취소시킬 Noti가 없을텐데 그걸 판단할 방법은 귀찮으므로 그냥 Cancel 호출하는거로 해둔다.
					//CancelEnergyNotification();
				}
			}

			if (OptionManager.instance.energyAlarm == 1)
			{
				//ReserveEnergyNotification();
			}
		}
		return true;
	}
	
}