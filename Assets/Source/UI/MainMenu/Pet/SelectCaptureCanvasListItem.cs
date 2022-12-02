using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectCaptureCanvasListItem : MonoBehaviour
{
	public int index;
	public Text countText;
	public Text nameText;

	void OnEnable()
	{
		if (index == 0)
			countText.text = "X 999";
	}

	public void OnClickButton()
	{
		SelectCaptureCanvas.instance.OnClickSelectItem(index);
	}
}