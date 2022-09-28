using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SevenDaysCanvas : MonoBehaviour
{
	public static SevenDaysCanvas instance;

	public Text remainTimeText;

	public Text currentSumPointText;
	public Text[] sumTextList;
	public Slider[] progressSliderList;
	public RewardIcon[] sumRewardIconList;

	public Transform[] buttonImageTransformList;
	public Transform[] lockImageTransformList;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<SevenDaysCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);

		OnValueChangedToggle(0);
	}

	void OnEnable()
	{
		SetRemainTimeInfo();
		RefreshSumReward();
		RefreshLockObjectList();

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


	void Update()
	{
		UpdateRemainTime();
	}

	void SetRemainTimeInfo()
	{
		if (MissionData.instance.sevenDaysId == 0)
			return;

		// show 상태가 아니면 안보이겠지만 혹시 모르니 안전하게 구해온다.
		_sevenDaysExpireDateTime = MissionData.instance.sevenDaysExpireTime;
	}

	DateTime _sevenDaysExpireDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateRemainTime()
	{
		if (ServerTime.UtcNow < _sevenDaysExpireDateTime)
		{
			if (remainTimeText != null)
			{
				TimeSpan remainTime = _sevenDaysExpireDateTime - ServerTime.UtcNow;
				if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
				{
					if (remainTime.Days > 0)
						remainTimeText.text = string.Format("{0}d {1:00}:{2:00}:{3:00}", remainTime.Days, remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
					else
						remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
					_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
				}
			}
		}
		else
		{
			// 이벤트 기간이 끝났으면 닫아버리는게 제일 편하다.
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_EventExpired"), 2.0f);
			gameObject.SetActive(false);
		}
	}

	List<SevenDaysCanvasListItem> _listSevenDaysCanvasListItem = new List<SevenDaysCanvasListItem>();
	void RefreshGrid(int day)
	{
		for (int i = 0; i < _listSevenDaysCanvasListItem.Count; ++i)
			_listSevenDaysCanvasListItem[i].gameObject.SetActive(false);
		_listSevenDaysCanvasListItem.Clear();

		for (int i = 0; i < TableDataManager.instance.sevenDaysRewardTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.sevenDaysRewardTable.dataArray[i].group != MissionData.instance.sevenDaysId)
				continue;
			if (TableDataManager.instance.sevenDaysRewardTable.dataArray[i].day != day)
				continue;

			SevenDaysCanvasListItem sevenDaysCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			sevenDaysCanvasListItem.Initialize(TableDataManager.instance.sevenDaysRewardTable.dataArray[i]);
			_listSevenDaysCanvasListItem.Add(sevenDaysCanvasListItem);
		}
	}

	public void RefreshSumReward()
	{
		// current progress
		int currentPoint = 830;
		currentSumPointText.text = currentPoint.ToString("N0");

		// sum reward
		int currentIndex = 0;
		for (int i = 0; i < TableDataManager.instance.sevenSumTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.sevenSumTable.dataArray[i].groupId != MissionData.instance.sevenDaysId)
				continue;

			if (currentIndex < sumTextList.Length)
				sumTextList[currentIndex].text = TableDataManager.instance.sevenSumTable.dataArray[i].count.ToString("N0");

			if (currentIndex < sumRewardIconList.Length)
				sumRewardIconList[currentIndex].RefreshReward(TableDataManager.instance.sevenSumTable.dataArray[i].rewardType, TableDataManager.instance.sevenSumTable.dataArray[i].rewardValue, TableDataManager.instance.sevenSumTable.dataArray[i].rewardCount);

			if (currentIndex < progressSliderList.Length)
			{
				if (currentPoint > TableDataManager.instance.sevenSumTable.dataArray[i].count)
				{
					progressSliderList[currentIndex].value = 1.0f;
					sumTextList[currentIndex].color = new Color(0.262f, 0.915f, 0.092f);
				}
				else
				{
					int max = TableDataManager.instance.sevenSumTable.dataArray[i].count;
					if (i > 0)
						max -= TableDataManager.instance.sevenSumTable.dataArray[i - 1].count;
					int current = currentPoint - TableDataManager.instance.sevenSumTable.dataArray[i - 1].count;
					progressSliderList[currentIndex].value = (float)current / max;
					sumTextList[currentIndex].color = Color.white;
				}
			}

			currentIndex += 1;
		}

		// progress
		
		for (int i = 0; i < TableDataManager.instance.sevenSumTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.sevenSumTable.dataArray[i].groupId != MissionData.instance.sevenDaysId)
				continue;

			if (currentIndex < sumTextList.Length)
				sumTextList[currentIndex].text = TableDataManager.instance.sevenSumTable.dataArray[i].count.ToString("N0");

			if (currentIndex < sumRewardIconList.Length)
				sumRewardIconList[currentIndex].RefreshReward(TableDataManager.instance.sevenSumTable.dataArray[i].rewardType, TableDataManager.instance.sevenSumTable.dataArray[i].rewardValue, TableDataManager.instance.sevenSumTable.dataArray[i].rewardCount);

			currentIndex += 1;
		}
	}

	public void RefreshLockObjectList()
	{
		for (int i = 0; i < lockImageTransformList.Length; ++i)
			lockImageTransformList[i].gameObject.SetActive(MissionData.instance.IsOpenDay(i + 1) == false);
	}



	#region Menu Button
	int _lastIndex = -1;
	public void OnValueChangedToggle(int index)
	{
		if (index == _lastIndex)
			return;
		
		for (int i = 0; i < buttonImageTransformList.Length; ++i)
		{
			buttonImageTransformList[i].localScale = (i == index) ? new Vector3(1.5f, 1.5f, 1.0f) : Vector3.one;
		}
		RefreshGrid(index + 1);

		_lastIndex = index;
	}
	#endregion
}
