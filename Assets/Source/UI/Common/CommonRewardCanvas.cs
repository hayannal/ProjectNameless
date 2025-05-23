﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class CommonRewardCanvas : MonoBehaviour
{
	public static CommonRewardCanvas instance;

	public RectTransform toastBackImageRectTransform;
	public GameObject titleLineObject;
	public GameObject exitObject;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<RewardIcon>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	public class CommonRewardData
	{
		public string type;
		public string value;
		public int count;
	}
	List<CommonRewardData> _listCommonRewardData = new List<CommonRewardData>();
	List<RewardIcon> _listRewardIcon = new List<RewardIcon>();

	System.Action _okAction;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	// 이런식으로 값을 직접 받을때도 있을거고
	public void RefreshReward(int gold, int dia, int energy, System.Action okAction = null)
	{
		_okAction = okAction;

		_listCommonRewardData.Clear();

		CommonRewardData commonRewardData = null;

		if (gold > 0)
		{
			commonRewardData = new CommonRewardData();
			commonRewardData.type = "cu";
			commonRewardData.value = "GO";
			commonRewardData.count = gold;
			_listCommonRewardData.Add(commonRewardData);
		}

		if (dia > 0)
		{
			commonRewardData = new CommonRewardData();
			commonRewardData.type = "cu";
			commonRewardData.value = "DI";
			commonRewardData.count = dia;
			_listCommonRewardData.Add(commonRewardData);
		}

		if (energy > 0)
		{
			commonRewardData = new CommonRewardData();
			commonRewardData.type = "cu";
			commonRewardData.value = "EN";
			commonRewardData.count = energy;
			_listCommonRewardData.Add(commonRewardData);
		}

		if (_listCommonRewardData.Count == 0)
		{
			gameObject.SetActive(false);
			return;
		}

		Timing.RunCoroutine(RewardProcess());
	}

	// 재화는 합산하고 나머지 아이템은 리스트로 보여주는 SummonEventPoint도 있다.
	public void RefreshReward(int gold, int dia, int energy, List<string> listItemValue, List<int> listItemCount, System.Action okAction = null)
	{
		_okAction = okAction;

		_listCommonRewardData.Clear();

		CommonRewardData commonRewardData = null;

		if (gold > 0)
		{
			commonRewardData = new CommonRewardData();
			commonRewardData.type = "cu";
			commonRewardData.value = "GO";
			commonRewardData.count = gold;
			_listCommonRewardData.Add(commonRewardData);
		}

		if (dia > 0)
		{
			commonRewardData = new CommonRewardData();
			commonRewardData.type = "cu";
			commonRewardData.value = "DI";
			commonRewardData.count = dia;
			_listCommonRewardData.Add(commonRewardData);
		}

		if (energy > 0)
		{
			commonRewardData = new CommonRewardData();
			commonRewardData.type = "cu";
			commonRewardData.value = "EN";
			commonRewardData.count = energy;
			_listCommonRewardData.Add(commonRewardData);
		}

		if (listItemValue.Count == listItemCount.Count)
		{
			for (int i = 0; i < listItemValue.Count; ++i)
			{
				commonRewardData = new CommonRewardData();
				commonRewardData.type = "it";
				commonRewardData.value = listItemValue[i];
				commonRewardData.count = listItemCount[i];
				_listCommonRewardData.Add(commonRewardData);
			}
		}

		if (_listCommonRewardData.Count == 0)
		{
			gameObject.SetActive(false);
			return;
		}

		Timing.RunCoroutine(RewardProcess());
	}

	// 이런식으로 하나짜리 받을 수도 있을거고
	public void RefreshReward(string type, string value, int count, System.Action okAction = null)
	{
		_okAction = okAction;

		_listCommonRewardData.Clear();

		CommonRewardData commonRewardData = new CommonRewardData();
		commonRewardData.type = type;
		commonRewardData.value = value;
		commonRewardData.count = count;
		_listCommonRewardData.Add(commonRewardData);

		Timing.RunCoroutine(RewardProcess());
	}

	// 테이블 행 하나를 받아서 처리하는 경우도 있을거다.
	public void RefreshReward(ShopProductTableData shopProductTableData, System.Action okAction = null)
	{
		// 필요에 따라 변환해서 사용하면 될듯
		_okAction = okAction;

		_listCommonRewardData.Clear();

		CommonRewardData commonRewardData = null;

		if (string.IsNullOrEmpty(shopProductTableData.rewardType1) == false)
		{
			commonRewardData = new CommonRewardData();
			commonRewardData.type = shopProductTableData.rewardType1;
			commonRewardData.value = shopProductTableData.rewardValue1;
			commonRewardData.count = shopProductTableData.rewardCount1;
			_listCommonRewardData.Add(commonRewardData);
		}

		if (string.IsNullOrEmpty(shopProductTableData.rewardType2) == false)
		{
			commonRewardData = new CommonRewardData();
			commonRewardData.type = shopProductTableData.rewardType2;
			commonRewardData.value = shopProductTableData.rewardValue2;
			commonRewardData.count = shopProductTableData.rewardCount2;
			_listCommonRewardData.Add(commonRewardData);
		}

		if (string.IsNullOrEmpty(shopProductTableData.rewardType3) == false)
		{
			commonRewardData = new CommonRewardData();
			commonRewardData.type = shopProductTableData.rewardType3;
			commonRewardData.value = shopProductTableData.rewardValue3;
			commonRewardData.count = shopProductTableData.rewardCount3;
			_listCommonRewardData.Add(commonRewardData);
		}

		if (string.IsNullOrEmpty(shopProductTableData.rewardType4) == false)
		{
			commonRewardData = new CommonRewardData();
			commonRewardData.type = shopProductTableData.rewardType4;
			commonRewardData.value = shopProductTableData.rewardValue4;
			commonRewardData.count = shopProductTableData.rewardCount4;
			_listCommonRewardData.Add(commonRewardData);
		}

		if (string.IsNullOrEmpty(shopProductTableData.rewardType5) == false)
		{
			commonRewardData = new CommonRewardData();
			commonRewardData.type = shopProductTableData.rewardType5;
			commonRewardData.value = shopProductTableData.rewardValue5;
			commonRewardData.count = shopProductTableData.rewardCount5;
			_listCommonRewardData.Add(commonRewardData);
		}

		if (_listCommonRewardData.Count == 0)
		{
			gameObject.SetActive(false);
			return;
		}

		Timing.RunCoroutine(RewardProcess());
	}

	IEnumerator<float> RewardProcess()
	{
		_processed = true;

		// 초기화
		toastBackImageRectTransform.gameObject.SetActive(false);
		titleLineObject.SetActive(false);
		exitObject.SetActive(false);

		for (int i = 0; i < _listRewardIcon.Count; ++i)
			_listRewardIcon[i].gameObject.SetActive(false);
		_listRewardIcon.Clear();

		// 0.1초 초기화 대기 후 시작
		yield return Timing.WaitForSeconds(0.1f);
		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		titleLineObject.SetActive(true);

		// list
		for (int i = 0; i < _listCommonRewardData.Count; ++i)
		{
			RewardIcon rewardIconItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			rewardIconItem.RefreshReward(_listCommonRewardData[i].type, _listCommonRewardData[i].value, _listCommonRewardData[i].count);
			_listRewardIcon.Add(rewardIconItem);
			yield return Timing.WaitForSeconds((_listCommonRewardData.Count < 10) ? 0.2f : 0.1f);
		}

		exitObject.SetActive(true);

		// 자꾸 exit가 보이는데도 안눌러진다고 해서 위로 올려둔다.
		_processed = false;
	}

	bool _processed = false;
	public void OnClickBackButton()
	{
		if (_processed)
			return;

		OnClickExitButton();
	}

	public void OnClickExitButton()
	{
		toastBackImageRectTransform.gameObject.SetActive(false);
		titleLineObject.SetActive(false);
		exitObject.SetActive(false);
		gameObject.SetActive(false);
		if (_okAction != null)
			_okAction();
	}
}