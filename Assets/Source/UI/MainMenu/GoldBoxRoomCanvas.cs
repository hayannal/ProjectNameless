using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GoldBoxRoomCanvas : RoomShowCanvasBase
{
	public static GoldBoxRoomCanvas instance;

	public GameObject goldBoxRoomTargetObject;
	public Text goldBoxRoomTargetValueText;
	public GameObject sumMyObject;
	public Text sumMyValueText;

	public GameObject[] remainIconImageList;

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
		MainCanvas.instance.OnEnterCharacterMenu(true);

		for (int i = 0; i < remainIconImageList.Length; ++i)
			remainIconImageList[i].SetActive(true);

		goldBoxRoomTargetValueText.text = CurrencyData.instance.currentGoldBoxRoomReward.ToString("N0");
		sumMyValueText.text = "0";
		_needAdjustRect1 = _needAdjustRect2 = true;

		_targetValue = 0;
		_currentValue = 0;
		sumMyValueText.text = "0";
	}

	void OnDisable()
	{
		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	bool _needAdjustRect1 = false;
	bool _needAdjustRect2 = false;
	void Update()
	{
		if (_needAdjustRect1)
		{
			goldBoxRoomTargetObject.SetActive(false);
			goldBoxRoomTargetObject.SetActive(true);
			_needAdjustRect1 = false;
		}

		if (_needAdjustRect2)
		{
			sumMyObject.SetActive(false);
			sumMyObject.SetActive(true);
			_needAdjustRect2 = false;
		}

		UpdateStoleValue();
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

	public void UseKey()
	{
		for (int i = remainIconImageList.Length - 1; i >= 0; --i)
		{
			if (remainIconImageList[i].activeSelf)
			{
				remainIconImageList[i].SetActive(false);
				return;
			}
		}
	}

	public void SetStoleValue(int getGold)
	{
		_targetValue += getGold;

		int diff = (int)(_targetValue - _currentValue);
		_valueChangeSpeed = diff / valueChangeTime;
		_updateValueText = true;
	}

	const float valueChangeTime = 1.5f;
	float _valueChangeSpeed = 0.0f;
	float _currentValue;
	int _lastValue;
	int _targetValue;
	bool _updateValueText;
	void UpdateStoleValue()
	{
		if (_updateValueText == false)
			return;

		_currentValue += _valueChangeSpeed * Time.deltaTime;
		int currentValueInt = (int)_currentValue;
		if (currentValueInt >= _targetValue)
		{
			currentValueInt = _targetValue;
			sumMyValueText.text = _targetValue.ToString("N0");
			_updateValueText = false;
		}
		if (currentValueInt != _lastValue)
		{
			_lastValue = currentValueInt;
			sumMyValueText.text = _lastValue.ToString("N0");
		}
	}
}