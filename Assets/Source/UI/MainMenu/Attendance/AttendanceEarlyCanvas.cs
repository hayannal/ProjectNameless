using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AttendanceEarlyCanvas : MonoBehaviour
{
	public static AttendanceEarlyCanvas instance;

	public Text earlyText;
	public RewardIcon rewardIcon;

	void Awake()
	{
		instance = this;
	}

	public void RefreshInfo(int earlyBonusDays)
	{
		earlyText.text = earlyBonusDays.ToString();
		rewardIcon.RefreshReward("cu", "EN", earlyBonusDays * BattleInstanceManager.instance.GetCachedGlobalConstantInt("AttendanceEarlyEnergy"));
	}

	public void OnClickExitButton()
	{
		if (AttendanceCanvas.instance != null)
			AttendanceCanvas.instance.RefreshEarlyBonusRectInfo();
		gameObject.SetActive(false);
	}
}