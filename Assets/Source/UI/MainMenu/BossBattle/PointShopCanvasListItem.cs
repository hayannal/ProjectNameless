using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointShopCanvasListItem : MonoBehaviour
{
	public int typeId;
	public int index;

	public Image blurImage;
	public Image backgroundImge;
	public Sprite[] backgroundSpriteList;

	public Text minMaxText;
	public Text priceText;

	public bool lastProduct;
	public RectTransform alarmRootTransform;

	PointShopTableData _pointShopTableData;
	void OnEnable()
	{
		PointShopTableData pointShopTableData = TableDataManager.instance.FindPointShopTableData(typeId, index);
		if (pointShopTableData == null)
			return;

		_pointShopTableData = pointShopTableData;

		minMaxText.text = string.Format("{0}\n{1}", GetPriceString(pointShopTableData.min), GetPriceString(pointShopTableData.max));
		priceText.text = string.Format("{0:N0} P", _pointShopTableData.price);

		AlarmObject.Hide(alarmRootTransform);
		if (lastProduct && pointShopTableData != null && SubMissionData.instance.bossBattlePoint >= pointShopTableData.price)
			AlarmObject.Show(alarmRootTransform);
	}

	string GetPriceString(int price)
	{
		if (price >= 100000)
			return string.Format("<size=20>{0:N0}</size>", price);
		if (price >= 10000)
			return string.Format("<size=22>{0:N0}</size>", price);
		return string.Format("{0:N0}", price);
	}

	public void OnClickButton()
	{
		if (SubMissionData.instance.bossBattlePoint < _pointShopTableData.price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughPoint"), 2.0f);
			return;
		}

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("PointShopUI_BuyConfirm"), () =>
		{
			int randomAmount = Random.Range(_pointShopTableData.min, _pointShopTableData.max + 1);
			int gold = 0;
			int dia = 0;
			int energy = 0;
			switch (typeId)
			{
				case 1: gold = randomAmount; break;
				case 2: dia = randomAmount; break;
				case 3: energy = randomAmount; break;
			}
			PlayFabApiManager.instance.RequestBuyPointShopItem(typeId, index, _pointShopTableData.price, randomAmount, _pointShopTableData.key, () =>
			{
				UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
				{
					CommonRewardCanvas.instance.RefreshReward(gold, dia, energy);
					PointShopTabCanvas.instance.gameObject.SetActive(false);
					PointShopTabCanvas.instance.gameObject.SetActive(true);
				});
			});
		});
	}
}