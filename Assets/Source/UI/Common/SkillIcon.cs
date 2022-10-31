using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillIcon : MonoBehaviour
{
	public Image spellImage;
	public Image normalFrameImage;
	public Image goldenFrameImage;
	public GridLayoutGroup gridLayoutGroup;
	public Image[] starImageList;
	public Coffee.UIExtensions.UIShiny[] shinyEffectList;

	public void SetInfo(SkillTableData skillTableData, bool gray)
	{
		// icon
		spellImage.sprite = SpellSpriteContainer.instance.FindSprite(skillTableData.iconPrefab);

		for (int i = 0; i < shinyEffectList.Length; ++i)
			shinyEffectList[i].enabled = !gray;
		spellImage.color = normalFrameImage.color = goldenFrameImage.color = gray ? Color.gray : Color.white;
		for (int i = 0; i < starImageList.Length; ++i)
			starImageList[i].color = gray ? Color.gray : Color.white;

		normalFrameImage.gameObject.SetActive(skillTableData.grade == 0);
		goldenFrameImage.gameObject.SetActive(skillTableData.grade == 1);

		int starCount = skillTableData.star;
		switch (starCount)
		{
			case 1: gridLayoutGroup.spacing = new Vector2(-2.0f, 0.0f); break;
			case 2: gridLayoutGroup.spacing = new Vector2(-8.0f, 0.0f); break;
			case 3: gridLayoutGroup.spacing = new Vector2(-14.0f, 0.0f); break;
			case 4: gridLayoutGroup.spacing = new Vector2(-20.0f, 0.0f); break;
			case 5: gridLayoutGroup.spacing = new Vector2(-26.0f, 0.0f); break;
		}
		for (int i = 0; i < starImageList.Length; ++i)
			starImageList[i].gameObject.SetActive(i < starCount);
	}
}