using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MEC;

public class CharacterInfoGrowthCanvas : MonoBehaviour
{
	public static CharacterInfoGrowthCanvas instance;

	public GameObject levelUpEffectPrefab;

	public Image gradeBackImage;
	public Text gradeText;
	public Text nameText;
	public Text powerSourceText;

	public Text levelText;
	public Text atkText;

	#region Transcend
	public GameObject[] fillImageObjectList;
	public GameObject[] tweenAnimationObjectList;
	public GameObject[] subTweenAnimationObjectList;

	public GameObject maxInfoObject;
	public GameObject materialInfoObject;
	public Transform needOriginTextTransform;
	public Text needOriginCountText;
	public GameObject transcendButtonObject;
	public GameObject transcendEmptyObject;

	public Image trPriceButtonImage;
	public Text trPriceText;
	public Coffee.UIExtensions.UIEffect trPriceGrayscaleEffect;
	#endregion

	public GameObject sliderRectObject;
	public Slider ppSlider;
	public Image sliderFillImage;
	public Text ppText;

	public GameObject priceButtonObject;
	public Image priceButtonImage;
	public Text priceText;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;
	public GameObject maxButtonObject;
	public Image maxButtonImage;
	public Text maxButtonText;

	public RectTransform trpAlarmRootTransform;
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
		RefreshInfo();
	}

	#region Info
	string _actorId;
	bool _contains;
	public void RefreshInfo()
	{
		string actorId = CharacterListCanvas.instance.selectedActorId;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);

		switch (actorTableData.grade)
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
		gradeText.SetLocalizedText(UIString.instance.GetString(string.Format("GameUI_CharGrade{0}", actorTableData.grade)));
		nameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
		powerSourceText.SetLocalizedText(PowerSource.Index2Name(actorTableData.powerSource));

		_actorId = actorId;
		_contains = CharacterManager.instance.ContainsActor(actorId);

		bool defaultTranscend = false;
		CharacterData characterData = CharacterManager.instance.GetCharacterData(actorId);
		if (characterData == null || characterData.transcendPoint == 0)
			defaultTranscend = true;

		if (defaultTranscend)
		{
			for (int i = 0; i < fillImageObjectList.Length; ++i)
			{
				fillImageObjectList[i].gameObject.SetActive(false);
				tweenAnimationObjectList[i].gameObject.SetActive(false);
				subTweenAnimationObjectList[i].gameObject.SetActive(false);
			}
			maxInfoObject.SetActive(false);
			materialInfoObject.SetActive(true);
			needOriginCountText.text = "0 / 1";
			transcendButtonObject.SetActive(false);
			transcendEmptyObject.SetActive(true);
		}
		else if (characterData != null)
		{
			bool appliedTranscendPoint = false;
			for (int i = 0; i < fillImageObjectList.Length; ++i)
			{
				if (characterData.transcend > i)
				{
					fillImageObjectList[i].gameObject.SetActive(true);
					tweenAnimationObjectList[i].gameObject.SetActive(false);
					subTweenAnimationObjectList[i].gameObject.SetActive(false);
				}
				else
				{
					if (characterData.transcendPoint >= characterData.transcend && appliedTranscendPoint == false)
					{
						fillImageObjectList[i].gameObject.SetActive(false);
						tweenAnimationObjectList[i].gameObject.SetActive(true);
						subTweenAnimationObjectList[i].gameObject.SetActive(true);
						appliedTranscendPoint = true;
					}
					else
					{
						fillImageObjectList[i].gameObject.SetActive(false);
						tweenAnimationObjectList[i].gameObject.SetActive(false);
						subTweenAnimationObjectList[i].gameObject.SetActive(false);
					}
				}
			}

			if (characterData.transcend >= TableDataManager.instance.GetGlobalConstantInt("GachaActorMaxTrp"))
			{
				maxInfoObject.SetActive(true);
				materialInfoObject.SetActive(false);
				transcendButtonObject.SetActive(false);
				transcendEmptyObject.SetActive(false);

				priceButtonObject.SetActive(false);
				maxButtonObject.SetActive(true);
				return;
			}

			maxInfoObject.SetActive(false);
			materialInfoObject.SetActive(true);
			transcendButtonObject.SetActive(true);
			transcendEmptyObject.SetActive(false);

			RefreshTranscendRequired();
		}
		RefreshStatus();
		RefreshRequired();
	}

	CharacterData _characterData;
	int _level;
	void RefreshStatus()
	{
		// 구조 바꾸면서 플레이 중에 못찾는건 없어졌는데 Canvas켜둔채 종료하니 자꾸 뜬다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedCanvasPlayerActor(_actorId);
		if (playerActor == null)
			return;

		int level = 1;
		int atk = 0;
		CharacterData characterData = CharacterManager.instance.GetCharacterData(_actorId);
		if (characterData != null)
		{
			level = characterData.level;
			atk = characterData.mainStatusValue;
		}
		else
		{
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
			if (actorTableData != null)
			{
				ActorLevelTableData actorLevelTableData = TableDataManager.instance.FindActorLevelTableData(actorTableData.grade, level);
				if (actorLevelTableData != null)
					atk = actorLevelTableData.accumulatedAtk;
			}
		}

		levelText.text = UIString.instance.GetString("GameUI_CharPower", level);
		atkText.text = atk.ToString("N0");

		_level = level;
		_characterData = characterData;
	}

	int _trpPrice;
	bool _needTranscendPoint;
	void RefreshTranscendRequired()
	{
		AlarmObject.Hide(alarmRootTransform);

		int grade = 0;
		int transcend = 0;
		int transcendPoint = 0;
		bool dontHave = true;
		CharacterData characterData = CharacterManager.instance.GetCharacterData(_actorId);
		if (characterData != null)
		{
			grade = characterData.cachedActorTableData.grade;
			transcend = characterData.transcend;
			transcendPoint = characterData.transcendPoint;
			dontHave = false;
		}
		else
		{
			// 캐릭 없을때도 위에서 다 처리했기 때문에 안들어올거다.
		}

		_needTranscendPoint = false;
		if (transcend >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("GachaActorMaxTrp"))
		{
			// 맥스 넘으면 위에서 다 처리했기때문에 이쪽으로 들어오지 않을거다.
		}
		else
		{
			int current = 0;
			int max = 0;
			int price = 0;
			bool notEnoughPrice = false;
			ActorTranscendTableData actorTranscendTableData = TableDataManager.instance.FindActorTranscendTableData(grade, transcend);
			ActorTranscendTableData nextActorTranscendTableData = TableDataManager.instance.FindActorTranscendTableData(grade, transcend + 1);
			current = transcendPoint - actorTranscendTableData.requiredAccumulatedCount;
			max = nextActorTranscendTableData.requiredCount;
			_needTranscendPoint = current < max;
			price = nextActorTranscendTableData.requiredGold;
			notEnoughPrice = (CurrencyData.instance.gold < price);

			if (!dontHave)
				needOriginCountText.text = UIString.instance.GetString("GameUI_SpacedFraction", current, max);

			trPriceText.text = price.ToString("N0");

			bool disablePrice = (dontHave || notEnoughPrice || current < max);
			trPriceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
			trPriceText.color = !disablePrice ? Color.white : Color.gray;
			trPriceGrayscaleEffect.enabled = disablePrice;
			_trpPrice = price;

			if (max != 0 && current >= max)
				AlarmObject.Show(trpAlarmRootTransform, false, false, true);
		}
	}

	bool _overMaxMode = false;
	int _price;
	bool _needPp;
	void RefreshRequired()
	{
		AlarmObject.Hide(alarmRootTransform);

		int grade = 0;
		int level = 1;
		int pp = 0;
		bool dontHave = true;
		CharacterData characterData = CharacterManager.instance.GetCharacterData(_actorId);
		if (characterData != null)
		{
			grade = characterData.cachedActorTableData.grade;
			level = characterData.level;
			pp = characterData.pp;
			dontHave = false;
			sliderRectObject.SetActive(true);
		}
		else
		{
			sliderRectObject.SetActive(false);
		}

		_overMaxMode = false;
		_needPp = false;
		if (level >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxActorLevel"))
		{
			_overMaxMode = true;
			ppText.text = UIString.instance.GetString("GameUI_OverPp", pp - characterData.maxPp);
			ppSlider.value = 1.0f;
			//sliderFrameImage.color = sliderFillImage.color = Color.white;
			sliderRectObject.SetActive(true);
			priceButtonObject.SetActive(false);

			maxButtonImage.color = ColorUtil.halfGray;
			maxButtonText.color = ColorUtil.halfGray;
			maxButtonObject.SetActive(true);
		}
		else
		{
			int current = 0;
			int max = 0;
			int price = 0;
			bool notEnoughPrice = false;
			ActorLevelTableData actorLevelTableData = TableDataManager.instance.FindActorLevelTableData(grade, level);
			ActorLevelTableData nextActorLevelTableData = TableDataManager.instance.FindActorLevelTableData(grade, level + 1);
			current = pp - actorLevelTableData.requiredAccumulatedCount;
			max = nextActorLevelTableData.requiredCount;
			_needPp = current < max;
			price = nextActorLevelTableData.requiredGold;
			notEnoughPrice = (CurrencyData.instance.gold < price);

			if (!dontHave)
			{
				ppText.text = UIString.instance.GetString("GameUI_SpacedFraction", current, max);
				ppSlider.value = Mathf.Min(1.0f, (float)current / (float)max);
			}
			sliderRectObject.SetActive(!dontHave);
			priceText.text = price.ToString("N0");

			bool disablePrice = (dontHave || notEnoughPrice || current < max);
			priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
			priceText.color = !disablePrice ? Color.white : Color.gray;
			priceGrayscaleEffect.enabled = disablePrice;
			priceButtonObject.SetActive(true);
			maxButtonObject.SetActive(false);
			_price = price;

			if (max != 0 && current >= max)
				AlarmObject.Show(alarmRootTransform, false, false, true);
		}
	}
	#endregion

	public void OnClickStoryButton()
	{
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_actorId);
		if (actorTableData == null)
			return;

		string desc = UIString.instance.GetString(actorTableData.descId);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, desc, 200, nameText.transform, new Vector2(0.0f, -35.0f));

		// 뽑기창에서는 이와 다르게
		// Char CharDesc는 기본으로 나오고 돋보기로만 Story를 본다.
	}

	public void OnClickAttackValueTextButton()
	{

	}

	public void OnClickOrganizeButton()
	{
		if (_contains == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MainCharacterDontHave"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("TeamPositionCanvas", null);
	}

	public void OnClickWingButton()
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("SystemUI_WaitUpdate"), 2.0f);
	}

	public void OnClickNeedOriginTextButton()
	{
		string text = UIString.instance.GetString("GameUI_TranscendenceMaterialMore");  // GameUI_TranscendenceMore
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, text, 200, needOriginTextTransform, new Vector2(0.0f, -30.0f));
	}

	public void OnClickTranscendButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("GameUI_TranscendenceConfirm"), CurrencyData.eCurrencyType.Gold, _trpPrice, false, () =>
			{
				PlayFabApiManager.instance.RequestCharacterTranscend(_characterData, _characterData.transcend + 1, _trpPrice, () =>
				{
					ConfirmSpendCanvas.instance.gameObject.SetActive(false);
					OnRecvTranscend();
				});
			});
		});
	}

	void OnRecvTranscend()
	{

	}


	public void OnClickGaugeDetailButton()
	{
		string text = "";
		if (_overMaxMode)
		{
			float percent = 0.0f;
			if (CharacterManager.instance.ContainsActor(_actorId))
			{
				PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(_actorId);
				if (playerActor != null)
				{
					CharacterData characterData = CharacterManager.instance.GetCharacterData(_actorId);
					if (characterData != null)
						//percent = playerActor.actorStatus.GetAttackAddRateByOverPP(characterData) * 100.0f;
						percent = 0.2f;
				}
			}
			text = UIString.instance.GetString("GameUI_OverMaxDesc", percent);
		}
		else
			text = UIString.instance.GetString("GameUI_CharGaugeDesc");
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, text, 200, ppSlider.transform, new Vector2(0.0f, -30.0f));
	}
	

	#region Press LevelUp
	// 홀드로 레벨업 할땐 클릭으로 할때와 다르게 클라에서 선처리 해야한다. CharacterLevelCanvas에서 하던거 가져와서 prev로 필요한 것들만 추려서 쓴다.
	float _prevCombatValue;
	int _prevCharacterLevel;
	int _prevGold;
	int _levelUpCount;
	bool _pressed = false;
	public void OnPressInitialize()
	{
		// 패킷에 전송할만한 초기화 내용을 기억해둔다.
		_prevCombatValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		_prevCharacterLevel = _level;
		_prevGold = CurrencyData.instance.gold;
		_levelUpCount = 0;
		_pressed = true;
	}

	public void OnPressLevelUp()
	{
		if (_pressed == false)
			return;

		if (_contains == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MainCharacterDontHave"), 2.0f);
			if (_pressed)
			{
				OnPressUpSync();
				_pressed = false;
			}
			return;
		}

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

		if (_needPp)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughPp"), 2.0f);
			if (_pressed)
			{
				OnPressUpSync();
				_pressed = false;
			}
			return;
		}

		// 맥스 넘어가는거도 막아놔야한다.
		if (_characterData.level >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxActorLevel"))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
			if (_pressed)
			{
				OnPressUpSync();
				_pressed = false;
			}
			return;
		}

		_levelUpCount += 1;
		CurrencyData.instance.gold -= _price;
		_characterData.OnLevelUp(_characterData.level + 1);
		PlayLevelUpEffect();
		RefreshInfo();
		CharacterInfoCanvas.instance.currencySmallInfo.RefreshInfo();
	}

	float _lastLevelUpEffectTime;
	void PlayLevelUpEffect()
	{
		if (Time.time < _lastLevelUpEffectTime + 0.8f)
			return;
		BattleInstanceManager.instance.GetCachedObject(levelUpEffectPrefab, CharacterListCanvas.instance.rootOffsetPosition, Quaternion.identity, null);
		_lastLevelUpEffectTime = Time.time;
	}

	public void OnPressUpSync()
	{
		if (_pressed == false)
			return;
		_pressed = false;

		if (_contains == false)
			return;
		if (_levelUpCount == 0)
			return;
		if (_prevCharacterLevel > _characterData.level)
			return;
		if (_prevGold < CurrencyData.instance.gold)
			return;

		PlayFabApiManager.instance.RequestCharacterPressLevelUp(_characterData, _prevCharacterLevel, _prevGold, _characterData.level, CurrencyData.instance.gold, _levelUpCount, () =>
		{
			CharacterListCanvas.instance.RefreshGrid();
			//MainCanvas.instance.RefreshSpellAlarmObject();

			float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
			UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
			{
				ChangePowerCanvas.instance.ShowInfo(_prevCombatValue, nextValue);
			});
		});
	}
	#endregion

	public void OnClickMaxReachedButton()
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MaxReachToast"), 2.0f);
	}
}