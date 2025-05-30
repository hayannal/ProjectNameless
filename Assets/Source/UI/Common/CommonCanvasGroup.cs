﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommonCanvasGroup : MonoBehaviour
{
	public static CommonCanvasGroup instance = null;

	public GameObject fadeCanvasPrefab;
	public GameObject toastCanvasPrefab;
	public GameObject toastNumberCanvasPrefab;
	public GameObject toastZigzagCanvasPrefab;
	public GameObject fullscreenYesNoCanvasPrefab;
	public GameObject yesNoCanvasPrefab;
	public GameObject okCanvasPrefab;
	public GameObject okBigCanvasPrefab;
	public GameObject delayedLoadingCanvasPrefab;
	public GameObject waitingNetworkCanvasPrefab;
	public GameObject tooltipCanvasPrefab;
	public GameObject maintenanceCanvasPrefab;
	public GameObject alarmObjectPrefab;
	public GameObject tutorialPlusAlarmObjectPrefab;
	public Sprite[] alarmObjectSpriteList;

	void Awake()
	{
		instance = this;
	}
}
