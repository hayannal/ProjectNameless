using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;

public class DroneUpgradeCanvas : MonoBehaviour
{
	public static DroneUpgradeCanvas instance;

	public static int s_DroneCountMax = 22;
	public static int s_DroneDefaultCount = 3;

	public Transform countTextTransform;
	public Text countLevelText;
	public Text countValueText;
	public Text countPriceText;
	public Text atkLevelText;
	public Text atkValueText;
	public Text remainPointText;

	public GameObject countLevelUpImageEffectObject;
	public GameObject atkLevelUpImageEffectObject;

	public Transform countAlarmRootTransform;
	public Transform atkAlarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	ObscuredInt _countLevelUpPrice;
	void OnEnable()
	{
		_countLevelUpPrice = BattleInstanceManager.instance.GetCachedGlobalConstantInt("RobotDefenseCountUpRequired");
		SetInfo();
	}

	public static int Level2DroneCount(int level)
	{
		return level + s_DroneDefaultCount - 1;
	}

	public static int DroneCountMaxLevel()
	{
		return s_DroneCountMax - s_DroneDefaultCount + 1;
	}

	public static int GetRemainDronePoint()
	{
		int accum = 0;
		RobotDefenseStepTableData robotDefenseStepTableData = TableDataManager.instance.FindRobotDefenseStepTableData(SubMissionData.instance.robotDefenseClearLevel);
		if (robotDefenseStepTableData != null)
			accum = robotDefenseStepTableData.droneAccumulatedPoint;

		int useCount = SubMissionData.instance.robotDefenseDroneCountLevel - 1;
		int useAtk = SubMissionData.instance.robotDefenseDroneAttackLevel - 1;
		return accum - useCount * BattleInstanceManager.instance.GetCachedGlobalConstantInt("RobotDefenseCountUpRequired") - useAtk;
	}

	void SetInfo()
	{
		int remainDronePoint = GetRemainDronePoint();

		AlarmObject.Hide(countAlarmRootTransform);
		AlarmObject.Hide(atkAlarmRootTransform);

		#region Count
		int countLevel = SubMissionData.instance.robotDefenseDroneCountLevel;
		if (countLevel >= DroneCountMaxLevel())
		{
			// level
			countLevelText.text = UIString.instance.GetString("GameUI_Lv", "Max");
		}
		else
		{
			// level
			countLevelText.text = UIString.instance.GetString("GameUI_Lv", countLevel);
			countValueText.text = string.Format("{0} / {1}", Level2DroneCount(countLevel), s_DroneCountMax);

			if (remainDronePoint >= _countLevelUpPrice)
				AlarmObject.Show(countAlarmRootTransform);
		}
		countPriceText.text = _countLevelUpPrice.ToString("N0");
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
			atkLevelText.text = UIString.instance.GetString("GameUI_Lv", atkLevel);
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
		if (SubMissionData.instance.robotDefenseDroneCountLevel >= DroneCountMaxLevel())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
			return;
		}

		if (GetRemainDronePoint() < _countLevelUpPrice)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_NotEnoughDronePoint"), 2.0f);
			return;
		}

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("PointShopUI_LevelUpConfirm"), () =>
		{
			PlayFabApiManager.instance.RequestLevelUpRobotDefenseCount(SubMissionData.instance.robotDefenseDroneCountLevel + 1, _countLevelUpPrice, () =>
			{
				// toast
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_DroneCountComplete"), 2.0f);

				countLevelUpImageEffectObject.SetActive(true);
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
				atkLevelUpImageEffectObject.SetActive(true);
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