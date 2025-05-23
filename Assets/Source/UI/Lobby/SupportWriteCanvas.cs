﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SupportWriteCanvas : MonoBehaviour
{
	public static SupportWriteCanvas instance;

	public InputField bodyInputField;
	public Text bodyText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		RefreshText();

		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		StackCanvas.Pop(gameObject);
	}

	public void OnClickBackButton()
	{
		gameObject.SetActive(false);
	}

	void RefreshText()
	{
		// 폰트 적용을 위해 호출해야한다.
		bodyText.font = UIString.instance.GetLocalizedFont();
		bodyInputField.text = "";
	}
	


	public void OnClickSendButton()
	{
		if (bodyInputField.text == "")
			return;

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_SupportSendConfirm"), () =>
		{
			PlayFabApiManager.instance.ReqeustWriteInquiry(bodyInputField.text, () =>
			{
				gameObject.SetActive(false);
				UIInstanceManager.instance.ShowCanvasAsync("SupportListCanvas", null);
			});
		});
	}
}