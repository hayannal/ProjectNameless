using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;

public class CharacterLevelCanvas : MonoBehaviour
{
	public static CharacterLevelCanvas instance;

	public GameObject subLevelUpEffectPrefab;
	public GameObject levelUpEffectPrefab;

	public Image gradeBackImage;
	public Text gradeText;
	public Text nameText;

	public Image[] subLevelImageList;
	public Image[] subLevelUpImagEffectList;

	public Text playerLevelText;
	public Text atkText;

	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;
	public GameObject maxButtonObject;
	public Image maxButtonImage;
	public Text maxButtonText;

	public GameObject saleObject;

	public RectTransform alarmRootTransform;

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
		// hardcode ev8
		_salePrice = CashShopData.instance.IsShowEvent("ev8");
		saleObject.SetActive(_salePrice);

		RefreshInfo();
	}

	#region Info
	ObscuredBool _salePrice = false;
	List<int> _listSubGoldPrice = new List<int>();
	public void RefreshInfo()
	{
		int playerClass = 0;
		switch (playerClass)
		{
			case 0:
				gradeBackImage.color = new Color(0.5f, 0.5f, 0.5f);
				break;
			case 1:
				gradeBackImage.color = new Color(0.0f, 0.51f, 1.0f);
				break;
			case 2:
				gradeBackImage.color = new Color(1.0f, 0.5f, 0.0f);
				break;
		}
		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_PlayerClass{0}", playerClass)));
		nameText.SetLocalizedText(UIString.instance.GetString("GameUI_PlayerName"));

		int level = PlayerData.instance.playerLevel;
		PlayerLevelTableData playerLevelTableData = TableDataManager.instance.FindPlayerLevelTableData(level);
		if (playerLevelTableData == null)
			return;

		playerLevelText.text = UIString.instance.GetString("GameUI_CharPower", level);
		atkText.text = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower).ToString("N0");

		AlarmObject.Hide(alarmRootTransform);
		bool maxLevel = false;
		if (level >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPlayerLevel"))
		{
			priceButtonObject.SetActive(false);

			maxButtonImage.color = ColorUtil.halfGray;
			maxButtonText.color = ColorUtil.halfGray;
			maxButtonObject.SetActive(true);
			maxLevel = true;
		}

		int subLevel = PlayerData.instance.subLevel;
		if (maxLevel)
		{
			// 최대 레벨에 도달하면 서브레벨은 0이지만 최대로 가득 찬것처럼 표현해야한다.
			PlayerLevelTableData prevPlayerLevelTableData = TableDataManager.instance.FindPlayerLevelTableData(level - 1);
			subLevel = prevPlayerLevelTableData.subLevelCount;
		}
		for (int i = 0; i < subLevelImageList.Length; ++i)
		{
			int imageStep = 0;
			int offset = 0;
			if (i == 0) offset = 8;
			else if (i == 1) offset = 7;
			else if (i == 2) offset = 6;
			else if (i == 3) offset = 5;
			else if (i == 4) offset = 4;
			else if (i == 5) offset = 3;
			else if (i == 6) offset = 2;
			else if (i == 7) offset = 1;
			else if (i == 8) offset = 0;
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

		if (!maxLevel)
		{
			int requiredGold = 0;
			// 여기서는 가격 표시
			bool subLevelComplete = (subLevel == playerLevelTableData.subLevelCount);
			if (subLevelComplete)
			{
				// 여기서는 다음 레벨업을 위한 비용을 표시하면 되고
				PlayerLevelTableData nextPlayerLevelTableData = TableDataManager.instance.FindPlayerLevelTableData(level + 1);
				requiredGold = _salePrice ? nextPlayerLevelTableData.saleRequiredGold : nextPlayerLevelTableData.requiredGold;
			}
			else
			{
				// 여기서는 서브 레벨업을 위한 비용을 표시하면 된다.
				if (_salePrice)
					StringUtil.SplitIntList(playerLevelTableData.subGoldSale, ref _listSubGoldPrice);
				else
					StringUtil.SplitIntList(playerLevelTableData.subGold, ref _listSubGoldPrice);
				requiredGold = _listSubGoldPrice[subLevel];
			}

			priceText.text = requiredGold.ToString("N0");
			bool disablePrice = (CurrencyData.instance.gold < requiredGold);
			priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
			priceText.color = !disablePrice ? Color.white : Color.gray;
			priceGrayscaleEffect.enabled = disablePrice;
			priceButtonObject.SetActive(true);
			maxButtonObject.SetActive(false);
			_price = requiredGold;
			_subLevelComplete = subLevelComplete;

			if (!disablePrice)
				AlarmObject.Show(alarmRootTransform);
		}

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
	#endregion

	public static bool CheckLevelUp()
	{
		int level = PlayerData.instance.playerLevel;
		if (level >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPlayerLevel"))
			return false;

		PlayerLevelTableData playerLevelTableData = TableDataManager.instance.FindPlayerLevelTableData(level);
		if (playerLevelTableData == null)
			return false;

		bool salePrice = CashShopData.instance.IsShowEvent("ev8");
		int requiredGold = 0;

		int subLevel = PlayerData.instance.subLevel;
		bool subLevelComplete = (subLevel == playerLevelTableData.subLevelCount);
		if (subLevelComplete)
		{
			PlayerLevelTableData nextPlayerLevelTableData = TableDataManager.instance.FindPlayerLevelTableData(level + 1);
			requiredGold = salePrice ? nextPlayerLevelTableData.saleRequiredGold : nextPlayerLevelTableData.requiredGold;
		}
		else
		{
			List<int> listSubGoldPrice = new List<int>();
			if (salePrice)
				StringUtil.SplitIntList(playerLevelTableData.subGoldSale, ref listSubGoldPrice);
			else
				StringUtil.SplitIntList(playerLevelTableData.subGold, ref listSubGoldPrice);
			requiredGold = listSubGoldPrice[subLevel];
		}
		return (CurrencyData.instance.gold >= requiredGold);
	}

	public void OnClickClassEnhanceButton()
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("SystemUI_WaitUpdate"), 2.0f);
	}

	public void OnClickStoryButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("GameUI_PlayerNameMore"), 200, nameText.transform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickAtkTextButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("StatusDetailCanvas", () =>
		{
			StatusDetailCanvas.instance.Initialize(6);
			StatusDetailCanvas.instance.AddStatus("GameUI_Growth", BattleInstanceManager.instance.playerActor.actorStatus.GetPlayerBaseAttack());
			StatusDetailCanvas.instance.AddStatus("GameUI_Costume", CostumeManager.instance.cachedValue);
			StatusDetailCanvas.instance.AddStatus("GameUI_Skill", SpellManager.instance.cachedValue);
			if (CharacterManager.instance.listCharacterData.Count > 0)
				StatusDetailCanvas.instance.AddStatus("GameUI_Companion", CharacterManager.instance.cachedValue);
			if (PetManager.instance.listPetData.Count > 0)
				StatusDetailCanvas.instance.AddStatus("GameUI_Pet", PetManager.instance.cachedValue);
			if (EquipManager.instance.inventoryItemCount > 0)
				StatusDetailCanvas.instance.AddStatus("GameUI_Equipment", EquipManager.instance.cachedValue);
			StatusDetailCanvas.instance.AddStatus("GameUI_Analysis", AnalysisData.instance.cachedValue);
		});
	}

	public void OnClickCostumeButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("CostumeListCanvas", null);
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
	
	ObscuredInt _price;
	bool _subLevelComplete;
	public void OnClickLevelUpButton()
	{
		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		if (_subLevelComplete)
		{
			float prevValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			PlayFabApiManager.instance.RequestLevelUp(_price, _salePrice, () =>
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
		else
		{
			float prevValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			PlayFabApiManager.instance.RequestSubLevelUp(_price, _salePrice, () =>
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
	}

	#region Press LevelUp
	// 홀드로 레벨업 할땐 클릭으로 할때와 다르게 클라에서 선처리 해야한다.
	float _prevCombatValue;
	int _prevLevel;
	int _prevSubLevel;
	int _prevGold;
	int _levelUpCount;
	int _subLevelUpCount;
	bool _pressed = false;
	public void OnPressInitialize()
	{
		// 패킷에 전송할만한 초기화 내용을 기억해둔다.
		_prevCombatValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		_prevLevel = PlayerData.instance.playerLevel;
		_prevSubLevel = PlayerData.instance.subLevel;
		_prevGold = CurrencyData.instance.gold;
		_levelUpCount = _subLevelUpCount = 0;
		_pressed = true;
	}

	public void OnPressLevelUp()
	{
		if (_pressed == false)
			return;

		if (CurrencyData.instance.gold < _price)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			if (_pressed)
			{
				OnPressUpSync();
				_pressed = false;
			}
			return;
		}

		// 맥스 넘어가는거도 막아놔야한다.
		if (PlayerData.instance.playerLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPlayerLevel"))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
			if (_pressed)
			{
				OnPressUpSync();
				_pressed = false;
			}
			return;
		}

		if (_subLevelComplete)
		{
			_levelUpCount += 1;
			CurrencyData.instance.gold -= _price;
			PlayerData.instance.OnLevelUp();
			PlayLevelUpEffect();
		}
		else
		{
			int subLevelUpImageEffectIndex = PlayerData.instance.subLevel % subLevelUpImagEffectList.Length;
			subLevelUpImagEffectList[subLevelUpImageEffectIndex].gameObject.SetActive(true);
			_subLevelUpCount += 1;
			CurrencyData.instance.gold -= _price;
			PlayerData.instance.OnSubLevelUp();
			PlaySubLevelUpEffect();
		}
		RefreshInfo();
		CharacterCanvas.instance.currencySmallInfo.RefreshInfo();
	}

	float _lastLevelUpEffectTime;
	void PlayLevelUpEffect()
	{
		if (Time.time < _lastLevelUpEffectTime + 2.0f)
			return;
		BattleInstanceManager.instance.GetCachedObject(levelUpEffectPrefab, CharacterCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		_lastLevelUpEffectTime = Time.time;
	}

	float _lastSubLevelUpEffectTime;
	void PlaySubLevelUpEffect()
	{
		if (Time.time < _lastSubLevelUpEffectTime + 0.8f)
			return;
		BattleInstanceManager.instance.GetCachedObject(subLevelUpEffectPrefab, CharacterCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		_lastSubLevelUpEffectTime = Time.time;
	}

	public void OnPressUpSync()
	{
		if (_pressed == false)
			return;
		_pressed = false;

		if (_levelUpCount == 0 && _subLevelUpCount == 0)
			return;
		if (_prevLevel > PlayerData.instance.playerLevel)
			return;
		if (_prevGold < CurrencyData.instance.gold)
			return;

		PlayFabApiManager.instance.RequestPressLevelUp(_prevLevel, _prevSubLevel, _prevGold, PlayerData.instance.playerLevel, PlayerData.instance.subLevel, CurrencyData.instance.gold, _levelUpCount, _subLevelUpCount, _salePrice, () =>
		{
			if (_levelUpCount > 0)
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.LevelUpCharacter, _levelUpCount);
			if (_subLevelUpCount > 0)
				GuideQuestData.instance.OnQuestEvent(GuideQuestData.eQuestClearType.EnhanceCharacter, _subLevelUpCount);

			MainCanvas.instance.RefreshPlayerAlarmObject();

			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(_prevCombatValue, nextValue);
			});
		});
	}
	#endregion


	public void OnClickLevelMaxButton()
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
	}
}