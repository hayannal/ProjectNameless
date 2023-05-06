using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class PickUpCharacterSmallListItem : MonoBehaviour
{
	public Text countText;
	public Text priceText;
	public Text notStreakCountText;

	CashShopData.PickUpCharacterInfo _info;
	void OnEnable()
	{
		_info = CashShopCanvas.instance.pickUpCharacterListItem.GetInfo();
		countText.text = CashShopCanvas.instance.pickUpCharacterListItem.countText.text;
		priceText.text = CashShopCanvas.instance.pickUpCharacterListItem.priceText.text;

		string gradeString = UIString.instance.GetString(string.Format("GameUI_CharGrade{0}", 2));
		notStreakCountText.SetLocalizedText(UIString.instance.GetString("ShopUI_PickUpCharRemainCount", gradeString, _info.bc - CashShopData.instance.GetCurrentPickUpCharacterNotStreakCount()));
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

		List<ObscuredString> listActorId = CharacterManager.instance.GetRandomIdList(_info.count, true);
		_count = listActorId.Count;
		PlayFabApiManager.instance.RequestOpenPickUpCharacterBox(listActorId, _info.count, _info.price, CharacterManager.instance.tempPickUpNotStreakCount, OnRecvResult);
	}

	void OnRecvResult(string itemGrantString)
	{
		WaitingNetworkCanvas.Show(false);

		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = CharacterManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.CharacterGacha, _count);

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvRetryResult(RandomBoxScreenCanvas.eBoxType.Character, listItemInstance);

		if (CashShopCanvas.instance != null)
			CashShopCanvas.instance.RefreshPickUpCharacterRect();
	}
}