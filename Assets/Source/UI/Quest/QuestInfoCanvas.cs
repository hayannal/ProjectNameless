using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;

public class QuestInfoCanvas : MonoBehaviour
{
	public static QuestInfoCanvas instance;

	public Transform subTitleTransform;
	public Text descText;
	public Text countText;
	public QuestInfoItem info;
	public Image gaugeImage;
	public Text gaugeText;
	public Text completeText;

	public Text priceText;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	public Text claimText;
	public Image claimButtonImage;

	void Awake()
	{
		instance = this;
	}

	ObscuredBool _complete;
	int _price;
	int _addEnergy;
	void OnEnable()
	{
		SubQuestData.QuestInfo questInfo = SubQuestData.instance.FindQuestInfoByIndex(SubQuestData.instance.currentQuestIndex);
		if (questInfo == null)
			return;

		info.RefreshInfo(questInfo);
		_addEnergy = questInfo.rwd;

		RefreshCountInfo();

		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("SubQuestGoldDoubleDiamond");
		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.dia < price || !_complete);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;

		bool disableButton = !_complete;
		claimButtonImage.color = !disableButton ? Color.white : ColorUtil.halfGray;
		claimText.color = !disableButton ? Color.white : Color.gray;

		_price = price;
	}

	public void RefreshCountInfo()
	{
		SubQuestData.QuestInfo questInfo = SubQuestData.instance.FindQuestInfoByIndex(SubQuestData.instance.currentQuestIndex);
		if (questInfo == null)
			return;

		// 진행도 표시
		int currentCount = SubQuestData.instance.GetProceedingCount();
		int maxCount = questInfo.cnt;

		// 완료 체크
		_complete = (currentCount >= maxCount);

		if (_complete)
		{
			descText.SetLocalizedText(UIString.instance.GetString("QuestUI_OneDone"));
			gaugeImage.color = MailCanvasListItem.GetGoldTextColor();
			completeText.text = UIString.instance.GetString("QuestUI_QuestCompleteNoti");
		}
		else
		{
			descText.SetLocalizedText(UIString.instance.GetString("QuestUI_NowQuest"));
			gaugeImage.color = Color.white;
		}
		gaugeImage.fillAmount = (float)(currentCount) / maxCount;
		gaugeText.text = string.Format("{0} / {1}", Mathf.Min(currentCount, maxCount), maxCount);
		completeText.gameObject.SetActive(_complete);
		countText.text = string.Format("{0} / {1}", SubQuestData.instance.todayQuestRewardedCount, SubQuestData.DailyMaxCount);
	}

	public void OnClickMoreButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("QuestUI_SubQuestMore"), 300, subTitleTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickDoubleClaimButton()
	{
		if (!_complete)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("QuestUI_CompleteFirst"), 2.0f);
			return;
		}

		if (CurrencyData.instance.dia < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughDiamond"), 2.0f);
			return;
		}

		_addEnergy *= 2;
		PlayFabApiManager.instance.RequestCompleteQuest(true, _price, _addEnergy, OnRecvCompleteQuest);
	}

	public void OnClickClaimButton()
	{
		if (!_complete)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("QuestUI_CompleteFirst"), 2.0f);
			return;
		}

		PlayFabApiManager.instance.RequestCompleteQuest(false, 0, _addEnergy, OnRecvCompleteQuest);
	}

	void OnRecvCompleteQuest()
	{
		UIInstanceManager.instance.ShowCanvasAsync("CommonRewardCanvas", () =>
		{
			// 서브 퀘스트 정보창은 직접 닫아야한다.
			gameObject.SetActive(false);
			SubQuestInfo.instance.CloseInfo();

			CommonRewardCanvas.instance.RefreshReward(0, 0, _addEnergy, () =>
			{
				OnCompleteResultCanvas();
			});
		});
	}

	public void OnCompleteResultCanvas()
	{
		// 연출 후 아직 3회 전부한게 아니라면 퀘스트 선택창을 띄운다.
		if (SubQuestData.instance.todayQuestRewardedCount < SubQuestData.DailyMaxCount)
		{
			UIInstanceManager.instance.ShowCanvasAsync("QuestSelectCanvas", null);
			return;
		}

		// 3회 전부 한거라면 토스트 띄울까 했는데 그냥 패스
	}
}