using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class SevenDaysCanvas : MonoBehaviour
{
	public static SevenDaysCanvas instance;

	public Text remainTimeText;

	public Text currentSumPointText;
	public Text[] sumTextList;
	public Slider[] progressSliderList;
	public RewardIcon[] sumRewardIconList;
	public GameObject[] sumRewardBlackObjectList;
	public RectTransform[] sumRewardAlarmRootTransformList;

	// 하단 탭
	public Transform[] buttonImageTransformList;
	public Transform[] lockImageTransformList;
	public Text[] lockTextList;
	public RectTransform[] dayAlarmRootTransformList;

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

	bool _started = false;
	void Start()
	{
		contentItemPrefab.SetActive(false);

		OnValueChangedToggle(0);

		_started = true;
	}

	void OnEnable()
	{
		SetRemainTimeInfo();
		RefreshSumReward();
		RefreshLockObjectList();
		RefreshDayAlarmObject();

		if (_started)
			RefreshGrid(_lastIndex + 1);
	}

	void Update()
	{
		UpdateRemainTime();
		UpdateLockRemainTime();
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
			SevenDaysTabCanvas.instance.gameObject.SetActive(false);
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

	#region SumReward
	public void RefreshSumReward()
	{
		// current progress
		int currentPoint = MissionData.instance.sevenDaysSumPoint;
		currentSumPointText.text = currentPoint.ToString("N0");

		// sum reward
		_listSumRewardCount.Clear();
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
				bool rewarded = (MissionData.instance.IsGetSevenDaysSumReward(TableDataManager.instance.sevenSumTable.dataArray[i].count));
				sumRewardBlackObjectList[currentIndex].SetActive(rewarded);

				if (currentPoint >= TableDataManager.instance.sevenSumTable.dataArray[i].count)
				{
					progressSliderList[currentIndex].value = 1.0f;
					sumTextList[currentIndex].color = new Color(0.262f, 0.915f, 0.092f);

					if (rewarded)
						AlarmObject.Hide(sumRewardAlarmRootTransformList[currentIndex]);
					else
						AlarmObject.Show(sumRewardAlarmRootTransformList[currentIndex]);
				}
				else
				{
					int max = TableDataManager.instance.sevenSumTable.dataArray[i].count;
					int current = currentPoint;
					if (currentIndex > 0)
					{
						max -= TableDataManager.instance.sevenSumTable.dataArray[i - 1].count;
						current -= TableDataManager.instance.sevenSumTable.dataArray[i - 1].count;
					}
					progressSliderList[currentIndex].value = (float)current / max;
					sumTextList[currentIndex].color = Color.white;
					AlarmObject.Hide(sumRewardAlarmRootTransformList[currentIndex]);
				}
			}

			_listSumRewardCount.Add(TableDataManager.instance.sevenSumTable.dataArray[i].count);
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

	List<int> _listSumRewardCount = new List<int>();
	SevenSumTableData _sevenSumTableData;
	public void OnClickSumRewardIcon(int index)
	{
		if (index >= _listSumRewardCount.Count)
		{
			Debug.LogFormat("invalid index : {0}", index);
			return;
		}

		int point = _listSumRewardCount[index];
		Debug.LogFormat("point : {0}", point);

		SevenSumTableData sevenSumTableData = TableDataManager.instance.FindSevenDaysSumTableData(MissionData.instance.sevenDaysId, point);
		if (sevenSumTableData == null)
		{
			Debug.LogFormat("invalid point : {0}", point);
			return;
		}

		if (sumRewardBlackObjectList[index].activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LevelPassUI_AlreadyClaimed"), 2.0f);
			return;
		}

		if (MissionData.instance.sevenDaysSumPoint < point)
		{
			//ToastCanvas.instance.ShowToast(UIString.instance.GetString("SevenDaysUI_NotEnoughtTotal"), 2.0f);
			// 부족하다는 메시지 대신 타입에 따라 상세정보창으로 가기로 한다.
			#region Detail Info
			switch (sevenSumTableData.rewardType)
			{
				case "cu":
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("SevenDaysUI_NotEnoughtTotal"), 2.0f);
					break;
				case "it":
					RewardIcon.ShowDetailInfo(sevenSumTableData.rewardType, sevenSumTableData.rewardValue);
					break;
			}
			#endregion
			return;
		}

		if (sevenSumTableData.rewardType == "cu" && sevenSumTableData.rewardValue == "GO" && CurrencyData.instance.CheckMaxGold())
			return;

		_sevenSumTableData = sevenSumTableData;
		PlayFabApiManager.instance.RequestGetSevenDaysSumReward(sevenSumTableData, OnRecvResult);
	}

	void OnRecvResult(string itemGrantString)
	{
		// 직접 수령이 있는 곳이라서 별도로 처리한다.
		if (_sevenSumTableData.rewardType == "it" && string.IsNullOrEmpty(itemGrantString) == false)
		{
			FestivalExchangeConfirmCanvas.GetItReward(_sevenSumTableData.rewardValue, itemGrantString, _sevenSumTableData.rewardCount);
		}

		SevenDaysTabCanvas.instance.currencySmallInfo.RefreshInfo();
		RefreshSumReward();
		RefreshDayAlarmObject();
		SevenDaysTabCanvas.instance.RefreshAlarmObject();
		MainCanvas.instance.RefreshSevenDaysAlarmObject();
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);
	}
	#endregion

	#region NotOpened
	public void OnNotOpened(int day)
	{
		SevenDaysTypeTableData sevenDaysTypeTableData = TableDataManager.instance.FindSevenDaysTypeTableData(MissionData.instance.sevenDaysId);
		if (sevenDaysTypeTableData == null)
			return;
		DateTime startTime = MissionData.instance.sevenDaysExpireTime - TimeSpan.FromSeconds(sevenDaysTypeTableData.givenTime);
		TimeSpan delta = ServerTime.UtcNow - startTime;

		if (delta.Days + 2 == day)
		{
			_lockTextIndex = day - 1;
			if (_lockTextIndex > lockTextList.Length)
				return;
			_dayLockReleaseDateTime = startTime + TimeSpan.FromDays(day - 1);
			showLockRemainTime = 5.0f;
			lockTextList[_lockTextIndex].gameObject.SetActive(true);
		}
	}

	int _lockTextIndex = -1;
	DateTime _dayLockReleaseDateTime;
	float showLockRemainTime = 0.0f;
	int _lastLockRemainTimeSecond;
	void UpdateLockRemainTime()
	{
		if (showLockRemainTime <= 0.0f)
			return;
		if (_lockTextIndex == -1)
			return;

		if (ServerTime.UtcNow < _dayLockReleaseDateTime)
		{
			TimeSpan remainTime = _dayLockReleaseDateTime - ServerTime.UtcNow;
			if (_lastLockRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				if (remainTime.Days > 0)
					lockTextList[_lockTextIndex].text = string.Format("{0}d {1:00}:{2:00}:{3:00}", remainTime.Days, remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				else
					lockTextList[_lockTextIndex].text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastLockRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}

		showLockRemainTime -= Time.deltaTime;
		if (showLockRemainTime <= 0.0f)
		{
			showLockRemainTime = 0.0f;
			lockTextList[_lockTextIndex].gameObject.SetActive(false);
		}
	}
	#endregion


	#region Detail Info
	// 시간이 없어서 후다닥 급조해서 세븐데이즈와 페스티벌에만 자세히 넘어가서 보기를 구현했는데
	// 언젠가 이게 너무 구려서 통합한다면 다 지우고 새로 만들어야할거 같다.
	// 지울게 여기랑 페스티벌의 디테일쪽 다 지우고
	// 픽업 장비 참고해서 통합해서 만들어야할거 같다.
	public void OnClickPetDetailButton(string rewardValue)
	{
		Timing.RunCoroutine(ShowPetDetailCanvasProcess(rewardValue));
	}

	IEnumerator<float> ShowPetDetailCanvasProcess(string rewardValue)
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		SevenDaysTabCanvas.instance.ignoreStartEventFlag = true;
		SevenDaysTabCanvas.instance.gameObject.SetActive(false);

		while (SevenDaysTabCanvas.instance.gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		// Pet
		MainCanvas.instance.OnClickPetButton();

		while ((PetListCanvas.instance != null && PetListCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		// 아무래도 카운트는 없는게 맞아보인다
		int count = 0;
		PetData petData = PetManager.instance.GetPetData(rewardValue);
		if (petData != null) count = petData.count;
		PetListCanvas.instance.OnClickListItem(rewardValue, count);

		while ((PetInfoCanvas.instance != null && PetInfoCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.4f);
	}

	public void OnClickEquipDetailButton(string rewardValue)
	{
		Timing.RunCoroutine(ShowEquipDetailCanvasProcess(rewardValue));
	}

	IEnumerator<float> ShowEquipDetailCanvasProcess(string rewardValue)
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		SevenDaysTabCanvas.instance.ignoreStartEventFlag = true;
		SevenDaysTabCanvas.instance.gameObject.SetActive(false);

		while (SevenDaysTabCanvas.instance.gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		MissionListCanvas.ShowCanvasAsyncWithPrepareGround("PickUpEquipDetailCanvas", null);

		while ((PickUpEquipDetailCanvas.instance != null && PickUpEquipDetailCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;
		PickUpEquipDetailCanvas.instance.RefreshInfo(rewardValue);
		PickUpEquipDetailCanvas.instance.SetRestoreCanvas("sevendays");

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.4f);
	}
	#endregion




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
		for (int i = 0; i < lockTextList.Length; ++i)
			lockTextList[i].gameObject.SetActive(false);
		RefreshGrid(index + 1);

		_lastIndex = index;
	}

	public void RefreshLockObjectList()
	{
		for (int i = 0; i < lockImageTransformList.Length; ++i)
			lockImageTransformList[i].gameObject.SetActive(MissionData.instance.IsOpenDay(i + 1) == false);

		for (int i = 0; i < lockTextList.Length; ++i)
			lockTextList[i].gameObject.SetActive(false);
	}

	public void RefreshDayAlarmObject()
	{
		for (int i = 0; i < dayAlarmRootTransformList.Length; ++i)
		{
			bool dayAlarm = IsDayAlarm(i + 1);
			if (dayAlarm)
				AlarmObject.Show(dayAlarmRootTransformList[i]);
			else
				AlarmObject.Hide(dayAlarmRootTransformList[i]);
		}
	}

	bool IsDayAlarm(int day)
	{
		for (int i = 0; i < TableDataManager.instance.sevenDaysRewardTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.sevenDaysRewardTable.dataArray[i].group != MissionData.instance.sevenDaysId)
				continue;
			if (day != TableDataManager.instance.sevenDaysRewardTable.dataArray[i].day)
				continue;
			if (MissionData.instance.IsOpenDay(TableDataManager.instance.sevenDaysRewardTable.dataArray[i].day) == false)
				continue;

			int currentCount = MissionData.instance.GetProceedingCount(TableDataManager.instance.sevenDaysRewardTable.dataArray[i].typeId);
			if (currentCount < TableDataManager.instance.sevenDaysRewardTable.dataArray[i].needCount)
				continue;
			if (MissionData.instance.IsGetSevenDaysReward(TableDataManager.instance.sevenDaysRewardTable.dataArray[i].day, TableDataManager.instance.sevenDaysRewardTable.dataArray[i].num))
				continue;

			return true;
		}
		return false;
	}
	#endregion
}
