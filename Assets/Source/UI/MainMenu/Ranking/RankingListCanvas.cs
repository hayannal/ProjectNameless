using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RankingListCanvas : MonoBehaviour
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
				UIInstanceManager.instance.ShowCanvasAsync("StageRankingCanvas", () =>
				{
					StageRankingCanvas.instance.RefreshInfo(StageRankingCanvas.eRankType.Stage, RankingData.instance.listDisplayStageRankingInfo);
				});
				break;
			case 1:
				RankingData.instance.RequestBattlePowerRankingData(() =>
				{
					if (StageRankingCanvas.instance != null && StageRankingCanvas.instance.gameObject.activeSelf && StageRankingCanvas.instance.rankType == StageRankingCanvas.eRankType.BattlePower)
						StageRankingCanvas.instance.RefreshInfo(StageRankingCanvas.eRankType.BattlePower, RankingData.instance.listDisplayPowerRankingInfo);
				});
				UIInstanceManager.instance.ShowCanvasAsync("StageRankingCanvas", () =>
				{
					StageRankingCanvas.instance.RefreshInfo(StageRankingCanvas.eRankType.BattlePower, RankingData.instance.listDisplayPowerRankingInfo);
				});
				break;
			case 2:
				// 펫은 각각 따로 호출해야해서 창에서 탭 바꿀때 얻어오는 형태로 만들어본다.
				if (PetSpriteContainer.instance == null)
				{
					DelayedLoadingCanvas.Show(true);
					AddressableAssetLoadManager.GetAddressableGameObject("PetSpriteContainer", "", (prefab) =>
					{
						BattleInstanceManager.instance.GetCachedObject(prefab, null);
						DelayedLoadingCanvas.Show(false);
						UIInstanceManager.instance.ShowCanvasAsync("PetRankingCanvas", null);
					});
				}
				else
					UIInstanceManager.instance.ShowCanvasAsync("PetRankingCanvas", null);
				break;
		}
	}
}