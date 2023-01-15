using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GoldBoxRoomCanvas : RoomShowCanvasBase
{
	public static GoldBoxRoomCanvas instance;

	public GameObject goldBoxRoomTargetObject;
	public Text goldBoxRoomTargetValueText;
	public GameObject sumMyObject;
	public Text sumMyValueText;
	public Text winValueText;
	public RectTransform winRectTransform;
	public DOTweenAnimation winTextTweenAnimation;

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

		int betRate = GachaInfoCanvas.instance.GetBetRate();
		winRectTransform.gameObject.SetActive(betRate > 1);
		_needAdjustRect3 = (betRate > 1);
		if (betRate > 1)
			winValueText.text = string.Format("x{0}", betRate);
	}

	void OnDisable()
	{
		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	bool _needAdjustRect1 = false;
	bool _needAdjustRect2 = false;
	bool _needAdjustRect3 = false;
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

		if (_needAdjustRect3)
		{
			winRectTransform.gameObject.SetActive(false);
			winRectTransform.gameObject.SetActive(true);
			_needAdjustRect3 = false;
		}

		UpdateStoleValue();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;
		winRectTransform.gameObject.SetActive(false);
		winRectTransform.localScale = Vector3.one;
		SetInfoCameraMode(false);

		// 닫을때는 어차피 가차창으로 돌아가는거니 해제하지 않는다.
		// 이거 호출될때 홈화면 돌아가는지 이벤트 검사하니 최대한 피해본다.
		//MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	public void UseKey()
	{
		for (int i = 0; i < remainIconImageList.Length; ++i)
		{
			if (remainIconImageList[i].activeSelf)
			{
				remainIconImageList[i].SetActive(false);
				return;
			}
		}
	}

	public void SetStoleValue(int setGold, bool fast = false)
	{
		_targetValue = setGold;

		int diff = (int)(_targetValue - _currentValue);
		_valueChangeSpeed = diff / (valueChangeTime * (fast ? 0.7f : 1.0f));
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

	public void PunchAnimateWinText()
	{
		winTextTweenAnimation.DORestart();
	}

	public void ScaleZeroWinText()
	{
		winRectTransform.DOScale(0.0f, 0.3f);
	}
}