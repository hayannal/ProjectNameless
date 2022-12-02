using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectCaptureCanvas : MonoBehaviour
{
	public static SelectCaptureCanvas instance;

	public Text titleText;
	public Transform percentTextRootTranform;

	Text[] _percentTextList;
	void Awake()
	{
		instance = this;

		_percentTextList = percentTextRootTranform.GetComponentsInChildren<Text>();
	}

	public void RefreshInfo(bool select)
	{
		// 선택창이냐 구매창이냐에 따라서 조금 다르게 처리할거 같다.
		titleText.SetLocalizedText(UIString.instance.GetString(select ? "PetUI_SelectCaptureTitle" : "PetUI_BuyCaptureTitle"));

		for (int i = 0; i < 3; ++i)
		{
			for (int j = 0; j < 3; ++j)
			{
				_percentTextList[i * 3 + j].text = "20%";
			}
		}
	}

	public void OnClickSelectItem(int index)
	{
		gameObject.SetActive(false);
		PetSearchGround.instance.ApplyCapture(index);
	}
}