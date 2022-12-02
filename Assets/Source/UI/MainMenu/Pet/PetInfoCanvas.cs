using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PetInfoCanvas : MonoBehaviour
{
	public static PetInfoCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject todayHeartObject;
	public RectTransform alarmRootTransform;

	public Text nameText;
	public GameObject starGridRootObject;
	public GameObject[] starObjectList;
	public GameObject fiveStarObject;

	public Text countText;
	public Text heartText;
	public Text atkText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		RefreshInfo();
	}

	void OnDisable()
	{
		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

	}

	bool _contains = false;
	void RefreshInfo()
	{
		PetData petData = PetManager.instance.GetPetData(PetListCanvas.instance.selectedPetId);
		PetTableData petTableData = TableDataManager.instance.FindPetTableData(PetListCanvas.instance.selectedPetId);
		_contains = (petData != null);

		if (_contains)
		{
			atkText.text = petData.mainStatusValue.ToString("N0");
		}
		else
		{
			if (petTableData != null)
			{
				atkText.text = petTableData.accumulatedAtk.ToString("N0");
			}
		}
		countText.gameObject.SetActive(_contains);
		heartText.gameObject.SetActive(_contains);

		nameText.SetLocalizedText(UIString.instance.GetString(petTableData.nameId));

		starGridRootObject.SetActive(petTableData.star <= 4);
		fiveStarObject.SetActive(petTableData.star == 5);
		for (int i = 0; i < starObjectList.Length; ++i)
			starObjectList[i].SetActive(i < petTableData.star);

		int count = 0;
		if (petData != null) count = petData.count;
		int maxCount = 20;
		if (count > maxCount)
			countText.text = string.Format("+{0} / <color=FF5500>{1}</color>", count, maxCount);
		else
			countText.text = string.Format("+{0} / {1}", count, maxCount);
	}

	public void OnClickAttackValueTextButton()
	{

	}



	public void OnDragRect(BaseEventData baseEventData)
	{
		PetListCanvas.instance.OnDragRect(baseEventData);
	}

	public void OnHeartDragRect(BaseEventData baseEventData)
	{
		Debug.Log("Heart Darg On");
		//PetListCanvas.instance.
	}

	public void OnClickButton()
	{

	}
}