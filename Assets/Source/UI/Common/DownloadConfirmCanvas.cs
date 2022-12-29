using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DownloadConfirmCanvas : MonoBehaviour
{
	public RewardIcon rewardIcon;

	public Text confirmMessageText;
	public GameObject confirmMessageObject;
	public GameObject confirmButtonObject;
	public GameObject restartMessageObject;

	public GameObject rewardMessageObject;
	public GameObject rewardButtonObject;

	public RectTransform alarmRootTransform;

	void OnEnable()
	{
		int amount = BattleInstanceManager.instance.GetCachedGlobalConstantInt("DownloadEnergyReward");
		rewardIcon.RefreshReward("cu", "EN", amount);

		confirmMessageText.SetLocalizedText(UIString.instance.GetString("SystemUI_DownloadConfirm", amount));
		confirmMessageObject.SetActive(PlayerData.instance.downloadConfirmed == false);
		confirmButtonObject.SetActive(PlayerData.instance.downloadConfirmed == false);
		restartMessageObject.SetActive(PlayerData.instance.downloadConfirmed == false);
		rewardMessageObject.SetActive(PlayerData.instance.downloadConfirmed);
		rewardButtonObject.SetActive(PlayerData.instance.downloadConfirmed);

		AlarmObject.Hide(alarmRootTransform);
		if (PlayerData.instance.downloadConfirmed)
			AlarmObject.Show(alarmRootTransform);
	}

	public void OnClickDownloadButton()
	{
		PlayFabApiManager.instance.RequestConfirmDownload(() =>
		{
			PlayerData.instance.ResetData();
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
				CommonRewardCanvas.instance.RefreshReward(0, 0, count);
			});
		});
	}
}