using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;

public class PointShopAttackCanvas : MonoBehaviour
{
	public static PointShopAttackCanvas instance;

	public Text pointText;
	public Text levelText;
	public Text attackValueText;

	public GameObject levelUpImageEffectObject;

	public RectTransform alarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	int _addPoint;
	void OnEnable()
	{
		pointText.text = "0";
		_addPoint = SubMissionData.instance.bossBattlePoint;
		if (_addPoint > 0)
		{
			_pointChangeRemainTime = pointChangeTime;
			_pointChangeSpeed = _addPoint / _pointChangeRemainTime;
			_currentPoint = 0.0f;
			_updatePointText = true;
		}

		RefreshAttackLevel();
	}

	ObscuredInt _price;
	ObscuredInt _needAccumulatedCount;
	void RefreshAttackLevel()
	{
		PointShopAtkTableData pointShopAtkTableData = TableDataManager.instance.FindPointShopAtkTableData(SubMissionData.instance.bossBattleAttackLevel);
		attackValueText.text = pointShopAtkTableData.accumulatedAtk.ToString("N0");

		AlarmObject.Hide(alarmRootTransform);
		if (SubMissionData.instance.bossBattleAttackLevel >= TableDataManager.instance.GetGlobalConstantInt("MaxPointShopAttackLevel"))
		{
			// level
			levelText.text = UIString.instance.GetString("GameUI_Lv", "Max");
		}
		else
		{
			// level
			levelText.text = UIString.instance.GetString("GameUI_Lv", SubMissionData.instance.bossBattleAttackLevel);

			if (CheckLevelUp())
				AlarmObject.Show(alarmRootTransform);
		}
	}

	void Update()
	{
		UpdatePointText();
	}

	const float pointChangeTime = 0.6f;
	float _pointChangeRemainTime;
	float _pointChangeSpeed;
	float _currentPoint;
	int _lastPoint;
	bool _updatePointText;
	void UpdatePointText()
	{
		if (_updatePointText == false)
			return;

		_currentPoint += _pointChangeSpeed * Time.deltaTime;
		int currentPointInt = (int)_currentPoint;
		if (currentPointInt >= _addPoint)
		{
			currentPointInt = _addPoint;
			_updatePointText = false;
		}
		if (currentPointInt != _lastPoint)
		{
			_lastPoint = currentPointInt;
			pointText.text = string.Format("{0:N0}", _lastPoint);
		}
	}

	public static bool CheckLevelUp()
	{
		if (SubMissionData.instance.bossBattleAttackLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPointShopAttackLevel"))
			return false;

		PointShopAtkTableData nextPointShopAtkTableData = TableDataManager.instance.FindPointShopAtkTableData(SubMissionData.instance.bossBattleAttackLevel + 1);
		if (nextPointShopAtkTableData == null)
			return false;

		if (SubMissionData.instance.bossBattlePoint >= nextPointShopAtkTableData.requiredCount)
			return true;

		return false;
	}

	public void OnClickLevelUpButton()
	{
		if (SubMissionData.instance.bossBattleAttackLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPointShopAttackLevel"))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("PointShopAttackConfirmCanvas", null);
	}
}
