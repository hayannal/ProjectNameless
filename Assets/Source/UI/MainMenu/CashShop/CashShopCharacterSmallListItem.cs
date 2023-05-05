using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class CashShopCharacterSmallListItem : MonoBehaviour
{
	public int index = 0;

	public Text priceText;

	ShopActorTableData _shopActorTableData;
	void OnEnable()
	{
		ShopActorTableData shopActorTableData = TableDataManager.instance.FindShopActorTableDataByIndex(index);
		if (shopActorTableData == null)
			return;

		priceText.text = shopActorTableData.price.ToString("N0");
		_shopActorTableData = shopActorTableData;
	}

	int _count;
	public void OnClickButton()
	{
		if (CurrencyData.instance.dia < _shopActorTableData.price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.SetLastItem(this, _shopActorTableData.price);

		// 이미 연출창이 열려있는 상태일테니 인풋을 막고 패킷 전송
		WaitingNetworkCanvas.Show(true);

		List<ObscuredString> listActorId = CharacterManager.instance.GetRandomIdList(_shopActorTableData.count);
		_count = listActorId.Count;
		PlayFabApiManager.instance.RequestOpenCharacterBox(listActorId, _shopActorTableData.count, _shopActorTableData.price, OnRecvResult);
	}

	void OnRecvResult(string itemGrantString)
	{
		WaitingNetworkCanvas.Show(false);

		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = CharacterManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.CharacterGacha, _shopActorTableData.count);

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvRetryResult(RandomBoxScreenCanvas.eBoxType.Character, listItemInstance);
	}
}