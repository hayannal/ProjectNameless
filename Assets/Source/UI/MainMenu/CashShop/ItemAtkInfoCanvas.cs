using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemAtkInfoCanvas : MonoBehaviour
{
	public static ItemAtkInfoCanvas instance;

	public Text itemAttackText;

	void Awake()
	{
		instance = this;
	}

	public void RefreshInfo(string key)
	{
		itemAttackText.text = PassManager.instance.GetItemAttackValue(key).ToString("N0");
	}
}