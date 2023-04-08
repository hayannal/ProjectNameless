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
	public Text proceedingCountText;
	public Text additionalRewardText;

	public RewardIcon rewardIcon;
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
			// 연출 도중에는 퀘스트 갱신되서 보이지 않게 하기위해 이렇게 예외처리 해둔다.
			bool ignore = false;
			if (RandomBoxScreenCanvas.instance != null && RandomBoxScreenCanvas.instance.gameObject.activeSelf)
				ignore = true;

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

		// reward
		rewardIcon.RefreshReward(guideQuestTableData.rewardType, guideQuestTableData.rewardValue, guideQuestTableData.rewardCount);
		rewardIcon.countText.gameObject.SetActive(false);
		rewardIcon.ShowOnlyIcon(true, 0.9f);
		rewardCountText.text = rewardIcon.countText.text;

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

	public void OnClickNameTextButton()
	{
		GuideQuestTableData guideQuestTableData = GuideQuestData.instance.GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		switch ((GuideQuestData.eQuestClearType)guideQuestTableData.typeId)
		{
			case GuideQuestData.eQuestClearType.KillBossMonster:
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("QuestUI_KillBossMonsterInfo"), 2.0f);
				break;
			case GuideQuestData.eQuestClearType.EnhancePlayer:
			case GuideQuestData.eQuestClearType.LevelUpPlayer:
				MainCanvas.instance.OnClickCharacterButton();
				break;
			case GuideQuestData.eQuestClearType.ClearStage:
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("QuestUI_ClearStageInfo"), 2.0f);
				break;
			case GuideQuestData.eQuestClearType.Analysis:
				MainCanvas.instance.OnClickAnalysisButton();
				break;
			case GuideQuestData.eQuestClearType.OpenSummonCanvas:
			case GuideQuestData.eQuestClearType.SpinChargeAlarm:
			case GuideQuestData.eQuestClearType.UseEnergy:
				MainCanvas.instance.OnClickGachaButton();
				break;
			case GuideQuestData.eQuestClearType.FreeFortuneWheel:
				// 더이상 이런 일일 제한 미션을 가이드 퀘에서는 사용하지 않기로 한다.
				break;
			case GuideQuestData.eQuestClearType.SpellGacha:
			case GuideQuestData.eQuestClearType.CharacterGacha:
			case GuideQuestData.eQuestClearType.EquipGacha:
				MainCanvas.instance.OnClickCashShopButton();
				break;
			case GuideQuestData.eQuestClearType.LevelUpSpellTotal:
				MainCanvas.instance.OnClickSpellButton();
				break;
			case GuideQuestData.eQuestClearType.GatherCharacter:
				MainCanvas.instance.OnClickCashShopButton();
				break;
			case GuideQuestData.eQuestClearType.LevelUpCharacter:
				MainCanvas.instance.OnClickTeamButton();
				break;
			case GuideQuestData.eQuestClearType.GatherPet:
			case GuideQuestData.eQuestClearType.GatherPetCount:
				MainCanvas.instance.OnClickContentsButton();
				break;
			case GuideQuestData.eQuestClearType.UseTicket:
				MainCanvas.instance.OnClickContentsButton();
				break;
			case GuideQuestData.eQuestClearType.ClearRushDefense:
			case GuideQuestData.eQuestClearType.ClearBossDefense:
			case GuideQuestData.eQuestClearType.ClearGoldDefense:
				// 더이상 이런 일일 제한 미션을 가이드 퀘에서는 사용하지 않기로 한다.
				// 그러니 가이드에서는 FreeFortune이나 이 3종 미션은 기능상으로는 문제 없지만 유저 체험이 별로일까봐 사용하지 않는거 뿐이다.
				break;
			case GuideQuestData.eQuestClearType.ClearBossBattle:
				MainCanvas.instance.OnClickContentsButton();
				break;
		}
	}
	
	public void OnClickBlinkImage()
	{
		// 이제 보상은 로비 아닌데서도 받을 수 있다.
		ClaimReward();
	}

	float _claimReopenRemainTime;
	List<ObscuredString> _listResultEventItemIdForPacket;
	void ClaimReward()
	{
		// CumulativeEventListItem에서 가져와서 적절히 변형시켜 쓴다.
		GuideQuestTableData guideQuestTableData = GuideQuestData.instance.GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		int addGold = 0;
		int addDia = 0;
		int addEnergy = 0;
		if (_listResultEventItemIdForPacket == null)
			_listResultEventItemIdForPacket = new List<ObscuredString>();
		_listResultEventItemIdForPacket.Clear();

		if (guideQuestTableData.rewardType == "cu")
		{
			if (guideQuestTableData.rewardValue == CurrencyData.GoldCode())
			{
				if (CurrencyData.instance.CheckMaxGold())
					return;

				addGold += guideQuestTableData.rewardCount;
			}
			else if (guideQuestTableData.rewardValue == CurrencyData.DiamondCode())
			{
				addDia += guideQuestTableData.rewardCount;
			}
			else if (guideQuestTableData.rewardValue == CurrencyData.EnergyCode())
				addEnergy += guideQuestTableData.rewardCount;
		}
		else if (guideQuestTableData.rewardType == "it")
		{
			for (int j = 0; j < guideQuestTableData.rewardCount; ++j)
				_listResultEventItemIdForPacket.Add(guideQuestTableData.rewardValue);
		}

		PlayFabApiManager.instance.RequestCompleteGuideQuest(GuideQuestData.instance.currentGuideQuestIndex, guideQuestTableData.rewardType, guideQuestTableData.key, addDia, addGold, addEnergy, _listResultEventItemIdForPacket, () =>
		{
			infoRootTweenAnimation.gameObject.SetActive(false);
			smallBackButtonRootObject.SetActive(false);
			_openRemainTime = _closeRemainTime = 0.0f;

			RefreshInfo();
			RefreshAlarmObject();

			if (guideQuestTableData.rewardType == "cu")
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("ShopUI_GotFreeItem"), 2.0f);

				// 1.5초 뒤에 바로 받은거처럼 
				_claimReopenRemainTime = 1.5f;
			}
			else if (guideQuestTableData.rewardType == "it")
			{
				// 다른 곳과 달리 바로 나오는게 좋을거 같다해서 CommonRewardCanvas 띄우지않고 바로 진행한다.
				ConsumeProductProcessor.instance.ConsumeGacha(guideQuestTableData.rewardValue, guideQuestTableData.rewardCount); 
				_claimReopenRemainTime = 1.0f;
			}

			if (PlayerData.instance.tutorialFlagClearGuideQuest)
				PlayerData.instance.OnCompleteTutorialStep(2);
		});
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