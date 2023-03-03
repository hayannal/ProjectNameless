using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SevenDaysTabCanvas : MonoBehaviour
{
	public static SevenDaysTabCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public RectTransform alarmRootTransform;

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

		MainCanvas.instance.OnEnterCharacterMenu(true);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	public bool ignoreStartEventFlag { get; set; }
	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		if (ignoreStartEventFlag)
		{
			ignoreStartEventFlag = false;
			MainCanvas.instance.OnEnterCharacterMenu(false, true);
			return;
		}
		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	public void RefreshAlarmObject()
	{
		AlarmObject.Hide(alarmRootTransform);
		if (MainCanvas.IsAlarmSevenDays())
			AlarmObject.Show(alarmRootTransform);
	}



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