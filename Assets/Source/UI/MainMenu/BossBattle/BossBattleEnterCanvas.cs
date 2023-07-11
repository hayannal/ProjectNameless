using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using DG.Tweening;
using MEC;

public class BossBattleEnterCanvas : MonoBehaviour
{
	public static BossBattleEnterCanvas instance;

	const int SELECT_MAX = 1;

	public Transform titleTextTransform;
	public Text levelText;
	public GameObject newObject;
	public GameObject changeDifficultyButtonObject;
	public Transform previewRootTransform;
	public GameObject callKingButtonObject;
	public Transform kingButtonRootTransform;
	public Text bossNameText;
	public Button bossInfoButton;
	public GameObject refreshButtonObject;
	public Transform xpLevelButtonTransform;
	public Text xpLevelText;
	public Text xpLevelExpText;
	public Image xpLevelExpImage;

	public Text suggestPowerLevelText;
	public Text stagePenaltyText;
	public GameObject selectStartText;
	public Text selectResultText;

	public Text priceText;
	public GameObject buttonObject;
	public Image priceButtonImage;
	public Coffee.UIExtensions.UIEffect priceGrayscaleEffect;

	public RectTransform alarmRootTransform;
	public RectTransform exchangeAlarmRootTransform;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<CharacterCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	void OnEnable()
	{
		RefreshExchangeAlarmObject();

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (restore)
			return;

		RefreshInfo();
		RefreshGrid(true);

		if (SubMissionData.instance.newBossRefreshed)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_NewAppearBoss"), 3.0f);
			SubMissionData.instance.newBossRefreshed = false;
		}
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;
		if (MainCanvas.instance == null)
			return;
	}

	public int selectedDifficulty { get { return _selectedDifficulty; } }
	public int clearDifficulty { get { return _clearDifficulty; } }
	public bool isKingMonster { get { if (kingButtonRootTransform != null) return kingButtonRootTransform.gameObject.activeSelf; return false; } }
	GameObject _cachedPreviewObject;
	StageTableData _bossStageTableData;
	ObscuredInt _selectedDifficulty;
	ObscuredInt _clearDifficulty;
	void RefreshInfo()
	{
		if (_cachedPreviewObject != null)
		{
			_cachedPreviewObject.SetActive(false);
			_cachedPreviewObject = null;
		}

		int currentBossId = SubMissionData.instance.bossBattleId;
		if (currentBossId == 0)
		{
			// 0이라면 처음 보스배틀을 시작하는 유저일거다.
			// 1번 몬스터를 가져와서 셋팅한다.
			currentBossId = 1;
		}

		_bossBattleTableData = TableDataManager.instance.FindBossBattleTableData(currentBossId);
		if (_bossBattleTableData == null)
			return;

		int clearDifficulty = SubMissionData.instance.GetBossBattleClearDifficulty(currentBossId.ToString());
		_selectedDifficulty = SubMissionData.instance.GetBossBattleSelectedDifficulty(currentBossId.ToString());
		if (_selectedDifficulty == 0)
		{
			// _selectedDifficulty이면 한번도 플레이 안했다는거니 bossBattleTable에서 시작 챕터를 가져와야한다.
			_selectedDifficulty = _bossBattleTableData.startDifficulty;
		}
		// 선택한게 클리어난이도+1 보다 크면 뭔가 이상한거다. 조정해준다.
		// 이제 챕터의 난이도에서 시작하게 되면서 이 로직을 사용할 수 없게 되었다.
		//if (_selectedDifficulty > (clearDifficulty + 1))
		//	_selectedDifficulty = (clearDifficulty + 1);

		int bossBattleCount = SubMissionData.instance.GetBossBattleCount(currentBossId.ToString());


		StageTableData bossStageTableData = TableDataManager.instance.FindStageTableData(_bossBattleTableData.stage);
		if (bossStageTableData == null)
			return;

		_bossStageTableData = bossStageTableData;
		levelText.text = string.Format("<size=24>DIFFICULTY</size> {0}", _selectedDifficulty);
		newObject.SetActive(_selectedDifficulty > clearDifficulty);

		int selectableDifficultyCount = clearDifficulty - _bossBattleTableData.startDifficulty + 2;
		changeDifficultyButtonObject.SetActive(selectableDifficultyCount > 1);
		_clearDifficulty = clearDifficulty;
		if (changeDifficultyButtonObject.activeSelf)
		{
			AlarmObject.Hide(alarmRootTransform);
			if (_selectedDifficulty != (clearDifficulty + 1) && ChangeDifficultyCanvasListItem.CheckSelectable(_clearDifficulty + 1) == 0)
				AlarmObject.Show(alarmRootTransform, false, false, true);
		}

		if (string.IsNullOrEmpty(_bossBattleTableData.bossAddress) == false)
		{
			AddressableAssetLoadManager.GetAddressableGameObject(string.Format("Preview_{0}", _bossBattleTableData.bossAddress), "Preview", (prefab) =>
			{
				_cachedPreviewObject = UIInstanceManager.instance.GetCachedObject(prefab, previewRootTransform);
			});
		}
		bossNameText.SetLocalizedText(UIString.instance.GetString(_bossBattleTableData.nameId));

		RefreshBossBattleCount(bossBattleCount);

		/*
		// 패널티를 구할땐 그냥 스테이지 테이블에서 구해오면 안되고 선택된 난이도의 1층을 구해와서 처리해야한다.
		StageTableData penaltyStageTableData = BattleInstanceManager.instance.GetCachedStageTableData(_selectedDifficulty, 1, false);
		if (penaltyStageTableData == null)
			return;
		*/

		stagePenaltyText.gameObject.SetActive(false);
		/*
		string penaltyString = ChapterCanvas.GetPenaltyString(penaltyStageTableData);
		if (string.IsNullOrEmpty(penaltyString) == false)
		{
			stagePenaltyText.SetLocalizedText(penaltyString);
			stagePenaltyText.gameObject.SetActive(true);
		}
		*/

		// king 체크
		int lastClearId = SubMissionData.instance.bossBattleClearId;
		bool currentKing = ((lastClearId + 1) == _bossBattleTableData.num && currentBossId > lastClearId);
		if (currentKing)
		{
			// 난이도 검사도 한다. 선택할 수 있는 최고 난이도여야한다.
			// 근데 최고 난이도로 검사하지 않아도 되는게
			// 최초로 조우하면 클리어한 난이도가 0으로 뜰테니 그거로 판단하기로 한다.
			if (_clearDifficulty != 0)
				currentKing = false;
		}
		kingButtonRootTransform.gameObject.SetActive(currentKing);

		bool callableKing = false;
		if (currentKing == false)
		{
			// 현재가 왕관 몬스터가 아닐때 호출 버튼은 하이드 하지 않기로 한다. 토스트로 알려줘야하기 때문.
			if (lastClearId <= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattle"))
				callableKing = true;
		}
		callKingButtonObject.SetActive(callableKing);

		// 3 이상은 되어야 현재 보스 제외하고 랜덤을 돌릴 수 있게 된다.
		refreshButtonObject.SetActive(lastClearId > 2);

		// price
		RefreshPrice();
	}

	void RefreshExchangeAlarmObject()
	{
		AlarmObject.Hide(exchangeAlarmRootTransform);

		bool showAlarm = false;
		if (PointShopAttackCanvas.CheckLevelUp())
			showAlarm = true;
		if (showAlarm == false)
		{
			PointShopTableData pointShopTableData = TableDataManager.instance.FindPointShopTableData(1, 5);
			if (pointShopTableData != null && SubMissionData.instance.bossBattlePoint >= pointShopTableData.price)
				showAlarm = true;
		}
		if (showAlarm)
			AlarmObject.Show(exchangeAlarmRootTransform);
	}

	BossBattleTableData _bossBattleTableData;
	public BossBattleTableData GetBossBattleTableData() { return _bossBattleTableData; }

	#region Preload Reopen
	public static void PreloadReadyToReopen()
	{
		// 보스전 하고와서 되돌아오자마자 바로 보스전 열때 끊기는거 같아서 넣는 프리로드
		// 위 RefreshInfo에서 하는 코드와 비슷해서 근처에 둔다.
		AddressableAssetLoadManager.GetAddressableGameObject("BossBattleEnterCanvas", "Canvas");

		int currentBossId = SubMissionData.instance.bossBattleId;
		BossBattleTableData bossBattleTableData = TableDataManager.instance.FindBossBattleTableData(currentBossId);
		if (bossBattleTableData == null)
			return;
		if (string.IsNullOrEmpty(bossBattleTableData.bossAddress) == false)
			AddressableAssetLoadManager.GetAddressableGameObject(string.Format("Preview_{0}", bossBattleTableData.bossAddress), "Preview");
	}
	#endregion

	ObscuredInt _xpLevel = 1;
	public int GetXpLevel() { return _xpLevel; }
	ObscuredInt _xp = 0;
	public int GetXp() { return _xp; }
	void RefreshBossBattleCount(int count)
	{
		// 현재 카운트가 속하는 테이블 구해와서 레벨 및 경험치로 표시.
		_xp = count;
		_xpLevel = 1;
		int maxXpLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattleXpLevel");
		int level = 0;
		float percent = 0.0f;
		int currentPeriodExp = 0;
		int currentPeriodExpMax = 0;
		for (int i = _xpLevel; i < TableDataManager.instance.bossExpTable.dataArray.Length; ++i)
		{
			if (count < TableDataManager.instance.bossExpTable.dataArray[i].requiredAccumulatedExp)
			{
				currentPeriodExp = count - TableDataManager.instance.bossExpTable.dataArray[i - 1].requiredAccumulatedExp;
				currentPeriodExpMax = TableDataManager.instance.bossExpTable.dataArray[i].requiredExp;
				percent = (float)currentPeriodExp / (float)currentPeriodExpMax;
				level = TableDataManager.instance.bossExpTable.dataArray[i].xpLevel - 1;
				break;
			}
			if (TableDataManager.instance.bossExpTable.dataArray[i].xpLevel >= maxXpLevel)
			{
				currentPeriodExp = count - TableDataManager.instance.bossExpTable.dataArray[i - 1].requiredAccumulatedExp;
				currentPeriodExpMax = TableDataManager.instance.bossExpTable.dataArray[i].requiredExp;
				level = maxXpLevel;
				percent = 1.0f;
				break;
			}
		}

		_xpLevel = level;
		string xpLevelString = "";
		if (level == maxXpLevel)
		{
			xpLevelString = UIString.instance.GetString("GameUI_Lv", "Max");
			xpLevelExpImage.color = MailCanvasListItem.GetGoldTextColor();
		}
		else
		{
			xpLevelString = UIString.instance.GetString("GameUI_Lv", level);
			xpLevelExpImage.color = Color.white;
		}
		xpLevelText.text = string.Format("XP {0}", xpLevelString);
		xpLevelExpText.text = string.Format("{0} / {1}", currentPeriodExp, currentPeriodExpMax);
		xpLevelExpImage.fillAmount = percent;
	}

	void RefreshPrice()
	{
		// 가격
		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyBossBattle");
		priceText.text = price.ToString("N0");
		bool disablePrice = (CurrencyData.instance.ticket < price);
		priceButtonImage.color = !disablePrice ? Color.white : ColorUtil.halfGray;
		priceText.color = !disablePrice ? Color.white : Color.gray;
		priceGrayscaleEffect.enabled = disablePrice;
		_price = price;
	}

	public void OnChangeDifficulty(int difficulty)
	{
		ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_ChangeDifficulty"), 2.0f);
		SubMissionData.instance.SelectBossBattleDifficulty(difficulty);
		RefreshInfo();
		//RefreshGrid(false);
		//OnClickListItem(_selectedActorId);
	}

	List<CharacterData> _listTempCharacterData = new List<CharacterData>();
	List<CharacterCanvasListItem> _listCharacterCanvasListItem = new List<CharacterCanvasListItem>();
	void RefreshGrid(bool onEnable)
	{
		for (int i = 0; i < _listCharacterCanvasListItem.Count; ++i)
			_listCharacterCanvasListItem[i].gameObject.SetActive(false);
		_listCharacterCanvasListItem.Clear();

		if (_bossBattleTableData == null)
			return;

		_listTempCharacterData.Clear();
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_PlayerActorId || TableDataManager.instance.actorTable.dataArray[i].actorId == CharacterData.s_DroneActorId)
				continue;
			if (CharacterManager.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId) == false)
				continue;
			_listTempCharacterData.Add(CharacterManager.instance.GetCharacterData(TableDataManager.instance.actorTable.dataArray[i].actorId));
		}

		if (_listTempCharacterData.Count > 0)
		{
			_listTempCharacterData.Sort(delegate (CharacterData x, CharacterData y)
			{
				if (x.cachedActorTableData.grade > y.cachedActorTableData.grade) return -1;
				else if (x.cachedActorTableData.grade < y.cachedActorTableData.grade) return 1;
				if (x.transcend > y.transcend) return -1;
				else if (x.transcend < y.transcend) return 1;
				if (x.level > y.level) return -1;
				else if (x.level < y.level) return 1;
				if (x.cachedActorTableData.orderIndex > y.cachedActorTableData.orderIndex) return 1;
				else if (x.cachedActorTableData.orderIndex < y.cachedActorTableData.orderIndex) return -1;
				return 0;
			});

			for (int i = 0; i < _listTempCharacterData.Count; ++i)
			{
				CharacterCanvasListItem characterCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
				characterCanvasListItem.Initialize(_listTempCharacterData[i].actorId, _listTempCharacterData[i].level, _listTempCharacterData[i].transcend, true, 0, _bossBattleTableData.suggestedActorId, null, OnClickListItem);
				_listCharacterCanvasListItem.Add(characterCanvasListItem);
			}
		}

		_selectedActorId = "";
		selectStartText.gameObject.SetActive(true);
		selectResultText.text = "";

		if (onEnable)
		{
			string cachedLastCharacter = GetCachedLastCharacter();
			if (string.IsNullOrEmpty(cachedLastCharacter) == false)
				OnClickListItem(cachedLastCharacter);
		}
		//else
		//	OnClickListItem(_selectedActorId);
	}

	public void OnClickListItem(string actorId)
	{
		_selectedActorId = actorId;

		bool recommanded = false;
		for (int i = 0; i < _listCharacterCanvasListItem.Count; ++i)
		{
			bool showSelectObject = (_listCharacterCanvasListItem[i].actorId == actorId);
			_listCharacterCanvasListItem[i].selectObject.SetActive(showSelectObject);
			if (showSelectObject)
				recommanded = _listCharacterCanvasListItem[i].recommandedText.gameObject.activeSelf;
		}

		selectStartText.gameObject.SetActive(string.IsNullOrEmpty(_selectedActorId));
		selectResultText.gameObject.SetActive(string.IsNullOrEmpty(_selectedActorId) == false);

		string firstText = "";
		//if (MainSceneBuilder.instance.lobby == false && BattleInstanceManager.instance.IsInBattlePlayerList(actorId))
		//	firstText = UIString.instance.GetString("GameUI_FirstSwapHealNotApplied");

		string secondText = "";
		/*
		if (BattleManager.instance != null && BattleManager.instance.IsNodeWar()) { }
		else if (_bossChapterTableData != null)
		{
			CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
			if (characterData.powerLevel > _bossChapterTableData.suggestedMaxPowerLevel)
				secondText = UIString.instance.GetString("BossUI_TooPowerfulToReward"); // GameUI_TooPowerfulToReward
			else if (characterData.powerLevel < _bossChapterTableData.suggestedPowerLevel && recommanded)
				secondText = UIString.instance.GetString("GameUI_TooWeakToBoss");
		}
		*/
		bool firstResult = string.IsNullOrEmpty(firstText);
		bool secondResult = string.IsNullOrEmpty(secondText);
		if (firstResult && secondResult)
			selectResultText.text = "";
		else if (firstResult == false && secondResult)
			selectResultText.SetLocalizedText(firstText);
		else if (firstResult && secondResult == false)
			selectResultText.SetLocalizedText(secondText);
		else
			selectResultText.SetLocalizedText(string.Format("{0}\n{1}", firstText, secondText));

		selectResultText.text = string.Format("{0} / {1}", string.IsNullOrEmpty(_selectedActorId) ? 0 : 1, SELECT_MAX);
	}

	public void OnClickTitleInfoButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, UIString.instance.GetString("MissionUI_BossBattleTitleMore"), 300, titleTextTransform, new Vector2(0.0f, -35.0f));
	}

	public void OnClickBossInfoButton()
	{
		if (_bossBattleTableData == null)
			return;

		string suggestString = GetSuggestString(_bossBattleTableData.descriptionId, _bossBattleTableData.suggestedActorId);
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.Bottom, suggestString, 200, bossInfoButton.transform, new Vector2(0.0f, -35.0f));
	}

	// SwapCanvas에서 그대로 가져옴
	StringBuilder _stringBuilderFull = new StringBuilder();
	StringBuilder _stringBuilderActor = new StringBuilder();
	string GetSuggestString(string descriptionId, string[] suggestedActorIdList)
	{
		_stringBuilderFull.Remove(0, _stringBuilderFull.Length);
		_stringBuilderActor.Remove(0, _stringBuilderActor.Length);
		for (int i = 0; i < suggestedActorIdList.Length; ++i)
		{
			string actorId = suggestedActorIdList[i];
			string actorName = CharacterData.GetNameByActorId(actorId);
			if (string.IsNullOrEmpty(actorName))
				continue;

			bool applyPenalty = false;
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
			//if (_listCachedPenaltyPowerSource != null && _listCachedPenaltyPowerSource.Contains(actorTableData.powerSource)) applyPenalty = true;

			//if (PlayerData.instance.ContainsActor(actorId) == false)
			//	continue;
			if (_stringBuilderActor.Length > 0)
				_stringBuilderActor.Append(", ");
			_stringBuilderActor.Append(applyPenalty ? "<color=#707070>" : "<color=#00AB00>");
			_stringBuilderActor.Append(actorName);
			_stringBuilderActor.Append("</color>");
		}
		if (_stringBuilderActor.Length == 0)
		{
			bool applyPenalty = false;
			ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(suggestedActorIdList[0]);
			//if (_listCachedPenaltyPowerSource != null && _listCachedPenaltyPowerSource.Contains(actorTableData.powerSource)) applyPenalty = true;

			_stringBuilderActor.Append(applyPenalty ? "<color=#707070>" : "<color=#00AB00>");
			_stringBuilderActor.Append(CharacterData.GetNameByActorId(suggestedActorIdList[0]));
			_stringBuilderActor.Append("</color>");
		}
		_stringBuilderFull.AppendFormat(UIString.instance.GetString(descriptionId), _stringBuilderActor.ToString());
		return _stringBuilderFull.ToString();
	}

	public void OnClickRefreshButton()
	{
		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleRefreshPrice");
		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("MissionUI_RefreshBoss"), CurrencyData.eCurrencyType.Gold, price, false, () =>
			{
				int nextId = SubMissionData.instance.GetNextRandomBossId();
				PlayFabApiManager.instance.RequestRefreshBoss(nextId, price, () =>
				{
					ConfirmSpendCanvas.instance.gameObject.SetActive(false);
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_NewAppearBoss"), 2.0f);
					gameObject.SetActive(false);
					gameObject.SetActive(true);
				});
			});
		});
	}

	public void OnClickChangeDifficultyButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("ChangeDifficultyCanvas", () =>
		{
			ChangeDifficultyCanvas.instance.RefreshInfo(_bossBattleTableData.startDifficulty, _selectedDifficulty, _clearDifficulty);
		});
	}

	public void OnClickXpLevelInfoButton()
	{
		string xpLevelString1 = UIString.instance.GetString("MissionUI_XpLevelMore1");
		string xpLevelString2 = GetDamageBonusString();

		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, string.Format("{0} {1}", xpLevelString1, xpLevelString2), 300, xpLevelButtonTransform, new Vector2(0.0f, -35.0f));
	}

	public float GetDamageBonusByXpLevel()
	{
		return (_xpLevel - 1) * BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleXpLevelBonus100") * 0.01f;
	}

	string GetDamageBonusString()
	{
		return string.Format("<color=#009F50>{0}%</color>", (_xpLevel - 1) * BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleXpLevelBonus100"));
	}

	public void OnClickCallKingButton()
	{
		// > 검사는 안해도 되긴 하지만 혹시 몰라 해둔다. MaxBossBattle까지 클리어 했으면 열려있는건 다 깬거다.
		if (SubMissionData.instance.bossBattleClearId >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattle"))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_CallMaxReached"), 2.0f);
			return;
		}

		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), UIString.instance.GetString("MissionUI_CallKing"), () =>
		{
			PlayFabApiManager.instance.RequestCallKingBoss(() =>
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_KingAppearBoss"), 2.0f);
				gameObject.SetActive(false);
				gameObject.SetActive(true);
			});
		});
	}

	public void OnClickKingInfoButton()
	{
		TooltipCanvas.Show(true, TooltipCanvas.eDirection.CharacterInfo, UIString.instance.GetString("MissionUI_KingBossMore"), 200, kingButtonRootTransform, new Vector2(24.0f, -35.0f));
	}

	public void OnClickExchangeButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("PointShopTabCanvas", null);
	}



	int _price;
	public string selectedActorId { get { return _selectedActorId; } }
	string _selectedActorId;
	public void OnClickYesButton()
	{
		if (_moveProcessed)
			return;
		if (string.IsNullOrEmpty(_selectedActorId))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_BossBattleSelectMember"), 2.0f);
			return;
		}

		Timing.RunCoroutine(BattleMoveProcess());
	}

	// 패킷을 날려놓고 페이드아웃쯤에 오는 서버 응답에 따라 처리가 나뉜다. 
	bool _waitEnterServerResponse;
	bool _enterBossBattleServerFailure;
	bool _networkFailure;
	void PrepareBossBattle()
	{
		// 입장패킷 보내서 서버로부터 제대로 응답오는지 기다려야한다.
		PlayFabApiManager.instance.RequestEnterBossBattle(_selectedDifficulty, (serverFailure) =>
		{
			DelayedLoadingCanvas.Show(false);
			if (_waitEnterServerResponse)
			{
				// 에너지가 없는데 도전
				_enterBossBattleServerFailure = serverFailure;
				_waitEnterServerResponse = false;
				SubMissionData.instance.SelectBossBattleDifficulty(_selectedDifficulty);
			}
		}, () =>
		{
			DelayedLoadingCanvas.Show(false);
			if (_waitEnterServerResponse)
			{
				// 그외 접속불가 네트워크 에러
				_networkFailure = true;
				_waitEnterServerResponse = false;
			}
		});
		_waitEnterServerResponse = true;
	}

	bool _moveProcessed;
	IEnumerator<float> BattleMoveProcess()
	{
		_moveProcessed = true;

		// 이거로 막아둔다.
		DelayedLoadingCanvas.Show(true);

		// 보안 이슈로 Enter Flag는 받아둔다. 기존꺼랑 겹치지 않게 별도의 enterFlag다.
		PrepareBossBattle();

		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		if (this == null)
			yield break;

		while (_waitEnterServerResponse)
			yield return Timing.WaitForOneFrame;
		if (_enterBossBattleServerFailure || _networkFailure)
		{
			FadeCanvas.instance.FadeIn(0.4f);

			// 서버 에러 오면 안된다. 뭔가 잘못된거다.
			if (_enterBossBattleServerFailure)
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("MissionUI_BossBattleWrong"), 2.0f);
			_enterBossBattleServerFailure = false;
			_networkFailure = false;
			// 알파가 어느정도 빠지면 _processing을 풀어준다.
			yield return Timing.WaitForSeconds(0.2f);
			_moveProcessed = false;
			yield break;
		}

		StageManager.instance.FinalizeStage();
		TeamManager.instance.HideForMoveMap(true);

		yield return Timing.WaitForSeconds(0.1f);

		if (this == null)
			yield break;

		StageManager.instance.InitializeMissionStage(_bossBattleTableData.stage);
		//TeamManager.instance.HideForMoveMap(false);
		TeamManager.instance.ClearTeamPlayerActorForMission();
		RecordLastCharacter();

		// preload
		AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(_selectedActorId), "");

		yield return Timing.WaitForSeconds(0.2f);

		// 보스전처럼
		Screen.sleepTimeout = SleepTimeout.NeverSleep;

		UIInstanceManager.instance.ShowCanvasAsync("BossBattleMissionCanvas", () =>
		{
			DelayedLoadingCanvas.Show(false);
			FadeCanvas.instance.FadeIn(0.5f);
		});

		_moveProcessed = false;
	}

	#region Record Last Character
	void RecordLastCharacter()
	{
		string key = _bossBattleTableData.num.ToString();
		string value = _selectedActorId;
		ObscuredPrefs.SetString(string.Format("_bbEnterCanvas_{0}___{1}", key, PlayFabApiManager.instance.playFabId), value);
	}

	string GetCachedLastCharacter()
	{
		string key = _bossBattleTableData.num.ToString();
		return ObscuredPrefs.GetString(string.Format("_bbEnterCanvas_{0}___{1}", key, PlayFabApiManager.instance.playFabId));
	}
	#endregion
}