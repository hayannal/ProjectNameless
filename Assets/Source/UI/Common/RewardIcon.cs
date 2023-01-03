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
	public GameObject goldObject;
	public GameObject diaObject;
	public GameObject energyObject;
	public GameObject sevenDaysObject;
	public GameObject spellGachaObject;
	public GameObject spellStarGachaObject;
	public GameObject spellGachaStarGroupRootObject;
	public Text spellGachaStarText;
	public GameObject characterGachaObject;
	public GameObject equipGachaObject;
	public Image equipIconImage;
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

	void OnEnable()
	{
		if (eventRewardId == "")
			return;

		EventRewardTableData eventRewardTableData = TableDataManager.instance.FindEventRewardTableData(eventRewardId, num);
		if (eventRewardTableData == null)
			return;

		RefreshReward(eventRewardTableData.rewardType, eventRewardTableData.rewardValue, eventRewardTableData.rewardCount);
	}

	public void RefreshReward(string rewardType, string rewardValue, int rewardCount)
	{
		goldObject.SetActive(false);
		if (diaObject != null) diaObject.SetActive(false);
		energyObject.SetActive(false);
		if (sevenDaysObject != null) sevenDaysObject.SetActive(false);
		if (spellGachaObject != null) spellGachaObject.SetActive(false);
		if (spellStarGachaObject != null) spellStarGachaObject.SetActive(false);
		if (spellGachaStarGroupRootObject != null) spellGachaStarGroupRootObject.SetActive(false);
		if (characterGachaObject != null) characterGachaObject.SetActive(false);
		if (equipGachaObject != null) equipGachaObject.SetActive(false);
		if (equipIconImage != null) equipIconImage.gameObject.SetActive(false);
		if (spellRootObject != null) spellRootObject.SetActive(false);
		if (petRootObject != null) petRootObject.SetActive(false);
		if (uncommonImage != null) uncommonImage.gameObject.SetActive(false);
		countText.text = rewardCount.ToString("N0");
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
						countText.color = _showOnlyIcon ? MailCanvasListItem.GetGoldTextColor() : Color.white;
						break;
					case "DI":
						diaObject.SetActive(true);
						countText.color = _showOnlyIcon ? MailCanvasListItem.GetDiaTextColor() : Color.white;
						break;
					case "EN":
						energyObject.SetActive(true);
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
				}
				if (rewardValue.StartsWith("Spell_"))
				{
					SkillTableData skillTableData = TableDataManager.instance.FindSkillTableData(rewardValue);
					if (skillTableData != null)
					{
						spellImage.sprite = SpellSpriteContainer.instance.FindSprite(skillTableData.iconPrefab);
						InitializeGrade(5);
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
						InitializeGrade(6);
						petRootObject.SetActive(true);
						petStarText.text = petTableData.star.ToString();
					}
				}
				else if (rewardValue.StartsWith("Equip"))
				{
					EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(rewardValue);
					if (equipTableData != null)
					{
						AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", (sprite) =>
						{
							equipIconImage.sprite = null;
							equipIconImage.sprite = sprite;
						});
						InitializeGrade(equipTableData.grade);
						equipIconImage.gameObject.SetActive(true);
					}
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
	}

	public void ActivePunchAnimation(bool active)
	{
		if (active)
			punchTweenAnimation.DORestart();
		else
			punchTweenAnimation.DOPause();
	}

	// EquipCanvasListItem 에서 복사해서 쓴다.
	public void InitializeGrade(int grade)
	{
		switch (grade)
		{
			case 0:
				blurImage.color = new Color(0.5f, 0.5f, 0.5f, 0.0f);
				gradient.color1 = Color.white;
				gradient.color2 = Color.black;
				lineColorImage.color = new Color(0.5f, 0.5f, 0.5f);
				break;
			case 1:
				blurImage.color = new Color(0.28f, 1.0f, 0.53f, 0.0f);
				gradient.color1 = new Color(0.0f, 1.0f, 0.3f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.1f, 0.84f, 0.1f);
				break;
			case 2:
				blurImage.color = new Color(0.28f, 0.78f, 1.0f, 0.0f);
				gradient.color1 = new Color(0.0f, 0.7f, 1.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.0f, 0.51f, 1.0f);
				break;
			case 3:
				blurImage.color = new Color(0.73f, 0.31f, 1.0f, 0.0f);
				gradient.color1 = new Color(0.66f, 0.0f, 1.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(0.63f, 0.0f, 1.0f);
				break;
			case 4:
				blurImage.color = new Color(1.0f, 0.78f, 0.31f, 0.0f);
				gradient.color1 = new Color(1.0f, 0.5f, 0.0f);
				gradient.color2 = new Color(0.8f, 0.8f, 0.8f);
				lineColorImage.color = new Color(1.0f, 0.5f, 0.0f);
				break;

			case 5:
				// for spell
				blurImage.color = new Color(0.5f, 0.5f, 0.5f, 0.0f);
				gradient.color1 = Color.white;
				gradient.color2 = Color.black;
				lineColorImage.color = new Color(0.0f, 0.8f, 0.8f);
				break;
				
			case 6:
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