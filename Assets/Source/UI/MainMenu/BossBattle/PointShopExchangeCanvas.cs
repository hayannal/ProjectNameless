using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointShopExchangeCanvas : MonoBehaviour
{
	public Text pointText;

	int _addPoint;
	void OnEnable()
	{
		pointText.text = "0";
		_addPoint = SubMissionData.instance.bossBattlePoint;
		if (_addPoint > 0)
		{
			_pointChangeRemainTime = pointChangeTime;
			_pointChangeSpeed = _addPoint / _pointChangeRemainTime;
			_currentPoint = 0.0f;
			_updatePointText = true;
		}
	}

	void Update()
	{
		UpdatePointText();
	}

	const float pointChangeTime = 0.6f;
	float _pointChangeRemainTime;
	float _pointChangeSpeed;
	float _currentPoint;
	int _lastPoint;
	bool _updatePointText;
	void UpdatePointText()
	{
		if (_updatePointText == false)
			return;

		_currentPoint += _pointChangeSpeed * Time.deltaTime;
		int currentPointInt = (int)_currentPoint;
		if (currentPointInt >= _addPoint)
		{
			currentPointInt = _addPoint;
			_updatePointText = false;
		}
		if (currentPointInt != _lastPoint)
		{
			_lastPoint = currentPointInt;
			pointText.text = string.Format("{0:N0}", _lastPoint);
		}
	}	
}