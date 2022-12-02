using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentsPrefabGroup : MonoBehaviour
{
	public static ContentsPrefabGroup instance = null;

	public GameObject petSearchGroundPrefab;

	void Awake()
	{
		instance = this;
	}
}