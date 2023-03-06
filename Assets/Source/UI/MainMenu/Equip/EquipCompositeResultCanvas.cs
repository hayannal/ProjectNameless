using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using MEC;

public class EquipCompositeResultCanvas : MonoBehaviour
{
	public static EquipCompositeResultCanvas instance;

	public RectTransform toastBackImageRectTransform;
	public GameObject titleLineObject;
	public GameObject exitObject;

	public EquipListStatusInfo materialSmallStatusInfo;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<EquipCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();
	
	System.Action _okAction;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	void OnDisable()
	{
		materialSmallStatusInfo.gameObject.SetActive(false);
	}

	float _materialSmallStatusInfoShowRemainTime;
	void Update()
	{
		if (_materialSmallStatusInfoShowRemainTime > 0.0f)
		{
			_materialSmallStatusInfoShowRemainTime -= Time.deltaTime;
			if (_materialSmallStatusInfoShowRemainTime <= 0.0f)
			{
				_materialSmallStatusInfoShowRemainTime = 0.0f;
				materialSmallStatusInfo.gameObject.SetActive(false);
			}
		}
	}

	List<EquipData> _listEquipData = new List<EquipData>();
	public void RefreshResult(List<ItemInstance> listItemInstance, System.Action okAction = null)
	{
		_okAction = okAction;

		_listEquipData.Clear();
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			EquipLevelTableData equipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(listItemInstance[i].ItemId);
			if (equipLevelTableData == null)
				continue;
			EquipTableData equipTableData = EquipManager.instance.GetCachedEquipTableData(equipLevelTableData.equipGroup);
			if (equipTableData == null)
				continue;
			EquipData equipData = EquipManager.instance.FindEquipData(listItemInstance[i].ItemInstanceId, (EquipManager.eEquipSlotType)equipTableData.equipType);
			if (equipData == null)
				continue;

			_listEquipData.Add(equipData);
		}

		if (_listEquipData.Count == 0)
		{
			gameObject.SetActive(false);
			return;
		}

		Timing.RunCoroutine(RewardProcess());
	}

	// 이런식으로 하나짜리 받을 수도 있을거고
	public void RefreshResult(EquipData equipData, System.Action okAction = null)
	{
		_okAction = okAction;

		_listEquipData.Clear();
		_listEquipData.Add(equipData);

		Timing.RunCoroutine(RewardProcess());
	}

	List<EquipCanvasListItem> _listEquipCanvasListItem = new List<EquipCanvasListItem>();
	IEnumerator<float> RewardProcess()
	{
		_processed = true;

		// 초기화
		toastBackImageRectTransform.gameObject.SetActive(false);
		titleLineObject.SetActive(false);
		exitObject.SetActive(false);

		for (int i = 0; i < _listEquipCanvasListItem.Count; ++i)
			_listEquipCanvasListItem[i].gameObject.SetActive(false);
		_listEquipCanvasListItem.Clear();

		// 0.1초 초기화 대기 후 시작
		yield return Timing.WaitForSeconds(0.1f);
		toastBackImageRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		titleLineObject.SetActive(true);

		// list
		for (int i = 0; i < _listEquipData.Count; ++i)
		{
			EquipCanvasListItem equipCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipCanvasListItem.Initialize(_listEquipData[i], OnClickListItem);
			_listEquipCanvasListItem.Add(equipCanvasListItem);
			yield return Timing.WaitForSeconds(0.2f);
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


	void OnClickListItem(EquipData equipData)
	{
		ShowSmallEquipInfo(equipData);
	}

	void ShowSmallEquipInfo(EquipData equipData)
	{
		if (equipData == null)
			return;

		EquipBoxResultCanvas.ShowSmallEquipInfo(materialSmallStatusInfo, equipData);
		_materialSmallStatusInfoShowRemainTime = 2.0f;
	}
}