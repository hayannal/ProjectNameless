using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PetCanvasListItem : MonoBehaviour
{
	public Image petImage;
	public Text nameText;
	public GameObject starGridRootObject;
	public GameObject[] starObjectList;
	public GameObject fiveStarObject;

	public GameObject equippedObject;
	public GameObject blackObject;

	public Text atkText;
	public Text plusCountText;

	public Text[] noGainGrayTextList;
	public Image[] noGainGrayImageList;

	public string petId { get; set; }
	public int count { get; set; }
	public void Initialize(PetTableData petTableData, int count, int mainStatusValue, Action<string, int> clickCallback)
	{
		petImage.sprite = PetListCanvas.instance.GetSprite(petTableData.spriteName);
		nameText.SetLocalizedText(UIString.instance.GetString(petTableData.nameId));

		starGridRootObject.SetActive(petTableData.star <= 4);
		fiveStarObject.SetActive(petTableData.star == 5);
		for (int i = 0; i < starObjectList.Length; ++i)
			starObjectList[i].SetActive(i < petTableData.star);

		bool contains = (count > 0);
		if (contains && PetManager.instance.activePetId == petTableData.petId)
			equippedObject.SetActive(true);
		else
			equippedObject.SetActive(false);
		blackObject.gameObject.SetActive(false);

		for (int i = 0; i < noGainGrayTextList.Length; ++i)
			noGainGrayTextList[i].color = contains ? Color.white : Color.gray;
		for (int i = 0; i < noGainGrayImageList.Length; ++i)
			noGainGrayImageList[i].color = contains ? Color.white : Color.gray;

		plusCountText.gameObject.SetActive(contains);
		int maxCount = 20;
		if (count > maxCount)
			plusCountText.text = string.Format("{0} / <color=FF5500>{1}</color>", count, maxCount);
		else
			plusCountText.text = string.Format("{0} / {1}", count, maxCount);
		atkText.text = mainStatusValue.ToString("N0");

		petId = petTableData.petId;
		this.count = count;
		_clickAction = clickCallback;
	}

	Action<string, int> _clickAction;
	public void OnClickButton()
	{
		if (_clickAction != null)
			_clickAction.Invoke(petId, count);
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