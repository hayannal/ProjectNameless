using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class BuyCaptureCanvasListItem : SimpleCashCanvas
{
	public int index;
	public Text unlimitedText;
	public Text countText;
	public Text nameText;
	public Text shopCountText;
	public Text freeText;

	void OnEnable()
	{
		unlimitedText.gameObject.SetActive(index == 0);
		countText.gameObject.SetActive(index != 0);

		PetCaptureTableData petCaptureTableData = TableDataManager.instance.FindPetCaptureTableDataByIndex(index);
		if (petCaptureTableData == null)
			return;

		nameText.SetLocalizedText(UIString.instance.GetString(petCaptureTableData.nameId));

		freeText.gameObject.SetActive(index == 0);
		shopCountText.gameObject.SetActive(index != 0);
		if (index != 0)
		{
			shopCountText.text = string.Format("X {0:N0}", petCaptureTableData.count);
			RefreshPrice(petCaptureTableData.shopProductId, 0, 0.0f);
		}

		int count = 0;
		switch (index)
		{
			case 1: count = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.CaptureBetter); break;
			case 2: count = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.CaptureBest); break;
		}
		countText.text = count.ToString("N0");
	}

	public void OnClickUnlimitedButton()
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CannotBuyCaptureBasic"), 2.0f);
	}
}