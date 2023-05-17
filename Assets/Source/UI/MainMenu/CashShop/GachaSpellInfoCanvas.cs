using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaSpellInfoCanvas : MonoBehaviour
{
	public static GachaSpellInfoCanvas instance;

	public Text skillTotalLevelText;
	public Text currentSkillTotalLevelText;
	public Text nextRemainCountText;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<GachaSpellInfoCanvasListItem>
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

	List<int> _listSpellGachaLevelAccumulatedCount = new List<int>();
	int _currentProbIndex;
	void OnEnable()
	{
		string spellGachaLevelAccumulatedCount = BattleInstanceManager.instance.GetCachedGlobalConstantString("SpellGachaLevelAccumulatedCount");
		if (_listSpellGachaLevelAccumulatedCount.Count == 0)
			StringUtil.SplitIntList(spellGachaLevelAccumulatedCount, ref _listSpellGachaLevelAccumulatedCount);

		int gachaStepIndex = -1;
		for (int i = _listSpellGachaLevelAccumulatedCount.Count - 1; i >= 0; --i)
		{
			if (SpellManager.instance.GetSumSpellCount() >= _listSpellGachaLevelAccumulatedCount[i])
			{
				gachaStepIndex = i;
				break;
			}
		}
		if (gachaStepIndex == -1)
			return;

		_currentProbIndex = gachaStepIndex;
		_baseLevelIndex = gachaStepIndex;

		RefreshSpellLevel();
		RefreshGrid();
	}

	int _baseLevelIndex = 0;
	void RefreshSpellLevel()
	{
		/*
		int nextValue = 0;
		int nextIndex = _currentProbIndex + 1;
		if (nextIndex < _listTotalSpellGachaStep.Count)
			nextValue = _listTotalSpellGachaStep[nextIndex];
		else
			nextValue = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxTotalSpellLevel");

		string leftString = UIString.instance.GetString("GameUI_Lv", _listTotalSpellGachaStep[_currentProbIndex]);
		string rightString = UIString.instance.GetString("GameUI_Lv", nextValue - 1);
		skillTotalLevelText.text = string.Format("{0}  -  {1}", leftString, rightString);
		*/

		skillTotalLevelText.text = UIString.instance.GetString("GameUI_Lv", _currentProbIndex + 1);


		currentSkillTotalLevelText.text = UIString.instance.GetString("GameUI_Lv", _baseLevelIndex + 1);
		nextRemainCountText.gameObject.SetActive(false);

		if (_baseLevelIndex < _listSpellGachaLevelAccumulatedCount.Count - 1)
		{
			int remainCount = _listSpellGachaLevelAccumulatedCount[_baseLevelIndex + 1] - SpellManager.instance.GetSumSpellCount();
			nextRemainCountText.SetLocalizedText(UIString.instance.GetString("SpellUI_NextRequired", remainCount));
			nextRemainCountText.gameObject.SetActive(true);
		}
	}

	List<GachaSpellInfoCanvasListItem> _listGachaSpellInfoCanvasListItem = new List<GachaSpellInfoCanvasListItem>();
	public void RefreshGrid()
	{
		for (int i = 0; i < _listGachaSpellInfoCanvasListItem.Count; ++i)
			_listGachaSpellInfoCanvasListItem[i].gameObject.SetActive(false);
		_listGachaSpellInfoCanvasListItem.Clear();

		for (int i = 0; i < TableDataManager.instance.gachaSpellTable.dataArray.Length; ++i)
		{
			GachaSpellInfoCanvasListItem gachaSpellInfoCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			gachaSpellInfoCanvasListItem.Initialize(TableDataManager.instance.gachaSpellTable.dataArray[i], _currentProbIndex);
			_listGachaSpellInfoCanvasListItem.Add(gachaSpellInfoCanvasListItem);
		}
	}

	public void OnClickLeftButton()
	{
		if (_currentProbIndex > 0)
		{
			_currentProbIndex -= 1;
			RefreshSpellLevel();
			RefreshGrid();
		}
	}

	public void OnClickRightButton()
	{
		if (_currentProbIndex < _listSpellGachaLevelAccumulatedCount.Count - 1)
		{
			_currentProbIndex += 1;
			RefreshSpellLevel();
			RefreshGrid();
		}
	}
}
