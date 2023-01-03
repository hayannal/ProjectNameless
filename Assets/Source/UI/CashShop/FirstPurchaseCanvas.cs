using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class FirstPurchaseCanvas : MonoBehaviour
{
	public GameObject shopButtonObject;
	public GameObject rewardButtonObject;

	ShopProductTableData _shopProductTableData;
	void OnEnable()
	{
		shopButtonObject.SetActive(PlayerData.instance.vtd == 0);
		rewardButtonObject.SetActive(PlayerData.instance.vtd > 0);

		_shopProductTableData = TableDataManager.instance.FindShopProductTableData("firstpurchase");
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
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType1, shopProductTableData.rewardValue1, shopProductTableData.rewardCount1);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType2, shopProductTableData.rewardValue2, shopProductTableData.rewardCount2);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType3, shopProductTableData.rewardValue3, shopProductTableData.rewardCount3);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType4, shopProductTableData.rewardValue4, shopProductTableData.rewardCount4);
				CurrencyData.instance.OnRecvProductReward(shopProductTableData.rewardType5, shopProductTableData.rewardValue5, shopProductTableData.rewardCount5);

				UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
				{
					CommonRewardCanvas.instance.RefreshReward(shopProductTableData);
				});
			}

			gameObject.SetActive(false);
			MainCanvas.instance.RefreshCashButton();
		});
	}
}