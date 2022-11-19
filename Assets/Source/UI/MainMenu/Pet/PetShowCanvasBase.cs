using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class PetShowCanvasBase : MonoBehaviour
{
	public Transform infoCameraTransform;
	public float infoCameraFov = 43.0f;
	public float charactorY = 180.0f;

	protected PetActor _petActor;

	#region Info Camera
	protected Vector3 _rootOffsetPosition = new Vector3(0.0f, 0.0f, 75.0f);
	public Vector3 rootOffsetPosition { get { return _rootOffsetPosition; } }
	bool _infoCameraMode = false;
	float _lastRendererResolutionFactor;
	float _lastBloomResolutionFactor;
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
	protected void SetInfoCameraMode(bool enable, string actorId)
	{
		if (_infoCameraMode == enable)
			return;

		if (enable)
		{
			// disable prev component
			CameraFovController.instance.enabled = false;
			CustomFollowCamera.instance.enabled = false;

			// save prev info
			_lastRendererResolutionFactor = CustomRenderer.instance.RenderTextureResolutionFactor;
			_lastBloomResolutionFactor = CustomRenderer.instance.bloom.RenderTextureResolutoinFactor;
			_lastFov = UIInstanceManager.instance.GetCachedCameraMain().fieldOfView;
			_lastBackgroundColor = UIInstanceManager.instance.GetCachedCameraMain().backgroundColor;
			_lastCameraPosition = CustomFollowCamera.instance.cachedTransform.position;
			_lastCameraRotation = CustomFollowCamera.instance.cachedTransform.rotation;

			// ground setting
			_prevEnvironmentSettingObject = StageGround.instance.DisableCurrentEnvironmentSetting();
			if (_groundTransform == null)
			{
				_groundTransform = BattleInstanceManager.instance.GetCachedObject(CommonMenuGroup.instance.menuInfoGroundPrefab, _rootOffsetPosition, Quaternion.identity).transform;
				_environmentSetting = _groundTransform.GetComponentInChildren<EnvironmentSetting>();
				_defaultLightIntensity = _environmentSetting.defaultDirectionalLightIntensity;
			}
			else
			{
				_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
				_groundTransform.gameObject.SetActive(true);
			}

			//if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
			//	TimeSpaceGround.instance.EnableObjectDeformer(false);

			// player setting
			if (_petActor != null)
				OnLoadedPlayerActor();

			// setting
			CustomRenderer.instance.RenderTextureResolutionFactor = (CustomRenderer.instance.RenderTextureResolutionFactor + 1.0f) * 0.5f;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = 0.8f;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = infoCameraFov;
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = new Color(0.183f, 0.19f, 0.208f);
			CustomFollowCamera.instance.cachedTransform.position = infoCameraTransform.localPosition + _rootOffsetPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = infoCameraTransform.localRotation;

			Thinksquirrel.CShake.CameraShake.CancelAllShakes();
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

			CustomRenderer.instance.RenderTextureResolutionFactor = _lastRendererResolutionFactor;
			CustomRenderer.instance.bloom.RenderTextureResolutoinFactor = _lastBloomResolutionFactor;
			UIInstanceManager.instance.GetCachedCameraMain().fieldOfView = _lastFov;
			UIInstanceManager.instance.GetCachedCameraMain().backgroundColor = _lastBackgroundColor;
			CustomFollowCamera.instance.cachedTransform.position = _lastCameraPosition;
			CustomFollowCamera.instance.cachedTransform.rotation = _lastCameraRotation;

			//if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
			//	TimeSpaceGround.instance.EnableObjectDeformer(true);

			CameraFovController.instance.enabled = true;
			CustomFollowCamera.instance.enabled = true;
			//MainCanvas.instance.OnEnterMainMenu(false);
		}
		_infoCameraMode = enable;
	}

	// 해당 Canvas보다 늦게 로딩될걸 대비해서 캐릭터 OnLoaded함수를 만들어놓는다.
	protected void OnLoadedPlayerActor(bool refreshActorInfoTable = false)
	{
		if (refreshActorInfoTable)
		{
			// 호출순서상 SetInfoCameraMode(true 호출하기 전에 이 콜백이 호출되는 경우가 있어서 예외처리 적용. null일땐 처리하지 않는다.
			if (_environmentSetting != null)
				_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
		}

		_petActor.cachedTransform.position = _rootOffsetPosition;
		float yaw = charactorY;
		_petActor.cachedTransform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
		_petActor.animator.Play("Idle");
	}
	#endregion

	#region Drag
	public void OnDragRect(BaseEventData baseEventData)
	{
		PointerEventData pointerEventData = baseEventData as PointerEventData;
		if (pointerEventData == null)
			return;
		if (_petActor == null)
			return;

		float ratio = -pointerEventData.delta.x * 2.54f;
		ratio /= Screen.dpi;
		ratio *= 80.0f; // rotate speed
		_petActor.cachedTransform.Rotate(0.0f, ratio, 0.0f, Space.Self);
	}
	#endregion

	#region Canvas Character
	// base안에 공용함수로 넣어둔다.
	bool _wait = false;
	string _id = "";
	Action _completeCallback;
	public void ShowCanvasPetActor(string petId, Action completeCallback)
	{
		if (_wait)
			return;

		// back이나 홈키 누르면서 동시에 누르면 이상하게 열리는듯 한데 우선 이렇게라도 체크해본다.
		if (gameObject.activeSelf == false)
			return;

		_id = petId;

		// 캐릭터 교체는 이 캔버스 담당이다.
		// 액터가 혹시나 미리 만들어져있다면 등록되어있을거니 가져다쓴다.
		PetActor petActor = BattleInstanceManager.instance.GetCachedCanvasPetActor(petId);
		if (petActor != null)
		{
			if (petActor != _petActor)
			{
				// 현재 캐릭터 하이드 시키고
				if (_petActor != null)
					_petActor.gameObject.SetActive(false);
				_petActor = petActor;
				_petActor.gameObject.SetActive(true);
				OnLoadedPlayerActor(true);
			}
			if (completeCallback != null) completeCallback.Invoke();
		}
		else
		{
			// 없다면 로딩 걸어두고 SetInfoCameraMode를 호출해둔다.
			// SetInfoCameraMode 안에는 이미 캐릭터가 없을때를 대비해서 코드가 짜여져있긴 하다.
			_wait = true;
			_completeCallback = completeCallback;
			AddressableAssetLoadManager.GetAddressableGameObject(PetData.GetAddressByPetId(petId), "", OnLoadedPetActor);
		}
	}
	
	void OnLoadedPetActor(GameObject prefab)
	{
		// 플레이어 캐릭터가 아닌 다른 캐릭터를 선택 후 Ok 누른다음에 로딩이 완료되기 전에 창을 나가버리면
		// 새 캐릭터를 만들 이유도 없고 인포창으로 넘어가서도 안된다.
		if (this == null) return;
		if (gameObject == null) return;
		if (gameObject.activeSelf == false)
		{
			if (PetListCanvas.instance == null || PetListCanvas.instance.gameObject.activeSelf == false)
				return;
		}

#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(prefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(prefab);
#endif

		_wait = false;
		PetActor petActor = newObject.GetComponent<PetActor>();
		if (petActor == null)
			return;

		BattleInstanceManager.instance.AddCanvasPetActor(petActor, _id);

		if (petActor != _petActor)
		{
			if (_petActor != null)
				_petActor.gameObject.SetActive(false);
			_petActor = petActor;
		}
		OnLoadedPlayerActor(true);

		//yield return Timing.WaitForOneFrame;
		if (_completeCallback != null) _completeCallback.Invoke();

		//Timing.RunCoroutine(DelayedShowCharacterInfoCanvas());
	}

	//IEnumerator<float> DelayedShowCharacterInfoCanvas()
	//{
	//	yield return Timing.WaitForOneFrame;
	//	ShowCharacterInfoCanvas();
	//}
	#endregion
}