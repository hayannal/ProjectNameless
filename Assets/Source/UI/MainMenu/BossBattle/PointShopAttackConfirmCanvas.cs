using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;

public class PointShopAttackConfirmCanvas : MonoBehaviour
{
	public static PointShopAttackConfirmCanvas instance;

	public Text countText;
	public Image minusButtonImage;
	public Image plusButtonImage;
	public Text maxText;
	public Text needPointText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		RefreshInfo();
	}

	int _baseCount = 1;
	int _maxCount = 0;
	void RefreshInfo()
	{
		PointShopAtkTableData nextPointShopAtkTableData = TableDataManager.instance.FindPointShopAtkTableData(SubMissionData.instance.bossBattleAttackLevel + 1);
		if (nextPointShopAtkTableData == null)
			return;

		_baseCount = 1;
		countText.text = _baseCount.ToString("N0");
		_price = nextPointShopAtkTableData.requiredCount;
		needPointText.text = string.Format("{0:N0} P", _price);

		// max를 구해야한다.
		_maxCount = 0;
		int nextLevelIndex = Level2TableIndex(SubMissionData.instance.bossBattleAttackLevel + 1);
		int totalNeeded = 0;
		for (int i = nextLevelIndex; i < TableDataManager.instance.pointShopAtkTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.pointShopAtkTable.dataArray[i].level > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPointShopAttackLevel"))
				break;

			totalNeeded += TableDataManager.instance.pointShopAtkTable.dataArray[i].requiredCount;
			if (SubMissionData.instance.bossBattlePoint < totalNeeded)
				break;

			_maxCount += 1;
		}
		if (_maxCount == 0) _maxCount = 1;

		minusButtonImage.color = (_baseCount == 1) ? Color.gray : Color.white;
		plusButtonImage.color = (_baseCount == _maxCount) ? Color.gray : Color.white;
		maxText.color = (_baseCount == _maxCount) ? Color.gray : Color.white;
	}

	int Level2TableIndex(int level)
	{
		for (int i = 0; i < TableDataManager.instance.pointShopAtkTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.pointShopAtkTable.dataArray[i].level == level)
				return i;
		}
		return -1;
	}

	public void OnClickMinusButton()
	{
		if (_baseCount > 1)
		{
			_baseCount -= 1;
			RefreshCount();
		}
	}

	public void OnClickPlusButton()
	{
		if (_baseCount < _maxCount)
		{
			_baseCount += 1;
			RefreshCount();
		}
	}

	public void OnClickMaxButton()
	{
		if (_baseCount != _maxCount)
		{
			_baseCount = _maxCount;
			RefreshCount();
		}
	}

	ObscuredInt _price;
	void RefreshCount()
	{
		countText.text = _baseCount.ToString("N0");
		_price = GetNeedPoint(_baseCount);
		needPointText.text = string.Format("{0:N0} P", _price);

		minusButtonImage.color = (_baseCount == 1) ? Color.gray : Color.white;
		plusButtonImage.color = (_baseCount == _maxCount) ? Color.gray : Color.white;
		maxText.color = (_baseCount == _maxCount) ? Color.gray : Color.white;
	}

	int GetNeedPoint(int count)
	{
		int nextLevelIndex = Level2TableIndex(SubMissionData.instance.bossBattleAttackLevel + 1);
		int maxRange = nextLevelIndex + count - 1;
		int totalNeeded = 0;
		for (int i = nextLevelIndex; i <= maxRange; ++i)
		{
			if (TableDataManager.instance.pointShopAtkTable.dataArray[i].level > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPointShopAttackLevel"))
				break;

			totalNeeded += TableDataManager.instance.pointShopAtkTable.dataArray[i].requiredCount;
		}
		return totalNeeded;
	}

	public void OnClickExchangeButton()
	{
		if (SubMissionData.instance.bossBattlePoint < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughPoint"), 2.0f);
			return;
		}

		_prevCombatValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		PlayFabApiManager.instance.RequestLevelUpPointShopAttack(SubMissionData.instance.bossBattleAttackLevel + _baseCount, _price, OnRecvResult);
	}

	float _prevCombatValue;
	void OnRecvResult()
	{
		gameObject.SetActive(false);
		PointShopTabCanvas.instance.gameObject.SetActive(false);
		PointShopTabCanvas.instance.gameObject.SetActive(true);
		if (PointShopAttackCanvas.instance != null)
			PointShopAttackCanvas.instance.levelUpImageEffectObject.SetActive(true);

		float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		if (nextValue > _prevCombatValue)
		{
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(_prevCombatValue, nextValue);
			});
		}
	}
}