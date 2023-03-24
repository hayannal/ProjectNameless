using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CashShopPackageCanvas : MonoBehaviour
{
	public static CashShopPackageCanvas instance;

	public GameObject termsGroupObject;
	public GameObject emptyTermsGroupObject;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		termsGroupObject.SetActive(OptionManager.instance.language == "KOR");
		emptyTermsGroupObject.SetActive(OptionManager.instance.language != "KOR");
	}
}