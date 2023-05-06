using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class PickUpEquipSmallListItem : MonoBehaviour
{
	public Text countText;
	public Text priceText;
	public Text notStreakCountText;

	CashShopData.PickUpEquipInfo _info;
	void OnEnable()
	{
		_info = CashShopCanvas.instance.pickUpEquipListItem.GetInfo();
		countText.text = CashShopCanvas.instance.pickUpEquipListItem.countText.text;
		priceText.text = CashShopCanvas.instance.pickUpEquipListItem.priceText.text;

		// 본체꺼에서 복사해와서 그대로 사용한다.
		string topString = UIString.instance.GetString("ShopUI_PickUpEquipRemainCount", "S", Mathf.Max(1, _info.sc - CashShopData.instance.GetCurrentPickUpEquipNotStreakCount1()));
		string bottomString = UIString.instance.GetString("ShopUI_PickUpEquipRemainCount", "SS", _info.ssc - CashShopData.instance.GetCurrentPickUpEquipNotStreakCount2());
		notStreakCountText.SetLocalizedText(string.Format("{0}\n{1}", topString, bottomString));
	}

	int _count;
	public void OnClickButton()
	{
		if (CurrencyData.instance.dia < _info.price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.SetLastItem(this, _info.price);

		// 이미 연출창이 열려있는 상태일테니 인풋을 막고 패킷 전송
		WaitingNetworkCanvas.Show(true);

		List<ObscuredString> listEquipId = EquipManager.instance.GetRandomIdList(_info.count, true);
		_count = listEquipId.Count;
		PlayFabApiManager.instance.RequestOpenPickUpEquipBox(listEquipId, _info.count, _info.price, EquipManager.instance.tempPickUpNotStreakCount1, EquipManager.instance.tempPickUpNotStreakCount2, OnRecvResult);
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

		if (CashShopCanvas.instance != null)
			CashShopCanvas.instance.RefreshPickUpEquipRect();
	}
}