using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SupportReadCanvas : MonoBehaviour
{
	public static SupportReadCanvas instance;

	public Text bodyText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
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

	public void RefreshText(SupportData.MySupportData data)
	{
		switch (data.type)
		{
			case 0:
				bodyText.supportRichText = false;
				break;
			case 1:
				bodyText.supportRichText = true;
				break;
		}

		if (string.IsNullOrEmpty(data.sid))
		{
			bodyText.SetLocalizedText(data.body);
		}
		else
		{
			bodyText.SetLocalizedText(UIString.instance.GetString(data.sid));
		}
	}
}