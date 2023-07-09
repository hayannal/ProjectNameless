using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissionTabCanvas : MonoBehaviour
{
	public static MissionTabCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public RectTransform missionAlarmRootTransform;
	public RectTransform adventureAlarmRootTransform;

	#region Tab Button
	public GameObject[] innerMenuPrefabList;
	public Transform innerMenuRootTransform;
	public TabButton[] tabButtonList;
	#endregion

	#region Energy
	public Text energyText;
	#endregion

	void Awake()
	{
		instance = this;
	}

	public int defaulMenuButtonIndex { get; set; }

	// Start is called before the first frame update
	void Start()
	{
		#region Tab Button
		OnValueChangedToggle(defaulMenuButtonIndex);
		#endregion
	}

	void OnEnable()
	{
		#region Energy
		RefreshEnergy();
		#endregion
		RefreshAlarmObject();

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (restore)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(true);
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

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

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	void Update()
	{
		#region Energy
		UpdateEnergy();
		#endregion
	}

	public static bool IsAlarmMission()
	{
		if (MissionListCanvas.IsAlarmPetSearch() || MissionListCanvas.IsAlarmFortuneWheel() || MissionListCanvas.IsAlarmRushDefense() || MissionListCanvas.IsAlarmBossDefense() || MissionListCanvas.IsAlarmBossBattle())
			return true;
		return false;
	}

	public static bool IsAlarmAdventure()
	{
		if (AdventureListCanvas.IsAlarmRobotDefense())
			return true;
		return false;
	}

	public void RefreshAlarmObject()
	{
		AlarmObject.Hide(missionAlarmRootTransform);
		if (IsAlarmMission())
			AlarmObject.Show(missionAlarmRootTransform);

		AlarmObject.Hide(adventureAlarmRootTransform);
		if (IsAlarmAdventure())
			AlarmObject.Show(adventureAlarmRootTransform);
	}


	#region Energy
	public void RefreshEnergy()
	{
		energyText.text = CurrencyData.instance.energy.ToString("N0");
	}

	int _lastEnergySecond = -1;
	void UpdateEnergy()
	{
		if (CurrencyData.instance.energy >= CurrencyData.instance.energyMax)
			return;

		if (_lastEnergySecond != (int)Time.time)
		{
			//Debug.Log(_lastEnergySecond);
			RefreshEnergy();
			_lastEnergySecond = (int)Time.time;
		}
	}
	#endregion


	#region Tab Button
	public void OnClickTabButton1() { OnValueChangedToggle(0); }
	public void OnClickTabButton2() { OnValueChangedToggle(1); }

	List<Transform> _listMenuTransform = new List<Transform>();
	int _lastIndex = -1;
	void OnValueChangedToggle(int index)
	{
		if (index == _lastIndex)
			return;

		if (_listMenuTransform.Count == 0)
		{
			for (int i = 0; i < tabButtonList.Length; ++i)
				_listMenuTransform.Add(null);
		}

		if (_listMenuTransform[index] == null && innerMenuPrefabList[index] != null)
		{
			GameObject newObject = Instantiate<GameObject>(innerMenuPrefabList[index], innerMenuRootTransform);
			_listMenuTransform[index] = newObject.transform;
		}

		for (int i = 0; i < _listMenuTransform.Count; ++i)
		{
			tabButtonList[i].isOn = (index == i);
			if (_listMenuTransform[i] == null)
				continue;
			_listMenuTransform[i].gameObject.SetActive(index == i);
		}

		_lastIndex = index;
	}
	#endregion
}