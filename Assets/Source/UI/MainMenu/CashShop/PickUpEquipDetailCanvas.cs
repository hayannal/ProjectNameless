using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class PickUpEquipDetailCanvas : EquipShowCanvasBase
{
	public static PickUpEquipDetailCanvas instance;

	public EquipListStatusInfo diffStatusInfo;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		SetInfoCameraMode(true);
		MainCanvas.instance.OnEnterCharacterMenu(true);
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

		if (EquipInfoGround.instance.diffMode)
			EquipInfoGround.instance.RestoreDiffMode();

		SetInfoCameraMode(false);
		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	public void RefreshInfo()
	{
		CashShopData.PickUpEquipInfo info = CashShopData.instance.GetCurrentPickUpEquipInfo();
		if (info == null)
			return;

		RefreshInfo(info.id);
	}

	public void RefreshInfo(string equipId)
	{
		EquipLevelTableData equipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(equipId);
		if (equipLevelTableData == null)
			return;

		EquipTableData equipTableData = EquipManager.instance.GetCachedEquipTableData(equipLevelTableData.equipGroup);
		if (equipTableData == null)
			return;

		EquipInfoGround.instance.ChangeDiffMode(equipTableData, equipLevelTableData);

		EquipData tempEquipData = new EquipData();
		tempEquipData.equipId = equipLevelTableData.equipId;
		tempEquipData.OnEnhance(0);

		diffStatusInfo.RefreshInfo(tempEquipData, false);
		diffStatusInfo.equipButtonObject.SetActive(false);
		diffStatusInfo.unlockButton.gameObject.SetActive(false);
		diffStatusInfo.detailShowButton.gameObject.SetActive(false);
	}

	string _restoreType;
	public void SetRestoreCanvas(string type)
	{
		_restoreType = type;
	}

	public void OnClickBackButton()
	{
		Timing.RunCoroutine(ShowEquipDetailCanvasProcess());
	}

	IEnumerator<float> ShowEquipDetailCanvasProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		gameObject.SetActive(false);

		while (gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		if (_restoreType == "sevendays")
		{
			_restoreType = "";
			if (MissionData.instance.sevenDaysId != 0 && ServerTime.UtcNow < MissionData.instance.sevenDaysExpireTime)
			{
				UIInstanceManager.instance.ShowCanvasAsync("SevenDaysTabCanvas", null);

				while ((SevenDaysTabCanvas.instance != null && SevenDaysTabCanvas.instance.gameObject.activeSelf) == false)
					yield return Timing.WaitForOneFrame;
			}
		}
		else if (_restoreType == "festival")
		{
			_restoreType = "";
			if (FestivalData.instance.festivalId != 0 && ServerTime.UtcNow < FestivalData.instance.festivalExpire2Time)
			{
				UIInstanceManager.instance.ShowCanvasAsync("FestivalTabCanvas", null);

				while ((FestivalTabCanvas.instance != null && FestivalTabCanvas.instance.gameObject.activeSelf) == false)
					yield return Timing.WaitForOneFrame;
			}
		}
		else
		{
			MainCanvas.instance.OnClickCashShopButton();

			while ((CashShopCanvas.instance != null && CashShopCanvas.instance.gameObject.activeSelf) == false)
				yield return Timing.WaitForOneFrame;
		}

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.4f);
	}
}