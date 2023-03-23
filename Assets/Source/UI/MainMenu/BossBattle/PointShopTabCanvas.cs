using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PointShopTabCanvas : MonoBehaviour
{
	public static PointShopTabCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public RectTransform alarmRootTransform;

	public RectTransform goldAlarmRootTransform;
	public RectTransform diaAlarmRootTransform;
	public RectTransform energyAlarmRootTransform;

	#region Tab Button
	public GameObject[] innerMenuPrefabList;
	public Transform innerMenuRootTransform;
	public TabButton[] tabButtonList;
	#endregion

	void Awake()
	{
		instance = this;
	}

	// Start is called before the first frame update
	void Start()
	{
		#region Tab Button
		// 항상 게임을 처음 켤땐 0번탭을 보게 해준다.
		OnValueChangedToggle(0);
		#endregion
	}

	void OnEnable()
	{
		RefreshAlarmObject();

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (restore)
			return;
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

	}

	public void RefreshAlarmObject()
	{
		AlarmObject.Hide(alarmRootTransform);
		if (PointShopAttackCanvas.CheckLevelUp())
			AlarmObject.Show(alarmRootTransform);

		AlarmObject.Hide(goldAlarmRootTransform);
		AlarmObject.Hide(diaAlarmRootTransform);
		AlarmObject.Hide(energyAlarmRootTransform);

		PointShopTableData pointShopTableData = TableDataManager.instance.FindPointShopTableData(1, 5);
		if (pointShopTableData != null && SubMissionData.instance.bossBattlePoint >= pointShopTableData.price)
			AlarmObject.Show(goldAlarmRootTransform);

		pointShopTableData = TableDataManager.instance.FindPointShopTableData(2, 3);
		if (pointShopTableData != null && SubMissionData.instance.bossBattlePoint >= pointShopTableData.price)
			AlarmObject.Show(diaAlarmRootTransform);

		pointShopTableData = TableDataManager.instance.FindPointShopTableData(3, 5);
		if (pointShopTableData != null && SubMissionData.instance.bossBattlePoint >= pointShopTableData.price)
			AlarmObject.Show(energyAlarmRootTransform);
	}



	#region Tab Button
	public void OnClickTabButton1() { OnValueChangedToggle(0); }
	public void OnClickTabButton2() { OnValueChangedToggle(1); }
	public void OnClickTabButton3() { OnValueChangedToggle(2); }
	public void OnClickTabButton4() { OnValueChangedToggle(3); }

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