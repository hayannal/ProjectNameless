using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReviewEventCanvas : SimpleCashEventCanvas
{
	public static ReviewEventCanvas instance;
	
	void Awake()
	{
		instance = this;
	}
	
	void OnEnable()
	{
		SetInfo();
		MainCanvas.instance.OnEnterCharacterMenu(true);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	public void OnClickMarketButton()
	{
		if (Application.platform == RuntimePlatform.IPhonePlayer)
		{
			Application.OpenURL(PlayerData.instance.iosUrl);
		}
		else if (Application.platform == RuntimePlatform.Android)
		{
			Application.OpenURL("market://details?id=" + Application.identifier);
		}
	}
}