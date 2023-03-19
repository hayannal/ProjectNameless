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

	public Coffee.UIExtensions.UIEffect grayscaleEffect;

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
		grayscaleEffect.enabled = false;

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

	public void Reinitialize()
	{
		grayscaleEffect.enabled = false;
	}

	void OnDisable()
	{
		if (_cooltimeInfo != null)
		{
			_cooltimeInfo.cooltimeStartAction = null;
			_cooltimeInfo.cooltimeEndAction = null;
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
		if (grayscaleEffect.enabled)
			return;

		if (SpellManager.instance.UseEquipSkill(_skillInfo))
		{
			// 별다른 처리가 없다면 1회 사용시 바로 회색처리로 바꿔야한다.
			grayscaleEffect.enabled = true;
		}
	}

	
	#region Cooltime
	void OnStartCooltime()
	{
		if (grayscaleEffect.enabled)
			return;

		cooltimeImage.gameObject.SetActive(true);
	}

	void OnEndCooltime()
	{
		cooltimeImage.gameObject.SetActive(false);

		activeSkillOutlineImageObject.gameObject.SetActive(true);
		blinkObject.SetActive(true);

		if (grayscaleEffect.enabled == false)
			activeAlarmObject.SetActive(true);
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