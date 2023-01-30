using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;

public class SpellCanvasListItem : MonoBehaviour
{
	public SkillIcon skillIcon;
	public Text levelText;
	public Text nameText;
	public Transform tooltipRootTransform;
	public Text atkText;
	public Text[] noGainGrayTextList;
	public Image[] noGainGrayImageList;
	public Text proceedingCountText;
	public Text levelUpCostText;
	public GameObject maxReachedTextObject;
	public GameObject blinkObject;
	public RectTransform alarmRootTransform;

	string _id = "";
	bool _noGain = false;
	ObscuredInt _level;
	SpellData _spellData;
	SkillTableData _skillTableData;
	public void Initialize(SpellData spellData, SkillTableData skillTableData)
	{
		_id = spellData.spellId;
		_level = spellData.level;
		_noGain = false;
		_spellData = spellData;
		_skillTableData = skillTableData;

		// 안구해질리 없을거다.
		SkillProcessor.SkillInfo skillInfo = BattleInstanceManager.instance.playerActor.skillProcessor.GetSpellInfo(_id);
		SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(_id, 1);
		SpellGradeLevelTableData spellGradeLevelTableData = TableDataManager.instance.FindSpellGradeLevelTableData(skillTableData.grade, skillTableData.star, spellData.level);
		if (skillInfo == null || skillLevelTableData == null || spellGradeLevelTableData == null)
			return;

		skillIcon.SetInfo(skillTableData, false);
		RefreshInfo(skillInfo.iconPrefab, skillInfo.nameId, skillInfo.descriptionId, skillInfo.cooltime, skillTableData.maxLevel, skillLevelTableData, spellGradeLevelTableData);

		for (int i = 0; i < noGainGrayTextList.Length; ++i)
			noGainGrayTextList[i].color = Color.white;
		for (int i = 0; i < noGainGrayImageList.Length; ++i)
			noGainGrayImageList[i].color = Color.white;
	}

	public void InitializeForNoGain(SkillTableData skillTableData, SkillLevelTableData skillLevelTableData, SpellGradeLevelTableData spellGradeLevelTableData)
	{
		_noGain = true;
		_skillTableData = skillTableData;
		skillIcon.SetInfo(skillTableData, true);
		RefreshInfo(skillTableData.iconPrefab,
			skillTableData.useNameIdOverriding ? skillLevelTableData.nameId : skillTableData.nameId,
			skillTableData.useDescriptionIdOverriding ? skillLevelTableData.descriptionId : skillTableData.descriptionId,
			skillTableData.useCooltimeOverriding ? skillLevelTableData.cooltime : skillTableData.cooltime,
			skillTableData.maxLevel,
			skillLevelTableData,
			spellGradeLevelTableData);

		for (int i = 0; i < noGainGrayTextList.Length; ++i)
			noGainGrayTextList[i].color = Color.gray;
		for (int i = 0; i < noGainGrayImageList.Length; ++i)
			noGainGrayImageList[i].color = Color.gray;
	}

	void RefreshInfo(string iconPrefabAddress, string nameId, string descriptionId, float cooltime, int maxLevel, SkillLevelTableData skillLevelTableData, SpellGradeLevelTableData spellGradeLevelTableData)
	{
		atkText.text = spellGradeLevelTableData.accumulatedAtk.ToString("N0");

		nameText.SetLocalizedText(UIString.instance.GetString(nameId));
		_descString = UIString.instance.GetString(descriptionId, skillLevelTableData.parameter);
		_cooltime = cooltime;

		AlarmObject.Hide(alarmRootTransform);
		if (spellGradeLevelTableData.level >= maxLevel)
		{
			levelText.text = UIString.instance.GetString("GameUI_LevelPackMaxLv");

			int overCount = 0;
			if (_spellData != null) overCount = _spellData.count - spellGradeLevelTableData.requiredAccumulatedPowerPoint;
			proceedingCountText.text = string.Format("Over:  +{0:N0}", overCount);
			levelUpCostText.gameObject.SetActive(false);
			maxReachedTextObject.SetActive(true);
		}
		else
		{
			levelText.text = UIString.instance.GetString("GameUI_LevelPackLv", spellGradeLevelTableData.level);

			int count = 0;
			if (_spellData != null) count = _spellData.count - spellGradeLevelTableData.requiredAccumulatedPowerPoint;
			int maxCount = 0;
			if (spellGradeLevelTableData != null)
			{
				SpellGradeLevelTableData nextSpellGradeLevelTableData = TableDataManager.instance.FindSpellGradeLevelTableData(spellGradeLevelTableData.grade, spellGradeLevelTableData.star, spellGradeLevelTableData.level + 1);
				maxCount = nextSpellGradeLevelTableData.requiredPowerPoint;
				levelUpCostText.text = nextSpellGradeLevelTableData.requiredGold.ToString("N0");
				levelUpCostText.gameObject.SetActive(true);
				maxReachedTextObject.SetActive(false);
				_price = nextSpellGradeLevelTableData.requiredGold;
				_needAccumulatedCount = nextSpellGradeLevelTableData.requiredAccumulatedPowerPoint;

				if (CurrencyData.instance.gold >= _price && count >= maxCount)
					AlarmObject.Show(alarmRootTransform);
			}
			proceedingCountText.text = string.Format("{0:N0} / {1:N0}", count, maxCount);
		}
	}

	string _descString = "";
	float _cooltime;
	public void OnClickDetailButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("SpellInfoCanvas", () =>
		{
			SpellInfoCanvas.instance.SetInfo(_skillTableData, levelText.text, nameText.text, _descString, _cooltime);
		});
	}

	ObscuredInt _needAccumulatedCount;
	ObscuredInt _price;
	public void OnClickLevelUpButton()
	{
		if (_noGain)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("SpellUI_NoGainSkill"), 2.0f);
			return;
		}

		float prevValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		PlayFabApiManager.instance.RequestLevelUpSpell(_spellData, (_level + 1), () =>
		{
			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

			blinkObject.SetActive(true);
			Initialize(_spellData, _skillTableData);
			//MainCanvas.instance.RefreshLevelPassAlarmObject();

			// 변경 완료를 알리고
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(prevValue, nextValue);
			});
		});
	}



	#region Press LevelUp
	// 홀드로 레벨업 할땐 클릭으로 할때와 다르게 클라에서 선처리 해야한다. CharacterLevelCanvas에서 하던거 가져와서 prev로 필요한 것들만 추려서 쓴다.
	float _prevCombatValue;
	int _prevSpellLevel;
	int _prevGold;
	int _levelUpCount;
	bool _pressed = false;
	public void OnPressInitialize()
	{
		// 패킷에 전송할만한 초기화 내용을 기억해둔다.
		_prevCombatValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		_prevSpellLevel = _level;
		_prevGold = CurrencyData.instance.gold;
		_levelUpCount = 0;
		_pressed = true;
	}

	public void OnPressLevelUp()
	{
		if (_pressed == false)
			return;

		if (_noGain)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("SpellUI_NoGainSkill"), 2.0f);
			if (_pressed)
			{
				OnPressUpSync();
				_pressed = false;
			}
			return;
		}

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

		if (_spellData.count < _needAccumulatedCount)
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
		if (_spellData.level >= _skillTableData.maxLevel)
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
		_spellData.OnLevelUp(_spellData.level + 1);
		blinkObject.SetActive(false);
		blinkObject.SetActive(true);
		Initialize(_spellData, _skillTableData);
		SpellCanvas.instance.currencySmallInfo.RefreshInfo();
	}
	
	public void OnPressUpSync()
	{
		if (_pressed == false)
			return;
		_pressed = false;

		if (_noGain)
			return;
		if (_levelUpCount == 0)
			return;
		if (_prevSpellLevel > _spellData.level)
			return;
		if (_prevGold < CurrencyData.instance.gold)
			return;

		PlayFabApiManager.instance.RequestSpellPressLevelUp(_spellData, _prevSpellLevel, _prevGold, _spellData.level, CurrencyData.instance.gold, _levelUpCount, () =>
		{
			MainCanvas.instance.RefreshSpellAlarmObject();

			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(_prevCombatValue, nextValue);
			});
		});
	}
	#endregion





	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}