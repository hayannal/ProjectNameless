using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaCharacterInfoCanvas : MonoBehaviour
{
	public static GachaCharacterInfoCanvas instance;

	public bool showPickUpCharacterInfo;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<GachaCharacterInfoCanvasListItem>
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

	List<GachaCharacterInfoCanvasListItem> _listGachaCharacterInfoCanvasListItem = new List<GachaCharacterInfoCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listGachaCharacterInfoCanvasListItem.Count; ++i)
			_listGachaCharacterInfoCanvasListItem[i].gameObject.SetActive(false);
		_listGachaCharacterInfoCanvasListItem.Clear();

		if (showPickUpCharacterInfo)
		{
			// 여기까지 들어왔다는건 픽업 정보가 있다는거다.
			CashShopData.PickUpCharacterInfo info = CashShopData.instance.GetCurrentPickUpCharacterInfo();
			if (info != null)
			{
				// 픽업 캐릭터는 전설일테고 가중치를 반만큼 가져갈테니 
				// 0번 항목에서 반을 구해온다.
				float firstProb = 0.0f;
				if (TableDataManager.instance.gachaActorTable.dataArray.Length > 0)
					firstProb = TableDataManager.instance.gachaActorTable.dataArray[0].prob;

				GachaCharacterInfoCanvasListItem gachaCharacterInfoCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
				gachaCharacterInfoCanvasListItem.Initialize(info.id, firstProb * 0.5f);
				_listGachaCharacterInfoCanvasListItem.Add(gachaCharacterInfoCanvasListItem);
			}
		}

		for (int i = 0; i < TableDataManager.instance.gachaActorTable.dataArray.Length; ++i)
		{
			GachaCharacterInfoCanvasListItem gachaCharacterInfoCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			gachaCharacterInfoCanvasListItem.Initialize(TableDataManager.instance.gachaActorTable.dataArray[i], i == 0 && showPickUpCharacterInfo);
			_listGachaCharacterInfoCanvasListItem.Add(gachaCharacterInfoCanvasListItem);
		}
	}
}