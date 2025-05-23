﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CharacterDetailCanvas : DetailShowCanvasBase
{
	public static CharacterDetailCanvas instance;

	public RectTransform backButtonRectTransform;
	public RectTransform backButtonHideRectTransform;
	public float noInputTime = 3.0f;

	Vector2 _defaultBackButtonPosition;
	void Awake()
	{
		instance = this;
		_defaultBackButtonPosition = backButtonRectTransform.anchoredPosition;
	}

	void OnEnable()
	{
		CenterOn();

		_noInputRemainTime = noInputTime;
		backButtonRectTransform.anchoredPosition = _defaultBackButtonPosition;

		StackCanvas.Push(gameObject);
	}

	void OnDisable()
	{
		StackCanvas.Pop(gameObject);
	}

	public void OnClickBackButton()
	{
		if (_buttonHideState)
		{
			_buttonHideState = false;
			return;
		}

		Hide(0.25f);
	}
	
	void Update()
	{
		UpdateNoInput();
		UpdateLerp();
	}

	float _noInputRemainTime = 0.0f;
	bool _buttonHideState = false;
	void UpdateNoInput()
	{
		if (_noInputRemainTime > 0.0f)
		{
			_noInputRemainTime -= Time.deltaTime;
			if (_noInputRemainTime <= 0.0f)
			{
				_buttonHideState = true;
				_noInputRemainTime = 0.0f;
			}
		}

		backButtonRectTransform.anchoredPosition = Vector3.Lerp(backButtonRectTransform.anchoredPosition, _buttonHideState ? backButtonHideRectTransform.anchoredPosition : _defaultBackButtonPosition, Time.deltaTime * 5.0f);
	}


	public void OnDragRect(BaseEventData baseEventData)
	{
		_buttonHideState = false;
		_noInputRemainTime = noInputTime;
		if (CharacterCanvas.instance != null && StackCanvas.IsInStack(CharacterCanvas.instance.gameObject))
			CharacterCanvas.instance.OnDragRect(baseEventData);
		else if (CharacterListCanvas.instance != null && StackCanvas.IsInStack(CharacterListCanvas.instance.gameObject))
			CharacterListCanvas.instance.OnDragRect(baseEventData);
	}

	public void OnPointerDown(BaseEventData baseEventData)
	{
		_buttonHideState = false;
		_noInputRemainTime = noInputTime;
	}
}