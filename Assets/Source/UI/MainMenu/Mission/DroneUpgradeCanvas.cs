using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DroneUpgradeCanvas : MonoBehaviour
{
	public static DroneUpgradeCanvas instance;

	public static int s_DroneCountMax = 22;

	public Transform countTextTransform;
	public Text countLevelText;
	public Text countValueText;
	public Text atkLevelText;
	public Text atkValueText;
	public Text remainPointText;

	public Transform countAlarmRootTransform;
	public Transform atkAlarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		SetInfo();
	}

	public static int GetRemainDronePoint()
	{
		RobotDefenseStepTableData robotDefenseStepTableData = TableDataManager.instance.FindRobotDefenseStepTableData(SubMissionData.instance.robotDefenseClearLevel);
		if (robotDefenseStepTableData == null)
			return 0;
		int useCount = SubMissionData.instance.robotDefenseDroneCountLevel - 1;
		int useAtk = SubMissionData.instance.robotDefenseDroneAttackLevel - 1;
		return robotDefenseStepTableData.droneAccumulatedPoint - useCount - useAtk;
	}

	void SetInfo()
	{
		int remainDronePoint = GetRemainDronePoint();

		AlarmObject.Hide(countAlarmRootTransform);
		AlarmObject.Hide(atkAlarmRootTransform);

		#region Count
		int countLevel = SubMissionData.instance.robotDefenseDroneCountLevel;
		if (countLevel >= s_DroneCountMax)
		{
			// level
			countLevelText.text = UIString.instance.GetString("GameUI_Lv", "Max");
		}
		else
		{
			// level
			countLevelText.text = UIString.instance.GetString("GameUI_Lv", countLevel);
			countValueText.text = string.Format("{0} / {1}", countLevel, s_DroneCountMax);

			if (remainDronePoint > 0)
				AlarmObject.Show(countAlarmRootTransform);
		}
		#endregion

		#region Atk
		int atkLevel = SubMissionData.instance.robotDefenseDroneAttackLevel;
		if (atkLevel >= TableDataManager.instance.GetGlobalConstantInt("MaxDroneAtkLevel"))
		{
			// level
			atkLevelText.text = UIString.instance.GetString("GameUI_Lv", "Max");
		}
		else
		{
			// level
			atkLevelText.text = UIString.instance.GetString("GameUI_Lv", countLevel);
			atkValueText.text = SubMissionData.instance.cachedValueByRobotDefense.ToString("N0");

			if (remainDronePoint > 0)
				AlarmObject.Show(atkAlarmRootTransform);
		}
		#endregion

		#region Remain
		remainPointText.text = remainDronePoint.ToString("N0");
		remainPointText.color = (remainDronePoint > 0) ? new Color(0.1f, 0.9f, 0.1f) : Color.gray;
		#endregion
	}

	public void OnClickDroneCountTextButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("MissionUI_UpgradeDroneCountMore"), 200, countTextTransform, new Vector2(5.0f, -25.0f));
	}

	public void OnClickCountLevelUpButton()
	{
		if (SubMissionData.instance.robotDefenseDroneCountLevel >= s_DroneCountMax)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
			return;
		}

		if (GetRemainDronePoint() == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_NotEnoughDronePoint"), 2.0f);
			return;
		}

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("PointShopUI_LevelUpConfirm"), () =>
		{
			PlayFabApiManager.instance.RequestLevelUpRobotDefenseCount(SubMissionData.instance.robotDefenseDroneCountLevel + 1, 1, () =>
			{
				// toast
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_DroneCountComplete"), 2.0f);

				SetInfo();
				RobotDefenseEnterCanvas.instance.RefreshAlarmObject();
			});
		});
	}

	float _prevCombatValue;
	public void OnClickAtkLevelUpButton()
	{
		if (SubMissionData.instance.robotDefenseDroneAttackLevel >= TableDataManager.instance.GetGlobalConstantInt("MaxDroneAtkLevel"))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
			return;
		}

		if (GetRemainDronePoint() == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_NotEnoughDronePoint"), 2.0f);
			return;
		}

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("PointShopUI_LevelUpConfirm"), () =>
		{
			_prevCombatValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			PlayFabApiManager.instance.RequestLevelUpRobotDefenseAttack(SubMissionData.instance.robotDefenseDroneAttackLevel + 1, 1, () =>
			{
				SetInfo();
				RobotDefenseEnterCanvas.instance.RefreshAlarmObject();

				float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
				if (nextValue > _prevCombatValue)
				{
					UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
					{
						ChangePowerCanvas.instance.ShowInfo(_prevCombatValue, nextValue);
					});
				}
			});
		});
	}
}