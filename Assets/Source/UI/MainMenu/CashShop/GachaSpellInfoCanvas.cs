using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaSpellInfoCanvas : MonoBehaviour
{
	public static GachaSpellInfoCanvas instance;

	public Text skillTotalLevelText;

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

	List<int> _listTotalSpellGachaStep = new List<int>();
	int _currentProbIndex;
	void OnEnable()
	{
		string totalSpellGachaStep = BattleInstanceManager.instance.GetCachedGlobalConstantString("TotalSpellGachaStep");
		if (_listTotalSpellGachaStep.Count == 0)
			StringUtil.SplitIntList(totalSpellGachaStep, ref _listTotalSpellGachaStep);

		int gachaStepIndex = -1;
		for (int i = _listTotalSpellGachaStep.Count - 1; i >= 0; --i)
		{
			if (SpellManager.instance.spellTotalLevel >= _listTotalSpellGachaStep[i])
			{
				gachaStepIndex = i;
				break;
			}
		}
		if (gachaStepIndex == -1)
			return;

		_currentProbIndex = gachaStepIndex;

		RefreshSpellLevel();
		RefreshGrid();
	}

	void RefreshSpellLevel()
	{
		int nextValue = 0;
		int nextIndex = _currentProbIndex + 1;
		if (nextIndex < _listTotalSpellGachaStep.Count)
			nextValue = _listTotalSpellGachaStep[nextIndex];
		else
			nextValue = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxTotalSpellLevel");

		string leftString = UIString.instance.GetString("GameUI_Lv", _listTotalSpellGachaStep[_currentProbIndex]);
		string rightString = UIString.instance.GetString("GameUI_Lv", nextValue);
		skillTotalLevelText.text = string.Format("{0}  -  {1}", leftString, rightString);
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
		if (_currentProbIndex < _listTotalSpellGachaStep.Count - 1)
		{
			_currentProbIndex += 1;
			RefreshSpellLevel();
			RefreshGrid();
		}
	}
}
