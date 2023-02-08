using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipSkillInfoCanvas : MonoBehaviour
{
	public static EquipSkillInfoCanvas instance;

	void Awake()
	{
		instance = this;
	}

	public Image equipSkillIconImage;
	public Text nameText;
	public Text descText;

	public Image equipSkillGradeCircleImage;
	public Text equipSkillGradeText;

	public void SetInfo(SkillTableData skillTableData, bool showGradeInfo, EquipTableData equipTableData, string nameString, string descString, float cooltime)
	{
		AddressableAssetLoadManager.GetAddressableSprite(skillTableData.iconPrefab, "Icon", (sprite) =>
		{
			equipSkillIconImage.sprite = null;
			equipSkillIconImage.sprite = sprite;
		});

		nameText.SetLocalizedText(nameString);

		string cooltimeString = UIString.instance.GetString("SpellUI_CoolTime", cooltime);
		descText.SetLocalizedText(string.Format("{0}\n\n{1}", descString, cooltimeString));

		equipSkillGradeText.gameObject.SetActive(showGradeInfo);
		if (showGradeInfo)
		{
			string gradeString = UIString.instance.GetString(string.Format("GameUI_EquipGrade{0}", equipTableData.skillActive));
			equipSkillGradeText.SetLocalizedText(UIString.instance.GetString("EquipUI_SkillActiveGrade", gradeString));
			equipSkillGradeCircleImage.color = EquipAltar.GetGradeOutlineColor(equipTableData.skillActive);
		}
	}

	public void OnClickBackground()
	{
		gameObject.SetActive(false);
	}
}