using System;
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

	public GameObject newGainObject;

	public SkillIcon skillIcon;
	public Text levelText;
	public Text nameText;
	public Text descText;

	public void SetInfo(SkillTableData skillTableData, string levelString, string nameString, string descString, float cooltime)
	{
		skillIcon.SetInfo(skillTableData, false);
		levelText.text = levelString;
		nameText.SetLocalizedText(nameString);

		string cooltimeString = UIString.instance.GetString("SkillUI_CoolTime", cooltime);
		descText.SetLocalizedText(string.Format("{0}\n\n{1}", descString, cooltimeString));
	}

	public void OnClickBackground()
	{
		#region New Show
		// 리스트 받아서 처리하고 있을땐 이렇게 처리한다.
		if (_listId.Count > 0)
		{
			if (_showIndex < _listId.Count)
			{
				RefreshNewInfo();
				return;
			}
			else
			{
				_listId.Clear();
				newGainObject.SetActive(false);
			}
		}

		if (_okAction != null)
		{
			_okAction();
			_okAction = null;
		}
		#endregion

		gameObject.SetActive(false);
	}

	#region New Show
	Action _okAction;
	int _showIndex;
	List<string> _listId = new List<string>();
	public void SetNewInfo(List<string> listNewSpellId, Action okAction)
	{
		_okAction = okAction;

		_showIndex = 0;
		_listId.Clear();
		for (int i = 0; i < listNewSpellId.Count; ++i)
			_listId.Add(listNewSpellId[i]);

		newGainObject.SetActive(true);
		RefreshNewInfo();
	}

	void RefreshNewInfo()
	{
		if (_showIndex < _listId.Count)
		{
			string skillId = _listId[_showIndex];
			SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(skillId);
			if (skillTableData == null)
				return;
			SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(skillId, 1);
			if (skillLevelTableData == null)
				return;

			string nameString = UIString.instance.GetString(skillTableData.useNameIdOverriding ? skillLevelTableData.nameId : skillTableData.nameId);
			string descString = UIString.instance.GetString(skillTableData.useDescriptionIdOverriding ? skillLevelTableData.descriptionId : skillTableData.descriptionId, skillLevelTableData.parameter);
			float cooltime = skillTableData.useCooltimeOverriding ? skillLevelTableData.cooltime : skillTableData.cooltime;

			SetInfo(skillTableData, "", nameString, descString, cooltime);
			++_showIndex;
		}
		else if (_showIndex == _listId.Count)
		{

		}
	}
	#endregion
}