using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using MEC;

public class EquipBoxResultCanvas : MonoBehaviour
{
	public static EquipBoxResultCanvas instance;

	public EquipListStatusInfo materialSmallStatusInfo;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<EquipBoxResultCanvasListItem>
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

	void OnDisable()
	{
		// 부모인 RandomBoxScreenCanvas가 닫힐때 함께 닫히도록 한다.
		gameObject.SetActive(false);

		// 작은 정보창도 함께 닫으면 된다.
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

	public void ShowResult(List<ItemInstance> listItemInstance)
	{
		Timing.RunCoroutine(ShowGridProcess(listItemInstance));
	}

	List<EquipBoxResultCanvasListItem> _listEquipBoxResultCanvasListItem = new List<EquipBoxResultCanvasListItem>();
	IEnumerator<float> ShowGridProcess(List<ItemInstance> listItemInstance)
	{
		for (int i = 0; i < _listEquipBoxResultCanvasListItem.Count; ++i)
			_listEquipBoxResultCanvasListItem[i].gameObject.SetActive(false);
		_listEquipBoxResultCanvasListItem.Clear();

		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			EquipLevelTableData equipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(listItemInstance[i].ItemId);
			if (equipLevelTableData == null)
				continue;
			EquipTableData equipTableData = EquipManager.instance.GetCachedEquipTableData(equipLevelTableData.equipGroup);
			if (equipTableData == null)
				continue;

			EquipBoxResultCanvasListItem equipBoxResultCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipBoxResultCanvasListItem.Initialize(listItemInstance[i], equipTableData);
			_listEquipBoxResultCanvasListItem.Add(equipBoxResultCanvasListItem);

			yield return Timing.WaitForSeconds(0.1f);
		}

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnEndGachaGridProcess();
	}

	public void ShowSmallEquipInfo(EquipData equipData)
	{
		if (equipData == null)
			return;

		ShowSmallEquipInfo(materialSmallStatusInfo, equipData);
		_materialSmallStatusInfoShowRemainTime = 2.0f;
	}

	public static void ShowSmallEquipInfo(EquipListStatusInfo smallStatusInfo, EquipData equipData)
	{
		smallStatusInfo.RefreshInfo(equipData, false);
		smallStatusInfo.detailShowButton.gameObject.SetActive(false);
		smallStatusInfo.lockButton.gameObject.SetActive(false);
		smallStatusInfo.unlockButton.gameObject.SetActive(false);
		smallStatusInfo.equipButtonObject.gameObject.SetActive(false);
		smallStatusInfo.unequipButtonObject.gameObject.SetActive(false);
		smallStatusInfo.gameObject.SetActive(false);
		smallStatusInfo.gameObject.SetActive(true);
	}
}