using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnalysisBoostCanvas : MonoBehaviour
{
	public static AnalysisBoostCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	
	void Awake()
	{
		instance = this;
	}
	
	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		// refresh
		//RefreshGrid();
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


	}
}