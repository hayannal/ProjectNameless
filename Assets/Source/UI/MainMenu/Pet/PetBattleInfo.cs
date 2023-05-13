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
		_damageRandomRatio = Random.Range(BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetDamageRandomMin100") * 0.01f, BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetDamageRandomMax100") * 0.01f);
		for (int i = 0; i < starObjectList.Length; ++i)
			starObjectList[i].SetActive(i < petTableData.star);
		fillImage.fillAmount = _fillImageTargetValue = 0.0f;
		gaugeObject.SetActive(false);
	}

	public void ShowGaugeObject(bool show)
	{
		gaugeObject.SetActive(show);
	}

	// 개체에 따라 랜덤편차
	float _damageRandomRatio = 1.0f;
	public void OnAttack(int attack)
	{
		// 느린사람은 100% 빠른 사람은 300%
		// 두번째 턴에선 200% 빠른 사람은 600%
		// 기본적으로 주어지는 두번째 턴까지의 누적은 300~900 정도다.
		// 엑스트라까지 따지면 300% 빠른 사람은 900% 정도로 고려해서 해둔다.
		// 엑스트라까지의 누적은 600~1800이다.
		//
		// 내가 데려가는 펫의 성에 따라서도 비율이 바뀌어야한다.
		int activePetStar = PetSearchGround.instance.activePetStar;

		// 이젠 랜덤 대신 제대로 계산식 해보기로 한다.
		//float resultRatio = Random.Range(0.3f, 0.8f);
		float resultRatio = attack * 0.001f;
		switch (star)
		{
			case 1:
				// 1성으로 1성 잡을때를 생각해보면
				// 2.5곱해보면 0.72 ~ 2.16 정도일테니 적당히 하면 잡을 수 있을거다.
				resultRatio *= (BattleInstanceManager.instance.GetCachedGlobalConstantInt("Pet1StarAttackRate100") * 0.01f);
				break;
			case 2:
				// 2성으로 2성 잡을때라면 
				// 2.1곱해보면 0.63 ~ 1.89 정도일테니 적당히 하면 잡을 수 있을거다.
				resultRatio *= (BattleInstanceManager.instance.GetCachedGlobalConstantInt("Pet2StarAttackRate100") * 0.01f);
				break;
			case 3:
				// 3성으로 3성 잡을때라면 
				// 1.8곱해보면 0.54 ~ 1.62 정도일테니 적당히 하면 잡을 수 있을거다.
				resultRatio *= (BattleInstanceManager.instance.GetCachedGlobalConstantInt("Pet3StarAttackRate100") * 0.01f);
				break;
			case 4:
				// 4성으로 4성 잡을때라면 
				// 1.5곱해보면 0.45 ~ 1.35 정도일테니 적당히 하면 잡을 수 있을거다.
				resultRatio *= (BattleInstanceManager.instance.GetCachedGlobalConstantInt("Pet4StarAttackRate100") * 0.01f);
				break;
			case 5:
				// 5성으로 5성 잡을때라면 
				// 1.2곱해보면 0.36 ~ 1.08 정도일테니 적당히 하면 잡을 수 있을거다.
				resultRatio *= (BattleInstanceManager.instance.GetCachedGlobalConstantInt("Pet5StarAttackRate100") * 0.01f);
				break;
		}

		// 모든 몹은 스스로의 HP 편차를 가진다.
		resultRatio *= _damageRandomRatio;

		// 활성화된 펫의 별에 따라 보너스나 패널티를 적용한다.
		int diffStar = activePetStar - star;
		if (diffStar < 0)
			resultRatio = resultRatio * (1.0f + diffStar * (BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetStarDiff100") * 0.01f));
		else
			resultRatio = resultRatio + diffStar * (BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetStarDiff100") * 0.01f);

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