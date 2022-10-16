using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;

public class SpellCanvasListItem : MonoBehaviour
{
	public Transform iconPrefabRootTransform;
	public Text levelText;
	public Text nameText;
	public Transform tooltipRootTransform;
	public Text atkText;
	public Text[] noGainGrayTextList;
	public Image[] noGainGrayImageList;
	public Slider proceedingCountSlider;
	public Text proceedingCountText;
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
		if (skillInfo == null || skillLevelTableData == null)
			return;

		RefreshInfo(skillInfo.iconPrefab, skillInfo.nameId, skillInfo.descriptionId, skillLevelTableData);

		for (int i = 0; i < noGainGrayTextList.Length; ++i)
			noGainGrayTextList[i].color = Color.white;
		for (int i = 0; i < noGainGrayImageList.Length; ++i)
			noGainGrayImageList[i].color = Color.white;
	}

	public void InitializeForNoGain(SkillTableData skillTableData, SkillLevelTableData skillLevelTableData)
	{
		_noGain = true;
		RefreshInfo(skillTableData.iconPrefab,
			skillTableData.useNameIdOverriding ? skillLevelTableData.nameId : skillTableData.nameId,
			skillTableData.useDescriptionIdOverriding ? skillLevelTableData.descriptionId : skillTableData.descriptionId,
			skillLevelTableData);

		for (int i = 0; i < noGainGrayTextList.Length; ++i)
			noGainGrayTextList[i].color = Color.gray;
		for (int i = 0; i < noGainGrayImageList.Length; ++i)
			noGainGrayImageList[i].color = Color.gray;

		proceedingCountSlider.value = 0.0f;
	}

	GameObject _cachedImageObject;
	void RefreshInfo(string iconPrefabAddress, string nameId, string descriptionId, SkillLevelTableData skillLevelTableData)
	{
		atkText.text = skillLevelTableData.accumulatedAtk.ToString("N0");

		levelText.text = UIString.instance.GetString("GameUI_LevelPackLv", skillLevelTableData.level);
		nameText.SetLocalizedText(UIString.instance.GetString(nameId));
		_descString = UIString.instance.GetString(descriptionId, skillLevelTableData.parameter);

		int count = 0;
		if (_spellData != null) count = _spellData.count;
		proceedingCountText.text = string.Format("{0:N0} / {1:N0}", count, 20);

		if (_cachedImageObject != null)
		{
			_cachedImageObject.SetActive(false);
			_cachedImageObject = null;
		}

		if (string.IsNullOrEmpty(iconPrefabAddress) == false)
		{
			AddressableAssetLoadManager.GetAddressableGameObject(iconPrefabAddress, "Preview", (prefab) =>
			{
				_cachedImageObject = UIInstanceManager.instance.GetCachedObject(prefab, iconPrefabRootTransform);

				Coffee.UIExtensions.UIShiny shinyComponent = _cachedImageObject.GetComponentInChildren<Coffee.UIExtensions.UIShiny>();
				Image image = shinyComponent.GetComponent<Image>();
				shinyComponent.enabled = _noGain ? false : true;
				image.color = _noGain ? Color.gray : Color.white;
			});
		}
	}

	string _descString = "";
	public void OnClickDetailButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, _descString, 200, tooltipRootTransform, new Vector2(0.0f, 0.0f));
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