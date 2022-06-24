using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventInputLockCanvas : MonoBehaviour
{
	public static EventInputLockCanvas instance;

	void Awake()
	{
		instance = this;
	}

	public void OnClickBackgroundButton()
	{
		/*
		EventManager.instance.OnClickScreen();
		*/
	}
}