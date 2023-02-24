using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipSkillSlotCanvas : MonoBehaviour
{
	public static EquipSkillSlotCanvas instance = null;

	public GameObject equipSkillSlotIconPrefab;
	public Transform[] equipSkillSlotTransformList;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		equipSkillSlotIconPrefab.SetActive(false);
	}

	void OnEnable()
	{
		InitializeSkillSlot();
	}

	void OnDisable()
	{
		if (_listEquipSkillSlotIcon == null)
			return;

		for (int i = 0; i < _listEquipSkillSlotIcon.Count; ++i)
		{
			if (_listEquipSkillSlotIcon[i] == null)
				continue;
			_listEquipSkillSlotIcon[i].gameObject.SetActive(false);
		}
	}

	List<EquipSkillSlotIcon> _listEquipSkillSlotIcon;
	void InitializeSkillSlot()
	{
		if (_listEquipSkillSlotIcon == null)
		{
			_listEquipSkillSlotIcon = new List<EquipSkillSlotIcon>();
			for (int i = 0; i < equipSkillSlotTransformList.Length; ++i)
				_listEquipSkillSlotIcon.Add(null);
		}

		int count = 0;
		for (int i = 0; i < (int)EquipManager.eEquipSlotType.Amount; ++i)
		{
			EquipData equipData = EquipManager.instance.GetEquippedDataByType((EquipManager.eEquipSlotType)i);
			if (equipData == null)
				continue;

			string equipSkillId = equipData.GetUsableEquipSkillId();
			if (string.IsNullOrEmpty(equipSkillId))
				continue;

			SkillProcessor.SkillInfo skillInfo = SpellManager.instance.GetEquipSkillInfo(equipSkillId);
			if (skillInfo == null)
				continue;

			if (_listEquipSkillSlotIcon[count] != null)
			{
				_listEquipSkillSlotIcon[count].Initialize(skillInfo);
				_listEquipSkillSlotIcon[count].gameObject.SetActive(true);
			}
			else
			{
				GameObject newObject = Instantiate<GameObject>(equipSkillSlotIconPrefab, equipSkillSlotTransformList[count]);
				EquipSkillSlotIcon equipSkillSlotIcon = newObject.GetComponent<EquipSkillSlotIcon>();
				if (equipSkillSlotIcon == null)
					continue;

				equipSkillSlotIcon.cachedRectTransform.anchoredPosition = Vector2.zero;
				equipSkillSlotIcon.Initialize(skillInfo);
				equipSkillSlotIcon.gameObject.SetActive(true);
				_listEquipSkillSlotIcon[count] = equipSkillSlotIcon;	
			}
			count += 1;
		}
	}

	public void ReinitializeSkillSlot()
	{
		for (int i = 0; i < _listEquipSkillSlotIcon.Count; ++i)
		{
			if (_listEquipSkillSlotIcon[i] == null)
				continue;
			if (_listEquipSkillSlotIcon[i].gameObject.activeSelf == false)
				continue;
			_listEquipSkillSlotIcon[i].Reinitialize();
		}
	}
}