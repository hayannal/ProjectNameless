using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class CharacterLevelCanvas : MonoBehaviour
{
	public static CharacterLevelCanvas instance;

	public GameObject subLevelUpEffectPrefab;
	public GameObject levelUpEffectPrefab;

	public Image[] subLevel0ImageList;
	public Image[] subLevel1ImageList;
	public Image[] subLevel2ImageList;

	//public Text changeWingText;
	//public Transform changeWingTextTransform;
	public GameObject[] subPriceButtonObjectList;
	public Image[] subPriceButtonImageList;
	public Text[] subPriceTextList;
	public Coffee.UIExtensions.UIEffect[] subPriceGrayscaleEffectList;
	public GameObject[] subMaxButtonObjectList;
	public Image[] subMaxButtonImageList;
	public Text[] subMaxButtonTextList;

	public Text playerLevelText;
	public Text atkText;

	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;
	public GameObject maxButtonObject;
	public Image maxButtonImage;
	public Text maxButtonText;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		GetComponent<Canvas>().worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		RefreshInfo();
	}

	#region Info
	List<int> _listFirstGoldPrice = new List<int>();
	List<int> _listSecondGoldPrice = new List<int>();
	List<int> _listThirdGoldPrice = new List<int>();
	public void RefreshInfo()
	{
		int level = PlayerData.instance.playerLevel;
		PlayerLevelTableData playerLevelTableData = TableDataManager.instance.FindPlayerLevelTableData(level);
		if (playerLevelTableData == null)
			return;

		int subLevel0 = PlayerData.instance.listSubLevel[0];
		int subLevel1 = PlayerData.instance.listSubLevel[1];
		int subLevel2 = PlayerData.instance.listSubLevel[2];
		FillSubLevelImage(subLevel0, playerLevelTableData.firstCount, subLevel0ImageList);
		FillSubLevelImage(subLevel1, playerLevelTableData.secondCount, subLevel1ImageList);
		FillSubLevelImage(subLevel2, playerLevelTableData.thirdCount, subLevel2ImageList);
		bool subLevelComplete = (subLevel0 == playerLevelTableData.firstCount && subLevel1 == playerLevelTableData.secondCount && subLevel2 == playerLevelTableData.thirdCount);


		StringUtil.SplitIntList(playerLevelTableData.firstGold, ref _listFirstGoldPrice);
		StringUtil.SplitIntList(playerLevelTableData.secondGold, ref _listSecondGoldPrice);
		StringUtil.SplitIntList(playerLevelTableData.thirdGold, ref _listThirdGoldPrice);
		for (int i = 0; i < subPriceButtonImageList.Length; ++i)
		{
			int price = 0;
			bool max = false;
			switch (i)
			{
				case 0:
					max = (subLevel0 == playerLevelTableData.firstCount);
					if (!max) price = _subLevelUp1Price = _listFirstGoldPrice[subLevel0];
					break;
				case 1:
					max = (subLevel1 == playerLevelTableData.secondCount);
					if (!max) price = _subLevelUp2Price = _listSecondGoldPrice[subLevel1];
					break;
				case 2:
					max = (subLevel2 == playerLevelTableData.thirdCount);
					if (!max) price = _subLevelUp3Price = _listThirdGoldPrice[subLevel2];
					break;
			}
			SetSubLevelPriceButton(i, price, max);
		}

		playerLevelText.text = UIString.instance.GetString("GameUI_CharPower", level);
		atkText.text = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower).ToString("N0");


		if (level >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPlayerLevel"))
		{
			priceButtonObject.SetActive(false);

			maxButtonImage.color = ColorUtil.halfGray;
			maxButtonText.color = ColorUtil.halfGray;
			maxButtonObject.SetActive(true);
		}
		else
		{
			PlayerLevelTableData nextPlayerLevelTableData = TableDataManager.instance.FindPlayerLevelTableData(level + 1);

			int requiredGold = nextPlayerLevelTableData.requiredGold;
			priceText.text = requiredGold.ToString("N0");
			bool disablePrice = (CurrencyData.instance.gold < requiredGold || subLevelComplete == false);
			priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
			priceText.color = !disablePrice ? Color.white : Color.gray;
			priceGrayscaleEffect.enabled = disablePrice;
			priceButtonObject.SetActive(true);
			maxButtonObject.SetActive(false);
			_price = requiredGold;
			_subLevelComplete = subLevelComplete;
		}

		if (StageFloorInfoCanvas.instance != null)
			StageFloorInfoCanvas.instance.RefreshCombatPower();

		/*
		changeWingText.SetLocalizedText(UIString.instance.GetString(hasWing ? "GameUI_ChangeWings" : "GameUI_CreateWings"));
		int requiredDia = 0;
		_changeWingPrice = requiredDia = BattleInstanceManager.instance.GetCachedGlobalConstantInt("WingsChange");
		priceText.text = requiredDia.ToString("N0");
		bool disablePrice = (CurrencyData.instance.dia < requiredDia);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;

		_changeWingLookPrice = requiredDia = BattleInstanceManager.instance.GetCachedGlobalConstantInt("WingsLook");
		lookPriceText.text = requiredDia.ToString("N0");
		disablePrice = (hasWing == false || CurrencyData.instance.dia < requiredDia);
		lookPriceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		lookPriceText.color = !disablePrice ? Color.white : Color.gray;
		lookPriceGrayscaleEffect.enabled = disablePrice;

		_changeWingOptionPrice = requiredDia = BattleInstanceManager.instance.GetCachedGlobalConstantInt("WingsAbility");
		optionPriceText.text = requiredDia.ToString("N0");
		disablePrice = (hasWing == false || CurrencyData.instance.dia < requiredDia);
		optionPriceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		optionPriceText.color = !disablePrice ? Color.white : Color.gray;
		optionPriceGrayscaleEffect.enabled = disablePrice;

		_hasWing = hasWing;
		_prevWingLookId = characterData.wingLookId;
		*/
	}

	void FillSubLevelImage(int subLevel, int subLevelMax, Image[] subLevelImageList)
	{
		for (int i = 0; i < subLevelImageList.Length; ++i)
		{
			int imageStep = 0;
			int offset = 0;
			if (i == 0) offset = 2;
			else if (i == 1) offset = 1;
			else if (i == 2) offset = 0;
			imageStep = (subLevel + offset) / subLevelImageList.Length;
			
			switch (imageStep)
			{
				case 0: subLevelImageList[i].color = Color.clear; break;
				case 1: subLevelImageList[i].color = Color.white; break;
				case 2: subLevelImageList[i].color = Color.yellow; break;
				case 3: subLevelImageList[i].color = Color.green; break;
				case 4: subLevelImageList[i].color = Color.red; break;
			}
		}
	}

	void SetSubLevelPriceButton(int subLevelIndex, int subLevelUpPrice, bool max)
	{
		if (max)
		{
			subPriceButtonObjectList[subLevelIndex].SetActive(false);

			subMaxButtonImageList[subLevelIndex].color = ColorUtil.halfGray;
			subMaxButtonTextList[subLevelIndex].color = ColorUtil.halfGray;
			subMaxButtonObjectList[subLevelIndex].SetActive(true);
		}
		else
		{
			int requiredGold = subLevelUpPrice;
			subPriceTextList[subLevelIndex].text = requiredGold.ToString("N0");
			bool disablePrice = (CurrencyData.instance.gold < requiredGold);
			subPriceButtonImageList[subLevelIndex].color = !disablePrice ? Color.white : ColorUtil.halfGray;
			subPriceTextList[subLevelIndex].color = !disablePrice ? Color.white : Color.gray;
			subPriceGrayscaleEffectList[subLevelIndex].enabled = disablePrice;
			subPriceButtonObjectList[subLevelIndex].SetActive(true);
			subMaxButtonObjectList[subLevelIndex].SetActive(false);
			//_price = price;
		}
	}
	#endregion


	int _subLevelUp1Price;
	public void OnClickSubLevelUp1Button()
	{
		if (CurrencyData.instance.gold < _subLevelUp1Price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		RequestSubLevelUp(0, _subLevelUp1Price);
	}

	int _subLevelUp2Price;
	public void OnClickSubLevelUp2Button()
	{
		if (CurrencyData.instance.gold < _subLevelUp2Price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		RequestSubLevelUp(1, _subLevelUp2Price);
	}

	int _subLevelUp3Price;
	public void OnClickSubLevelUp3Button()
	{
		if (CurrencyData.instance.gold < _subLevelUp3Price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		RequestSubLevelUp(2, _subLevelUp3Price);
	}

	void RequestSubLevelUp(int subLevelIndex, int price)
	{
		if (CheatingListener.detectedCheatTable)
			return;

		float prevValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		PlayFabApiManager.instance.RequestSubLevelUp(subLevelIndex, price, () =>
		{
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.EnhanceCharacter);

			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

			RefreshInfo();
			CharacterCanvas.instance.currencySmallInfo.RefreshInfo();
			BattleInstanceManager.instance.GetCachedObject(subLevelUpEffectPrefab, CharacterCanvas.instance.rootOffsetPosition, Quaternion.identity, null);

			// 변경 완료를 알리고
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(prevValue, nextValue);
			});
		});
	}

	public void OnClickSubLevelMaxButton()
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
	}

	/*
	GameObject _nextWingPrefab;
	IEnumerator<float> ChangeWingProcess(int changeType, int wingLookId)
	{
		// 인풋 차단
		CharacterInfoCanvas.instance.inputLockObject.SetActive(true);

		// 날개 교체는 이펙트 끝날때 바로 해야하니 이펙트가 로딩되었는지를 확인한다.
		if (changeType == 0 || changeType == 1)
		{
			while (_nextWingPrefab == null)
				yield return Timing.WaitForOneFrame;
		}

		// priceButton은 3개나 있기도 하고 골드가 다이아로 바뀌는거 같은 아이콘 변경은 없으니 그냥 둔다.
		//priceButtonObject.SetActive(false);

		// 인풋 막은 상태에서 이펙트
		BattleInstanceManager.instance.GetCachedObject(effectPrefab, CharacterListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(1.5f);

		// 사전 이펙트 끝나갈때쯤 화이트 페이드
		FadeCanvas.instance.FadeOut(0.3f, 0.85f);
		yield return Timing.WaitForSeconds(0.3f);

		// 화이트 페이드의 끝나는 시점에 날개 교체하면서 캔버스 갱신하고 툴팁 표시
		if (changeType == 0 || changeType == 1)
		{
			PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_characterData.actorId);
			if (playerActor != null)
				playerActor.RefreshWing();

			// 들고있을 필요는 없으니 null
			_nextWingPrefab = null;
		}

		RefreshInfo();
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_WingsChangeDone"), 2.0f);
		FadeCanvas.instance.FadeIn(1.5f);

		// 페이드 복구중 1초 지나면
		yield return Timing.WaitForSeconds(1.0f);

		// 인풋 복구
		CharacterInfoCanvas.instance.inputLockObject.SetActive(false);
	}
	*/

	int _price;
	bool _subLevelComplete;
	public void OnClickLevelUpButton()
	{
		if (_subLevelComplete == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotCompleteSubLevel"), 2.0f);
			return;
		}

		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		float prevValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		PlayFabApiManager.instance.RequestLevelUp(_price, () =>
		{
			GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.LevelUpCharacter);

			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

			RefreshInfo();
			CharacterCanvas.instance.currencySmallInfo.RefreshInfo();
			BattleInstanceManager.instance.GetCachedObject(levelUpEffectPrefab, CharacterCanvas.instance.rootOffsetPosition, Quaternion.identity, null);

			// 변경 완료를 알리고
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(prevValue, nextValue);
			});
		});
	}

	public void OnClickLevelMaxButton()
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
	}
}