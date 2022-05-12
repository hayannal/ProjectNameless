using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonMenuGroup : MonoBehaviour
{
	public static CommonMenuGroup instance = null;

	public GameObject menuInfoGroundPrefab;

	void Awake()
	{
		instance = this;
	}
}
