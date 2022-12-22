using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class MissionListCanvas : MonoBehaviour
{
	public static MissionListCanvas instance;

	#region Energy
	public Text energyText;
	public Transform energyIconTransform;
	public Canvas[] backImageCanvasList;
	#endregion

	public Text petMenuRemainCount;
	public Text petTodayResetRemainTimeText;
	public Text petSearchEnergyText;
	public RectTransform petAlarmRootTransform;

	public Text wheelRemainCount;
	public Text wheelTodayResetRemainTimeText;
	public Text wheelEnergyText;
	public GameObject wheelEnergyObject;
	public GameObject wheelEnterObject;
	public RectTransform wheelAlarmRootTransform;

	void Awake()
	{
		instance = this;

		#region Energy
		_canvas = GetComponent<Canvas>();
		#endregion
	}

	Canvas _canvas;
	void OnEnable()
	{
		#region Energy
		RefreshEnergy();
		#endregion
		RefreshInfo();

		#region Energy
		if (_canvas != null)
		{
			for (int i = 0; i < backImageCanvasList.Length; ++i)
				backImageCanvasList[i].sortingOrder = _canvas.sortingOrder - 1;
		}
		#endregion

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

	void Update()
	{
		UpdateResetRemainTime();
		UpdateEnergy();
	}

	#region Energy
	public void RefreshEnergy()
	{
		energyText.text = CurrencyData.instance.energy.ToString("N0");
	}

	public void OnClickEnergyButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.LeftBottom, UIString.instance.GetString("GameUI_EnergyDesc"), 200, energyIconTransform, new Vector2(-40.0f, 9.0f));
	}
	#endregion

	public static bool IsAlarmPetSearch()
	{
		if (PetManager.instance.dailySearchCount < BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetDailySearchCount"))
			return true;
		return false;
	}

	public static bool IsAlarmFortuneWheel()
	{
		if (SubMissionData.instance.fortuneWheelDailyCount < BattleInstanceManager.instance.GetCachedGlobalConstantInt("FortuneWheelDailyCount"))
			return true;
		return false;
	}

	void RefreshInfo()
	{
		int cost = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyPet");
		petSearchEnergyText.text = cost.ToString("N0");
		petMenuRemainCount.text = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetDailySearchCount") - PetManager.instance.dailySearchCount).ToString();
		AlarmObject.Hide(petAlarmRootTransform);
		if (IsAlarmPetSearch())
			AlarmObject.Show(petAlarmRootTransform);

		cost = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyRoulette");
		wheelEnergyText.text = cost.ToString("N0");
		int count = BattleInstanceManager.instance.GetCachedGlobalConstantInt("FortuneWheelDailyCount") - SubMissionData.instance.fortuneWheelDailyCount;
		if (count < 0) count = 0;
		wheelRemainCount.text = count.ToString();
		wheelEnergyObject.SetActive(SubMissionData.instance.fortuneWheelDailyCount == 0);
		wheelEnterObject.SetActive(SubMissionData.instance.fortuneWheelDailyCount > 0);
		AlarmObject.Hide(wheelAlarmRootTransform);
		if (IsAlarmFortuneWheel())
			AlarmObject.Show(wheelAlarmRootTransform);
	}

	public void OnClickButton(int index)
	{
		switch (index)
		{
			case 0:

				// 횟수 검사
				if (IsAlarmPetSearch() == false)
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

	int _lastEnergySecond = -1;
	void UpdateEnergy()
	{
		if (CurrencyData.instance.energy >= CurrencyData.instance.energyMax)
			return;

		if (_lastEnergySecond != (int)Time.time)
		{
			//Debug.Log(_lastEnergySecond);
			RefreshEnergy();
			_lastEnergySecond = (int)Time.time;
		}
	}
}