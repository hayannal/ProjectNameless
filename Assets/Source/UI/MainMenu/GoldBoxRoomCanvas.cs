using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoldBoxRoomCanvas : RoomShowCanvasBase
{
	public static GoldBoxRoomCanvas instance;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		SetInfoCameraMode(true);
	}

	void OnDisable()
	{
		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;
		SetInfoCameraMode(false);
	}
}