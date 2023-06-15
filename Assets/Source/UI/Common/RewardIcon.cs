using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class RewardIcon : MonoBehaviour
{
	public string eventRewardId;
	public int num;

	public Text countText;
	public RectTransform countTextRectTransform;
	public GameObject goldObject;
	public GameObject diaObject;
	public GameObject energyObject;
	public GameObject goldMediumObject;
	public GameObject diaMediumObject;
	public GameObject energyMediumObject;
	public GameObject goldLargeObject;
	public GameObject diaLargeObject;
	public GameObject energyLargeObject;
	public GameObject sevenDaysObject;
	public GameObject spellGachaObject;
	public GameObject spellStarGachaObject;
	public GameObject spellGachaStarGroupRootObject;
	public Text spellGachaStarText;
	public GameObject characterGachaObject;
	public GameObject equipGachaObject;
	public GameObject equipRootObject;
	public Image equipIconImage;
	public Text equipRarityText;
	public Coffee.UIExtensions.UIGradient equipRarityGradient;
	public GameObject spellRootObject;
	public Image spellImage;
	public Text spellStarText;
	public GameObject petRootObject;
	public Image petImage;
	public Text petStarText;
	public Image uncommonImage;

	public Image blurImage;
	public Coffee.UIExtensions.UIGradient gradient;
	public Image lineColorImage;

	public GameObject[] frameObjectList;
	public Transform iconRootTransform;
	public DOTweenAnimation punchTweenAnimation;

	Vector2 _defaultCountTextSizeDelta;
	void Awake()
	{
		if (countTextRectTransform != null)
			_defaultCountTextSizeDelta = countTextRectTransform.sizeDelta;
	}

	void OnEnable()
	{
		if (eventRewardId == "")
			return;

		EventRewardTableData eventRewardTableData = TableDataManager.instance.FindEventRewardTableData(eventRewardId, num);
		if (eventRewardTableData == null)
			return;

		RefreshReward(eventRewardTableData.rewardType, eventRewardTableData.rewardValue, eventRewardTableData.rewardCount);
	}

	int _rewardCount = 0;
	public void RefreshReward(string rewardType, string rewardValue, int rewardCount)
	{
		goldObject.SetActive(false);
		if (diaObject != null) diaObject.SetActive(false);
		energyObject.SetActive(false);
		if (goldMediumObject != null) goldMediumObject.SetActive(false);
		if (diaMediumObject != null) diaMediumObject.SetActive(false);
		if (energyMediumObject != null) energyMediumObject.SetActive(false);
		if (goldLargeObject != null) goldLargeObject.SetActive(false);
		if (diaLargeObject != null) diaLargeObject.SetActive(false);
		if (energyLargeObject != null) energyLargeObject.SetActive(false);
		if (goldMediumObject != null) goldMediumObject.SetActive(false);
		if (sevenDaysObject != null) sevenDaysObject.SetActive(false);
		if (spellGachaObject != null) spellGachaObject.SetActive(false);
		if (spellStarGachaObject != null) spellStarGachaObject.SetActive(false);
		if (spellGachaStarGroupRootObject != null) spellGachaStarGroupRootObject.SetActive(false);
		if (characterGachaObject != null) characterGachaObject.SetActive(false);
		if (equipGachaObject != null) equipGachaObject.SetActive(false);
		if (equipRootObject != null) equipRootObject.SetActive(false);
		if (spellRootObject != null) spellRootObject.SetActive(false);
		if (petRootObject != null) petRootObject.SetActive(false);
		if (uncommonImage != null) uncommonImage.gameObject.SetActive(false);
		countText.text = rewardCount.ToString("N0");
		_rewardCount = rewardCount;
		switch (rewardType)
		{
			case "cu":
				if (blurImage != null) blurImage.color = new Color(0.5f, 0.5f, 0.5f, 0.0f);
				if (gradient != null) gradient.color1 = Color.white;
				if (gradient != null) gradient.color2 = Color.black;
				if (lineColorImage != null) lineColorImage.color = new Color(1.0f, 1.0f, 1.0f);
				switch (rewardValue)
				{
					case "GO":
						goldObject.SetActive(true);
						if (eventRewardId != "" && goldMediumObject != null && goldLargeObject != null)
						{
							if (rewardCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("GoldRewardIconLargeBase"))
							{
								goldLargeObject.SetActive(true);
								goldObject.SetActive(false);
							}
							else if (rewardCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("GoldRewardIconMediumBase"))
							{
								goldMediumObject.SetActive(true);
								goldObject.SetActive(false);
							}
						}
						countText.color = _showOnlyIcon ? MailCanvasListItem.GetGoldTextColor() : Color.white;
						break;
					case "DI":
						if (diaObject != null) diaObject.SetActive(true);
						if (eventRewardId != "" && diaMediumObject != null && diaLargeObject != null)
						{
							if (rewardCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("DiaRewardIconLargeBase"))
							{
								diaLargeObject.SetActive(true);
								if (diaObject != null) diaObject.SetActive(false);
							}
							else if (rewardCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("DiaRewardIconMediumBase"))
							{
								diaMediumObject.SetActive(true);
								if (diaObject != null) diaObject.SetActive(false);
							}
						}
						countText.color = _showOnlyIcon ? MailCanvasListItem.GetDiaTextColor() : Color.white;
						break;
					case "EN":
						energyObject.SetActive(true);
						if (eventRewardId != "" && energyMediumObject != null && energyLargeObject != null)
						{
							if (rewardCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("EnergyRewardIconLargeBase"))
							{
								energyLargeObject.SetActive(true);
								energyObject.SetActive(false);
							}
							else if (rewardCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("EnergyRewardIconMediumBase"))
							{
								energyMediumObject.SetActive(true);
								energyObject.SetActive(false);
							}
						}
						countText.color = _showOnlyIcon ? MailCanvasListItem.GetEnergyTextColor() : Color.white;
						break;
				}
				break;
			case "it":
				switch (rewardValue)
				{
					case "Cash_sSevenTotal":
						if (sevenDaysObject != null) sevenDaysObject.SetActive(true);
						countText.color = Color.white;
						break;
					case "Cash_sFestivalTotal":
						FestivalTypeTableData festivalTypeTableData = TableDataManager.instance.FindFestivalTypeTableData(FestivalData.instance.festivalId);
						if (festivalTypeTableData != null && uncommonImage != null)
						{
							AddressableAssetLoadManager.GetAddressableSprite(festivalTypeTableData.iconAddress, "Icon", (sprite) =>
							{
								uncommonImage.sprite = null;
								uncommonImage.sprite = sprite;
								uncommonImage.gameObject.SetActive(true);
							});
						}
						countText.color = Color.white;
						break;
					case "Cash_sSpellGacha":
						if (spellGachaObject != null) spellGachaObject.SetActive(true);
						countText.color = Color.white;
						break;
					case "Cash_sSpell3Gacha":
					case "Cash_sSpell4Gacha":
					case "Cash_sSpell5Gacha":
						if (spellStarGachaObject != null) spellStarGachaObject.SetActive(true);
						countText.color = Color.white;
						if (rewardValue == "Cash_sSpell3Gacha" && spellGachaStarGroupRootObject != null)
						{
							spellGachaStarGroupRootObject.SetActive(true);
							spellGachaStarText.text = "3";
						}
						if (rewardValue == "Cash_sSpell4Gacha" && spellGachaStarGroupRootObject != null)
						{
							spellGachaStarGroupRootObject.SetActive(true);
							spellGachaStarText.text = "4";
						}
						if (rewardValue == "Cash_sSpell5Gacha" && spellGachaStarGroupRootObject != null)
						{
							spellGachaStarGroupRootObject.SetActive(true);
							spellGachaStarText.text = "5";
						}
						break;
					case "Cash_sCharacterGacha":
						if (characterGachaObject != null) characterGachaObject.SetActive(true);
						countText.color = Color.white;
						break;
					case "Cash_sEquipGacha":
						if (equipGachaObject != null) equipGachaObject.SetActive(true);
						countText.color = Color.white;
						break;
					case "Cash_sEquipTypeGacha314":
						EquipCanvasListItem.RefreshGrade(3, blurImage, gradient, lineColorImage);
						EquipCanvasListItem.RefreshRarity(1, equipRarityText, equipRarityGradient);
						equipIconImage.sprite = MainCanvas.instance.equipTypeSpriteList[4];
						equipRootObject.SetActive(true);
						countText.color = Color.white;
						break;
					case "Cash_sEquipTypeGacha316":
						EquipCanvasListItem.RefreshGrade(3, blurImage, gradient, lineColorImage);
						EquipCanvasListItem.RefreshRarity(1, equipRarityText, equipRarityGradient);
						equipIconImage.sprite = MainCanvas.instance.equipTypeSpriteList[6];
						equipRootObject.SetActive(true);
						countText.color = Color.white;
						break;
					case "Cash_sEquipTypeGacha410":
						EquipCanvasListItem.RefreshGrade(4, blurImage, gradient, lineColorImage);
						EquipCanvasListItem.RefreshRarity(1, equipRarityText, equipRarityGradient);
						equipIconImage.sprite = MainCanvas.instance.equipTypeSpriteList[0];
						equipRootObject.SetActive(true);
						countText.color = Color.white;
						break;
					case "Cash_sEquipTypeGacha411":
						EquipCanvasListItem.RefreshGrade(4, blurImage, gradient, lineColorImage);
						EquipCanvasListItem.RefreshRarity(1, equipRarityText, equipRarityGradient);
						equipIconImage.sprite = MainCanvas.instance.equipTypeSpriteList[1];
						equipRootObject.SetActive(true);
						countText.color = Color.white;
						break;
					case "Cash_sEquipTypeGacha412":
						EquipCanvasListItem.RefreshGrade(4, blurImage, gradient, lineColorImage);
						EquipCanvasListItem.RefreshRarity(1, equipRarityText, equipRarityGradient);
						equipIconImage.sprite = MainCanvas.instance.equipTypeSpriteList[2];
						equipRootObject.SetActive(true);
						countText.color = Color.white;
						break;
					case "Cash_sEquipTypeGacha415":
						EquipCanvasListItem.RefreshGrade(4, blurImage, gradient, lineColorImage);
						EquipCanvasListItem.RefreshRarity(1, equipRarityText, equipRarityGradient);
						equipIconImage.sprite = MainCanvas.instance.equipTypeSpriteList[5];
						equipRootObject.SetActive(true);
						countText.color = Color.white;
						break;
				}
				if (rewardValue.StartsWith("Spell_"))
				{
					SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(rewardValue);
					if (skillTableData != null)
					{
						spellImage.sprite = SpellSpriteContainer.instance.FindSprite(skillTableData.iconPrefab);
						InitializeOtherGrade(7);
						spellRootObject.SetActive(true);
						spellStarText.text = skillTableData.star.ToString();
					}
				}
				else if (rewardValue.StartsWith("Actor"))
				{
					// 액터는 현재 패스
				}
				else if (rewardValue.StartsWith("Pet_"))
				{
					PetTableData petTableData = TableDataManager.instance.FindPetTableData(rewardValue);
					if (petTableData != null)
					{
						petImage.sprite = PetSpriteContainer.instance.FindSprite(petTableData.spriteName);
						InitializeOtherGrade(8);
						petRootObject.SetActive(true);
						petStarText.text = petTableData.star.ToString();
					}
				}
				else if (rewardValue.StartsWith("Equip"))
				{
					EquipLevelTableData equipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(rewardValue);
					if (equipLevelTableData != null)
					{
						EquipTableData equipTableData = EquipManager.instance.GetCachedEquipTableData(equipLevelTableData.equipGroup);
						if (equipTableData != null)
						{
							AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", (sprite) =>
							{
								equipIconImage.sprite = null;
								equipIconImage.sprite = sprite;
							});
							EquipCanvasListItem.RefreshGrade(equipLevelTableData.grade, blurImage, gradient, lineColorImage);
							EquipCanvasListItem.RefreshRarity(equipTableData.rarity, equipRarityText, equipRarityGradient);
							equipRootObject.SetActive(true);
						}
					}
				}
				break;
		}
	}

	public static void ShowDetailInfo(string rewardType, string rewardValue)
	{
		// 타입에 따라 상세정보창으로 가기로 한다.
		switch (rewardType)
		{
			case "it":
				if (rewardValue.StartsWith("Spell_"))
				{
					// unacquiredSpellSelectedId
					string selectedSpellId = rewardValue;
					if (string.IsNullOrEmpty(selectedSpellId))
						return;
					SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(selectedSpellId);
					if (skillTableData == null)
						return;
					SkillLevelTableData skillLevelTableData = TableDataManager.instance.FindSkillLevelTableData(selectedSpellId, 1);
					if (skillLevelTableData == null)
						return;
					SpellGradeLevelTableData spellGradeLevelTableData = TableDataManager.instance.FindSpellGradeLevelTableData(skillTableData.grade, skillTableData.star, 1);
					if (spellGradeLevelTableData == null)
						return;

					UIInstanceManager.instance.ShowCanvasAsync("SpellInfoCanvas", () =>
					{
						SpellInfoCanvas.instance.SetInfo(skillTableData, "", UIString.instance.GetString(skillTableData.useNameIdOverriding ? skillLevelTableData.nameId : skillTableData.nameId),
							UIString.instance.GetString(skillTableData.useDescriptionIdOverriding ? skillLevelTableData.descriptionId : skillTableData.descriptionId, skillLevelTableData.parameter),
							skillTableData.useCooltimeOverriding ? skillLevelTableData.cooltime : skillTableData.cooltime);
					});
				}
				else if (rewardValue.StartsWith("Actor"))
				{
					// 액터는 현재 패스
				}
				else if (rewardValue.StartsWith("Pet_"))
				{
					if (SevenDaysTabCanvas.instance != null && SevenDaysTabCanvas.instance.gameObject.activeSelf)
						SevenDaysCanvas.instance.OnClickPetDetailButton(rewardValue);
					if (FestivalTabCanvas.instance != null && FestivalTabCanvas.instance.gameObject.activeSelf)
						FestivalRewardCanvas.instance.OnClickPetDetailButton(rewardValue);
					if (CashShopTabCanvas.instance != null && CashShopTabCanvas.instance.gameObject.activeSelf)
						CashShopPackageCanvas.instance.OnClickPetDetailButton(rewardValue);
					if (ContinuousShopProductCanvas.instance != null && ContinuousShopProductCanvas.instance.gameObject.activeSelf)
						ContinuousShopProductCanvas.instance.OnClickPetDetailButton(rewardValue);
				}
				else if (rewardValue.StartsWith("Equip"))
				{
					if (SevenDaysTabCanvas.instance != null && SevenDaysTabCanvas.instance.gameObject.activeSelf)
						SevenDaysCanvas.instance.OnClickEquipDetailButton(rewardValue);
					if (FestivalTabCanvas.instance != null && FestivalTabCanvas.instance.gameObject.activeSelf)
						FestivalRewardCanvas.instance.OnClickEquipDetailButton(rewardValue);
					if (CashShopTabCanvas.instance != null && CashShopTabCanvas.instance.gameObject.activeSelf)
						CashShopPackageCanvas.instance.OnClickEquipDetailButton(rewardValue);
					if (ContinuousShopProductCanvas.instance != null && ContinuousShopProductCanvas.instance.gameObject.activeSelf)
						ContinuousShopProductCanvas.instance.OnClickEquipDetailButton(rewardValue);
				}
				break;
		}
	}

	bool _showOnlyIcon = false;
	public void ShowOnlyIcon(bool onlyIcon, float onlyIconScale = 1.5f)
	{
		_showOnlyIcon = onlyIcon;
		for (int i = 0; i < frameObjectList.Length; ++i)
			frameObjectList[i].SetActive(!onlyIcon);

		iconRootTransform.localScale = onlyIcon ? new Vector3(onlyIconScale, onlyIconScale, onlyIconScale) : Vector3.one;

		if (countTextRectTransform != null)
		{
			if (_defaultCountTextSizeDelta.x == 0.0f) _defaultCountTextSizeDelta.x = 70.0f;
			if (_defaultCountTextSizeDelta.y == 0.0f) _defaultCountTextSizeDelta.y = 24.0f;

			if (onlyIcon && _rewardCount >= 1000000)
				countTextRectTransform.sizeDelta = new Vector2(100.0f, _defaultCountTextSizeDelta.y);
			else if (onlyIcon && _rewardCount >= 100000)
				countTextRectTransform.sizeDelta = new Vector2(90.0f, _defaultCountTextSizeDelta.y);
			else
				countTextRectTransform.sizeDelta = _defaultCountTextSizeDelta;
		}
	}

	public void ActivePunchAnimation(bool active)
	{
		if (active)
			punchTweenAnimation.DORestart();
		else
			punchTweenAnimation.DOPause();
	}

	// EquipCanvasListItem 에서 복사해서 쓴다.
	public void InitializeOtherGrade(int grade)
	{
		switch (grade)
		{
			case 7:
				// for spell
				blurImage.color = new Color(0.5f, 0.5f, 0.5f, 0.0f);
				gradient.color1 = Color.white;
				gradient.color2 = Color.black;
				lineColorImage.color = new Color(0.95f, 1.0f, 0.2f);
				break;

			case 8:
				// for pet
				blurImage.color = new Color(0.5f, 0.5f, 0.5f, 0.0f);
				gradient.color1 = Color.white;
				gradient.color2 = Color.black;
				lineColorImage.color = new Color(0.0f, 0.9f, 0.0f);
				break;
		}
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