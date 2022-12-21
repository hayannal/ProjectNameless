using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class MissionListCanvas : MonoBehaviour
{
	public static MissionListCanvas instance;

	public Text petMenuRemainCount;
	public Text petTodayResetRemainTimeText;

	public Text wheelRemainCount;
	public Text wheelTodayResetRemainTimeText;

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

	void Update()
	{
		UpdateResetRemainTime();
	}

	void RefreshInfo()
	{
		petMenuRemainCount.text = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetDailySearchCount") - PetManager.instance.dailySearchCount).ToString();
		int count = BattleInstanceManager.instance.GetCachedGlobalConstantInt("FortuneWheelDailyCount") - SubMissionData.instance.fortuneWheelDailyCount;
		if (count < 0) count = 0;
		wheelRemainCount.text = count.ToString();
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

				if (CurrencyData.instance.energy < BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyPet"))
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughEnergy"), 2.0f);
					return;
				}

				Timing.RunCoroutine(PetSearchMoveProcess());
				break;

			case 1:

				if (CurrencyData.instance.energy < BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyRoulette"))
				{
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughEnergy"), 2.0f);
					return;
				}

				UIInstanceManager.instance.ShowCanvasAsync("FortuneWheelCanvas", null);
				break;
		}
	}

	public static void ShowCanvasAsyncWithPrepareGround(string canvasAddress, System.Action callback)
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



	int _lastRemainTimeSecond = -1;
	void UpdateResetRemainTime()
	{
		#region Pet
		bool petProcess = false;
		if (PetManager.instance.dailySearchCount == 0)
			petTodayResetRemainTimeText.text = "";
		else
			petProcess = true;
		#endregion

		#region Wheel
		bool wheelProcess = false;
		if (SubMissionData.instance.fortuneWheelDailyCount == 0)
			wheelTodayResetRemainTimeText.text = "";
		else
			wheelProcess = true;
		#endregion

		if (petProcess == false && wheelProcess == false)
		{
			_lastRemainTimeSecond = -1;
			return;
		}

		if (ServerTime.UtcNow < PlayerData.instance.dayRefreshTime)
		{
			System.TimeSpan remainTime = PlayerData.instance.dayRefreshTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				if (petProcess) petTodayResetRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				if (wheelProcess) wheelTodayResetRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			if (petProcess) petTodayResetRemainTimeText.text = "";
			if (wheelProcess) wheelTodayResetRemainTimeText.text = "";
		}
	}
}