using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PetBattleInfo : MonoBehaviour
{
	public GameObject nameBoardObject;
	public Text nameText;
	public GameObject starGridRootObject;
	public GameObject[] starObjectList;
	public GameObject fiveStarObject;

	public GameObject gaugeObject;
	public Image fillImage;

	void Update()
	{
		UpdateFillImage();
	}

	public int star { get; private set; }
	public string petId { get; private set; }
	public void SetInfo(string petId)
	{
		PetTableData petTableData = TableDataManager.instance.FindPetTableData(petId);
		if (petTableData == null)
			return;

		SetInfo(petTableData);
	}

	float _fillImageTargetValue = 0.0f;
	public void SetInfo(PetTableData petTableData)
	{
		petId = petTableData.petId;
		nameText.SetLocalizedText(UIString.instance.GetString(petTableData.nameId));
		star = petTableData.star;
		starGridRootObject.SetActive(petTableData.star <= 4);
		fiveStarObject.SetActive(petTableData.star == 5);
		for (int i = 0; i < starObjectList.Length; ++i)
			starObjectList[i].SetActive(i < petTableData.star);
		fillImage.fillAmount = _fillImageTargetValue = 0.0f;
		gaugeObject.SetActive(false);
	}

	public void ShowGaugeObject(bool show)
	{
		gaugeObject.SetActive(show);
	}

	public void OnAttack(int attack)
	{
		float resultRatio = Random.Range(0.5f, 2.0f);

		_fillImageTargetValue += resultRatio;
		if (_fillImageTargetValue > 1.0f)
			_fillImageTargetValue = 1.0f;
		DOTween.To(() => fillImage.fillAmount, x => fillImage.fillAmount = x, _fillImageTargetValue, 0.6f).SetEase(Ease.Linear);
	}

	public bool IsDie()
	{
		return (_fillImageTargetValue >= 1.0f);
	}

	void UpdateFillImage()
	{

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