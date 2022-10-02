using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RankingListCanvas : MonoBehaviour
{
	void OnEnable()
	{
		MainCanvas.instance.OnEnterCharacterMenu(true);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	public void OnClickButton(int index)
	{

	}
}