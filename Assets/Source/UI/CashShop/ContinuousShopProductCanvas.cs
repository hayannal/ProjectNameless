using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

public class ContinuousShopProductCanvas : SimpleCashEventCanvas
{
	public static ContinuousShopProductCanvas instance;

	public GameObject[] allPurchasedObjectList;

	void Awake()
	{
		instance = this;
	}

	bool _started = false;
	ContinuousShopProductInfo[] _continuousShopProductInfoList;
	void Start()
	{
		_continuousShopProductInfoList = transform.GetComponentsInChildren<ContinuousShopProductInfo>(true);
		_started = true;
	}

	void OnEnable()
	{
		SetInfo();

		MainCanvas.instance.OnEnterCharacterMenu(true);

		bool allPurchased = false;
		EventTypeTableData eventTypeTableData = TableDataManager.instance.FindEventTypeTableData(cashEventId);
		if (eventTypeTableData != null)
		{
			if (CashShopData.instance.GetContinuousProductStep(cashEventId) >= eventTypeTableData.productCount)
				allPurchased = true;
		}
		for (int i = 0; i < allPurchasedObjectList.Length; ++i)
			allPurchasedObjectList[i].SetActive(allPurchased);

		if (_started == false)
			return;
		if (_continuousShopProductInfoList == null)
			return;

		RefreshActiveList();
	}

	public void RefreshActiveList()
	{
		for (int i = 0; i < _continuousShopProductInfoList.Length; ++i)
			_continuousShopProductInfoList[i].RefreshActive();
	}

	void OnDisable()
	{
		MainCanvas.instance.OnEnterCharacterMenu(false);
	}



	// 해당 캔버스를 열지 않고 복구 로직을 진행하려면 
	public static void ExternalRetryPurchase(Product product)
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_NotDoneBuyingProgress", product.metadata.localizedTitle), () =>
		{
			WaitingNetworkCanvas.Show(true);
			ContinuousShopProductInfo.ExternalRequestServerPacket(product, null, null, null);
		}, () =>
		{
		}, true);
	}
}