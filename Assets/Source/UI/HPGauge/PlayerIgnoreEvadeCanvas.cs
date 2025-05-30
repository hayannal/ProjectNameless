﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class PlayerIgnoreEvadeCanvas : MonoBehaviour
{
	public static PlayerIgnoreEvadeCanvas instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(CommonBattleGroup.instance.playerIgnoreEvadeCanvasPrefab).GetComponent<PlayerIgnoreEvadeCanvas>();
#if UNITY_EDITOR
				AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
				if (settings.ActivePlayModeDataBuilderIndex == 2)
					ObjectUtil.ReloadShader(_instance.gameObject);
#endif
			}
			return _instance;
		}
	}
	static PlayerIgnoreEvadeCanvas _instance = null;

	public enum eImageType
	{
		Accuracy,
		Charging,
	}

	public GameObject rootObject;
	public GameObject[] imageObjectList;
	public Text percentText;
	
	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		_prevTargetPosition = -Vector3.up;
	}

	public void ShowIgnoreEvade(bool show, PlayerActor playerActor)
	{
		if (!show)
		{
			gameObject.SetActive(false);
			return;
		}

		gameObject.SetActive(true);

		if (_targetTransform == playerActor.cachedTransform)
		{
			UpdateGaugePosition();
			return;
		}

		_offsetY = playerActor.gaugeOffsetY;
		_targetTransform = playerActor.cachedTransform;
		GetTargetHeight(_targetTransform);
	}

	public void SetImageType(eImageType imageType)
	{
		for (int i = 0; i < imageObjectList.Length; ++i)
			imageObjectList[i].SetActive((int)imageType == i);
	}

	public void SetPercent(float rate)
	{
		percentText.text = string.Format("{0:N0}%", (int)((rate + 0.005f) * 100.0f));
	}

	// Update is called once per frame
	Vector3 _prevTargetPosition = -Vector3.up;
	void Update()
	{
		if (_targetTransform != null)
		{
			if (_targetTransform.position != _prevTargetPosition)
			{
				UpdateGaugePosition();
				_prevTargetPosition = _targetTransform.position;
			}
		}

		// 캐릭터창 예외처리. 캐릭터창 안에서는 보이면 안된다. 로비에서만 따로 검사한다.
		// 그런데 캐릭터창 안에서도 체험모드면 또 보여야해서 CharacterListCanvas의 Stack 여부로는 판단하기가 어렵다.
		// 그래서 아예 카메라와의 거리를 보고 판단하기로 한다.
		// 이렇게 처리하면 Accuracy든 Charging이든 상관없이 이후에 어떤 타입이 추가되어도 알아서 다 처리되게 된다.
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby)
		{
			if (rootObject.activeSelf)
			{
				/*
				if (CharacterListCanvas.instance != null && StackCanvas.IsInStack(CharacterListCanvas.instance.gameObject, false))
				{
					Vector3 diff = CustomFollowCamera.instance.cachedTransform.position - cachedTransform.position;
					if (diff.x * diff.x + diff.y * diff.y + diff.z * diff.z < 8.0f * 8.0f)
						rootObject.SetActive(false);
				}
				*/
			}
			else
			{
				Vector3 diff = CustomFollowCamera.instance.cachedTransform.position - cachedTransform.position;
				if (diff.x * diff.x + diff.y * diff.y + diff.z * diff.z > 8.0f * 8.0f)
					rootObject.SetActive(true);
			}
		}

		// 아무래도 이 캔버스가 나와있는 상태에서 플레이어가 죽을 경우(혹은 아예 게임오브젝트가 꺼질 경우) 자동으로 Disable 하는 처리를 여기서 해야할거 같다.
		// 패시브 어펙터가 Finalize하기도 전에 삭제될 경우를 대비해서다.
		//
	}

	void GetTargetHeight(Transform t)
	{
		Collider collider = t.GetComponentInChildren<Collider>();
		if (collider == null)
			return;

		_targetHeight = ColliderUtil.GetHeight(collider);
	}

	Transform _targetTransform;
	float _targetHeight;
	float _offsetY;
	void UpdateGaugePosition()
	{
		Vector3 desiredPosition = _targetTransform.position;
		desiredPosition.y += _targetHeight;
		desiredPosition.y += _offsetY;
		cachedTransform.position = desiredPosition;
	}





	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}