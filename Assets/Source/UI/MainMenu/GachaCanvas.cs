using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MEC;

public class GachaCanvas : ResearchShowCanvasBase
{
	public static GachaCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject innerMenuPrefab;
	public GameObject gachaGroundObjectPrefab;
	public GameObject inputLockObject;
	public Button backKeyButton;

	public GameObject subResultRootObject;
	public DOTweenAnimation subResultTweenAnimation;
	public Image subResultIconImage;
	public Text subResultText;
	public GameObject maxObject;

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

	void Update()
	{
		UpdateSubResultTargetValue();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}


	#region SubResult
	bool _useMaxObject = false;
	int _maxValue = 0;
	public void ShowSubResult(GachaInfoCanvas.eGachaResult gachaResult, int currentValue, int targetValue)
	{
		_useMaxObject = false;
		switch (gachaResult)
		{
			case GachaInfoCanvas.eGachaResult.BrokenEnergy1:
			case GachaInfoCanvas.eGachaResult.BrokenEnergy2:
			case GachaInfoCanvas.eGachaResult.BrokenEnergy3:
				_useMaxObject = true;
				_maxValue = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBrokenEnergy");
				//subResultIconImage.sprite = ;
				break;
			case GachaInfoCanvas.eGachaResult.Junk1:
			case GachaInfoCanvas.eGachaResult.Junk2:
			case GachaInfoCanvas.eGachaResult.Junk3:
				//subResultIconImage.sprite = ;
				break;
		}
		subResultText.text = currentValue.ToString("N0");
		_currentValue = currentValue;
		_targetValue = targetValue;
		maxObject.SetActive(_useMaxObject && _currentValue >= _maxValue);
		subResultRootObject.SetActive(true);
	}

	public void OnCompleteSubResultTweenAnimation()
	{
		Timing.RunCoroutine(DelayedBackwardSubResultTweenAnimation());
	}

	IEnumerator<float> DelayedBackwardSubResultTweenAnimation()
	{
		yield return Timing.WaitForSeconds(0.3f);

		int diff = (int)(_targetValue - _currentValue);
		_valueChangeSpeed = diff / valueChangeTime;
		_updateValueText = true;

		yield return Timing.WaitForSeconds(1.2f);

		// avoid gc
		if (this == null)
			yield break;

		subResultTweenAnimation.DOPlayBackwards();

		yield return Timing.WaitForSeconds(0.3f);

		// avoid gc
		if (this == null)
			yield break;

		subResultRootObject.SetActive(false);
	}

	const float valueChangeTime = 0.8f;
	float _valueChangeSpeed = 0.0f;
	float _currentValue;
	int _lastValue;
	int _targetValue;
	bool _updateValueText;
	void UpdateSubResultTargetValue()
	{
		if (_updateValueText == false)
			return;

		_currentValue += _valueChangeSpeed * Time.deltaTime;
		int currentValueInt = (int)_currentValue;
		if (currentValueInt >= _targetValue)
		{
			currentValueInt = _targetValue;
			subResultText.text = _targetValue.ToString("N0");
			if (_useMaxObject)
				maxObject.SetActive(_targetValue >= _maxValue);
			_updateValueText = false;
		}
		if (currentValueInt != _lastValue)
		{
			_lastValue = currentValueInt;
			subResultText.text = _lastValue.ToString("N0");
		}
	}
	#endregion
}