using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SubQuestInfo : MonoBehaviour
{
	public static SubQuestInfo instance;

	public GameObject smallButtonRootObject;
	public DOTweenAnimation infoRootTweenAnimation;
	public GameObject smallBackButtonRootObject;

	public GameObject contentsRootObject;
	public Text descriptionText;
	public GameObject smallBlinkObject;
	public GameObject blinkObject;
	public GameObject completeTextObject;

	public RectTransform alarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		// 처음 로그인하면서 들어갈때는 GuideQuest와 달리 퀘스트 리스트가 제대로 처리되지 않은 상태라서 유효성 검사를 해야한다.
		if (SubQuestData.instance.CheckValidQuestList(false) == false)
		{
			infoRootTweenAnimation.gameObject.SetActive(false);
			smallBackButtonRootObject.SetActive(false);
			return;
		}

		RefreshSmallButton();
		RefreshInfo();
		RefreshAlarmObject();
	}

	void OnDisable()
	{
		infoRootTweenAnimation.gameObject.SetActive(false);
		smallBackButtonRootObject.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{
		if (_openRemainTime > 0.0f)
		{
			_openRemainTime -= Time.deltaTime;
			if (_openRemainTime <= 0.0f)
			{
				_openRemainTime = 0.0f;
				OnClickSmallBackButton();
			}
		}

		if (_closeRemainTime > 0.0f)
		{
			_closeRemainTime -= Time.deltaTime;
			if (_closeRemainTime <= 0.0f)
			{
				_closeRemainTime = 0.0f;
				infoRootTweenAnimation.gameObject.SetActive(false);
				smallButtonRootObject.SetActive(true);
			}
		}
	}

	void RefreshSmallButton()
	{
		bool show = true;

		// 서브퀘는 진행중이지 않을때가 많다.
		if (SubQuestData.instance.currentQuestStep != SubQuestData.eQuestStep.Proceeding)
			show = false;

		SubQuestData.QuestInfo questInfo = SubQuestData.instance.FindQuestInfoByIndex(SubQuestData.instance.currentQuestIndex);
		if (questInfo == null)
			show = false;

		// 받아야하는 상황이라면
		if (show == false && SubQuestData.instance.todayQuestRewardedCount < SubQuestData.DailyMaxCount)
			show = true;

		bool todayComplete = false;
		if (show == false && SubQuestData.instance.todayQuestRewardedCount >= SubQuestData.DailyMaxCount)
		{
			todayComplete = true;
			show = true;
		}

		smallButtonRootObject.SetActive(show);

		if (show)
		{
			infoRootTweenAnimation.gameObject.SetActive(false);
			smallBackButtonRootObject.SetActive(false);
			_openRemainTime = _closeRemainTime = 0.0f;

			completeTextObject.SetActive(todayComplete);
			contentsRootObject.SetActive(!todayComplete);
		}
	}

	void RefreshInfo()
	{
		if (SubQuestData.instance.currentQuestStep != SubQuestData.eQuestStep.Proceeding)
			return;
		SubQuestData.QuestInfo questInfo = SubQuestData.instance.FindQuestInfoByIndex(SubQuestData.instance.currentQuestIndex);
		if (questInfo == null)
			return;

		RefreshCountInfo();
	}

	public void RefreshCountInfo()
	{
		SubQuestData.QuestInfo questInfo = SubQuestData.instance.FindQuestInfoByIndex(SubQuestData.instance.currentQuestIndex);
		if (questInfo == null)
			return;

		int currentCount = SubQuestData.instance.GetProceedingCount();
		int maxCount = questInfo.cnt;

		SubQuestTableData subQuestTableData = TableDataManager.instance.FindSubQuestTableData(questInfo.tp);
		descriptionText.SetLocalizedText(string.Format("{0} {1} / {2}", UIString.instance.GetString(subQuestTableData.shortDescriptionId), Mathf.Min(currentCount, maxCount), maxCount));

		bool isCompleteQuest = (currentCount >= maxCount);
		blinkObject.SetActive(isCompleteQuest);
		smallBlinkObject.SetActive(isCompleteQuest);
	}

	public void RefreshAlarmObject()
	{
		bool showAlarm = SubQuestData.instance.IsCompleteQuest();
		if (showAlarm == false)
		{
			if (SubQuestData.instance.currentQuestStep == SubQuestData.eQuestStep.Select && SubQuestData.instance.todayQuestRewardedCount < SubQuestData.DailyMaxCount)
				showAlarm = true;
		}
		AlarmObject.Hide(alarmRootTransform);
		if (showAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);
	}

	public void CloseInfo()
	{
		smallButtonRootObject.SetActive(false);
		infoRootTweenAnimation.gameObject.SetActive(false);
		smallBackButtonRootObject.SetActive(false);
		_openRemainTime = _closeRemainTime = 0.0f;

		blinkObject.SetActive(false);
		smallBlinkObject.SetActive(false);

		RefreshSmallButton();
		RefreshAlarmObject();
	}

	public void OnClickNameTextButton()
	{
		SubQuestData.QuestInfo questInfo = SubQuestData.instance.FindQuestInfoByIndex(SubQuestData.instance.currentQuestIndex);
		if (questInfo == null)
			return;

		GuideQuestInfo.OnClickQuickMenu((GuideQuestData.eQuestClearType)questInfo.tp);
	}

	public void OnClickCompleteTextButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("QuestEndCanvas", null);
	}

	public void OnClickBlinkImage()
	{
		OnClickDetailButton();
	}

	public void OnClickDetailButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("QuestInfoCanvas", null);
	}



	#region Show Hide
	public void OnClickSmallButton()
	{
		if (SubQuestData.instance.currentQuestStep == SubQuestData.eQuestStep.Select && SubQuestData.instance.todayQuestRewardedCount < SubQuestData.DailyMaxCount)
		{
			UIInstanceManager.instance.ShowCanvasAsync("QuestSelectCanvas", null);
			return;
		}

		smallButtonRootObject.SetActive(false);
		infoRootTweenAnimation.gameObject.SetActive(true);
		smallBackButtonRootObject.SetActive(true);
	}

	float _closeRemainTime;
	public void OnClickSmallBackButton()
	{
		if (_closeRemainTime > 0.0f)
			return;
		if (smallBackButtonRootObject.activeSelf == false)
			return;

		smallBackButtonRootObject.SetActive(false);
		infoRootTweenAnimation.DOPlayBackwards();
		_closeRemainTime = 0.6f;
	}

	float _openRemainTime;
	public void OnCompleteInfoRootTweenAnimation()
	{
		if (smallButtonRootObject.activeSelf)
			return;

		smallBackButtonRootObject.SetActive(true);
		_openRemainTime = 4.0f;
	}
	#endregion
}