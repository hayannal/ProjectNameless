using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class CashShopEquipListItem : MonoBehaviour
{
	public int index = 0;

	public Text countText;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	ShopEquipTableData _shopEquipTableData;
	void OnEnable()
	{
		ShopEquipTableData shopEquipTableData = TableDataManager.instance.FindShopEquipTableDataByIndex(index);
		if (shopEquipTableData == null)
			return;

		countText.text = string.Format("X {0:N0}", shopEquipTableData.count);
		priceText.text = shopEquipTableData.price.ToString("N0");
		_shopEquipTableData = shopEquipTableData;
	}

	public void OnClickInfoButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("GachaEquipInfoCanvas", null);
	}

	int _count;
	public void OnClickButton()
	{
		if (CurrencyData.instance.gold < _shopEquipTableData.price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		// 연출 및 보상 처리. 100개씩 뽑으면 느릴 수 있으니 패킷 대기 없이 바로 시작한다.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// 연출창 시작과 동시에 패킷을 보내고
			List<ObscuredString> listEquipId = EquipManager.instance.GetRandomIdList(_shopEquipTableData.count);
			_count = listEquipId.Count;
			PlayFabApiManager.instance.RequestOpenEquipBox(listEquipId, _shopEquipTableData.count, _shopEquipTableData.price, OnRecvResult);
		});
	}

	void OnRecvResult(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = EquipManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvResult(RandomBoxScreenCanvas.eBoxType.Equip, listItemInstance);

		MainCanvas.instance.RefreshMenuButton();
	}
}