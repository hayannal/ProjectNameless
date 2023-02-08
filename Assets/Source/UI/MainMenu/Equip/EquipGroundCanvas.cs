using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EquipGroundCanvas : MonoBehaviour
{
	public static EquipGroundCanvas instance;

	public Transform roomCameraTransform;

	#region UI
	public GameObject equipButtonObject;
	public GameObject optionViewButtonObject;
	#endregion

	Vector3 _rootOffsetPosition = new Vector3(0.0f, 0.0f, -300.0f);
	public Vector3 rootOffsetPosition { get { return _rootOffsetPosition; } }

	bool _roomCameraMode = false;
	//float _lastRendererResolutionFactor;
	//float _lastBloomResolutionFactor;
	Vector3 _lastCameraPosition;
	//Quaternion _lastCameraRotation;
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
		//CameraFovController.instance.enabled = false;
		CustomFollowCamera.instance.enabled = false;

		_lastCameraPosition = CustomFollowCamera.instance.cachedTransform.position;
		//_lastCameraRotation = CustomFollowCamera.instance.cachedTransform.rotation;

		// ground setting
		_prevEnvironmentSettingObject = StageGround.instance.DisableCurrentEnvironmentSetting();
		if (_groundTransform == null)
		{
			_groundTransform = BattleInstanceManager.instance.GetCachedObject(ContentsPrefabGroup.instance.equipGroundPrefab, _rootOffsetPosition, Quaternion.identity).transform;
			_environmentSetting = _groundTransform.GetComponentInChildren<EnvironmentSetting>();
			_defaultLightIntensity = _environmentSetting.defaultDirectionalLightIntensity;
		}
		else
		{
			_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
			_groundTransform.gameObject.SetActive(true);
		}

		CustomFollowCamera.instance.cachedTransform.position = roomCameraTransform.localPosition + _rootOffsetPosition;
		//CustomFollowCamera.instance.cachedTransform.rotation = roomCameraTransform.localRotation;

		MainCanvas.instance.OnEnterCharacterMenu(true);

		#region UI
		optionViewButtonObject.SetActive(EquipManager.instance.cachedValue > 0);
		#endregion
	}

	void OnDisable()
	{
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

		if (CustomFollowCamera.instance == null || CameraFovController.instance == null || MainCanvas.instance == null)
			return;
		if (CustomFollowCamera.instance.gameObject == null)
			return;

		_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
		_groundTransform.gameObject.SetActive(false);
		_prevEnvironmentSettingObject.SetActive(true);

		CustomFollowCamera.instance.cachedTransform.position = _lastCameraPosition;
		//CustomFollowCamera.instance.cachedTransform.rotation = _lastCameraRotation;

		//CameraFovController.instance.enabled = true;
		CustomFollowCamera.instance.enabled = true;

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	#region UI
	public void OnClickEquipButton()
	{
		if (EquipListCanvas.instance != null && EquipListCanvas.instance.gameObject.activeSelf == false)
		{
			EquipListCanvas.instance.gameObject.SetActive(true);
			EquipListCanvas.instance.RefreshInfo((int)EquipListCanvas.instance.currentEquipType);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("EquipListCanvas", () =>
		{
			EquipListCanvas.instance.RefreshInfo(0);
		});
	}

	public void OnClickViewOptionButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("EquipStatusDetailCanvas", null);
	}
	#endregion
}