using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using PlayFab.ClientModels;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class GuideQuestInfo : MonoBehaviour
{
	public static GuideQuestInfo instance;

	public GameObject smallButtonRootObject;
	public DOTweenAnimation infoRootTweenAnimation;
	public GameObject smallBackButtonRootObject;

	public GameObject contentsRootObject;
	public Sprite[] iconSpriteList;

	public Image iconImage;
	public Text nameText;
	public Image proceedingCountImage;
	public GameObject proceedingCountTextRootObject;
	public Text proceedingCountText;
	public Text additionalRewardText;

	public GameObject goldIconObject;
	public GameObject diaIconObject;
	public GameObject energyIconObject;
	public Text rewardCountText;
	public GameObject specialRewardRootObject;
	public Text specialRewardText;
	public GameObject smallBlinkObject;
	public GameObject blinkObject;
	public GameObject disableTextObject;
	public GameObject needUpdateTextObject;

	public RectTransform alarmRootTransform;
	public RectTransform infoAlarmRootTransform;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		/*
		// GuideQuestInfo오브젝트는 LobbyCanvas에 붙어있기때문에 씬 구축할때 호출되고 이후 쭉 살아있게 된다.
		if (ContentsManager.IsTutorialChapter() || PlayerData.instance.lobbyDownloadState)
			return;
		*/

		RefreshSmallButton();
		RefreshInfo();
		RefreshAlarmObject();
	}

	// Update is called once per frame
	void Update()
    {
		if (_openRemainTime > 0.0f)
		{
			_openRemainTime -= Time.deltaTime;
			if (_openRemainTime <= 0.0f)
			{
				_openRemainTime = 0.0f;
				OnClickSmallBackButton();
			}
		}

		if (_closeRemainTime > 0.0f)
		{
			_closeRemainTime -= Time.deltaTime;
			if (_closeRemainTime <= 0.0f)
			{
				_closeRemainTime = 0.0f;
				infoRootTweenAnimation.gameObject.SetActive(false);
				smallButtonRootObject.SetActive(true);
			}
		}

		if (_claimReopenRemainTime > 0.0f)
		{
			// 이상하게 CharacterBoxShowCanvas가 나오고 나서 CharacterBoxResultCanvas가 나올때는 LobbyCanvas가 미리 보이게 된다.
			// 그래서 결과창 뒤에서 퀘스트 알람이 나오게 되길래 이렇게 예외처리 해둔다.
			bool ignore = false;
			/*
			if (CharacterBoxResultCanvas.instance != null && CharacterBoxResultCanvas.instance.gameObject.activeSelf)
				ignore = true;
			*/

			if (ignore == false)
				_claimReopenRemainTime -= Time.deltaTime;

			if (_claimReopenRemainTime <= 0.0f)
			{
				_claimReopenRemainTime = 0.0f;
				OnClickSmallButton();
			}
		}
	}

	void RefreshSmallButton()
	{
		bool show = false;
		if (GuideQuestData.instance.currentGuideQuestIndex > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxGuideQuestId"))
		{
			// 맥스를 넘으면 업데이트를 기다려야한다는 메세지를 띄워야하므로 보여야한다.
			show = true;
		}

		GuideQuestTableData guideQuestTableData = GuideQuestData.instance.GetCurrentGuideQuestTableData();
		if (guideQuestTableData != null)
			show = true;

		smallButtonRootObject.SetActive(show);

		if (show)
		{
			infoRootTweenAnimation.gameObject.SetActive(false);
			smallBackButtonRootObject.SetActive(false);
			_openRemainTime = _closeRemainTime = 0.0f;
		}
	}

	bool _complete = false;
	void RefreshInfo()
	{
		if (GuideQuestData.instance.currentGuideQuestIndex > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxGuideQuestId"))
		{
			blinkObject.SetActive(false);
			smallBlinkObject.SetActive(false);
			contentsRootObject.SetActive(false);
			needUpdateTextObject.SetActive(true);
			return;
		}

		GuideQuestTableData guideQuestTableData = GuideQuestData.instance.GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		if (guideQuestTableData.iconId < iconSpriteList.Length)
			iconImage.sprite = iconSpriteList[guideQuestTableData.iconId];

		if ((GuideQuestData.eQuestClearType)guideQuestTableData.typeId == GuideQuestData.eQuestClearType.ClearStage)
		{
			//if (int.TryParse(guideQuestTableData.param, out int paramStage))
				nameText.SetLocalizedText(UIString.instance.GetString(guideQuestTableData.descriptionId, guideQuestTableData.needCount));
		}
		else
			nameText.SetLocalizedText(UIString.instance.GetString(guideQuestTableData.descriptionId, guideQuestTableData.needCount));
		RefreshCountInfo();

		if (string.IsNullOrEmpty(guideQuestTableData.rewardAdditionalText))
			additionalRewardText.text = "";
		else
			additionalRewardText.SetLocalizedText(UIString.instance.GetString(guideQuestTableData.rewardAdditionalText));

		goldIconObject.SetActive(false);
		diaIconObject.SetActive(false);
		energyIconObject.SetActive(false);

		if (guideQuestTableData.rewardType == "cu")
		{
			if (guideQuestTableData.rewardValue == CurrencyData.GoldCode())
				goldIconObject.SetActive(true);
			else if (guideQuestTableData.rewardValue == CurrencyData.DiamondCode())
				diaIconObject.SetActive(true);
			else if (guideQuestTableData.rewardValue == CurrencyData.EnergyCode())
				energyIconObject.SetActive(true);
			rewardCountText.text = guideQuestTableData.rewardCount.ToString("N0");
		}
		else if (guideQuestTableData.rewardType == "be")
		{
			/*
			if (guideQuestTableData.rewardValue == "3" && guideQuestTableData.rewardCount >= 3)
				equipBigBoxObject.SetActive(true);
			else
				equipBoxObject.SetActive(true);
			rewardCountText.text = "";
			*/
		}

		specialRewardRootObject.SetActive(false);
		specialRewardText.text = "";
		if (guideQuestTableData.nextNoti > 0)
		{
			string rewardText = string.Format("<color=#FFFF00>{0}</color>", UIString.instance.GetString(guideQuestTableData.nextParameter));
			string countText = string.Format("<size=20><color=#47C9E7>{0}</color></size>", guideQuestTableData.nextNoti);
			specialRewardText.SetLocalizedText(UIString.instance.GetString("QuestUI_NextSpecialReward", rewardText, countText));
			specialRewardRootObject.SetActive(true);
		}
	}

	public void RefreshCountInfo()
	{
		GuideQuestTableData guideQuestTableData = GuideQuestData.instance.GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		//proceedingCountText.text = string.Format("{0} / {1}", GuideQuestData.instance.currentGuideQuestProceedingCount, guideQuestTableData.needCount);
		//proceedingCountImage.fillAmount = (float)GuideQuestData.instance.currentGuideQuestProceedingCount / guideQuestTableData.needCount;

		// 진행도 표시
		int currentCount = GuideQuestData.instance.GetProceedingCount();
		int maxCount = guideQuestTableData.needCount;

		// 완료 체크
		_complete = (currentCount >= maxCount);

		if (_complete)
		{
			//descText.SetLocalizedText(UIString.instance.GetString("QuestUI_OneDone"));
			proceedingCountImage.color = MailCanvasListItem.GetGoldTextColor();
			//completeText.text = UIString.instance.GetString("QuestUI_QuestCompleteNoti");
		}
		else
		{
			//if (PlayerData.instance.currentChaosMode)
			//	descText.SetLocalizedText(UIString.instance.GetString("QuestUI_NowQuest"));
			//else
			//	descText.SetLocalizedText(string.Format("{0}\n{1}", UIString.instance.GetString("QuestUI_NowQuest"), UIString.instance.GetString("QuestUI_NotChaos")));
			proceedingCountImage.color = Color.white;
		}
		proceedingCountImage.fillAmount = (float)(currentCount) / maxCount;
		proceedingCountText.text = string.Format("{0} / {1}", currentCount, maxCount);
		//completeText.gameObject.SetActive(_complete);

		blinkObject.SetActive(_complete);
		smallBlinkObject.SetActive(_complete);
	}

	public void RefreshAlarmObject()
	{
		bool isCompleteQuest = GuideQuestData.instance.IsCompleteQuest();
		bool showAlarm = false;
		bool onlySmallButtonAlarm = false;
		if (isCompleteQuest) showAlarm = true;
		/*
		if (showAlarm == false && GuideQuestData.instance.currentGuideQuestIndex == 0) onlySmallButtonAlarm = true;
		*/

		AlarmObject.Hide(alarmRootTransform);
		AlarmObject.Hide(infoAlarmRootTransform);
		if (showAlarm)
		{
			AlarmObject.Show(alarmRootTransform, true, true);
			AlarmObject.Show(infoAlarmRootTransform, true, true);
		}
		if (onlySmallButtonAlarm)
			AlarmObject.Show(alarmRootTransform, true, true);
	}

	public void CloseInfo()
	{
		smallButtonRootObject.SetActive(true);
		infoRootTweenAnimation.gameObject.SetActive(false);
		smallBackButtonRootObject.SetActive(false);
		_openRemainTime = _closeRemainTime = _claimReopenRemainTime = 0.0f;
	}
	
	public void OnClickBlinkImage()
	{
		// 이제 보상은 로비 아닌데서도 받을 수 있다.
		ClaimReward();
	}

	float _claimReopenRemainTime;
	void ClaimReward()
	{
		// CumulativeEventListItem에서 가져와서 적절히 변형시켜 쓴다.
		GuideQuestTableData guideQuestTableData = GuideQuestData.instance.GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		if (guideQuestTableData.rewardType == "be" || guideQuestTableData.rewardType == "bm")
		{
			/*
			// 상자일 경우 드랍프로세서
			if (TimeSpaceData.instance.IsInventoryVisualMax())
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_ManageInventory"), 2.0f);
				return;
			}

			// 장비박스 했을때처럼 드랍프로세서로부터 하나 뽑아와야한다.
			bool result = PrepareDropProcessor(guideQuestTableData.rewardType, guideQuestTableData.rewardValue, guideQuestTableData.rewardCount);
			if (CheatingListener.detectedCheatTable)
				return;
			if (result == false)
				return;

			if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
				DotMainMenuCanvas.instance.OnClickBackButton();

			PlayFabApiManager.instance.RequestCompleteGuideQuest(GuideQuestData.instance.currentGuideQuestIndex, guideQuestTableData.rewardType, guideQuestTableData.key, 0, 0, 0, 0, DropManager.instance.GetLobbyDropItemInfo(), false, OnRecvEquipBox);
			*/
		}
		else if (guideQuestTableData.rewardType == "bc")
		{
			/*
			// 가장 핵심은 드랍부터 굴려서 보상정보를 얻어오는거다.
			_cachedDropProcessor = DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, "Zoflrfh", "", true, true);
			_cachedDropProcessor.AdjustDropRange(3.7f);
			if (CheatingListener.detectedCheatTable)
				return;

			if (DotMainMenuCanvas.instance != null && DotMainMenuCanvas.instance.gameObject.activeSelf)
				DotMainMenuCanvas.instance.OnClickBackButton();

			PlayFabApiManager.instance.RequestCompleteGuideQuest(GuideQuestData.instance.currentGuideQuestIndex, guideQuestTableData.rewardType, guideQuestTableData.key, 0, 0, 0, 0, null, true, OnRecvCharacterBox);
			*/
		}
		else if (guideQuestTableData.rewardType == "cu")
		{
			bool showCurrencySmallInfo = false;

			int addGold = 0;
			int addDia = 0;
			int addEnergy = 0;
			if (guideQuestTableData.rewardValue == CurrencyData.GoldCode())
			{
				if (CurrencyData.instance.CheckMaxGold())
					return;

				addGold += guideQuestTableData.rewardCount;
				showCurrencySmallInfo = true;
			}
			else if (guideQuestTableData.rewardValue == CurrencyData.DiamondCode())
			{
				addDia += guideQuestTableData.rewardCount;
				showCurrencySmallInfo = true;
			}
			else if (guideQuestTableData.rewardValue == CurrencyData.EnergyCode())
				addEnergy += guideQuestTableData.rewardCount;

			/*
			if (showCurrencySmallInfo)
				CurrencySmallInfoCanvas.Show(true);
			*/

			PlayFabApiManager.instance.RequestCompleteGuideQuest(GuideQuestData.instance.currentGuideQuestIndex, guideQuestTableData.rewardType, guideQuestTableData.key, addDia, addGold, addEnergy, () =>
			{
				if (showCurrencySmallInfo)
				{
					/*
					CurrencySmallInfoCanvas.RefreshInfo();
					*/
					Timing.RunCoroutine(DelayedHideCurrencySmallInfo(2.0f));
				}

				infoRootTweenAnimation.gameObject.SetActive(false);
				smallBackButtonRootObject.SetActive(false);
				_openRemainTime = _closeRemainTime = 0.0f;

				RefreshInfo();
				RefreshAlarmObject();
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);

				// 1.5초 뒤에 바로 받은거처럼 
				_claimReopenRemainTime = 1.5f;
			});
		}
	}

	IEnumerator<float> DelayedHideCurrencySmallInfo(float delay)
	{
		yield return Timing.WaitForSeconds(delay);

		// avoid gc
		if (this == null)
			yield break;
		if (gameObject.activeSelf == false)
			yield break;

		/*
		CurrencySmallInfoCanvas.Show(false);
		*/
	}
	

	#region Show Hide
	public void OnClickSmallButton()
	{
		smallButtonRootObject.SetActive(false);
		infoRootTweenAnimation.gameObject.SetActive(true);
		smallBackButtonRootObject.SetActive(true);
	}

	float _closeRemainTime;
	public void OnClickSmallBackButton()
	{
		if (_closeRemainTime > 0.0f)
			return;
		if (smallBackButtonRootObject.activeSelf == false)
			return;

		smallBackButtonRootObject.SetActive(false);
		infoRootTweenAnimation.DOPlayBackwards();
		_closeRemainTime = 0.6f;
	}

	float _openRemainTime;
	public void OnCompleteInfoRootTweenAnimation()
	{
		if (smallButtonRootObject.activeSelf)
			return;

		smallBackButtonRootObject.SetActive(true);
		_openRemainTime = 4.0f;
	}
	#endregion
}