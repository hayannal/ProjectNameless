using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissionListCanvas : MonoBehaviour
{
	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (restore)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(true);
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

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

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	public void OnClickButton(int index)
	{
		switch (index)
		{
			case 0:
				UIInstanceManager.instance.ShowCanvasAsync("PetSearchCanvas", null);
				break;
		}
	}
}