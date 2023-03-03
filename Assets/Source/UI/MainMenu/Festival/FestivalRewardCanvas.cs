using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class FestivalRewardCanvas : MonoBehaviour
{
	public static FestivalRewardCanvas instance;

	public Text remainTimeText;

	public Image currentSumPointImage;
	public Text currentSumPointText;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<FestivalRewardCanvasListItem>
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
		RefreshCount();
		RefreshGrid();

		FestivalTypeTableData festivalTypeTableData = TableDataManager.instance.FindFestivalTypeTableData(FestivalData.instance.festivalId);
		if (festivalTypeTableData == null)
			return;

		AddressableAssetLoadManager.GetAddressableSprite(festivalTypeTableData.iconAddress, "Icon", (sprite) =>
		{
			currentSumPointImage.sprite = null;
			currentSumPointImage.sprite = sprite;
		});

		AlarmObject.Hide(FestivalTabCanvas.instance.rewardAlarmRootTransform);
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
		_festivalExpireDateTime = FestivalData.instance.festivalExpire2Time;
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

	public void RefreshCount()
	{
		currentSumPointText.text = FestivalData.instance.festivalSumPoint.ToString("N0");
	}

	List<FestivalRewardCanvasListItem> _listFestivalRewardCanvasListItem = new List<FestivalRewardCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listFestivalRewardCanvasListItem.Count; ++i)
			_listFestivalRewardCanvasListItem[i].gameObject.SetActive(false);
		_listFestivalRewardCanvasListItem.Clear();

		for (int i = 0; i < TableDataManager.instance.festivalExchangeTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.festivalExchangeTable.dataArray[i].groupId != FestivalData.instance.festivalId)
				continue;

			FestivalRewardCanvasListItem festivalCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			festivalCanvasListItem.Initialize(TableDataManager.instance.festivalExchangeTable.dataArray[i]);
			_listFestivalRewardCanvasListItem.Add(festivalCanvasListItem);
		}
	}



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

		FestivalTabCanvas.instance.ignoreStartEventFlag = true;
		FestivalTabCanvas.instance.gameObject.SetActive(false);

		while (FestivalTabCanvas.instance.gameObject.activeSelf)
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

		FestivalTabCanvas.instance.ignoreStartEventFlag = true;
		FestivalTabCanvas.instance.gameObject.SetActive(false);

		while (FestivalTabCanvas.instance.gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		MissionListCanvas.ShowCanvasAsyncWithPrepareGround("PickUpEquipDetailCanvas", null);

		while ((PickUpEquipDetailCanvas.instance != null && PickUpEquipDetailCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;
		PickUpEquipDetailCanvas.instance.RefreshInfo(rewardValue);
		PickUpEquipDetailCanvas.instance.SetRestoreCanvas("festival");

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.4f);
	}
	#endregion
}