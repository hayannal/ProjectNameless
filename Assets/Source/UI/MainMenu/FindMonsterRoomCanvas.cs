﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FindMonsterRoomCanvas : MonoBehaviour
{
	public static FindMonsterRoomCanvas instance;

	public Transform roomCameraTransform;

	#region UI
	public GameObject sumMyObject;
	public Text sumMyValueText;
	public Text winValueText;
	public RectTransform winRectTransform;
	public DOTweenAnimation winTextTweenAnimation;
	#endregion

	Vector3 _rootOffsetPosition = new Vector3(0.0f, 0.0f, -200.0f);
	public Vector3 rootOffsetPosition { get { return _rootOffsetPosition; } }

	bool _roomCameraMode = false;
	//float _lastRendererResolutionFactor;
	//float _lastBloomResolutionFactor;
	Vector3 _lastCameraPosition;
	Quaternion _lastCameraRotation;
	Transform _groundTransform;
	EnvironmentSetting _environmentSetting;
	GameObject _prevEnvironmentSettingObject;
	float _defaultLightIntensity;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		// disable prev component
		CameraFovController.instance.enabled = false;
		CustomFollowCamera.instance.enabled = false;
		
		_lastCameraPosition = CustomFollowCamera.instance.cachedTransform.position;
		_lastCameraRotation = CustomFollowCamera.instance.cachedTransform.rotation;

		// ground setting
		_prevEnvironmentSettingObject = StageGround.instance.DisableCurrentEnvironmentSetting();
		if (_groundTransform == null)
		{
			_groundTransform = BattleInstanceManager.instance.GetCachedObject(CommonMenuGroup.instance.findMonsterRoomGroundPrefab, _rootOffsetPosition, Quaternion.identity).transform;
			_environmentSetting = _groundTransform.GetComponentInChildren<EnvironmentSetting>();
			_defaultLightIntensity = _environmentSetting.defaultDirectionalLightIntensity;
		}
		else
		{
			_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
			_groundTransform.gameObject.SetActive(true);
		}

		CustomFollowCamera.instance.cachedTransform.position = roomCameraTransform.localPosition + _rootOffsetPosition;
		CustomFollowCamera.instance.cachedTransform.rotation = roomCameraTransform.localRotation;

		MainCanvas.instance.OnEnterCharacterMenu(true);

		#region UI
		sumMyValueText.text = "0";
		_needAdjustRect2 = true;

		_targetValue = 0;
		_currentValue = 0;
		sumMyValueText.text = "0";

		int betRate = GachaInfoCanvas.instance.GetBetRate();
		winRectTransform.localScale = Vector3.one;
		winRectTransform.gameObject.SetActive(betRate > 1);
		_needAdjustRect3 = (betRate > 1);
		if (betRate > 1)
			winValueText.text = string.Format("x{0}", betRate);
		#endregion
	}

	void OnDisable()
	{
		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	bool _needAdjustRect2 = false;
	bool _needAdjustRect3 = false;
	void Update()
	{
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

		if (CustomFollowCamera.instance == null || CameraFovController.instance == null || MainCanvas.instance == null)
			return;
		if (CustomFollowCamera.instance.gameObject == null)
			return;

		_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
		_groundTransform.gameObject.SetActive(false);
		_prevEnvironmentSettingObject.SetActive(true);

		CustomFollowCamera.instance.cachedTransform.position = _lastCameraPosition;
		CustomFollowCamera.instance.cachedTransform.rotation = _lastCameraRotation;

		CameraFovController.instance.enabled = true;
		CustomFollowCamera.instance.enabled = true;

		// 닫을때는 어차피 가차창으로 돌아가는거니 해제하지 않는다.
		// 이거 호출될때 홈화면 돌아가는지 이벤트 검사하니 최대한 피해본다.
		//MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	#region UI
	public void SetStoleValue(int getGold)
	{
		_targetValue = getGold;

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

	public void PunchAnimateWinText()
	{
		winTextTweenAnimation.DORestart();
	}

	public void ScaleZeroWinText()
	{
		winRectTransform.DOScale(0.0f, 0.3f);
	}
	#endregion
}