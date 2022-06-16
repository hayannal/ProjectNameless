using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomShowCanvasBase : MonoBehaviour
{
	public Transform roomCameraTransform;
	public float roomCameraFov = 43.0f;

	#region Room Camera
	protected Vector3 _rootOffsetPosition = new Vector3(0.0f, 0.0f, -75.0f);
	public Vector3 rootOffsetPosition { get { return _rootOffsetPosition; } }
	bool _roomCameraMode = false;
	//float _lastRendererResolutionFactor;
	//float _lastBloomResolutionFactor;
	float _lastFov;
	Color _lastBackgroundColor;
	Vector3 _lastCameraPosition;
	Quaternion _lastCameraRotation;
	Vector3 _lastCharacterPosition;
	Quaternion _lastCharacterRotation;
	Transform _groundTransform;
	EnvironmentSetting _environmentSetting;
	GameObject _prevEnvironmentSettingObject;
	float _defaultLightIntensity;
	protected void SetInfoCameraMode(bool enable)
	{
		if (_roomCameraMode == enable)
			return;

		if (enable)
		{
			// disable prev component
			CameraFovController.instance.enabled = false;
			CustomFollowCamera.instance.enabled = false;

			// save prev info
			//_lastRendererResolutionFactor = CustomRenderer.instance.RenderTextureResolutionFactor;
			//_lastBloomResolutionFactor = CustomRenderer.instance.bloom.RenderTextureResolutoinFactor;
			_lastFov = UIInstanceManager.instance.GetCachedCameraMain().fieldOfView;
			_lastBackgroundColor = UIInstanceManager.instance.GetCachedCameraMain().backgroundColor;
			_lastCameraPosition = CustomFollowCamera.instance.cachedTransform.position;
			_lastCameraRotation = CustomFollowCamera.instance.cachedTransform.rotation;

			// ground setting
			_prevEnvironmentSettingObject = StageGround.instance.DisableCurrentEnvironmentSetting();
			if (_groundTransform == null)
			{
				_groundTransform = BattleInstanceManager.instance.GetCachedObject(CommonMenuGroup.instance.goldBoxRoomGroundPrefab, _rootOffsetPosition, Quaternion.identity).transform;
				_environmentSetting = _groundTransform.GetComponentInChildren<EnvironmentSetting>();
				_defaultLightIntensity = _environmentSetting.defaultDirectionalLightIntensity;
			}
			else
			{
				_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
				_groundTransform.gameObject.SetActive(true);
			}

			// setting
			CustomRenderer.instance.RenderTextureResolutionFactor = (CustomRenderer.instance.RenderTextureResolutionFactor + 1.0f) * 0.5f;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = 0.7f;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = roomCameraFov;
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = new Color(0.183f, 0.19f, 0.208f);
			CustomFollowCamera.instance.cachedTransform.position = roomCameraTransform.localPosition + _rootOffsetPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = roomCameraTransform.localRotation;
		}
		else
		{
			if (CustomFollowCamera.instance == null || CameraFovController.instance == null || MainCanvas.instance == null)
				return;
			if (CustomFollowCamera.instance.gameObject == null)
				return;
			if (StageManager.instance == null)
				return;
			if (BattleInstanceManager.instance.playerActor.gameObject == null)
				return;

			_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
			_groundTransform.gameObject.SetActive(false);
			_prevEnvironmentSettingObject.SetActive(true);

			//CustomRenderer.instance.RenderTextureResolutionFactor = _lastRendererResolutionFactor;
			//CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = _lastBloomResolutionFactor;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = _lastFov;
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = _lastBackgroundColor;
			CustomFollowCamera.instance.cachedTransform.position = _lastCameraPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = _lastCameraRotation;

			CameraFovController.instance.enabled = true;
			CustomFollowCamera.instance.enabled = true;
		}
		_roomCameraMode = enable;
	}
	#endregion
}