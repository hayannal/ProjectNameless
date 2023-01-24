using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;

public class AttendanceCanvasListItem : MonoBehaviour
{
	public RewardIcon rewardIcon;
	public RewardIcon rewardIcon2;

	public Text countText;
	public Text claimText;
	public Text dayText;
	public Text completeText;
	public GameObject blackObject;

	public RectTransform alarmRootTransform;

	AttendanceRewardTableData _attendanceRewardTableData;
	ObscuredBool _lastItem;
	public void RefreshInfo(int lastRewardNum, AttendanceRewardTableData attendanceRewardTableData)
	{
		_attendanceRewardTableData = attendanceRewardTableData;

		rewardIcon.RefreshReward(attendanceRewardTableData.rewardType1, attendanceRewardTableData.rewardValue1, attendanceRewardTableData.rewardCount1);
		RefreshClaimState(attendanceRewardTableData);

		_lastItem = false;
		if (lastRewardNum == attendanceRewardTableData.num)
		{
			// last item
			_lastItem = true;
			rewardIcon.ShowOnlyIcon(true, 1.3f);

			if (rewardIcon2 != null)
			{
				rewardIcon2.RefreshReward(attendanceRewardTableData.rewardType2, attendanceRewardTableData.rewardValue2, attendanceRewardTableData.rewardCount2);
				rewardIcon2.ShowOnlyIcon(true, 1.3f);
			}
		}
		else
		{
			rewardIcon.ShowOnlyIcon(true, 1.0f);
			rewardIcon.countText.gameObject.SetActive(false);
			countText.text = rewardIcon.countText.text;
		}
	}
	
	void RefreshClaimState(AttendanceRewardTableData attendanceRewardTableData)
	{
		int count = AttendanceData.instance.rewardReceiveCount;
		bool recorded = AttendanceData.instance.todayReceiveRecorded;
		
		if (attendanceRewardTableData.num <= count)
		{
			claimText.gameObject.SetActive(false);
			dayText.gameObject.SetActive(false);
			completeText.gameObject.SetActive(true);
			blackObject.gameObject.SetActive(true);
			AlarmObject.Hide(alarmRootTransform);
		}
		else if (recorded == false && attendanceRewardTableData.num == (count + 1))
		{
			claimText.gameObject.SetActive(true);
			dayText.gameObject.SetActive(false);
			completeText.gameObject.SetActive(false);
			blackObject.gameObject.SetActive(false);
			claimText.color = new Color(0.0f, 1.0f, 0.0f);
			AlarmObject.Show(alarmRootTransform);
		}
		else
		{
			claimText.gameObject.SetActive(false);
			dayText.SetLocalizedText(UIString.instance.GetString("LoginUI_DayNumber", attendanceRewardTableData.num));
			dayText.gameObject.SetActive(true);
			completeText.gameObject.SetActive(false);
			blackObject.gameObject.SetActive(false);
			AlarmObject.Hide(alarmRootTransform);
		}
	}

	public void OnClickButton()
	{
		if (blackObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_AlreadyFreeItem"), 2.0f);
			return;
		}

		if (dayText.gameObject.activeSelf)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("LoginUI_CannotClaimYet"), 2.0f);
			return;
		}

		if (_attendanceRewardTableData.rewardType1 == "cu")
		{
			int addGold = 0;
			int addDia = 0;
			int addEnergy = 0;
			if (_attendanceRewardTableData.rewardValue1 == CurrencyData.GoldCode())
				addGold += _attendanceRewardTableData.rewardCount1;
			else if (_attendanceRewardTableData.rewardValue1 == CurrencyData.DiamondCode())
				addDia += _attendanceRewardTableData.rewardCount1;
			else if (_attendanceRewardTableData.rewardValue1 == CurrencyData.EnergyCode())
				addEnergy += _attendanceRewardTableData.rewardCount1;

			if (_attendanceRewardTableData.rewardType2 == "cu")
			{
				if (_attendanceRewardTableData.rewardValue2 == CurrencyData.GoldCode())
					addGold += _attendanceRewardTableData.rewardCount2;
				else if (_attendanceRewardTableData.rewardValue2 == CurrencyData.DiamondCode())
					addDia += _attendanceRewardTableData.rewardCount2;
				else if (_attendanceRewardTableData.rewardValue2 == CurrencyData.EnergyCode())
					addEnergy += _attendanceRewardTableData.rewardCount2;
			}

			if (addGold > 0 && CurrencyData.instance.CheckMaxGold())
				return;

			int earlyBonus = 0;
			if (_lastItem)
			{
				TimeSpan remainTime = AttendanceData.instance.attendanceExpireTime - ServerTime.UtcNow;
				earlyBonus = remainTime.Days;
				if (earlyBonus > 10) earlyBonus = 10;
			}

			PlayFabApiManager.instance.RequestGetAttendanceReward(_attendanceRewardTableData.rewardType1, _attendanceRewardTableData.key, addDia, addGold, addEnergy, earlyBonus, () =>
			{
				RefreshClaimState(_attendanceRewardTableData);
				AttendanceCanvas.instance.currencySmallInfo.RefreshInfo();
				AttendanceCanvas.instance.RefreshNextInfo();
				if (MainCanvas.instance != null)
					MainCanvas.instance.RefreshAttendanceAlarmObject();

				UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
				{
					CommonRewardCanvas.instance.RefreshReward(addGold, addDia, addEnergy, () =>
					{
						if (earlyBonus > 0)
						{
							AttendanceCanvas.instance.RefreshRemainTime();
							UIInstanceManager.instance.ShowCanvasAsync("AttendanceEarlyCanvas", () =>
							{
								AttendanceEarlyCanvas.instance.RefreshInfo(earlyBonus);
							});
						}
					});
				});
			});
		}
	}
}