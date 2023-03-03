using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class FirstPurchaseCanvas : MonoBehaviour
{
	public SkillIcon skillIcon;
	public RewardIcon[] rewardIconList;

	public GameObject shopButtonObject;
	public GameObject rewardButtonObject;

	public RectTransform alarmRootTransform;

	void Start()
	{
		// 그냥 두기로 한다.
		//for (int i = 0; i < rewardIconList.Length; ++i)
		//	rewardIconList[i].ShowOnlyIcon(false, 1.0f);
	}

	SkillTableData _skillTableData;
	ShopProductTableData _shopProductTableData;
	void OnEnable()
	{
		_shopProductTableData = TableDataManager.instance.FindShopProductTableData("firstpurchase");

		// unacquiredSpellSelectedId
		string selectedSpellId = _shopProductTableData.rewardValue1;
		if (string.IsNullOrEmpty(selectedSpellId))
			return;
		SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(selectedSpellId);
		if (skillTableData == null)
			return;
		skillIcon.SetInfo(skillTableData, false);
		_skillTableData = skillTableData;

		shopButtonObject.SetActive(PlayerData.instance.vtd == 0);
		rewardButtonObject.SetActive(PlayerData.instance.vtd > 0);

		AlarmObject.Hide(alarmRootTransform);
		if (PlayerData.instance.vtd > 0)
			AlarmObject.Show(alarmRootTransform);
	}

	public void OnClickDetailButton()
	{
		// 첫번째 항목이 스펠 고정이다.
		RewardIcon.ShowDetailInfo(_shopProductTableData.rewardType1, _shopProductTableData.rewardValue1);
	}

	public void OnClickShopButton()
	{
		Timing.RunCoroutine(MoveShopProcess());
	}

	IEnumerator<float> MoveShopProcess()
	{
		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		gameObject.SetActive(false);

		while (gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		if (MainCanvas.instance == null)
			yield break;
		if (MainCanvas.instance.gameObject.activeSelf == false)
			yield break;

		DelayedLoadingCanvas.Show(false);
		MainCanvas.instance.OnClickCashShopButton();
	}

	public void OnClickRewardButton()
	{
		PlayFabApiManager.instance.RequestFirstPurchaseReward(_shopProductTableData, () =>
		{
			ShopProductTableData shopProductTableData = _shopProductTableData;

			if (shopProductTableData != null)
			{
				float prevPowerValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

				// 인앱결제는 아니지만 보상이 스펠 고정이기때문에 캐시상품 구매하는거처럼 처리한다.
				//CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType1, shopProductTableData.rewardValue1, shopProductTableData.rewardCount1);
				if (shopProductTableData.rewardType1 == "it")
					SpellManager.instance.OnRecvPurchaseItem(shopProductTableData.rewardValue1, shopProductTableData.rewardCount1);

				float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
				if (nextValue > prevPowerValue)
				{
					// 변경 완료를 알리고
					UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
					{
						ChangePowerCanvas.instance.ShowInfo(prevPowerValue, nextValue);
					});
				}

				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType2, shopProductTableData.rewardValue2, shopProductTableData.rewardCount2);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType3, shopProductTableData.rewardValue3, shopProductTableData.rewardCount3);
				//CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType4, shopProductTableData.rewardValue4, shopProductTableData.rewardCount4);
				//CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType5, shopProductTableData.rewardValue5, shopProductTableData.rewardCount5);

				ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);
			}

			gameObject.SetActive(false);
			MainCanvas.instance.RefreshCashButton();
		});
	}
}