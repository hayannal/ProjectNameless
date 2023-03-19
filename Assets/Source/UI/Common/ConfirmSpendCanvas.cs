using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmSpendCanvas : MonoBehaviour
{
	public static ConfirmSpendCanvas instance = null;

	public Text titleText;
	public Text messageText;

	public Text priceText;
	public GameObject[] priceTypeObjectList;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect[] priceGrayscaleEffect;

	System.Action _okAction;

	void Awake()
	{
		instance = this;
	}

	public void ShowCanvas(bool show, string title, string message, CurrencyData.eCurrencyType currencyType, int spendCount, bool showCurrencySmallInfoCanvas, System.Action okAction = null)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		titleText.SetLocalizedText(title);
		messageText.SetLocalizedText(message);

		priceText.text = spendCount.ToString("N0");
		bool disablePrice = false;
		if (currencyType == CurrencyData.eCurrencyType.Gold)
			disablePrice = (CurrencyData.instance.gold < spendCount);
		else
			disablePrice = (CurrencyData.instance.dia < spendCount);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		for (int i = 0; i < priceTypeObjectList.Length; ++i)
		{
			priceTypeObjectList[i].SetActive((int)currencyType == i);
			if ((int)currencyType == i)
				priceGrayscaleEffect[i].enabled = disablePrice;
		}
		_okAction = okAction;

		_currencyType = currencyType;
		_disablePrice = disablePrice;
	}

	CurrencyData.eCurrencyType _currencyType;
	bool _disablePrice;
	public void OnClickOkButton()
	{
		if (_disablePrice)
		{
			if (_currencyType == CurrencyData.eCurrencyType.Gold)
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			else
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);

			return;
		}

		//gameObject.SetActive(false);
		if (_okAction != null)
			_okAction();
	}
}