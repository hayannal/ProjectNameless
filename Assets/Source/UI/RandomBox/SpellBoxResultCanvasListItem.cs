using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class SpellBoxResultCanvasListItem : MonoBehaviour
{
	public SkillIcon skillIcon;
	public Text countText;
	public Text newText;

	SkillTableData _skillTableData;
	public void Initialize(ItemInstance itemInstance, SkillTableData skillTableData)
	{
		SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(skillTableData.id, 1);
		if (skillLevelTableData == null)
			return;
		SpellGradeLevelTableData spellGradeLevelTableData = TableDataManager.instance.FindSpellGradeLevelTableData(skillTableData.grade, skillTableData.star, 1);
		if (spellGradeLevelTableData == null)
			return;

		_skillTableData = skillTableData;
		skillIcon.SetInfo(skillTableData, false);

		_nameString = UIString.instance.GetString(skillTableData.useNameIdOverriding ? skillLevelTableData.nameId : skillTableData.nameId);
		_descString = UIString.instance.GetString(skillTableData.useDescriptionIdOverriding ? skillLevelTableData.descriptionId : skillTableData.descriptionId, skillLevelTableData.parameter);
		_cooltime = skillTableData.useCooltimeOverriding ? skillLevelTableData.cooltime : skillTableData.cooltime;

		countText.text = string.Format("X {0:N0}", itemInstance.UsesIncrementedBy);
		newText.gameObject.SetActive(false);

		if (itemInstance.UsesIncrementedBy == itemInstance.RemainingUses)
		{
			newText.SetLocalizedText(UIString.instance.GetString("ShopUI_NewCharacter"));
			newText.gameObject.SetActive(true);
		}
	}
	
	string _nameString = "";
	string _descString = "";
	float _cooltime;
	public void OnClickDetailButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("SpellInfoCanvas", () =>
		{
			SpellInfoCanvas.instance.SetInfo(_skillTableData, "", _nameString, _descString, _cooltime);
		});
	}
}