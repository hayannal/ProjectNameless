﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttendanceCanvas : MonoBehaviour
{
	public static AttendanceCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public Text remainTimeText;
	public AttendanceCanvasListItem lastItem;
	public AttendanceCanvasListItem lastItemForEquip;
	public GameObject earlyInfoRectObject;
	public GameObject earlyBonusRectObject;
	public Text earlyBonusNumberText;
	public AttendanceCanvasListItem earlyBonusItem;

	public GameObject nextAttendanceRootObject;
	public Text nextAttendanceRemainTimeText;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<AttendanceCanvasListItem>
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
	}

	void OnEnable()
	{
		RefreshRemainTime();
		RefreshGrid();
		RefreshNextInfo();
		RefreshEarlyBonusRectInfo();

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

	public void RefreshRemainTime()
	{
		// show 상태가 아니면 안보이겠지만 혹시 모르니 안전하게 구해온다.
		if (AttendanceData.instance.attendanceId == "")
			return;

		_eventExpireDateTime = AttendanceData.instance.attendanceExpireTime;
	}

	void Update()
	{
		UpdateRemainTime();
		UpdateNextRemainTime();
	}

	DateTime _eventExpireDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateRemainTime()
	{
		if (ServerTime.UtcNow < _eventExpireDateTime)
		{
			if (remainTimeText != null)
			{
				TimeSpan remainTime = _eventExpireDateTime - ServerTime.UtcNow;
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

	List<AttendanceCanvasListItem> _listAttendanceCanvasListItem = new List<AttendanceCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listAttendanceCanvasListItem.Count; ++i)
			_listAttendanceCanvasListItem[i].gameObject.SetActive(false);
		_listAttendanceCanvasListItem.Clear();

		AttendanceTypeTableData attendanceTypeTableData = TableDataManager.instance.FindAttendanceTypeTableData(AttendanceData.instance.attendanceId);
		if (attendanceTypeTableData == null)
			return;

		// 최종꺼만 크게 표시하기로 한다.
		for (int i = 0; i < (attendanceTypeTableData.lastRewardNum - 1); ++i)
		{
			AttendanceRewardTableData attendanceRewardTableData = TableDataManager.instance.FindAttendanceRewardTableData(attendanceTypeTableData.attendanceId, i + 1);
			if (attendanceRewardTableData == null)
				continue;

			AttendanceCanvasListItem attendanceCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			attendanceCanvasListItem.RefreshInfo(attendanceTypeTableData.lastRewardNum, attendanceRewardTableData);
			_listAttendanceCanvasListItem.Add(attendanceCanvasListItem);
		}

		AttendanceRewardTableData lastAttendanceRewardTableData = TableDataManager.instance.FindAttendanceRewardTableData(attendanceTypeTableData.attendanceId, attendanceTypeTableData.lastRewardNum);
		if (lastAttendanceRewardTableData.rewardType1 == "it")
		{
			lastItem.gameObject.SetActive(false);
			lastItemForEquip.RefreshInfo(attendanceTypeTableData.lastRewardNum, lastAttendanceRewardTableData);
			lastItemForEquip.gameObject.SetActive(true);
			return;
		}
		lastItemForEquip.gameObject.SetActive(false);
		lastItem.RefreshInfo(attendanceTypeTableData.lastRewardNum, lastAttendanceRewardTableData);
		lastItem.gameObject.SetActive(true);
	}

	public void RefreshNextInfo()
	{
		bool showNext = AttendanceData.instance.todayReceiveRecorded;
		nextAttendanceRootObject.SetActive(showNext);
		if (showNext)
			_nextResetDateTime = PlayerData.instance.dayRefreshTime;
	}

	DateTime _nextResetDateTime;
	int _lastNextRemainTimeSecond = -1;
	void UpdateNextRemainTime()
	{
		if (nextAttendanceRootObject.activeSelf == false)
			return;

		if (ServerTime.UtcNow < _nextResetDateTime)
		{
			TimeSpan remainTime = _nextResetDateTime - ServerTime.UtcNow;
			if (_lastNextRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				nextAttendanceRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastNextRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			nextAttendanceRootObject.SetActive(false);
		}
	}

	public void RefreshEarlyBonusRectInfo()
	{
		int earlyBonusDays = AttendanceData.instance.earlyBonusDays;
		earlyInfoRectObject.SetActive(earlyBonusDays == 0);
		earlyBonusRectObject.SetActive(earlyBonusDays > 0);

		if (earlyBonusDays == 0)
			return;

		earlyBonusNumberText.text = earlyBonusDays.ToString();

		// 아무거나 가져와서 셋팅부터 하고
		AttendanceRewardTableData tempAttendanceRewardTableData = TableDataManager.instance.FindAttendanceRewardTableData(AttendanceData.instance.attendanceId, 1);
		earlyBonusItem.RefreshInfo(0, tempAttendanceRewardTableData);

		// earlyBonus로 받은 에너지로 덮어쓴다. 보여주려고 하는거라 이렇게 처리해도 된다.
		earlyBonusItem.rewardIcon.RefreshReward("cu", "EN", earlyBonusDays * BattleInstanceManager.instance.GetCachedGlobalConstantInt("AttendanceEarlyEnergy"));
		earlyBonusItem.countText.text = earlyBonusItem.rewardIcon.countText.text;
	}
}