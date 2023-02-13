using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaEquipInfoCanvas : MonoBehaviour
{
	public static GachaEquipInfoCanvas instance;

	public bool showPickUpEquipInfo;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<GachaEquipInfoCanvasListItem>
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
		RefreshGrid();
	}

	List<GachaEquipInfoCanvasListItem> _listGachaEquipInfoCanvasListItem = new List<GachaEquipInfoCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listGachaEquipInfoCanvasListItem.Count; ++i)
			_listGachaEquipInfoCanvasListItem[i].gameObject.SetActive(false);
		_listGachaEquipInfoCanvasListItem.Clear();

		if (showPickUpEquipInfo)
		{
			// 여기까지 들어왔다는건 픽업 정보가 있다는거다.
			CashShopData.PickUpEquipInfo info = CashShopData.instance.GetCurrentPickUpEquipInfo();
			if (info != null)
			{
				// 픽업 장비는 보라일테고 가중치를 반만큼 가져갈테니 
				// 0번 항목에서 반을 구해온다.
				// 캐릭과 달리 추가확률도 적용할 수 있다.
				float firstProb = 0.0f;
				if (TableDataManager.instance.gachaEquipTable.dataArray.Length > 0)
					firstProb = TableDataManager.instance.gachaEquipTable.dataArray[0].prob;
				firstProb += info.add;

				GachaEquipInfoCanvasListItem gachaEquipInfoCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
				gachaEquipInfoCanvasListItem.Initialize(info.id, firstProb * 0.5f);
				_listGachaEquipInfoCanvasListItem.Add(gachaEquipInfoCanvasListItem);
			}
		}

		for (int i = 0; i < TableDataManager.instance.gachaEquipTable.dataArray.Length; ++i)
		{
			GachaEquipInfoCanvasListItem gachaEquipInfoCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			gachaEquipInfoCanvasListItem.Initialize(TableDataManager.instance.gachaEquipTable.dataArray[i]);
			_listGachaEquipInfoCanvasListItem.Add(gachaEquipInfoCanvasListItem);
		}
	}
}