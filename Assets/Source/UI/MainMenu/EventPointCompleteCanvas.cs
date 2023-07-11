using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventPointCompleteCanvas : MonoBehaviour
{
	public static EventPointCompleteCanvas instance;

	public RewardIcon rewardIcon;

	System.Action _okAction;

	void Awake()
	{
		instance = this;
	}

	public void SetInfo(bool show, int energy, System.Action okAction = null)
	{
		gameObject.SetActive(show);
		if (show == false)
			return;

		rewardIcon.RefreshReward("cu", "EN", energy);

		_okAction = okAction;
	}

	public void OnClickOkButton()
	{
		gameObject.SetActive(false);
		if (_okAction != null)
			_okAction();
	}
}