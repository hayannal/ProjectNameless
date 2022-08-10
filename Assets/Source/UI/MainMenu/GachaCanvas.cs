using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaCanvas : ResearchShowCanvasBase
{
	public static GachaCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject innerMenuPrefab;
	public GameObject gachaGroundObjectPrefab;
	public GameObject inputLockObject;
	public Button backKeyButton;

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

		SetInfoCameraMode(true);

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(true);
	}

	void OnDisable()
	{
		_menuObject.SetActive(false);
		_gachaGroundObject.SetActive(false);

		SetInfoCameraMode(false);

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

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}
}