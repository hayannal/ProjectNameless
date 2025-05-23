﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectCaptureCanvasListItem : MonoBehaviour
{
	public int index;
	public Text unlimitedText;
	public Text countText;
	public Text nameText;
	public Text shopCountText;

	void OnEnable()
	{
		unlimitedText.gameObject.SetActive(index == 0);
		countText.gameObject.SetActive(index != 0);

		PetCaptureTableData petCaptureTableData = TableDataManager.instance.FindPetCaptureTableDataByIndex(index);
		if (petCaptureTableData == null)
			return;

		nameText.SetLocalizedText(UIString.instance.GetString(petCaptureTableData.nameId));

		if (index != 0)
			shopCountText.text = petCaptureTableData.count.ToString("N0");

		int count = 0;
		switch (index)
		{
			case 1: count = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.CaptureBetter); break;
			case 2: count = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.CaptureBest); break;
		}
		countText.text = count.ToString("N0");
		_empty = (count == 0);
		if (index == 0) _empty = false;
	}

	bool _empty = false;
	public void OnClickButton()
	{
		if (_empty)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughCapture"), 2.0f);
			return;
		}

		SelectCaptureCanvas.instance.OnClickSelectItem(index);
	}
}