using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class MissionListCanvas : MonoBehaviour
{
	public static MissionListCanvas instance;

	#region Ticket
	public Text ticketText;
	public Text fillRemainTimeText;
	#endregion

	#region Energy
	public Text energyText;
	#endregion

	public Text petMenuRemainCount;
	public Text petTodayResetRemainTimeText;
	public Text petSearchEnergyText;
	public RectTransform petAlarmRootTransform;

	public Text wheelRemainCount;
	public Text wheelTodayResetRemainTimeText;
	public Text wheelEnergyText;
	public GameObject wheelEnergyObject;
	public GameObject wheelEnterObject;
	public RectTransform wheelAlarmRootTransform;

	public Text rushDefenseMenuRemainCount;
	public Text rushDefenseTodayResetRemainTimeText;
	public Text rushDefenseEnergyText;
	public RectTransform rushDefenseAlarmRootTransform;

	public Text bossDefenseMenuRemainCount;
	public Text bossDefenseTodayResetRemainTimeText;
	public Text bossDefenseEnergyText;
	public RectTransform bossDefenseAlarmRootTransform;

	public Text goldDefenseMenuRemainCount;
	public Text goldDefenseTodayResetRemainTimeText;
	public Text goldDefenseEnergyText;
	public RectTransform goldDefenseAlarmRootTransform;

	public Text bossBattleMenuRemainCount;
	public Text bossBattleTodayResetRemainTimeText;
	public Text bossBattleEnergyText;
	public Image bossBattleImage;
	public RectTransform bossBattleAlarmRootTransform;

	void Awake()
	{
		instance = this;

		#region Energy
		_canvas = GetComponent<Canvas>();
		#endregion
	}

	Canvas _canvas;
	void OnEnable()
	{
		#region Energy
		RefreshEnergy();
		#endregion
		#region Ticket
		RefreshTicket();
		#endregion
		RefreshInfo();

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (restore)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(true);
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	void Update()
	{
		UpdateResetRemainTime();
		UpdateEnergy();

		#region Ticket
		UpdateFillRemainTime();
		UpdateRefresh();
		#endregion
	}

	#region Energy
	public void RefreshEnergy()
	{
		energyText.text = CurrencyData.instance.energy.ToString("N0");
	}
	#endregion

	public static bool IsAlarmPetSearch()
	{
		if (PetManager.instance.dailySearchCount < BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetDailySearchCount"))
			return true;
		return false;
	}

	public static bool IsAlarmFortuneWheel()
	{
		if (SubMissionData.instance.fortuneWheelDailyCount < BattleInstanceManager.instance.GetCachedGlobalConstantInt("FortuneWheelDailyCount"))
			return true;
		return false;
	}

	public static bool IsAlarmRushDefense()
	{
		if (SubMissionData.instance.rushDefenseDailyCount < BattleInstanceManager.instance.GetCachedGlobalConstantInt("RushDefenseDailyCount"))
			return true;
		return false;
	}

	public static bool IsAlarmBossDefense()
	{
		if (SubMissionData.instance.bossDefenseDailyCount < BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossDefenseDailyCount"))
			return true;
		return false;
	}

	public static bool IsAlarmGoldDefense()
	{
		if (SubMissionData.instance.goldDefenseDailyCount < BattleInstanceManager.instance.GetCachedGlobalConstantInt("GoldDefenseDailyCount"))
			return true;
		return false;
	}

	public static bool IsAlarmBossBattle()
	{
		if (SubMissionData.instance.bossBattleDailyCount < BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleDailyCount"))
			return true;
		return false;
	}

	void RefreshInfo()
	{
		int cost = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyPet");
		petSearchEnergyText.text = cost.ToString("N0");
		petMenuRemainCount.text = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetDailySearchCount") - PetManager.instance.dailySearchCount).ToString();
		AlarmObject.Hide(petAlarmRootTransform);
		if (IsAlarmPetSearch())
			AlarmObject.Show(petAlarmRootTransform);

		cost = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyRoulette");
		wheelEnergyText.text = cost.ToString("N0");
		int count = BattleInstanceManager.instance.GetCachedGlobalConstantInt("FortuneWheelDailyCount") - SubMissionData.instance.fortuneWheelDailyCount;
		if (count < 0) count = 0;
		wheelRemainCount.text = count.ToString();
		wheelEnergyObject.SetActive(SubMissionData.instance.fortuneWheelDailyCount == 0);
		wheelEnterObject.SetActive(SubMissionData.instance.fortuneWheelDailyCount > 0);
		AlarmObject.Hide(wheelAlarmRootTransform);
		if (IsAlarmFortuneWheel())
			AlarmObject.Show(wheelAlarmRootTransform);

		cost = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyRushDefense");
		rushDefenseEnergyText.text = cost.ToString("N0");
		rushDefenseMenuRemainCount.text = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("RushDefenseDailyCount") - SubMissionData.instance.rushDefenseDailyCount).ToString();
		AlarmObject.Hide(rushDefenseAlarmRootTransform);
		if (IsAlarmRushDefense())
			AlarmObject.Show(rushDefenseAlarmRootTransform);

		cost = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyBossDefense");
		bossDefenseEnergyText.text = cost.ToString("N0");
		bossDefenseMenuRemainCount.text = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossDefenseDailyCount") - SubMissionData.instance.bossDefenseDailyCount).ToString();
		AlarmObject.Hide(bossDefenseAlarmRootTransform);
		if (IsAlarmBossDefense())
			AlarmObject.Show(bossDefenseAlarmRootTransform);

		cost = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyGoldDefense");
		goldDefenseEnergyText.text = cost.ToString("N0");
		goldDefenseMenuRemainCount.text = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("GoldDefenseDailyCount") - SubMissionData.instance.goldDefenseDailyCount).ToString();
		AlarmObject.Hide(goldDefenseAlarmRootTransform);
		if (IsAlarmGoldDefense())
			AlarmObject.Show(goldDefenseAlarmRootTransform);

		if (bossBattleImage.sprite == null)
		{
			AddressableAssetLoadManager.GetAddressableSprite("Portrait_SpiritKing", "Icon", (sprite) =>
			{
				bossBattleImage.sprite = null;
				bossBattleImage.sprite = sprite;
				bossBattleImage.gameObject.SetActive(true);
			});
		}
		cost = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyBossBattle");
		bossBattleEnergyText.text = cost.ToString("N0");
		int remainBonusCount = BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleDailyCount") - SubMissionData.instance.bossBattleDailyCount;
		if (remainBonusCount < 0) remainBonusCount = 0;
		bossBattleMenuRemainCount.text = remainBonusCount.ToString();
		AlarmObject.Hide(bossBattleAlarmRootTransform);
		if (IsAlarmBossBattle())
			AlarmObject.Show(bossBattleAlarmRootTransform);
	}

	public void OnClickButton(int index)
	{
		switch (index)
		{
			case 0:

				// 횟수 검사
				if (IsAlarmPetSearch() == false)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TodayCountComplete"), 2.0f);
					return;
				}

				if (CurrencyData.instance.ticket < BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyPet"))
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughTicket"), 2.0f);
					return;
				}

				Timing.RunCoroutine(PetSearchMoveProcess());
				break;

			case 1:

				if (SubMissionData.instance.fortuneWheelDailyCount == 0 && CurrencyData.instance.ticket < BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyRoulette"))
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughTicket"), 2.0f);
					return;
				}

				UIInstanceManager.instance.ShowCanvasAsync("FortuneWheelCanvas", null);
				break;

			case 2:

				if (IsAlarmRushDefense() == false)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TodayCountComplete"), 2.0f);
					return;
				}

				if (CurrencyData.instance.ticket < BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyRushDefense"))
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughTicket"), 2.0f);
					return;
				}

				UIInstanceManager.instance.ShowCanvasAsync("RushDefenseEnterCanvas", null);
				break;
			case 3:

				if (CharacterManager.instance.listCharacterData.Count < BossDefenseEnterCanvas.MINIMUM_COUNT)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_BossDefenseMemberLimit"), 2.0f);
					return;
				}

				if (IsAlarmBossDefense() == false)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TodayCountComplete"), 2.0f);
					return;
				}

				if (CurrencyData.instance.ticket < BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyBossDefense"))
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughTicket"), 2.0f);
					return;
				}

				UIInstanceManager.instance.ShowCanvasAsync("BossDefenseEnterCanvas", null);
				break;

			case 4:
				if (CharacterManager.instance.listCharacterData.Count < GoldDefenseEnterCanvas.MINIMUM_COUNT)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_GoldDefenseMemberLimit"), 2.0f);
					return;
				}

				if (IsAlarmGoldDefense() == false)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TodayCountComplete"), 2.0f);
					return;
				}

				if (CurrencyData.instance.ticket < BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyGoldDefense"))
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughTicket"), 2.0f);
					return;
				}

				UIInstanceManager.instance.ShowCanvasAsync("GoldDefenseEnterCanvas", null);
				break;
			case 5:

				if (CharacterManager.instance.listCharacterData.Count == 0)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_BossBattleMemberLimit"), 2.0f);
					return;
				}

				if (CurrencyData.instance.ticket < BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyBossBattle"))
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughTicket"), 2.0f);
					return;
				}

				UIInstanceManager.instance.ShowCanvasAsync("BossBattleEnterCanvas", null);
				break;
		}
	}

	public static void ShowCanvasAsyncWithPrepareGround(string canvasAddress, System.Action callback)
	{
		if (ContentsPrefabGroup.instance == null)
		{
			DelayedLoadingCanvas.Show(true);
			AddressableAssetLoadManager.GetAddressableGameObject("ContentsPrefabGroup", "Map", (prefab) =>
			{
				BattleInstanceManager.instance.GetCachedObject(prefab, null);
				DelayedLoadingCanvas.Show(false);
				UIInstanceManager.instance.ShowCanvasAsync(canvasAddress, callback);
			});
		}
		else
			UIInstanceManager.instance.ShowCanvasAsync(canvasAddress, callback);
	}


	IEnumerator<float> PetSearchMoveProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);
		
		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		StageManager.instance.FinalizeStage();
		TeamManager.instance.HideForMoveMap(true);

		// StackCanvas로 이동하는거라 안닫아도 된다.
		//gameObject.SetActive(false);

		//while (gameObject.activeSelf)
		//	yield return Timing.WaitForOneFrame;
		//yield return Timing.WaitForOneFrame;

		ShowCanvasAsyncWithPrepareGround("PetSearchCanvas", () =>
		{
			// 
			DelayedLoadingCanvas.Show(false);
			FadeCanvas.instance.FadeIn(0.5f);
		});
	}



	int _lastRefreshRemainTimeSecond = -1;
	void UpdateResetRemainTime()
	{
		#region Pet
		bool petProcess = false;
		if (PetManager.instance.dailySearchCount == 0)
			petTodayResetRemainTimeText.text = "";
		else
			petProcess = true;
		#endregion

		#region Wheel
		bool wheelProcess = false;
		if (SubMissionData.instance.fortuneWheelDailyCount == 0)
			wheelTodayResetRemainTimeText.text = "";
		else
			wheelProcess = true;
		#endregion

		#region Rush Defense
		bool rushDefenseProcess = false;
		if (SubMissionData.instance.rushDefenseDailyCount == 0)
			rushDefenseTodayResetRemainTimeText.text = "";
		else
			rushDefenseProcess = true;
		#endregion

		#region Boss Defense
		bool bossDefenseProcess = false;
		if (SubMissionData.instance.bossDefenseDailyCount == 0)
			bossDefenseTodayResetRemainTimeText.text = "";
		else
			bossDefenseProcess = true;
		#endregion

		#region Gold Defense
		bool goldDefenseProcess = false;
		if (SubMissionData.instance.goldDefenseDailyCount == 0)
			goldDefenseTodayResetRemainTimeText.text = "";
		else
			goldDefenseProcess = true;
		#endregion

		#region Boss Battle
		bool bossBattleProcess = false;
		if (SubMissionData.instance.bossBattleDailyCount == 0)
			bossBattleTodayResetRemainTimeText.text = "";
		else
			bossBattleProcess = true;
		#endregion

		if (petProcess == false && wheelProcess == false && rushDefenseProcess == false && bossDefenseProcess == false && goldDefenseProcess == false && bossBattleProcess == false)
		{
			_lastRefreshRemainTimeSecond = -1;
			return;
		}

		if (ServerTime.UtcNow < PlayerData.instance.dayRefreshTime)
		{
			System.TimeSpan remainTime = PlayerData.instance.dayRefreshTime - ServerTime.UtcNow;
			if (_lastRefreshRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				if (petProcess) petTodayResetRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				if (wheelProcess) wheelTodayResetRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				if (rushDefenseProcess) rushDefenseTodayResetRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				if (bossDefenseProcess) bossDefenseTodayResetRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				if (goldDefenseProcess) goldDefenseTodayResetRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				if (bossBattleProcess) bossBattleTodayResetRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRefreshRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			if (petProcess) petTodayResetRemainTimeText.text = "";
			if (wheelProcess) wheelTodayResetRemainTimeText.text = "";
			if (rushDefenseProcess) rushDefenseTodayResetRemainTimeText.text = "";
			if (bossDefenseProcess) bossDefenseTodayResetRemainTimeText.text = "";
			if (goldDefenseProcess) goldDefenseTodayResetRemainTimeText.text = "";
			if (bossBattleProcess) bossBattleTodayResetRemainTimeText.text = "";
		}
	}

	int _lastEnergySecond = -1;
	void UpdateEnergy()
	{
		if (CurrencyData.instance.energy >= CurrencyData.instance.energyMax)
			return;

		if (_lastEnergySecond != (int)Time.time)
		{
			//Debug.Log(_lastEnergySecond);
			RefreshEnergy();
			_lastEnergySecond = (int)Time.time;
		}
	}


	#region Ticket
	public void RefreshTicket()
	{
		int current = CurrencyData.instance.ticket;
		int max = CurrencyData.instance.ticketMax;
		ticketText.text = string.Format("{0} / {1}", current, max);
		_lastCurrent = current;
		if (current >= max)
		{
			fillRemainTimeText.text = "";
			_needUpdate = false;
		}
		else
		{
			_nextFillDateTime = CurrencyData.instance.ticketRechargeTime;
			_needUpdate = true;
			_lastRemainTimeSecond = -1;
		}
	}

	bool _needUpdate = false;
	System.DateTime _nextFillDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateFillRemainTime()
	{
		if (_needUpdate == false)
			return;

		if (ServerTime.UtcNow < _nextFillDateTime)
		{
			System.TimeSpan remainTime = _nextFillDateTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				fillRemainTimeText.text = string.Format("{0}:{1:00}", remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			// 우선 클라단에서 하기로 했으니 서버랑 통신해서 바꾸진 않는다.
			// 대신 CurrencyData의 값과 비교하면서 바뀌는지 확인한다.
			_needUpdate = false;
			fillRemainTimeText.text = "0:00";
			_needRefresh = true;
		}
	}

	bool _needRefresh = false;
	int _lastCurrent;
	void UpdateRefresh()
	{
		if (_needRefresh == false)
			return;

		if (_lastCurrent != CurrencyData.instance.ticket)
		{
			RefreshTicket();
			_needRefresh = false;
		}
	}
	#endregion
}