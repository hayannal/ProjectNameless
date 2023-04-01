using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CashShopFreePackageCanvas : MonoBehaviour
{
	public static CashShopFreePackageCanvas instance;

	void Awake()
	{
		instance = this;
	}
}