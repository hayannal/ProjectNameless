using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class CashShopCanvas : MonoBehaviour
{
	public static CashShopCanvas instance;

	public GameObject iapInitializeFailedRectObject;

	public PickUpCharacterListItem pickUpCharacterListItem;
	public PickUpEquipListItem pickUpEquipListItem;
	
	public GameObject diaRectObject;
	//public DiaListItem[] diaListItemList;
	//public GoldListItem[] goldListItemList;
	
	public GameObject termsGroupObject;
	public GameObject emptyTermsGroupObject;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		RefreshInfo();

		termsGroupObject.SetActive(OptionManager.instance.language == "KOR");
		emptyTermsGroupObject.SetActive(OptionManager.instance.language != "KOR");

		// 자동 복구 코드 호출은 밖에서 했을거다. 여기선 하지 않는다.
		//CheckIAPListener();
	}

	void RefreshInfo()
	{
		iapInitializeFailedRectObject.SetActive(!CodelessIAPStoreListener.initializationComplete);
		
		diaRectObject.SetActive(CodelessIAPStoreListener.initializationComplete);

		RefreshPickUpCharacterRect();
		RefreshPickUpEquipRect();
	}

	public static bool IsUsablePickUpCharacter()
	{
		// 캐릭터의 경우엔 장비랑 달리 다 뽑았는지도 판단해야한다. 이런 상황에선 굴려봤자 의미없으니 하이드 시킨다.
		CashShopData.PickUpCharacterInfo characterInfo = CashShopData.instance.GetCurrentPickUpCharacterInfo();
		bool maxReached = false;
		if (characterInfo != null)
		{
			CharacterData characterData = CharacterManager.instance.GetCharacterData(characterInfo.id);
			if (characterData != null && characterData.transcendPoint >= TableDataManager.instance.GetGlobalConstantInt("GachaActorMaxTrp"))
				maxReached = true;
		}
		return (characterInfo != null && maxReached == false);
	}

	public void RefreshPickUpCharacterRect()
	{
		bool usablePickUp = IsUsablePickUpCharacter();
		pickUpCharacterListItem.gameObject.SetActive(PlayerData.instance.downloadConfirmed && usablePickUp);
		if (pickUpCharacterListItem.gameObject.activeSelf)
			pickUpCharacterListItem.RefreshInfo(CashShopData.instance.GetCurrentPickUpCharacterInfo());
	}

	public static bool IsUsablePickUpEquip()
	{
		CashShopData.PickUpEquipInfo equipInfo = CashShopData.instance.GetCurrentPickUpEquipInfo();
		return (equipInfo != null);
	}

	public void RefreshPickUpEquipRect()
	{
		bool usablePickUp = IsUsablePickUpEquip();
		pickUpEquipListItem.gameObject.SetActive(PlayerData.instance.downloadConfirmed && usablePickUp);
		if (pickUpEquipListItem.gameObject.activeSelf)
			pickUpEquipListItem.RefreshInfo(CashShopData.instance.GetCurrentPickUpEquipInfo());
	}
}