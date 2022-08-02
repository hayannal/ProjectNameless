using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GachaCanvas : ResearchShowCanvasBase
{
	public static GachaCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject innerMenuPrefab;
	public GameObject gachaGroundObjectPrefab;
	public GameObject inputLockObject;

	void Awake()
	{
		instance = this;
	}

	GameObject _menuObject;
	GameObject _gachaGroundObject;
	void Start()
	{
		_gachaGroundObject = Instantiate<GameObject>(gachaGroundObjectPrefab, _rootOffsetPosition, Quaternion.identity);
		_menuObject = Instantiate<GameObject>(innerMenuPrefab);
	}

	void OnEnable()
	{
		if (_gachaGroundObject != null)
			_gachaGroundObject.SetActive(true);
		if (_menuObject != null)
			_menuObject.SetActive(true);

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (restore)
			return;

		SetInfoCameraMode(true);
		MainCanvas.instance.OnEnterCharacterMenu(true);
	}

	void OnDisable()
	{
		_menuObject.SetActive(false);
		_gachaGroundObject.SetActive(false);

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
		MainCanvas.instance.OnEnterCharacterMenu(false);
	}
}