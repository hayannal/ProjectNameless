using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankingToggleButton : MonoBehaviour
{
	public Image iconImage;
	public GameObject selectObject;
	public Text nameText;
	
	public string petId { get; private set; }
	public void RefreshInfo(PetTableData petTableData)
	{
		petId = petTableData.petId;
		nameText.SetLocalizedText(UIString.instance.GetString(petTableData.nameId));
		iconImage.sprite = PetSpriteContainer.instance.FindSprite(petTableData.spriteName);
	}

	public void OnSelect(bool select)
	{
		selectObject.SetActive(select);
		nameText.gameObject.SetActive(select);
	}

	public void OnClickButton()
	{
		PetRankingCanvas.instance.OnValueChangedToggle(petId);
	}
}