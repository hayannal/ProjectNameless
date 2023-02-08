using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ActorStatusDefine;

public class EquipStatusDetailCanvasListItem : MonoBehaviour
{
	public Text nameText;
	public Text valueText;

	public void Initialize(eActorStatus statusType, float value)
	{
		nameText.SetLocalizedText(UIString.instance.GetString(string.Format("Op_{0}", statusType.ToString())));
		valueText.text = string.Format("{0:0.##}%", value * 100.0f);
	}
}