using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StatusDetailCanvasListItem : MonoBehaviour
{
	public GameObject backgroundObject;
	public Text nameText;
	public Text valueText;
	
	public void Initialize(bool showBackground, string stringId, int value)
	{
		backgroundObject.SetActive(showBackground);
		nameText.SetLocalizedText(UIString.instance.GetString(stringId));
		valueText.text = value.ToString("N0");
	}
}