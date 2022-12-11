using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContentsPrefabGroup : MonoBehaviour
{
	public static ContentsPrefabGroup instance = null;

	public GameObject petSearchGroundPrefab;
	public GameObject equipGroundPrefab;
	public GameObject equipInfoGroundPrefab;

	void Awake()
	{
		instance = this;
	}
}