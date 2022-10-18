using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangePowerCanvas : MonoBehaviour
{
	public static ChangePowerCanvas instance;

	public CanvasGroup canvasGroup;
	public GameObject textRootObject;
	public Text diffValueText;
	public Text scaleDiffValueText;

	Color _defaultFontColor;
	void Awake()
	{
		instance = this;
		_defaultFontColor = diffValueText.color;
	}

	void OnEnable()
	{
		canvasGroup.alpha = 1.0f;
	}

	void OnDisable()
	{
		textRootObject.SetActive(false);
		_currentValue = 0.0f;
	}

	public void ShowInfo(float prevValue, float nextValue)
	{
		if (gameObject != null && gameObject.activeSelf)
		{
			gameObject.SetActive(false);
			gameObject.SetActive(true);
		}

		string prevString = prevValue.ToString("N0");
		string nextString = nextValue.ToString("N0");
		prevString = prevString.Replace(",", "");
		nextString = nextString.Replace(",", "");
		int prevIntValue = int.Parse(prevString);
		int nextIntValue = int.Parse(nextString);
		int diff = (int)(nextIntValue - prevIntValue);
		_baseValue = prevIntValue;
		_targetValue = diff;
		_valueChangeSpeed = _targetValue / valueChangeTime;

		diffValueText.color = _defaultFontColor;
		diffValueText.text = prevString;
		scaleDiffValueText.text = "";
	}

	public void OnCompleteScaleAnimation()
	{
		textRootObject.SetActive(true);
		_lastValue = -1;
		_updateValueText = true;
	}


	const float valueChangeTime = 0.35f;
	float _valueChangeRemainTime;
	float _valueChangeSpeed = 0.0f;
	float _currentValue;
	int _lastValue;
	int _targetValue;
	int _baseValue;
	bool _updateValueText;
	void Update()
	{
		if (_updateValueText == false)
			return;

		_currentValue += _valueChangeSpeed * Time.deltaTime;
		int currentValueInt = (int)_currentValue;
		if (currentValueInt >= _targetValue)
		{
			currentValueInt = _targetValue;
			diffValueText.color = Color.clear;
			scaleDiffValueText.text = (_baseValue + _targetValue).ToString("N0");
			_updateValueText = false;
		}
		if (currentValueInt != _lastValue)
		{
			_lastValue = currentValueInt;
			diffValueText.text = (_baseValue + _lastValue).ToString("N0");
		}
	}
}