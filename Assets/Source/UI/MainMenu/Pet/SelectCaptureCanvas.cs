using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectCaptureCanvas : MonoBehaviour
{
	public static SelectCaptureCanvas instance;

	public GameObject backButtonObject;
	public Text titleText;
	public RectTransform titleTextTransform;
	public GameObject selectRootObject;
	public GameObject buyRootObject;
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
		titleText.SetLocalizedText(UIString.instance.GetString(select ? "PetUI_SelectCapture" : "PetUI_BuyCapture"));
		titleTextTransform.anchoredPosition = new Vector2(titleTextTransform.anchoredPosition.x, select ? 230.0f : 270.0f);
		backButtonObject.SetActive(!select);

		selectRootObject.SetActive(select);
		buyRootObject.SetActive(!select);

		for (int i = 0; i < 3; ++i)
		{
			PetCaptureTableData petCaptureTableData = TableDataManager.instance.FindPetCaptureTableDataByIndex(i);
			if (petCaptureTableData == null)
				continue;

			_percentTextList[3 * 0 + i].text = string.Format("{0}%", petCaptureTableData.starProb_3 * 100.0f);
			_percentTextList[3 * 1 + i].text = string.Format("{0}%", petCaptureTableData.starProb_4 * 100.0f);
			_percentTextList[3 * 2 + i].text = string.Format("{0}%", petCaptureTableData.starProb_5 * 100.0f);
		}
	}

	public void OnClickSelectItem(int index)
	{
		gameObject.SetActive(false);
		PetSearchGround.instance.ApplyCapture(index);
	}
}