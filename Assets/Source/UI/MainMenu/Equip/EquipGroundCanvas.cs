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

	public RectTransform autoEquipAlarmRootTransform;
	public RectTransform compositeAlarmRootTransform;
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
		#region UI
		RefreshAutoEquipAlarmObject();
		RefreshAutoCompositeAlarmObject();
		#endregion

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
		RefreshOptionViewButton();
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
	public void RefreshOptionViewButton()
	{
		optionViewButtonObject.SetActive(EquipManager.instance.cachedValue > 0);
	}

	public void OnClickAutoEquipButton()
	{
		AutoEquip();
	}

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

	public void OnClickCompositeButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("EquipCompositeCanvas", null);
	}
	#endregion




	#region Auto Equip
	public void RefreshAutoEquipAlarmObject()
	{
		bool showAlarm = CheckAutoEquip();
		if (showAlarm)
			AlarmObject.Show(autoEquipAlarmRootTransform);
		else
			AlarmObject.Hide(autoEquipAlarmRootTransform);
	}

	public static bool CheckAutoEquip()
	{
		for (int i = 0; i < (int)EquipManager.eEquipSlotType.Amount; ++i)
		{
			List<EquipData> listEquipData = EquipManager.instance.GetEquipListByType((EquipManager.eEquipSlotType)i);
			if (listEquipData.Count == 0)
				continue;

			EquipData equippedData = EquipManager.instance.GetEquippedDataByType((EquipManager.eEquipSlotType)i);
			if (equippedData == null)
				return true;
		}
		return false;
	}

	public void RefreshAutoCompositeAlarmObject()
	{
		bool showAlarm = CheckAutoComposite();
		if (showAlarm)
			AlarmObject.Show(compositeAlarmRootTransform);
		else
			AlarmObject.Hide(compositeAlarmRootTransform);
	}

	public static bool CheckAutoComposite()
	{
		for (int i = 0; i < (int)EquipManager.eEquipSlotType.Amount; ++i)
		{
			List<EquipData> listEquipData = EquipManager.instance.GetEquipListByType((EquipManager.eEquipSlotType)i);
			for (int j = 0; j < listEquipData.Count; ++j)
			{
				if (EquipManager.instance.IsCompositeAvailable(listEquipData[j], listEquipData))
				{
					if (listEquipData[j].cachedEquipTableData.grade <= 2)
						return true;
				}
			}
		}
		return false;
	}

	List<EquipData> _listAutoEquipData = new List<EquipData>();
	List<string> _listUniqueId = new List<string>();
	List<int> _listEquipPos = new List<int>();
	void AutoEquip()
	{
		// 현재 장착된 장비보다 공격력이 높다면 auto리스트에 넣는다.
		_listAutoEquipData.Clear();
		int sumPrevValue = 0;
		int sumNextValue = 0;
		for (int i = 0; i < (int)EquipManager.eEquipSlotType.Amount; ++i)
		{
			List<EquipData> listEquipData = EquipManager.instance.GetEquipListByType((EquipManager.eEquipSlotType)i);
			if (listEquipData.Count == 0)
				continue;

			EquipData selectedEquipData = null;
			int maxValue = 0;
			EquipData equippedData = EquipManager.instance.GetEquippedDataByType((EquipManager.eEquipSlotType)i);
			if (equippedData != null)
				maxValue = equippedData.mainStatusValue;
			sumPrevValue += maxValue;

			for (int j = 0; j < listEquipData.Count; ++j)
			{
				if (maxValue < listEquipData[j].mainStatusValue)
				{
					maxValue = listEquipData[j].mainStatusValue;
					selectedEquipData = listEquipData[j];
				}
			}

			if (selectedEquipData != null)
				_listAutoEquipData.Add(selectedEquipData);

			sumNextValue += maxValue;
		}

		// auto리스트가 하나도 없다면 변경할게 없는거니 안내 토스트를 출력한다.
		if (_listAutoEquipData.Count == 0)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_CompleteAuto"), 2.0f);
			return;
		}

		// 변경할게 있다면
		_listUniqueId.Clear();
		_listEquipPos.Clear();
		for (int i = 0; i < _listAutoEquipData.Count; ++i)
		{
			_listUniqueId.Add(_listAutoEquipData[i].uniqueId);
			_listEquipPos.Add(_listAutoEquipData[i].cachedEquipTableData.equipType);
		}
		float prevValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		PlayFabApiManager.instance.RequestEquipList(_listAutoEquipData, _listUniqueId, _listEquipPos, () =>
		{
			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

			// 제단을 갱신한다.
			for (int i = 0; i < _listAutoEquipData.Count; ++i)
			{
				int positionIndex = _listAutoEquipData[i].cachedEquipTableData.equipType;
				EquipGround.instance.equipAltarList[positionIndex].RefreshEquipObject();
			}
			_listAutoEquipData.Clear();
			_listUniqueId.Clear();
			_listEquipPos.Clear();

			// UI도 갱신
			RefreshOptionViewButton();
			RefreshAutoEquipAlarmObject();
			MainCanvas.instance.RefreshEquipAlarmObject();

			// 변경 완료를 알리고
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(prevValue, nextValue);
			});
		});
	}
	#endregion
}