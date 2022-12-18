using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DownloadConfirmCanvas : MonoBehaviour
{
	public RewardIcon rewardIcon;

	public GameObject confirmMessageObject;
	public GameObject confirmButtonObject;
	public GameObject restartMessageObject;

	public GameObject rewardMessageObject;
	public GameObject rewardButtonObject;

	void OnEnable()
	{
		rewardIcon.RefreshReward("cu", "EN", BattleInstanceManager.instance.GetCachedGlobalConstantInt("DownloadEnergyReward"));

		confirmMessageObject.SetActive(PlayerData.instance.downloadConfirmed == false);
		confirmButtonObject.SetActive(PlayerData.instance.downloadConfirmed == false);
		restartMessageObject.SetActive(PlayerData.instance.downloadConfirmed == false);
		rewardMessageObject.SetActive(PlayerData.instance.downloadConfirmed);
		rewardButtonObject.SetActive(PlayerData.instance.downloadConfirmed);
	}

	public void OnClickDownloadButton()
	{
		PlayFabApiManager.instance.RequestConfirmDownload(() =>
		{
			SceneManager.LoadScene(0);
		});
	}

	public void OnClickRewardButton()
	{
		int count = BattleInstanceManager.instance.GetCachedGlobalConstantInt("DownloadEnergyReward");
		PlayFabApiManager.instance.RequestConfirmDownloadReward(count, () =>
		{
			UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
			{
				MainCanvas.instance.RefreshCashButton();
				gameObject.SetActive(false);
				CommonRewardCanvas.instance.RefreshReward(0, count);
			});
		});
	}
}