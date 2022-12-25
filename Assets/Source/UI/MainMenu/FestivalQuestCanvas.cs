using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FestivalQuestCanvas : MonoBehaviour
{
	public static FestivalQuestCanvas instance;

	public Text remainTimeText;

	public Text currentSumPointText;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<FestivalQuestCanvasListItem>
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
		SetRemainTimeInfo();
		RefreshGrid();
	}

	void Update()
	{
		UpdateRemainTime();
	}

	void SetRemainTimeInfo()
	{
		if (FestivalData.instance.festivalId == 0)
			return;

		// show 상태가 아니면 안보이겠지만 혹시 모르니 안전하게 구해온다.
		_festivalExpireDateTime = FestivalData.instance.festivalExpireTime;
	}

	DateTime _festivalExpireDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateRemainTime()
	{
		if (ServerTime.UtcNow < _festivalExpireDateTime)
		{
			if (remainTimeText != null)
			{
				TimeSpan remainTime = _festivalExpireDateTime - ServerTime.UtcNow;
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
			FestivalTabCanvas.instance.gameObject.SetActive(false);
		}
	}

	List<FestivalQuestCanvasListItem> _listFestivalQuestCanvasListItem = new List<FestivalQuestCanvasListItem>();
	void RefreshGrid()
	{
		for (int i = 0; i < _listFestivalQuestCanvasListItem.Count; ++i)
			_listFestivalQuestCanvasListItem[i].gameObject.SetActive(false);
		_listFestivalQuestCanvasListItem.Clear();

		for (int i = 0; i < TableDataManager.instance.festivalCollectTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.festivalCollectTable.dataArray[i].group != FestivalData.instance.festivalId)
				continue;

			FestivalQuestCanvasListItem festivalCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			festivalCanvasListItem.Initialize(TableDataManager.instance.festivalCollectTable.dataArray[i]);
			_listFestivalQuestCanvasListItem.Add(festivalCanvasListItem);
		}
	}
}