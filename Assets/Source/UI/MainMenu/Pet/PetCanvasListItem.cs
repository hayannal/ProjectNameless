using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PetCanvasListItem : MonoBehaviour
{
	public Image petImage;
	public Text nameText;
	public GameObject equippedObject;
	public GameObject blackObject;

	public Text atkText;
	public Text plusCountText;

	public Text[] noGainGrayTextList;
	public Image[] noGainGrayImageList;

	public string petId { get; set; }
	public void Initialize(PetTableData petTableData, int count, int mainStatusValue, Action<string> clickCallback)
	{
		//petImage.sprite = SpellSpriteContainer.instance.FindSprite(petTableData.iconName);
		//nameText.SetLocalizedText(UIString.instance.GetString(nameId));

		bool contains = (count > 0);
		if (contains && PetManager.instance.activePetId == petId)
			equippedObject.SetActive(true);
		else
			equippedObject.SetActive(false);
		blackObject.gameObject.SetActive(false);

		for (int i = 0; i < noGainGrayTextList.Length; ++i)
			noGainGrayTextList[i].color = contains ? Color.white : Color.gray;
		for (int i = 0; i < noGainGrayImageList.Length; ++i)
			noGainGrayImageList[i].color = contains ? Color.white : Color.gray;

		plusCountText.gameObject.SetActive(contains);
		plusCountText.text = string.Format("+{0} / {1}", count, PetManager.instance.maxCountLevel);
		atkText.text = mainStatusValue.ToString("N0");

		petId = petTableData.petId;
		_clickAction = clickCallback;
	}

	Action<string> _clickAction;
	public void OnClickButton()
	{
		if (_clickAction != null)
			_clickAction.Invoke(petId);
		//SoundManager.instance.PlaySFX("GridOn");
	}



	RectTransform _rectTransform;
	public RectTransform cachedRectTransform
	{
		get
		{
			if (_rectTransform == null)
				_rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}
}