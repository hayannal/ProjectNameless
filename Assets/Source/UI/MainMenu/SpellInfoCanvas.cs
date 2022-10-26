using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpellInfoCanvas : MonoBehaviour
{
	public static SpellInfoCanvas instance;

	void Awake()
	{
		instance = this;
	}

	public SkillIcon skillIcon;
	public Text levelText;
	public Text nameText;
	public Text descText;

	public void SetInfo(SkillTableData skillTableData, string levelString, string nameString, string descString)
	{
		skillIcon.SetInfo(skillTableData, false);
		levelText.text = levelString;
		nameText.SetLocalizedText(nameString);
		descText.SetLocalizedText(descString);
	}
}