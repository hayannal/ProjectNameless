using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GachaObjects : MonoBehaviour
{
	public static GachaObjects instance;
	
	public Transform effectRootTransform;

	void Awake()
	{
		instance = this;
	}
}