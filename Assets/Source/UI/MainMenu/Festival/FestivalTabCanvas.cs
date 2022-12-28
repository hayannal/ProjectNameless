using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FestivalTabCanvas : MonoBehaviour
{
	public static FestivalTabCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public RectTransform questAlarmRootTransform;
	public RectTransform rewardAlarmRootTransform;

	#region Tab Button
	public GameObject[] innerMenuPrefabList;
	public Transform innerMenuRootTransform;
	public TabButton[] tabButtonList;
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
		RefreshAlarmObject();

		MainCanvas.instance.OnEnterCharacterMenu(true);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	public static bool IsAlarmFestivalQuest()
	{
		bool getable = false;
		for (int i = 0; i < TableDataManager.instance.festivalCollectTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.festivalCollectTable.dataArray[i].group != FestivalData.instance.festivalId)
				continue;

			int currentCount = FestivalData.instance.GetProceedingCount(TableDataManager.instance.festivalCollectTable.dataArray[i].typeId);
			if (currentCount < TableDataManager.instance.festivalCollectTable.dataArray[i].needCount)
				continue;
			if (FestivalData.instance.IsGetFestivalCollect(TableDataManager.instance.festivalCollectTable.dataArray[i].num))
				continue;

			getable = true;
			break;
		}
		return getable;
	}

	public static bool IsAlarmFestivalReward()
	{
		bool buyable = false;
		for (int i = 0; i < TableDataManager.instance.festivalExchangeTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.festivalExchangeTable.dataArray[i].groupId != FestivalData.instance.festivalId)
				continue;

			if (FestivalData.instance.GetExchangeTime(TableDataManager.instance.festivalExchangeTable.dataArray[i].num) >= TableDataManager.instance.festivalExchangeTable.dataArray[i].exchangeTimes)
				continue;

			if (FestivalData.instance.festivalSumPoint < TableDataManager.instance.festivalExchangeTable.dataArray[i].neededCount)
				continue;

			buyable = true;
			break;
		}
		return buyable;
	}

	public void RefreshAlarmObject()
	{
		AlarmObject.Hide(questAlarmRootTransform);
		if (IsAlarmFestivalQuest())
			AlarmObject.Show(questAlarmRootTransform);

		AlarmObject.Hide(rewardAlarmRootTransform);
		if (IsAlarmFestivalReward())
			AlarmObject.Show(rewardAlarmRootTransform);
	}



	#region Tab Button
	public void OnClickTabButton1() { OnValueChangedToggle(0); }
	public void OnClickTabButton2() { OnValueChangedToggle(1); }
	public void OnClickTabButton3() { OnValueChangedToggle(2); }

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