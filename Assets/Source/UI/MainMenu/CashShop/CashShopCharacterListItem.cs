using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;

public class CashShopCharacterListItem : MonoBehaviour
{
	public int index = 0;

	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	ShopActorTableData _shopActorTableData;
	void OnEnable()
	{
		ShopActorTableData shopActorTableData = TableDataManager.instance.FindShopActorTableDataByIndex(index);
		if (shopActorTableData == null)
			return;

		priceText.text = shopActorTableData.price.ToString("N0");
		_shopActorTableData = shopActorTableData;
	}

	public void OnClickInfoButton()
	{
		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("GachaCharacterInfoCanvas", null);
	}

	int _count;
	public void OnClickButton()
	{
		if (PlayerData.instance.CheckConfirmDownload() == false)
			return;

		if (CurrencyData.instance.dia < _shopActorTableData.price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		// 연출 및 보상 처리. 100개씩 뽑으면 느릴 수 있으니 패킷 대기 없이 바로 시작한다.
		UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
		{
			// 연출창 시작과 동시에 패킷을 보내고
			List<ObscuredString> listActorId = CharacterManager.instance.GetRandomIdList(_shopActorTableData.count);
			_count = listActorId.Count;
			PlayFabApiManager.instance.RequestOpenCharacterBox(listActorId, _shopActorTableData.count, _shopActorTableData.price, OnRecvResult);
		});
	}

	void OnRecvResult(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = CharacterManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.CharacterGacha, _shopActorTableData.count);

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvResult(RandomBoxScreenCanvas.eBoxType.Character, listItemInstance);

		MainCanvas.instance.RefreshMenuButton();
	}
}