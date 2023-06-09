using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class CashShopEquipSmallListItem : MonoBehaviour
{
	public int index = 0;

	public Text countText;
	public Text priceText;

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

	int _count;
	public void OnClickButton()
	{
		if (EquipManager.instance.IsInventoryVisualMax())
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
			return;
		}

		if (CurrencyData.instance.dia < _shopEquipTableData.price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.SetLastItem(this, _shopEquipTableData.price);

		// 이미 연출창이 열려있는 상태일테니 인풋을 막고 패킷 전송
		WaitingNetworkCanvas.Show(true);

		List<ObscuredString> listEquipId = EquipManager.instance.GetRandomIdList(_shopEquipTableData.count);
		_count = listEquipId.Count;
		PlayFabApiManager.instance.RequestOpenEquipBox(listEquipId, _shopEquipTableData.count, _shopEquipTableData.price, OnRecvResult);
	}

	void OnRecvResult(string itemGrantString)
	{
		WaitingNetworkCanvas.Show(false);

		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = EquipManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.EquipGacha, _count);

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvRetryResult(RandomBoxScreenCanvas.eBoxType.Equip, listItemInstance);
	}
}