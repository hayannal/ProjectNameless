using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using MEC;

public class CharacterBoxResultCanvas : MonoBehaviour
{
	public static CharacterBoxResultCanvas instance;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<CharacterBoxResultCanvasListItem>
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
	}

	public void ShowResult(List<ItemInstance> listItemInstance)
	{
		Timing.RunCoroutine(ShowGridProcess(listItemInstance));
	}

	List<CharacterBoxResultCanvasListItem> _listCharacterBoxResultCanvasListItem = new List<CharacterBoxResultCanvasListItem>();
	IEnumerator<float> ShowGridProcess(List<ItemInstance> listItemInstance)
	{
		for (int i = 0; i < _listCharacterBoxResultCanvasListItem.Count; ++i)
			_listCharacterBoxResultCanvasListItem[i].gameObject.SetActive(false);
		_listCharacterBoxResultCanvasListItem.Clear();

		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(listItemInstance[i].ItemId);
			if (actorTableData == null)
				continue;

			CharacterBoxResultCanvasListItem characterBoxResultCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			characterBoxResultCanvasListItem.Initialize(listItemInstance[i], actorTableData);
			_listCharacterBoxResultCanvasListItem.Add(characterBoxResultCanvasListItem);

			yield return Timing.WaitForSeconds(0.1f);
		}

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.exitObject.SetActive(true);
	}
}