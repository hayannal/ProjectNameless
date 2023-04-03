using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FreePackageBox : MonoBehaviour
{
	public RewardIcon[] rewardIconList;
	public Text nameText;

	public Text itemAtkValueText;
	public GameObject priceObject;
	public GameObject completeObject;
	public GameObject blackObject;

	public RectTransform alarmRootTransform;

	FreePackageTableData _freePackageTableData;
	ShopProductTableData _shopProductTableData;
	public void RefreshInfo(FreePackageTableData freePackageTableData)
	{
		_freePackageTableData = freePackageTableData;
		_shopProductTableData = TableDataManager.instance.FindShopProductTableData(freePackageTableData.shopProductId);

		nameText.SetLocalizedText(UIString.instance.GetString(freePackageTableData.type == (int)FreePackageGroupInfo.eFreeType.Level ? "ShopUI_FreeLevelPackage" : "ShopUI_FreeStagePackage", freePackageTableData.conValue));

		// reward icon list
		for (int i = 0; i < rewardIconList.Length; ++i)
		{
			switch (i)
			{
				case 0: rewardIconList[i].RefreshReward(_shopProductTableData.rewardType1, _shopProductTableData.rewardValue1, _shopProductTableData.rewardCount1); break;
				case 1: rewardIconList[i].RefreshReward(_shopProductTableData.rewardType2, _shopProductTableData.rewardValue2, _shopProductTableData.rewardCount2); break;
				case 2: rewardIconList[i].RefreshReward(_shopProductTableData.rewardType3, _shopProductTableData.rewardValue3, _shopProductTableData.rewardCount3); break;
				case 3: rewardIconList[i].RefreshReward(_shopProductTableData.rewardType4, _shopProductTableData.rewardValue4, _shopProductTableData.rewardCount4); break;
				case 4: rewardIconList[i].RefreshReward(_shopProductTableData.rewardType5, _shopProductTableData.rewardValue5, _shopProductTableData.rewardCount5); break;
			}
			rewardIconList[i].ShowOnlyIcon(true, 1.0f);
		}

		string attackItemId = PassManager.ShopProductId2ItemId(_freePackageTableData.shopProductId);
		itemAtkValueText.text = PassManager.instance.GetItemAttackValue(attackItemId).ToString("N0");

		bool rewarded = false;
		if (_freePackageTableData.type == (int)FreePackageGroupInfo.eFreeType.Level)
		{
			if (CashShopData.instance.IsRewardedFreeLevelPackage(_freePackageTableData.conValue))
				rewarded = true;
		}
		else if (_freePackageTableData.type == (int)FreePackageGroupInfo.eFreeType.Stage)
		{
			if (CashShopData.instance.IsRewardedFreeStagePackage(_freePackageTableData.conValue))
				rewarded = true;
		}
		priceObject.SetActive(!rewarded);
		completeObject.SetActive(rewarded);
		blackObject.SetActive(rewarded);

		AlarmObject.Hide(alarmRootTransform);
		if (Getable() && blackObject.activeSelf == false)
			AlarmObject.Show(alarmRootTransform);
	}

	bool Getable()
	{
		if (_freePackageTableData.type == (int)FreePackageGroupInfo.eFreeType.Level)
		{
			if (PlayerData.instance.playerLevel >= _freePackageTableData.conValue)
				return true;
		}
		else if (_freePackageTableData.type == (int)FreePackageGroupInfo.eFreeType.Stage)
		{
			if (PlayerData.instance.highestClearStage >= _freePackageTableData.conValue)
				return true;
		}
		return false;
	}

	public void OnClickAtkInfoButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ItemAtkInfoCanvas", () =>
		{
			ItemAtkInfoCanvas.instance.RefreshInfo(PassManager.ShopProductId2ItemId(_freePackageTableData.shopProductId));
		});
	}
	
	public void OnClickButton()
	{
		if (blackObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyFreeItem"), 2.0f);
			return;
		}

		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		if (CurrencyData.instance.CheckMaxGold())
			return;

		if (_freePackageTableData.type == (int)FreePackageGroupInfo.eFreeType.Level)
		{
			if (PlayerData.instance.playerLevel < _freePackageTableData.conValue)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("LevelPassUI_NotEnoughLevel"), 2.0f);
				return;
			}
		}
		else if (_freePackageTableData.type == (int)FreePackageGroupInfo.eFreeType.Stage)
		{
			if (PlayerData.instance.highestClearStage < _freePackageTableData.conValue)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_FreeStagePackageLimit"), 2.0f);
				return;
			}
		}

		PlayFabApiManager.instance.RequestGetFreePackage(_freePackageTableData, _shopProductTableData, () =>
		{
			float prevPowerValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

			if (_freePackageTableData.type == (int)FreePackageGroupInfo.eFreeType.Level)
				CashShopData.instance.OnRecvFreeLevelPackage(_freePackageTableData.conValue);
			else if (_freePackageTableData.type == (int)FreePackageGroupInfo.eFreeType.Stage)
				CashShopData.instance.OnRecvFreeStagePackage(_freePackageTableData.conValue);

			ShopProductTableData shopProductTableData = _shopProductTableData;
			if (shopProductTableData != null)
			{
				// 1번 아이템이 공격력 영구적으로 올려주는 아이템이라 공용 로직으로 처리하지 않는다.
				//CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType1, shopProductTableData.rewardValue1, shopProductTableData.rewardCount1);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType2, shopProductTableData.rewardValue2, shopProductTableData.rewardCount2);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType3, shopProductTableData.rewardValue3, shopProductTableData.rewardCount3);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType4, shopProductTableData.rewardValue4, shopProductTableData.rewardCount4);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType5, shopProductTableData.rewardValue5, shopProductTableData.rewardCount5);
			}

			string attackItemId = PassManager.ShopProductId2ItemId(_freePackageTableData.shopProductId);
			PassManager.instance.OnRecvItemGrantResult(attackItemId);

			// hide
			gameObject.SetActive(false);
			CashShopFreePackageCanvas.instance.gameObject.SetActive(false);
			CashShopFreePackageCanvas.instance.gameObject.SetActive(true);
			CashShopTabCanvas.instance.currencySmallInfo.RefreshInfo();
			CashShopTabCanvas.instance.RefreshAlarmObject();
			MainCanvas.instance.RefreshCashShopAlarmObject();
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);

			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			if (nextValue > prevPowerValue)
			{
				UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
				{
					ChangePowerCanvas.instance.ShowInfo(prevPowerValue, nextValue);
				});
			}
		});
	}
}