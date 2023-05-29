using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class EquipAltar : MonoBehaviour
{
	public int positionIndex;
	public Transform equipRootTransform;
	public DOTweenAnimation rotateTweenAnimation;
	public GameObject emptyIconObject;
	public ParticleSystem gradeParticleSystem;
	public Image enhanceBackgroundImage;
	public Text enhanceLevelText;
	public Text rarityText;
	public Coffee.UIExtensions.UIGradient rarityGradient;
	public RectTransform alarmRootTransform;

	bool _started = false;
	void Start()
	{
		_started = true;
	}

	void OnEnable()
	{
		RefreshEquipObject();
	}

	void OnDisable()
	{
		DisableEquipObject();
	}

	void Update()
	{
		UpdateRotateTweenAnimation();
	}

	bool _reserveRotateTweenAnimation;
	void UpdateRotateTweenAnimation()
	{
		if (_reserveRotateTweenAnimation)
		{
			rotateTweenAnimation.DORestart();
			_reserveRotateTweenAnimation = false;
		}
	}


	EquipPrefabInfo _currentEquipObject = null;
	public void RefreshEquipObject()
	{
		// 비쥬얼용 오브젝트들은 우선 끄고 처리
		DisableEquipObject();

		// 알람
		RefreshAlarmObject();

		EquipData equipData = EquipManager.instance.GetEquippedDataByType((EquipManager.eEquipSlotType)positionIndex);
		if (equipData == null)
		{
			gradeParticleSystem.gameObject.SetActive(false);
			rarityText.gameObject.SetActive(false);
			emptyIconObject.SetActive(true);
			return;
		}

		// 제단은 9개가 동시에 있다보니 오브젝트 로딩을 기다리다보면 강화수치도 등급 이펙트도 아무것도 안떠서 휑해질 수 있다.
		// 그러니 등급 이펙트까지 다 미리 보여지게 한채 오브젝트를 로드한다.
		// EquipInfoGround 로 가서는 하나의 오브젝트만 줌인해서 보는거라 로딩이 다 되서 오브젝트가 바뀔때 등급 이펙트도 같이 바꾼다.
		ParticleSystem.MainModule main = gradeParticleSystem.main;
		main.startColor = GetGradeParticleColor(equipData.cachedEquipLevelTableData.grade);
		gradeParticleSystem.gameObject.SetActive(true);
		RefreshRarity(equipData.cachedEquipTableData.rarity);
		RefreshEnhanceLevel(equipData.enhanceLevel, equipData.cachedEquipLevelTableData.grade);
		emptyIconObject.SetActive(false);
		AddressableAssetLoadManager.GetAddressableGameObject(equipData.cachedEquipTableData.prefabAddress, "Equip", OnLoadedEquip);
	}

	public void RefreshEnhanceLevel(int enhanceLevel, int grade)
	{
		enhanceBackgroundImage.gameObject.SetActive(enhanceLevel > 0);
		enhanceBackgroundImage.color = GetLineGradeColor(grade);
		enhanceLevelText.text = enhanceLevel.ToString();
	}

	public Color GetLineGradeColor(int grade)
	{
		switch (grade)
		{
			case 0: return new Color(0.5f, 0.5f, 0.5f);
			case 1: return new Color(0.1f, 0.84f, 0.1f);
			case 2: return new Color(0.0f, 0.51f, 1.0f);
			case 3: return new Color(0.63f, 0.0f, 1.0f);
			case 4: return new Color(1.0f, 0.5f, 0.0f);
			case 5: return new Color(0.85f, 0.15f, 0.06f);
			case 6: return new Color(0.93f, 0.93f, 0.29f);
		}
		return Color.gray;
	}

	public void RefreshRarity(int rarity)
	{
		// 여긴 이탤릭 하지 않는다.

		switch (rarity)
		{
			case 0:
				rarityText.text = "A";
				rarityGradient.direction = Coffee.UIExtensions.UIGradient.Direction.Angle;
				rarityGradient.color1 = new Color(0.81f, 0.92f, 1.0f);
				rarityGradient.color2 = new Color(0.52f, 0.53f, 1.0f);
				rarityGradient.rotation = 155.0f;
				rarityGradient.offset = -0.19f;
				break;
			case 1:
				rarityText.text = "S";
				rarityGradient.direction = Coffee.UIExtensions.UIGradient.Direction.Angle;
				rarityGradient.color1 = new Color(1.0f, 0.45f, 0.5f);
				rarityGradient.color2 = new Color(1.0f, 1.0f, 0.48f);
				rarityGradient.rotation = 155.0f;
				rarityGradient.offset = -0.19f;
				break;
			case 2:
				rarityText.text = "SS";
				rarityGradient.direction = Coffee.UIExtensions.UIGradient.Direction.Angle;
				rarityGradient.color1 = new Color(1.0f, 0.45f, 0.5f);
				rarityGradient.color2 = new Color(1.0f, 1.0f, 0.48f);
				rarityGradient.rotation = 155.0f;
				rarityGradient.offset = 0.11f;
				break;
		}
		rarityText.gameObject.SetActive(true);
	}

	void DisableEquipObject()
	{
		if (_currentEquipObject != null)
		{
			ShowOutline(false, _currentEquipObject.gameObject, -1);
			_currentEquipObject.gameObject.SetActive(false);
			_currentEquipObject = null;
			rotateTweenAnimation.DORewind();
		}
	}

	public static Color GetGradeParticleColor(int grade)
	{
		switch (grade)
		{
			case 0: return new Color(0.5f, 0.5f, 0.5f);
			case 1: return new Color(0.35f, 0.84f, 0.35f);
			case 2: return new Color(0.2f, 0.51f, 1.0f);
			case 3: return new Color(0.63f, 0.2f, 1.0f);
			case 4: return new Color(1.0f, 0.5f, 0.2f);
			case 5: return new Color(0.84f, 0.12f, 0.12f);
			case 6: return new Color(0.91f, 0.82f, 0.15f);
		}
		return Color.white;
	}

	public static Color GetGradeOutlineColor(int grade)
	{
		switch (grade)
		{
			case 0: return new Color(0.8f, 0.8f, 0.8f);
			case 1: return new Color(0.1f, 0.84f, 0.1f);
			case 2: return new Color(0.0f, 0.51f, 1.0f);
			case 3: return new Color(0.75f, 0.05f, 1.0f);
			case 4: return new Color(1.0f, 0.5f, 0.0f);
			case 5: return new Color(0.84f, 0.12f, 0.12f);
			case 6: return new Color(0.92f, 0.8f, 0.12f);
		}
		return Color.white;
	}

	void OnLoadedEquip(GameObject prefab)
	{
		if (this == null) return;
		if (gameObject == null) return;
		if (gameObject.activeSelf == false) return;

		// 로딩 중에 다른 장비로 Refresh되었다면 이전 로드를 반영하지 않고 그냥 리턴
		EquipData equipData = EquipManager.instance.GetEquippedDataByType((EquipManager.eEquipSlotType)positionIndex);
		if (equipData == null)
			return;
		if (equipData.cachedEquipTableData.prefabAddress != prefab.name)
			return;

		EquipPrefabInfo newEquipPrefabInfo = BattleInstanceManager.instance.GetCachedEquipObject(prefab, equipRootTransform);
		newEquipPrefabInfo.cachedTransform.localPosition = Vector3.zero;
		newEquipPrefabInfo.cachedTransform.Translate(0.0f, newEquipPrefabInfo.pivotOffset, 0.0f, Space.World);
		ShowOutline(true, newEquipPrefabInfo.gameObject, equipData.cachedEquipLevelTableData.grade);
		_currentEquipObject = newEquipPrefabInfo;
		if (_started)
			rotateTweenAnimation.DORestart();
		else
			_reserveRotateTweenAnimation = true;
	}

	void ShowOutline(bool show, GameObject newObject, int grade)
	{
		if (show)
		{
			QuickOutline quickOutline = newObject.GetComponent<QuickOutline>();
			if (quickOutline == null)
			{
				quickOutline = newObject.AddComponent<QuickOutline>();
				quickOutline.OutlineWidth = 0.9f;
				quickOutline.SetBlink(1.0f);
			}
			quickOutline.OutlineColor = GetGradeOutlineColor(grade);
			quickOutline.enabled = true;
		}
		else
		{
			QuickOutline quickOutline = newObject.GetComponent<QuickOutline>();
			if (quickOutline != null)
				quickOutline.enabled = false;
		}
	}

	#region AlarmObject
	public void RefreshAlarmObject()
	{
		// 필요없다고 해서 주석처리
		/*
		AlarmObject.Hide(alarmRootTransform);

		// 뭔가 장착중이면 월드캔버스가 사라지니 보여줄 수 없다.
		EquipData equipData = EquipManager.instance.GetEquippedDataByType((EquipManager.eEquipSlotType)positionIndex);
		if (equipData != null)
			return;

		// 알람은 두가지 조건이다. 제단이 비어있는데 장착할 수 있는 장비가 있거나 새로운 템을 얻었거나
		bool show = false;
		List<EquipData> listEquipData = EquipManager.instance.GetEquipListByType((EquipManager.eEquipSlotType)positionIndex);
		show = (listEquipData.Count > 0);
		if (show)
			AlarmObject.Show(alarmRootTransform);
		*/
	}
	#endregion
}