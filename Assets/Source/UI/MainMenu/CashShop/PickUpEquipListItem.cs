using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;
using MEC;

public class PickUpEquipListItem : MonoBehaviour
{
	public Text countText;
	public Text priceText;
	public Image equipIconImage;
	public Text remainTimeText;
	public Text notStreakCountText;

	CashShopData.PickUpEquipInfo _info;
	public void RefreshInfo(CashShopData.PickUpEquipInfo info)
	{
		EquipLevelTableData equipLevelTableData = TableDataManager.instance.FindEquipLevelTableData(info.id);
		if (equipLevelTableData == null)
			return;
		EquipTableData equipTableData = EquipManager.instance.GetCachedEquipTableData(equipLevelTableData.equipGroup);
		if (equipTableData == null)
			return;

		_info = info;
		AddressableAssetLoadManager.GetAddressableSprite(equipTableData.shotAddress, "Icon", (sprite) =>
		{
			equipIconImage.sprite = null;
			equipIconImage.sprite = sprite;
		});

		countText.text = string.Format("X {0:N0}", info.count);
		priceText.text = info.price.ToString("N0");

		// 만약 s남은 횟수도 1이고 ss남은 횟수도 1인 상태에서 ss가 나오게되면 s남은 횟수만 증가하면서 0회 남음이 되어버린다.
		// 이걸 방지하기 위해서 S에 대해서만 예외처리 해둔다.
		string topString = UIString.instance.GetString("ShopUI_PickUpEquipRemainCount", "S", Mathf.Max(1, info.sc - CashShopData.instance.GetCurrentPickUpEquipNotStreakCount1()));
		string bottomString = UIString.instance.GetString("ShopUI_PickUpEquipRemainCount", "SS", info.ssc - CashShopData.instance.GetCurrentPickUpEquipNotStreakCount2());
		notStreakCountText.SetLocalizedText(string.Format("{0}\n{1}", topString, bottomString));
		_eventExpireDateTime = new DateTime(info.ey, info.em, info.ed);
	}

	void Update()
	{
		UpdateRemainTime();
	}

	DateTime _eventExpireDateTime;
	int _lastRemainTimeSecond = -1;
	void UpdateRemainTime()
	{
		if (ServerTime.UtcNow < _eventExpireDateTime)
		{
			if (remainTimeText != null)
			{
				TimeSpan remainTime = _eventExpireDateTime - ServerTime.UtcNow;
				if (_lastRemainTimeSecond != (int)remainTime.TotalSeconds)
				{
					if (remainTime.Days > 0)
						remainTimeText.text = string.Format("{0}d {1:00}:{2:00}:{3:00}", remainTime.Days, remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
					else
						remainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
					_lastRemainTimeSecond = (int)remainTime.TotalSeconds;
				}
			}
		}
		else
		{
			gameObject.SetActive(false);
		}
	}

	public void OnClickInfoButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("GachaPickUpEquipInfoCanvas", null);
	}

	public void OnClickDetailButton()
	{
		Timing.RunCoroutine(ShowEquipDetailCanvasProcess());
	}

	IEnumerator<float> ShowEquipDetailCanvasProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		CashShopCanvas.instance.ignoreStartEventFlag = true;
		CashShopCanvas.instance.gameObject.SetActive(false);

		while (CashShopCanvas.instance.gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		MissionListCanvas.ShowCanvasAsyncWithPrepareGround("PickUpEquipDetailCanvas", null);

		while ((PickUpEquipDetailCanvas.instance != null && PickUpEquipDetailCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;

		DelayedLoadingCanvas.Show(false);
		FadeCanvas.instance.FadeIn(0.4f);
	}

	int _count;
	public void OnClickButton()
	{
		if (CurrencyData.instance.dia < _info.price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("ShopUI_ConfirmPurchase"), () => {

			// 연출 및 보상 처리. 100개씩 뽑으면 느릴 수 있으니 패킷 대기 없이 바로 시작한다.
			UIInstanceManager.instance.ShowCanvasAsync("RandomBoxScreenCanvas", () =>
			{
				// 연출창 시작과 동시에 패킷을 보내고
				List<ObscuredString> listEquipId = EquipManager.instance.GetRandomIdList(_info.count, true);
				_count = listEquipId.Count;
				PlayFabApiManager.instance.RequestOpenPickUpEquipBox(listEquipId, _info.count, _info.price, EquipManager.instance.tempPickUpNotStreakCount1, EquipManager.instance.tempPickUpNotStreakCount2, OnRecvResult);
			});
		});
	}

	void OnRecvResult(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = EquipManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.EquipGacha, _count);

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvResult(RandomBoxScreenCanvas.eBoxType.Equip, listItemInstance);

		MainCanvas.instance.RefreshMenuButton();

		if (CashShopCanvas.instance != null)
			CashShopCanvas.instance.RefreshPickUpEquipRect();
	}
}