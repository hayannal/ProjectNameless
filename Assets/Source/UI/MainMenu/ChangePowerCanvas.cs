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
	public Text changeValueText;
	public Image upImage;
	public RectTransform upImageRectTransform;

	void Awake()
	{
		instance = this;
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

		diffValueText.text = prevString;
		changeValueText.text = diff.ToString("N0");

		_checkUnder = (diff < 0);
		upImage.color = _checkUnder ? new Color(1.0f, 0.3f, 0.38f, 0.86f) : new Color(0.3f, 1.0f, 0.38f, 0.86f);
		changeValueText.color = _checkUnder ? new Color(1.0f, 0.3f, 0.38f, 0.86f) : new Color(0.3f, 1.0f, 0.38f, 0.86f);
		upImageRectTransform.eulerAngles = _checkUnder ? new Vector3(0.0f, 0.0f, 270.0f) : new Vector3(0.0f, 0.0f, 90.0f);
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
	bool _checkUnder = false;
	void Update()
	{
		if (_updateValueText == false)
			return;

		_currentValue += _valueChangeSpeed * Time.deltaTime;
		int currentValueInt = (int)_currentValue;
		if (_checkUnder == false && currentValueInt >= _targetValue)
		{
			currentValueInt = _targetValue;
			diffValueText.text = (_baseValue + _targetValue).ToString("N0");
			_updateValueText = false;
		}
		if (_checkUnder && currentValueInt <= _targetValue)
		{
			currentValueInt = _targetValue;
			diffValueText.text = (_baseValue + _targetValue).ToString("N0");
			_updateValueText = false;
		}
		if (currentValueInt != _lastValue)
		{
			_lastValue = currentValueInt;
			diffValueText.text = (_baseValue + _lastValue).ToString("N0");
		}
	}
}