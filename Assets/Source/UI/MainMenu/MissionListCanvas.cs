using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class MissionListCanvas : MonoBehaviour
{
	public static MissionListCanvas instance;

	public Text petMenuRemainCount;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (restore)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(true);

		RefreshInfo();
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

	void RefreshInfo()
	{
		petMenuRemainCount.text = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetDailySearchCount") - PetManager.instance.dailySearchCount).ToString();
	}

	public void OnClickButton(int index)
	{
		switch (index)
		{
			case 0:

				// 횟수 검사
				if (PetManager.instance.dailySearchCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetDailySearchCount"))
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_TodayCountComplete"), 2.0f);
					return;
				}

				Timing.RunCoroutine(PetSearchMoveProcess());
				break;
		}
	}

	void ShowCanvasAsyncWithPrepareGround(string canvasAddress, System.Action callback)
	{
		if (ContentsPrefabGroup.instance == null)
		{
			DelayedLoadingCanvas.Show(true);
			AddressableAssetLoadManager.GetAddressableGameObject("ContentsPrefabGroup", "Map", (prefab) =>
			{
				BattleInstanceManager.instance.GetCachedObject(prefab, null);
				DelayedLoadingCanvas.Show(false);
				UIInstanceManager.instance.ShowCanvasAsync(canvasAddress, callback);
			});
		}
		else
			UIInstanceManager.instance.ShowCanvasAsync(canvasAddress, callback);
	}


	IEnumerator<float> PetSearchMoveProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);
		
		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		StageManager.instance.FinalizeStage();
		TeamManager.instance.HideForMoveMap(true);

		// StackCanvas로 이동하는거라 안닫아도 된다.
		//gameObject.SetActive(false);

		//while (gameObject.activeSelf)
		//	yield return Timing.WaitForOneFrame;
		//yield return Timing.WaitForOneFrame;

		ShowCanvasAsyncWithPrepareGround("PetSearchCanvas", () =>
		{
			// 
			DelayedLoadingCanvas.Show(false);
			FadeCanvas.instance.FadeIn(0.5f);
		});
	}
}