using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipSkillSlotIcon : MonoBehaviour
{
	public Image spellIconImage;
	public GameObject activeSkillOutlineImageObject;
	public GameObject activeAlarmObject;
	public GameObject blinkObject;

	public Image cooltimeImage;
	public Text cooltimeText;

	SkillProcessor.SkillInfo _skillInfo;
	Cooltime _cooltimeInfo;
	public void Initialize(SkillProcessor.SkillInfo skillInfo)
	{
		_skillInfo = skillInfo;

		SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(skillInfo.skillId);
		if (skillTableData != null)
		{
			AddressableAssetLoadManager.GetAddressableSprite(skillTableData.iconPrefab, "Icon", (sprite) =>
			{
				spellIconImage.sprite = null;
				spellIconImage.sprite = sprite;
			});
		}

		activeSkillOutlineImageObject.SetActive(false);
		activeAlarmObject.SetActive(false);
		blinkObject.SetActive(false);

		Cooltime cooltimeInfo = SpellManager.instance.cooltimeProcessor.GetCooltime(skillInfo.skillId);
		if (cooltimeInfo != null)
		{
			cooltimeInfo.cooltimeStartAction = OnStartCooltime;
			cooltimeInfo.cooltimeEndAction = OnEndCooltime;
			if (cooltimeInfo.CheckCooltime())
			{
				OnStartCooltime();
				UpdateCooltime();
			}
			else
				OnEndCooltime();

			_cooltimeInfo = cooltimeInfo;
		}
	}
	
	void Update()
	{
		UpdateCooltime();
	}

	public void OnClickButton()
	{
		if (cooltimeImage.gameObject.activeSelf)
			return;

		SpellManager.instance.UseEquipSkill(_skillInfo);
	}

	
	#region Cooltime
	void OnStartCooltime()
	{
		cooltimeImage.gameObject.SetActive(true);
	}

	void OnEndCooltime()
	{
		cooltimeImage.gameObject.SetActive(false);

		activeSkillOutlineImageObject.gameObject.SetActive(true);
		activeAlarmObject.SetActive(true);
		blinkObject.SetActive(true);
	}
	
	void UpdateCooltime()
	{
		if (_cooltimeInfo == null)
			return;

		if (!cooltimeImage.gameObject.activeSelf)
			return;

		cooltimeImage.fillAmount = _cooltimeInfo.cooltimeRatio;
		cooltimeText.text = _cooltimeInfo.cooltimeRatioText;
	}
	#endregion
	


	RectTransform _transform;
	public RectTransform cachedRectTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<RectTransform>();
			return _transform;
		}
	}
}