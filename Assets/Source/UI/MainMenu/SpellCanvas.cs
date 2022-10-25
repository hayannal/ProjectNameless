using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellCanvas : ResearchShowCanvasBase
{
	public static SpellCanvas instance;

	public GameObject spellGroundObjectPrefab;

	public Transform skillTotalLevelButtonTransform;
	public Slider proceedingCountSlider;
	public Text proceedingCountText;
	public Text skillTotalLevelText;
	public Text skillTotalLevelValueText;
	public RectTransform alarmRootTransform;

	public Transform separateLineTransform;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<SpellCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	GameObject _spellGroundObject;
	void Start()
	{
		contentItemPrefab.SetActive(false);

		_spellGroundObject = Instantiate<GameObject>(spellGroundObjectPrefab, _rootOffsetPosition, Quaternion.Euler(0.0f, -90.0f, 0.0f));
	}

	void OnEnable()
	{
		if (_spellGroundObject != null)
			_spellGroundObject.SetActive(true);

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (restore)
			return;

		SetInfoCameraMode(true);
		MainCanvas.instance.OnEnterCharacterMenu(true);

		// refresh
		RefreshSpellLevel();
		RefreshGrid();
	}

	void OnDisable()
	{
		_spellGroundObject.SetActive(false);

		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

		SetInfoCameraMode(false);
		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	void RefreshSpellLevel()
	{
		SpellTotalTableData spellTotalTableData = TableDataManager.instance.FindSpellTotalTableData(SpellManager.instance.spellTotalLevel);
		skillTotalLevelValueText.text = spellTotalTableData.accumulatedAtk.ToString("N0");

		if (SpellManager.instance.spellTotalLevel >= TableDataManager.instance.GetGlobalConstantInt("MaxTotalSkillLevel"))
		{
			// level
			skillTotalLevelText.text = string.Format("Lv. {0:N0}", "Max");
		}
		else
		{
			// level
			skillTotalLevelText.text = string.Format("Lv. {0:N0}", SpellManager.instance.spellTotalLevel);

			SpellTotalTableData nextSpellTotalTableData = TableDataManager.instance.FindSpellTotalTableData(SpellManager.instance.spellTotalLevel + 1);
			if (nextSpellTotalTableData != null)
			{
				// gauge
				int current = SpellManager.instance.GetSumSpellCount() - spellTotalTableData.requiredAccumulatedCount;				
				proceedingCountText.text = string.Format("{0:N0} / {1:N0}", current, nextSpellTotalTableData.requiredCount);
				proceedingCountSlider.value = (float)current / nextSpellTotalTableData.requiredAccumulatedCount;
			}
		}
	}

	List<SpellCanvasListItem> _listSpellCanvasListItem = new List<SpellCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listSpellCanvasListItem.Count; ++i)
			_listSpellCanvasListItem[i].gameObject.SetActive(false);
		_listSpellCanvasListItem.Clear();

		separateLineTransform.gameObject.SetActive(false);
		int noGainCount = 0;
		for (int i = 0; i < TableDataManager.instance.skillTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.skillTable.dataArray[i].spell == false)
				continue;

			SpellData spellData = SpellManager.instance.GetSpellData(TableDataManager.instance.skillTable.dataArray[i].id);
			if (spellData == null)
			{
				++noGainCount;
				continue;
			}

			SpellCanvasListItem spellCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			spellCanvasListItem.Initialize(spellData, TableDataManager.instance.skillTable.dataArray[i]);
			spellCanvasListItem.cachedTransform.SetAsLastSibling();
			_listSpellCanvasListItem.Add(spellCanvasListItem);
		}

		if (noGainCount == 0)
			return;

		separateLineTransform.gameObject.SetActive(true);
		separateLineTransform.SetAsLastSibling();

		for (int i = 0; i < TableDataManager.instance.skillTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.skillTable.dataArray[i].spell == false)
				continue;

			if (SpellManager.instance.GetSpellData(TableDataManager.instance.skillTable.dataArray[i].id) != null)
				continue;
			SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(TableDataManager.instance.skillTable.dataArray[i].id, 1);
			if (skillLevelTableData == null)
				continue;
			SpellGradeLevelTableData spellGradeLevelTableData = TableDataManager.instance.FindSpellGradeLevelTableData(TableDataManager.instance.skillTable.dataArray[i].grade, TableDataManager.instance.skillTable.dataArray[i].star, 1);
			if (spellGradeLevelTableData == null)
				continue;

			SpellCanvasListItem spellCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			spellCanvasListItem.InitializeForNoGain(TableDataManager.instance.skillTable.dataArray[i], skillLevelTableData, spellGradeLevelTableData);
			spellCanvasListItem.cachedTransform.SetAsLastSibling();
			_listSpellCanvasListItem.Add(spellCanvasListItem);
		}
	}

	public void OnClickTitleDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("SpellUI_TotalSkillLevelMore"), 300, skillTotalLevelButtonTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickLevelUpTotalSkill()
	{
		
	}
}