using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResearchCanvas : ResearchShowCanvasBase
{
	public static ResearchCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject innerMenuPrefab;
	public GameObject researchGroundObjectPrefab;
	public GameObject inputLockObject;

	void Awake()
	{
		instance = this;
	}

	GameObject _menuObject;
	GameObject _researchGroundObject;
	void Start()
	{
		_researchGroundObject = Instantiate<GameObject>(researchGroundObjectPrefab, _rootOffsetPosition, Quaternion.identity);
		_menuObject = Instantiate<GameObject>(innerMenuPrefab);
	}

	void OnEnable()
	{
		if (_menuObject != null)
			_menuObject.SetActive(true);

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		
		if (restore)
			return;

		SetInfoCameraMode(true);
		MainCanvas.instance.OnEnterCharacterMenu(true);

		if (_researchGroundObject != null)
			_researchGroundObject.SetActive(true);
	}

	void OnDisable()
	{
		_menuObject.SetActive(false);

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

		if (_researchGroundObject != null)
			_researchGroundObject.SetActive(false);

		SetInfoCameraMode(false);
		MainCanvas.instance.OnEnterCharacterMenu(false);
	}
}