using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class CharacterShowCanvasBase : MonoBehaviour
{
	public Transform infoCameraTransform;
	public float infoCameraFov = 43.0f;
	public float charactorY = 180.0f;

	protected PlayerActor _playerActor;

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
	ActorInfoTableData _cachedActorInfoTableData;
	protected void SetInfoCameraMode(bool enable, string actorId)
	{
		if (_infoCameraMode == enable)
			return;

		if (enable)
		{
			/*
			if (MainSceneBuilder.instance.lobby)
				LobbyCanvas.instance.OnEnterMainMenu(true);
			else
			{
				// lobby가 아닐때란건 아마 전투 후 열리는 영입창이란 얘기다. 불필요한 캔버스들을 다 가려둔다.
				LobbyCanvas.instance.gameObject.SetActive(false);
				SkillSlotCanvas.instance.gameObject.SetActive(false);
				if (BattleResultCanvas.instance != null)
					BattleResultCanvas.instance.gameObject.SetActive(false);
			}
			*/

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
			/*
			if (BattleInstanceManager.instance.playerActor == _playerActor)
			{
				_lastCharacterPosition = _playerActor.cachedTransform.position;
				_lastCharacterRotation = _playerActor.cachedTransform.rotation;
			}
			*/

			// table override
			_cachedActorInfoTableData = TableDataManager.instance.FindActorInfoTableData(actorId);

			// ground setting
			_prevEnvironmentSettingObject = StageGround.instance.DisableCurrentEnvironmentSetting();
			if (_groundTransform == null)
			{
				_groundTransform = BattleInstanceManager.instance.GetCachedObject(CommonMenuGroup.instance.menuInfoGroundPrefab, _rootOffsetPosition, Quaternion.identity).transform;
				_environmentSetting = _groundTransform.GetComponentInChildren<EnvironmentSetting>();
				_defaultLightIntensity = _environmentSetting.defaultDirectionalLightIntensity;

				// override setting
				if (_cachedActorInfoTableData != null && _cachedActorInfoTableData.infoLightIntensity > 0.0f)
					_environmentSetting.SetDefaultLightIntensity(_cachedActorInfoTableData.infoLightIntensity);
			}
			else
			{
				// override setting
				if (_cachedActorInfoTableData != null && _cachedActorInfoTableData.infoLightIntensity > 0.0f)
					_environmentSetting.SetDefaultLightIntensity(_cachedActorInfoTableData.infoLightIntensity);
				else
					_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);

				_groundTransform.gameObject.SetActive(true);
			}

			//if (TimeSpaceGround.instance != null && TimeSpaceGround.instance.gameObject.activeSelf)
			//	TimeSpaceGround.instance.EnableObjectDeformer(false);

			// player setting
			if (_playerActor != null)
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
			/*
			if (BattleInstanceManager.instance.playerActor == _playerActor)
			{
				BattleInstanceManager.instance.playerActor.cachedTransform.position = _lastCharacterPosition;
				BattleInstanceManager.instance.playerActor.cachedTransform.rotation = _lastCharacterRotation;
				TailAnimatorUpdater.UpdateAnimator(BattleInstanceManager.instance.playerActor.cachedTransform, 15);
			}
			*/

			if (_cachedActorInfoTableData != null)
			{
				if (_cachedActorInfoTableData.useInfoIdle)
					_playerActor.actionController.animator.Play("Idle");
				_cachedActorInfoTableData = null;
			}

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
			_cachedActorInfoTableData = TableDataManager.instance.FindActorInfoTableData(_playerActor.actorId);

			// 호출순서상 SetInfoCameraMode(true 호출하기 전에 이 콜백이 호출되는 경우가 있어서 예외처리 적용. null일땐 처리하지 않는다.
			// override setting
			if (_environmentSetting != null)
			{
				if (_cachedActorInfoTableData != null && _cachedActorInfoTableData.infoLightIntensity > 0.0f)
					_environmentSetting.SetDefaultLightIntensity(_cachedActorInfoTableData.infoLightIntensity);
				else
					_environmentSetting.SetDefaultLightIntensity(_defaultLightIntensity);
			}
		}

		_playerActor.cachedTransform.position = _rootOffsetPosition;
		float yaw = charactorY;
		/*
		if (CharacterListCanvas.instance != null && CharacterListCanvas.instance.gameObject.activeSelf && _cachedActorInfoTableData != null && _cachedActorInfoTableData.infoRotate != 0.0f)
			yaw = _cachedActorInfoTableData.infoRotate;
		*/
		_playerActor.cachedTransform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
		TailAnimatorUpdater.UpdateAnimator(_playerActor.cachedTransform, 15);
		bool applyInfoIdle = (_cachedActorInfoTableData != null && _cachedActorInfoTableData.useInfoIdle);
		_playerActor.actionController.animator.Play(applyInfoIdle ? "InfoIdle" : "Idle");
	}
	#endregion

	#region Drag
	public void OnDragRect(BaseEventData baseEventData)
	{
		PointerEventData pointerEventData = baseEventData as PointerEventData;
		if (pointerEventData == null)
			return;
		if (_playerActor == null)
			return;

		float ratio = -pointerEventData.delta.x * 2.54f;
		ratio /= Screen.dpi;
		ratio *= 80.0f; // rotate speed
		_playerActor.cachedTransform.Rotate(0.0f, ratio, 0.0f, Space.Self);
	}
	#endregion

	#region Canvas Character
	// base안에 공용함수로 넣어둔다.
	bool _wait = false;
	public void ShowCanvasPlayerActor(string actorId, Action completeCallback)
	{
		if (_wait)
			return;

		// back이나 홈키 누르면서 동시에 누르면 이상하게 열리는듯 한데 우선 이렇게라도 체크해본다.
		if (gameObject.activeSelf == false)
			return;

		_idWithCostume = actorId;

		// 캐릭터 교체는 이 캔버스 담당이다.
		// 액터가 혹시나 미리 만들어져있다면 등록되어있을거니 가져다쓴다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedCanvasPlayerActor(actorId);
		if (playerActor != null)
		{
			if (playerActor != _playerActor)
			{
				// 현재 캐릭터 하이드 시키고
				if (_playerActor != null)
					_playerActor.gameObject.SetActive(false);
				_playerActor = playerActor;
				_playerActor.gameObject.SetActive(true);
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
			AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(actorId), "", OnLoadedPlayerActor);
		}
	}

	string _idWithCostume;
	Action _completeCallback;
	public void ShowCanvasPlayerActorWithCostume(string overridePreviewCostumeId, Action completeCallback)
	{
		if (_wait)
			return;

		// back이나 홈키 누르면서 동시에 누르면 이상하게 열리는듯 한데 우선 이렇게라도 체크해본다.
		if (gameObject.activeSelf == false)
		{
			if (CostumeListCanvas.instance == null || CostumeListCanvas.instance.gameObject.activeSelf == false)
				return;
		}

		string address = "";
		string idWithCostume = "";
		if (string.IsNullOrEmpty(overridePreviewCostumeId))
		{
			idWithCostume = string.Format("{0}_{1}", CharacterData.s_PlayerActorId, CostumeManager.instance.selectedCostumeId);
			address = CostumeManager.instance.GetCurrentPlayerPrefabAddress();
		}
		else
		{
			idWithCostume = string.Format("{0}_{1}", CharacterData.s_PlayerActorId, overridePreviewCostumeId);
			address = CostumeManager.GetAddressByCostumeId(overridePreviewCostumeId);
		}
		_idWithCostume = idWithCostume;

		// 캐릭터 교체는 이 캔버스 담당이다.
		// 액터가 혹시나 미리 만들어져있다면 등록되어있을거니 가져다쓴다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedCanvasPlayerActor(idWithCostume);
		if (playerActor != null)
		{
			if (playerActor != _playerActor)
			{
				// 현재 캐릭터 하이드 시키고
				if (_playerActor != null)
					_playerActor.gameObject.SetActive(false);
				_playerActor = playerActor;
				_playerActor.gameObject.SetActive(true);
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
			AddressableAssetLoadManager.GetAddressableGameObject(address, "", OnLoadedPlayerActor);
		}
	}

	void OnLoadedPlayerActor(GameObject prefab)
	{
		// 플레이어 캐릭터가 아닌 다른 캐릭터를 선택 후 Ok 누른다음에 로딩이 완료되기 전에 창을 나가버리면
		// 새 캐릭터를 만들 이유도 없고 인포창으로 넘어가서도 안된다.
		if (this == null) return;
		if (gameObject == null) return;
		if (gameObject.activeSelf == false)
		{
			if (CostumeListCanvas.instance == null || CostumeListCanvas.instance.gameObject.activeSelf == false)
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
		PlayerActor playerActor = newObject.GetComponent<PlayerActor>();
		if (playerActor == null)
			return;

		// 캔버스용 캐릭터의 AI는 꺼둬야한다.
		playerActor.playerAI.enabled = false;
		playerActor.baseCharacterController.enabled = false;
		playerActor.enabled = false;
		BattleInstanceManager.instance.AddCanvasPlayerActor(playerActor, _idWithCostume);

		if (playerActor != _playerActor)
		{
			if (_playerActor != null)
				_playerActor.gameObject.SetActive(false);
			_playerActor = playerActor;
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