using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab.ClientModels;
using MEC;

public class PickUpCharacterListItem : MonoBehaviour
{
	public Text priceText;
	public Image characterImage;
	public Text remainTimeText;
	public Text notStreakCountText;

	CashShopData.PickUpCharacterInfo _info;
	public void RefreshInfo(CashShopData.PickUpCharacterInfo info)
	{
		_info = info;

		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(info.id);
		AddressableAssetLoadManager.GetAddressableSprite(actorTableData.portraitAddress, "Icon", (sprite) =>
		{
			characterImage.sprite = null;
			characterImage.sprite = sprite;
		});

		priceText.text = info.price.ToString("N0");

		string gradeString = UIString.instance.GetString(string.Format("GameUI_CharGrade{0}", 2));
		notStreakCountText.SetLocalizedText(UIString.instance.GetString("ShopUI_PickUpCharRemainCount", gradeString, info.bc - CashShopData.instance.GetCurrentPickUpCharacterNotStreakCount()));
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
		UIInstanceManager.instance.ShowCanvasAsync("GachaPickUpCharacterInfoCanvas", null);
	}

	public void OnClickDetailButton()
	{
		Timing.RunCoroutine(ShowCharacterInfoCanvasProcess());
	}

	IEnumerator<float> ShowCharacterInfoCanvasProcess()
	{
		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		CashShopTabCanvas.instance.ignoreStartEventFlag = true;
		CashShopTabCanvas.instance.gameObject.SetActive(false);

		while (CashShopTabCanvas.instance.gameObject.activeSelf)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		MainCanvas.instance.OnClickTeamButton();
		while ((CharacterListCanvas.instance != null && CharacterListCanvas.instance.gameObject.activeSelf) == false)
			yield return Timing.WaitForOneFrame;
		yield return Timing.WaitForOneFrame;

		CharacterListCanvas.instance.OnClickListItem(_info.id);
		while ((CharacterInfoCanvas.instance != null && CharacterInfoCanvas.instance.gameObject.activeSelf) == false)
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
				List<ObscuredString> listActorId = CharacterManager.instance.GetRandomIdList(_info.count, true);
				_count = listActorId.Count;
				PlayFabApiManager.instance.RequestOpenPickUpCharacterBox(listActorId, _info.count, _info.price, CharacterManager.instance.tempPickUpNotStreakCount, OnRecvResult);
			});
		});
	}

	void OnRecvResult(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = CharacterManager.instance.OnRecvItemGrantResult(itemGrantString, _count);
		if (listItemInstance == null)
			return;

		GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.CharacterGacha, _count);

		// 분명히 창은 띄워져있을거다. 없으면 말이 안된다.
		// 
		if (RandomBoxScreenCanvas.instance != null)
			RandomBoxScreenCanvas.instance.OnRecvResult(RandomBoxScreenCanvas.eBoxType.Character, listItemInstance);

		MainCanvas.instance.RefreshMenuButton();

		if (CashShopCanvas.instance != null)
			CashShopCanvas.instance.RefreshPickUpCharacterRect();
	}
}