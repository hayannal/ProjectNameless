using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayFab.ClientModels;
using MEC;

public class SpellBoxResultCanvas : MonoBehaviour
{
	public static SpellBoxResultCanvas instance;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<SpellBoxResultCanvasListItem>
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

	List<SpellBoxResultCanvasListItem> _listSpellBoxResultCanvasListItem = new List<SpellBoxResultCanvasListItem>();
	IEnumerator<float> ShowGridProcess(List<ItemInstance> listItemInstance)
	{
		for (int i = 0; i < _listSpellBoxResultCanvasListItem.Count; ++i)
			_listSpellBoxResultCanvasListItem[i].gameObject.SetActive(false);
		_listSpellBoxResultCanvasListItem.Clear();

		for (int i = 0; i < listItemInstance.Count; ++i)
		{
			SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(listItemInstance[i].ItemId);
			if (skillTableData == null)
				continue;

			SpellBoxResultCanvasListItem spellBoxResultCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			spellBoxResultCanvasListItem.Initialize(listItemInstance[i], skillTableData);
			_listSpellBoxResultCanvasListItem.Add(spellBoxResultCanvasListItem);

			yield return Timing.WaitForSeconds(0.1f);
		}

		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnEndGachaGridProcess();
	}
}