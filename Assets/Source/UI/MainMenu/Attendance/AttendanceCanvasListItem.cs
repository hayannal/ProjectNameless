using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class AttendanceCanvasListItem : MonoBehaviour
{
	public RewardIcon rewardIcon;
	public RewardIcon rewardIcon2;

	public Image equipIconImage;
	public Text equipNameText;
	public Text rarityText;
	public Coffee.UIExtensions.UIGradient rarityGradient;

	public GameObject blurImageObject;
	public GameObject underLegendBlurImageObject;
	public GameObject backImageObject;
	public GameObject underLegendBackImageObject;

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

		#region Equip
		if (lastRewardNum == attendanceRewardTableData.num && attendanceRewardTableData.rewardType1 == "it")
		{
			_lastItem = true;
			rewardIcon.gameObject.SetActive(false);
			if (rewardIcon2 != null) rewardIcon2.gameObject.SetActive(false);
			RefreshEquipReward();
			RefreshClaimState(attendanceRewardTableData);
			return;
		}
		#endregion

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

	#region Equip
	void RefreshEquipReward()
	{
		string id = _attendanceRewardTableData.rewardValue1;
		EquipLevelTableData equipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(id);
		if (equipLevelTableData == null)
			return;
		EquipTableData equipTableData = EquipManager.instance.GetCachedEquipTableData(equipLevelTableData.equipGroup);
		if (equipTableData == null)
			return;

		bool useUnderLegend = (equipLevelTableData.grade <= 3);
		blurImageObject.SetActive(!useUnderLegend);
		backImageObject.SetActive(!useUnderLegend);
		underLegendBlurImageObject.SetActive(useUnderLegend);
		underLegendBackImageObject.SetActive(useUnderLegend);

		equipNameText.SetLocalizedText(UIString.instance.GetString(equipTableData.nameId));

		AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", (sprite) =>
		{
			equipIconImage.sprite = null;
			equipIconImage.sprite = sprite;
		});
		EquipCanvasListItem.RefreshRarity(equipTableData.rarity, rarityText, rarityGradient);
	}

	public void OnClickEquipDetailButton()
	{
		Timing.RunCoroutine(ShowEquipDetailCanvasProcess());
	}

	IEnumerator<float> ShowEquipDetailCanvasProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		AttendanceCanvas.instance.ignoreStartEventFlag = true;
		AttendanceCanvas.instance.gameObject.SetActive(false);

		while (AttendanceCanvas.instance.gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		MissionListCanvas.ShowCanvasAsyncWithPrepareGround("PickUpEquipDetailCanvas", null);

		while ((PickUpEquipDetailCanvas.instance != null && PickUpEquipDetailCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;
		PickUpEquipDetailCanvas.instance.RefreshInfo(_attendanceRewardTableData.rewardValue1);
		PickUpEquipDetailCanvas.instance.SetRestoreCanvas("attendance");

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.4f);
	}
	#endregion

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

	int _earlyBonus = 0;
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

		int earlyBonus = 0;
		if (_lastItem)
		{
			TimeSpan remainTime = AttendanceData.instance.attendanceExpireTime - ServerTime.UtcNow;
			earlyBonus = remainTime.Days;
			if (earlyBonus > 10) earlyBonus = 10;
			_earlyBonus = earlyBonus;
		}

		if (_attendanceRewardTableData.rewardType1 == "it")
		{
			// 보상이 장비면 서버가 itemGrantString 을 보내줄거다.
			_count = _attendanceRewardTableData.rewardCount1;
			PlayFabApiManager.instance.RequestGetAttendanceReward(_attendanceRewardTableData.rewardType1, _attendanceRewardTableData.key, 0, 0, 0, earlyBonus, OnRecvResult);
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

			PlayFabApiManager.instance.RequestGetAttendanceReward(_attendanceRewardTableData.rewardType1, _attendanceRewardTableData.key, addDia, addGold, addEnergy, earlyBonus, (itemGrantString) =>
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

	int _count;
	void OnRecvResult(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = EquipManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		RefreshClaimState(_attendanceRewardTableData);
		AttendanceCanvas.instance.RefreshNextInfo();
		if (MainCanvas.instance != null)
			MainCanvas.instance.RefreshAttendanceAlarmObject();

		UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
		{
			CommonRewardCanvas.instance.RefreshReward(_attendanceRewardTableData.rewardType1, _attendanceRewardTableData.rewardValue1, _attendanceRewardTableData.rewardCount1, () =>
			{
				if (_earlyBonus > 0)
				{
					AttendanceCanvas.instance.RefreshRemainTime();
					UIInstanceManager.instance.ShowCanvasAsync("AttendanceEarlyCanvas", () =>
					{
						AttendanceEarlyCanvas.instance.RefreshInfo(_earlyBonus);
					});
				}
			});
		});

		MainCanvas.instance.RefreshMenuButton();
	}
}