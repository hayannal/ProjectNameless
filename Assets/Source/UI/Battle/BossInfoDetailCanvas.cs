using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ActorStatusDefine;

public class BossInfoDetailCanvas : MonoBehaviour
{
	public static BossInfoDetailCanvas instance;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<EquipStatusDetailCanvasListItem>
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

	List<EquipStatusDetailCanvasListItem> _listEquipStatusDetailCanvasListItem = new List<EquipStatusDetailCanvasListItem>();
	void OnEnable()
	{
		for (int i = 0; i < _listEquipStatusDetailCanvasListItem.Count; ++i)
			_listEquipStatusDetailCanvasListItem[i].gameObject.SetActive(false);
		_listEquipStatusDetailCanvasListItem.Clear();

		// 4가지 속성만 있으면 될거같다.
		// currentMonstrStandardDef
		// currentMonsterEvadeRate
		// currentMonsterCriticalDefenseRate
		// currentMonsterStrikeDefenseRate

		Color valueTextColor = new Color(0.9f, 0.1f, 0.1f);

		float diff = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(eActorStatus.Attack) - StageManager.instance.currentMonstrStandardDef;
		float maxHp = StageManager.instance.currentMonstrStandardHp;
		float minValue = maxHp * BattleInstanceManager.instance.GetCachedGlobalConstantInt("RepeatDamageMinValue10000") * 0.0001f;
		if (diff < minValue || diff < 1.5f)
		{
			EquipStatusDetailCanvasListItem equipStatusDetailCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipStatusDetailCanvasListItem.Initialize(eActorStatus.Defense, StageManager.instance.currentMonstrStandardDef);
			equipStatusDetailCanvasListItem.valueText.color = valueTextColor;
			_listEquipStatusDetailCanvasListItem.Add(equipStatusDetailCanvasListItem);
		}

		if (StageManager.instance.currentMonsterEvadeRate > 0.0f)
		{
			EquipStatusDetailCanvasListItem equipStatusDetailCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipStatusDetailCanvasListItem.Initialize(eActorStatus.EvadeRate, StageManager.instance.currentMonsterEvadeRate);
			equipStatusDetailCanvasListItem.valueText.color = valueTextColor;
			_listEquipStatusDetailCanvasListItem.Add(equipStatusDetailCanvasListItem);
		}

		if (StageManager.instance.currentMonsterCriticalDefenseRate > 0.0f)
		{
			EquipStatusDetailCanvasListItem equipStatusDetailCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipStatusDetailCanvasListItem.Initialize(eActorStatus.CriticalDefenseRate, StageManager.instance.currentMonsterCriticalDefenseRate);
			equipStatusDetailCanvasListItem.valueText.color = valueTextColor;
			_listEquipStatusDetailCanvasListItem.Add(equipStatusDetailCanvasListItem);
		}

		if (StageManager.instance.currentMonsterStrikeDefenseRate > 0.0f)
		{
			EquipStatusDetailCanvasListItem equipStatusDetailCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			equipStatusDetailCanvasListItem.Initialize(eActorStatus.StrikeDefenseRate, StageManager.instance.currentMonsterStrikeDefenseRate);
			equipStatusDetailCanvasListItem.valueText.color = valueTextColor;
			_listEquipStatusDetailCanvasListItem.Add(equipStatusDetailCanvasListItem);
		}
	}
}