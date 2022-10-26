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
	public Slider proceedingCountSlider;
	public Text proceedingCountText;
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
		SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(_id, spellData.level);
		SpellGradeLevelTableData spellGradeLevelTableData = TableDataManager.instance.FindSpellGradeLevelTableData(skillTableData.grade, skillTableData.star, spellData.level);
		if (skillInfo == null || skillLevelTableData == null || spellGradeLevelTableData == null)
			return;

		skillIcon.SetInfo(skillTableData, false);
		RefreshInfo(skillInfo.iconPrefab, skillInfo.nameId, skillInfo.descriptionId, skillLevelTableData, spellGradeLevelTableData);

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
			skillLevelTableData,
			spellGradeLevelTableData);

		for (int i = 0; i < noGainGrayTextList.Length; ++i)
			noGainGrayTextList[i].color = Color.gray;
		for (int i = 0; i < noGainGrayImageList.Length; ++i)
			noGainGrayImageList[i].color = Color.gray;

		proceedingCountSlider.value = 0.0f;
	}

	void RefreshInfo(string iconPrefabAddress, string nameId, string descriptionId, SkillLevelTableData skillLevelTableData, SpellGradeLevelTableData spellGradeLevelTableData)
	{
		atkText.text = spellGradeLevelTableData.accumulatedAtk.ToString("N0");

		levelText.text = UIString.instance.GetString("GameUI_LevelPackLv", spellGradeLevelTableData.level);
		nameText.SetLocalizedText(UIString.instance.GetString(nameId));
		_descString = UIString.instance.GetString(descriptionId, skillLevelTableData.parameter);

		int count = 0;
		if (_spellData != null) count = _spellData.count;
		proceedingCountText.text = string.Format("{0:N0} / {1:N0}", count, 20);
	}

	string _descString = "";
	public void OnClickDetailButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("SpellInfoCanvas", () =>
		{
			SpellInfoCanvas.instance.SetInfo(_skillTableData, levelText.text, nameText.text, _descString);
		});
	}

	public void OnClickLevelUpButton()
	{
		if (_noGain)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("SpellUI_NoGainSkill"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestLevelUpSpell(_spellData, (_level + 1), () =>
		{
			Initialize(_spellData, _skillTableData);
			//MainCanvas.instance.RefreshLevelPassAlarmObject();
		});
	}





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