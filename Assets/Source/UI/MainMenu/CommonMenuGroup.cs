using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonMenuGroup : MonoBehaviour
{
	public static CommonMenuGroup instance = null;

	public GameObject menuInfoGroundPrefab;
	public GameObject goldBoxRoomGroundPrefab;
	public GameObject findMonsterRoomGroundPrefab;
	public GameObject petInfoGroundPrefab;

	void Awake()
	{
		instance = this;
	}
}
