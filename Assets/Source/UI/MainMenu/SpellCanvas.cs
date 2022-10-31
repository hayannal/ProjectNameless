using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;

public class SpellCanvas : ResearchShowCanvasBase
{
	public static SpellCanvas instance;

	public GameObject spellGroundObjectPrefab;

	public CurrencySmallInfo currencySmallInfo;
	public Transform skillTotalLevelButtonTransform;
	public Slider proceedingCountSlider;
	public Text proceedingCountText;
	public Text skillTotalLevelText;
	public Text skillTotalLevelValueText;
	public Text skillTotalLevelUpCostText;
	public GameObject maxReachedTextObject;
	public GameObject blinkObject;
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

	ObscuredInt _price;
	ObscuredInt _needAccumulatedCount;
	void RefreshSpellLevel()
	{
		SpellTotalTableData spellTotalTableData = TableDataManager.instance.FindSpellTotalTableData(SpellManager.instance.spellTotalLevel);
		skillTotalLevelValueText.text = spellTotalTableData.accumulatedAtk.ToString("N0");

		AlarmObject.Hide(alarmRootTransform);
		if (SpellManager.instance.spellTotalLevel >= TableDataManager.instance.GetGlobalConstantInt("MaxTotalSkillLevel"))
		{
			// level
			skillTotalLevelText.text = string.Format("Lv. {0:N0}", "Max");

			int overCount = SpellManager.instance.GetSumSpellCount() - spellTotalTableData.requiredAccumulatedCount;
			proceedingCountText.text = string.Format("Over:  +{0:N0}", overCount);
			skillTotalLevelUpCostText.gameObject.SetActive(false);
			maxReachedTextObject.SetActive(true);
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
				proceedingCountSlider.value = (float)current / nextSpellTotalTableData.requiredCount;
				skillTotalLevelUpCostText.text = nextSpellTotalTableData.requiredGold.ToString("N0");
				skillTotalLevelUpCostText.gameObject.SetActive(true);
				maxReachedTextObject.SetActive(false);
				_price = nextSpellTotalTableData.requiredGold;
				_needAccumulatedCount = nextSpellTotalTableData.requiredAccumulatedCount;

				if (CurrencyData.instance.gold >= _price && current >= nextSpellTotalTableData.requiredCount)
					AlarmObject.Show(alarmRootTransform);
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

	#region Press LevelUp
	// 홀드로 레벨업 할땐 클릭으로 할때와 다르게 클라에서 선처리 해야한다. CharacterLevelCanvas에서 하던거 가져와서 prev로 필요한 것들만 추려서 쓴다.
	float _prevCombatValue;
	int _prevTotalSpellLevel;
	int _prevGold;
	int _levelUpCount;
	bool _pressed = false;
	public void OnPressInitialize()
	{
		// 패킷에 전송할만한 초기화 내용을 기억해둔다.
		_prevCombatValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		_prevTotalSpellLevel = SpellManager.instance.spellTotalLevel;
		_prevGold = CurrencyData.instance.gold;
		_levelUpCount = 0;
		_pressed = true;
	}

	public void OnPressLevelUp()
	{
		if (_pressed == false)
			return;

		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			if (_pressed)
			{
				OnPressUpSync();
				_pressed = false;
			}
			return;
		}

		if (SpellManager.instance.GetSumSpellCount() < _needAccumulatedCount)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughSpellCount"), 2.0f);
			if (_pressed)
			{
				OnPressUpSync();
				_pressed = false;
			}
			return;
		}

		// 맥스 넘어가는거도 막아놔야한다.
		if (SpellManager.instance.spellTotalLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxTotalSkillLevel"))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
			if (_pressed)
			{
				OnPressUpSync();
				_pressed = false;
			}
			return;
		}

		_levelUpCount += 1;
		CurrencyData.instance.gold -= _price;
		SpellManager.instance.OnLevelUpTotalSpell(SpellManager.instance.spellTotalLevel + 1);
		blinkObject.SetActive(false);
		blinkObject.SetActive(true);
		RefreshSpellLevel();
		currencySmallInfo.RefreshInfo();
	}

	public void OnPressUpSync()
	{
		if (_pressed == false)
			return;
		_pressed = false;

		if (_levelUpCount == 0)
			return;
		if (_prevTotalSpellLevel > SpellManager.instance.spellTotalLevel)
			return;
		if (_prevGold < CurrencyData.instance.gold)
			return;

		PlayFabApiManager.instance.RequestTotalSpellPressLevelUp(_prevTotalSpellLevel, _prevGold, SpellManager.instance.spellTotalLevel, CurrencyData.instance.gold, _levelUpCount, () =>
		{
			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(_prevCombatValue, nextValue);
			});
		});
	}
	#endregion
}