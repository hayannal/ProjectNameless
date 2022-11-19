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
	public Text atkText;
	public Text countText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;
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
	}

	public void OnClickButton()
	{

	}
}