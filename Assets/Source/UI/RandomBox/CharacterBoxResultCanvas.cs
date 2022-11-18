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

	public void ShowResult(List<ItemInstance> listItemInstance, List<string> listNewCharacterId, List<string> listTrpCharacterId)
	{
		// 로직상 호출 부분에서 미리 파싱해서 신캐와 Trp캐릭의 리스트를 넘겨준다.
		// 이거 따로 처리하고 pp만 추가로 보여주면 될거다.
		Timing.RunCoroutine(ShowGridProcess(listItemInstance, listNewCharacterId, listTrpCharacterId));
	}

	List<CharacterBoxResultCanvasListItem> _listCharacterBoxResultCanvasListItem = new List<CharacterBoxResultCanvasListItem>();
	IEnumerator<float> ShowGridProcess(List<ItemInstance> listItemInstance, List<string> listNewCharacterId, List<string> listTrpCharacterId)
	{
		for (int i = 0; i < _listCharacterBoxResultCanvasListItem.Count; ++i)
			_listCharacterBoxResultCanvasListItem[i].gameObject.SetActive(false);
		_listCharacterBoxResultCanvasListItem.Clear();

		for (int i = 0; i < listNewCharacterId.Count; ++i)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(listNewCharacterId[i]);
			if (actorTableData == null)
				continue;

			CharacterBoxResultCanvasListItem characterBoxResultCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			characterBoxResultCanvasListItem.InitializeForNewCharacter(true, actorTableData);
			_listCharacterBoxResultCanvasListItem.Add(characterBoxResultCanvasListItem);

			yield return Timing.WaitForSeconds(0.1f);
		}

		for (int i = 0; i < listTrpCharacterId.Count; ++i)
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(listTrpCharacterId[i]);
			if (actorTableData == null)
				continue;

			CharacterBoxResultCanvasListItem characterBoxResultCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			characterBoxResultCanvasListItem.InitializeForNewCharacter(false, actorTableData);
			_listCharacterBoxResultCanvasListItem.Add(characterBoxResultCanvasListItem);

			yield return Timing.WaitForSeconds(0.1f);
		}

		// 위에서 신캐와 Trp를 보여준 상태니 여기선 pp쪽만 처리하면 된다.
		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			if (listItemInstance[i].ItemId.Substring(listItemInstance[i].ItemId.Length - 2) != "pp")
				continue;

			string itemId = listItemInstance[i].ItemId.Substring(0, listItemInstance[i].ItemId.Length - 2);
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(itemId);
			if (actorTableData == null)
				continue;

			CharacterBoxResultCanvasListItem characterBoxResultCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			characterBoxResultCanvasListItem.InitializeForCharacterPp(listItemInstance[i], actorTableData);
			_listCharacterBoxResultCanvasListItem.Add(characterBoxResultCanvasListItem);

			yield return Timing.WaitForSeconds(0.1f);
		}

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.exitObject.SetActive(true);
	}
}