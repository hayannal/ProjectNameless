using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class AdventureListCanvas : MonoBehaviour
{
	public static AdventureListCanvas instance;

	public Text robotDefenseMenuRemainCount;
	public Text robotDefenseTodayResetRemainTimeText;
	public RectTransform robotDefenseAlarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		RefreshInfo();
	}

	void Update()
	{
		UpdateResetRemainTime();
	}
	
	public static bool IsAlarmRobotDefense()
	{
		if (SubMissionData.instance.robotDefenseDailyCount < BattleInstanceManager.instance.GetCachedGlobalConstantInt("RobotDefenseDailyCount"))
			return true;
		return false;
	}

	void RefreshInfo()
	{
		robotDefenseMenuRemainCount.text = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("RobotDefenseDailyCount") - SubMissionData.instance.robotDefenseDailyCount).ToString();
		AlarmObject.Hide(robotDefenseAlarmRootTransform);
		if (IsAlarmRobotDefense())
			AlarmObject.Show(robotDefenseAlarmRootTransform);
	}

	public void OnClickButton(int index)
	{
		switch (index)
		{
			case 6:
				if (BattleInstanceManager.instance.GetCachedGlobalConstantInt("RobotDefenseOn") == 0)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("SystemUI_WaitUpdate"), 2.0f);
					return;
				}

				//if (CharacterManager.instance.listCharacterData.Count < RobotDefenseEnterCanvas.MINIMUM_COUNT)
				//{
				//	ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_RobotDefenseMemberLimit"), 2.0f);
				//	return;
				//}

				if (IsAlarmRobotDefense() == false)
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TodayCountComplete"), 2.0f);
					return;
				}

				if (CurrencyData.instance.ticket < BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyRobotDefense"))
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughTicket"), 2.0f);
					return;
				}

				UIInstanceManager.instance.ShowCanvasAsync("RobotDefenseEnterCanvas", null);
				break;
		}
	}

	int _lastRefreshRemainTimeSecond = -1;
	void UpdateResetRemainTime()
	{
		#region Robot Defense
		bool robotDefenseProcess = false;
		if (SubMissionData.instance.robotDefenseDailyCount == 0)
			robotDefenseTodayResetRemainTimeText.text = "";
		else
			robotDefenseProcess = true;
		#endregion

		if (robotDefenseProcess == false)
		{
			_lastRefreshRemainTimeSecond = -1;
			return;
		}

		if (ServerTime.UtcNow < PlayerData.instance.dayRefreshTime)
		{
			System.TimeSpan remainTime = PlayerData.instance.dayRefreshTime - ServerTime.UtcNow;
			if (_lastRefreshRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				if (robotDefenseProcess) robotDefenseTodayResetRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRefreshRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			if (robotDefenseProcess) robotDefenseTodayResetRemainTimeText.text = "";
		}
	}
}