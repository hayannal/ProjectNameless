using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;
using MEC;

public class PetInfoCanvas : MonoBehaviour
{
	public static PetInfoCanvas instance;

	public GameObject rootObject;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject todayHeartObject;
	public GameObject todayHeartButtonObject;
	public GameObject todayHeartPassBonusObject;
	public RectTransform alarmRootTransform;

	public Text nameText;
	public GameObject starGridRootObject;
	public GameObject[] starObjectList;
	public GameObject fiveStarObject;

	public Text countText;
	public GameObject countLevelUpButtonObject;
	public Text heartText;
	public GameObject heartImageEffectObject;
	public GameObject heartEffectPrefab;
	public Text atkText;

	#region Pet Sale
	public GameObject petSaleButtonRootObject;
	public Text petSaleResetRemainText;
	#endregion

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		RefreshInfo();
		RefreshHeart();
	}

	void OnDisable()
	{
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

		DisableAdditionalObjectList();
	}

	void Update()
	{
		PetListCanvas.instance.UpdateAdditionalObject();

		#region Pet Sale
		UpdateStartPetSale();
		UpdatePetSaleResetRemainTime();
		UpdateAdditionalObject();
		#endregion
	}

	bool _contains = false;
	PetData _petData = null;
	public void RefreshInfo()
	{
		PetData petData = PetManager.instance.GetPetData(PetListCanvas.instance.selectedPetId);
		PetTableData petTableData = TableDataManager.instance.FindPetTableData(PetListCanvas.instance.selectedPetId);
		_contains = (petData != null);
		_petData = petData;

		if (_contains)
		{
			atkText.text = petData.mainStatusValue.ToString("N0");
		}
		else
		{
			if (petTableData != null)
			{
				atkText.text = petTableData.accumulatedAtk.ToString("N0");
			}
		}
		countText.gameObject.SetActive(_contains);
		heartText.gameObject.SetActive(_contains);
		if (petData != null)
			heartText.text = petData.heart.ToString("N0");

		nameText.SetLocalizedText(UIString.instance.GetString(petTableData.nameId));

		starGridRootObject.SetActive(petTableData.star <= 4);
		fiveStarObject.SetActive(petTableData.star == 5);
		for (int i = 0; i < starObjectList.Length; ++i)
			starObjectList[i].SetActive(i < petTableData.star);

		int count = 0;
		int step = 0;
		if (petData != null)
		{
			count = petData.count;
			step = petData.step;
		}
		int maxCount = PetCanvasListItem.GetMaxCount(petTableData.star, step);
		if (count > maxCount)
			countText.text = string.Format("{0} / <color=#FF3300>{1}</color>", count, maxCount);
		else
			countText.text = string.Format("{0} / {1}", count, maxCount);

		countLevelUpButtonObject.SetActive(false);
		if (count > maxCount)
		{
			PetCountTableData petCountTableData = TableDataManager.instance.FindPetCountTableData(petTableData.star, step + 1);
			if (petCountTableData != null)
				countLevelUpButtonObject.SetActive(true);
		}

		RefreshPetSale();
	}

	public void RefreshHeart()
	{
		bool showHeart = _contains && PetListCanvas.CheckTodayHeart();
		todayHeartObject.SetActive(showHeart);
		todayHeartButtonObject.SetActive(showHeart);
		todayHeartPassBonusObject.SetActive(showHeart && PetManager.instance.IsPetPass());
		if (showHeart)
			AlarmObject.Show(alarmRootTransform);
		else
			AlarmObject.Hide(alarmRootTransform);

		if (_petData != null)
			heartText.text = _petData.heart.ToString("N0");
	}

	#region Pet Sale
	bool _waitPacket = false;
	void UpdateStartPetSale()
	{
		if (_waitPacket)
			return;
		if (WaitingNetworkCanvas.IsShow())
			return;
		// 2마리 이상일때만 등장한다.
		if (PetManager.instance.listPetData.Count < 2)
			return;
		// petSale 상품이 consume 대기라면 갱신하면 안된다.
		if (CashShopData.instance.IsPurchasedFlag(CashShopData.eCashConsumeFlagType.PetSale))
			return;

		bool send = false;
		if (PetManager.instance.petSaleId == "")
			send = true;
		if (!send && ServerTime.UtcNow > PetManager.instance.petSaleExpireTime && ServerTime.UtcNow > PetManager.instance.petSaleCoolTimeExpireTime)
			send = true;
		if (send == false)
			return;

		// Sale로 선택되는 펫은 현재 인벤에 있는 가장 높은 펫의 등급 + 1 단계다.
		// 예를 들어 인벤에 있는 최대가 2성 펫이면 3성까지 선택된다.
		int highestStar = 1;
		List<PetData> listPetData = new List<PetData>();
		for (int i = 0; i < listPetData.Count; ++i)
		{
			if (highestStar < listPetData[i].cachedPetTableData.star)
				highestStar = listPetData[i].cachedPetTableData.star;
		}
		string startPetSaleId = PetManager.instance.GetRandomResultByStar(highestStar + 1);
		PetTableData petTableData = TableDataManager.instance.FindPetTableData(startPetSaleId);
		_waitPacket = true;
		int givenTime = BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetSaleGivenTime");
		int coolTime = BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetSaleCoolTime");
		PlayFabApiManager.instance.RequestStartPetSale(startPetSaleId, givenTime, coolTime, () =>
		{
			_waitPacket = false;
			RefreshPetSale();
		});
	}

	int _lastRemainTimeSecondForPetSale = -1;
	void UpdatePetSaleResetRemainTime()
	{
		if (_waitPacket)
			return;
		if (WaitingNetworkCanvas.IsShow())
			return;
		if (PetManager.instance.petSaleId == "")
			return;

		if (ServerTime.UtcNow < PetManager.instance.petSaleExpireTime)
		{
			System.TimeSpan remainTime = PetManager.instance.petSaleExpireTime - ServerTime.UtcNow;
			if (_lastRemainTimeSecondForPetSale != (int)remainTime.TotalSeconds)
			{
				if (remainTime.Days > 0)
					petSaleResetRemainText.text = string.Format("{0}d", remainTime.Days, remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				else
					petSaleResetRemainText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastRemainTimeSecondForPetSale = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			petSaleResetRemainText.text = "";

		}
	}

	void RefreshPetSale()
	{
		petSaleButtonRootObject.SetActive(false);
		if (PetManager.instance.IsPetSale() == false)
			return;

		// 버튼 보이게 하고
		petSaleButtonRootObject.SetActive(true);

		// 좌측 오브젝트 준비
		string id = PetManager.instance.petSaleId;
		AddressableAssetLoadManager.GetAddressableGameObject(PetData.GetAddressByPetId(id), "", (prefab) =>
		{
			PetTableData petTableData = TableDataManager.instance.FindPetTableData(id);
			PetSaleTableData petSaleTableData = TableDataManager.instance.FindPetSaleTableData(petTableData.star);
			_additionalPrefab = prefab;
			_additionalCount = petSaleTableData.count;
			_spawnAdditionalFlag = true;
		});
	}

	public void OnClickPetSaleButton()
	{
		Timing.RunCoroutine(ShowSaleProcess());
	}

	IEnumerator<float> ShowSaleProcess()
	{
		rootObject.SetActive(false);
		PetInfoGround.instance.petBattleInfo.gameObject.SetActive(false);

		CustomFollowCamera.instance.cachedTransform.DORotate(new Vector3(CustomFollowCamera.instance.cachedTransform.eulerAngles.x, -40.0f, 0.0f), 0.4f);
		yield return Timing.WaitForSeconds(0.4f);

		// 업데이트를 멈추기 애매해서
		// UI 오브젝트를 숨기고 위에다가 SaleCanvas를 띄우기로 한다.
		UIInstanceManager.instance.ShowCanvasAsync("PetSaleCanvas", null);
	}
	#endregion

	public void OnClickAttackValueTextButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("StatusDetailCanvas", () =>
		{
			StatusDetailCanvas.instance.Initialize(1);

			int baseAtk = 0;
			PetTableData petTableData = TableDataManager.instance.FindPetTableData(PetListCanvas.instance.selectedPetId);
			if (petTableData != null)
			{
				baseAtk = petTableData.accumulatedAtk;
			}


			int count = 1;
			int step = 0;
			if (_petData != null)
			{
				count = _petData.count;
				step = _petData.step;
			}
			int maxCount = PetCanvasListItem.GetMaxCount(petTableData.star, step);
			string countString = "";
			if (count > maxCount)
				countString = string.Format("<color=#FF0000>{0}</color>", maxCount);
			else
				countString = string.Format("{0}", count);

			// limit
			StatusDetailCanvas.instance.AddStatus("PetUI_Status", string.Format("{0} x {1}", baseAtk, countString));
		});
	}



	public void OnDragRect(BaseEventData baseEventData)
	{
		PetListCanvas.instance.OnDragRect(baseEventData);
	}

	public void OnClickHeartButton()
	{
		string message = UIString.instance.GetString("PetUI_UseTodayHeartConfirm");
		if (PetManager.instance.IsPetPass())
			message = string.Format("{0}\n{1}", message, UIString.instance.GetString("PetUI_UseTodayHeartThree"));
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), message, () =>
		{
			PlayFabApiManager.instance.RequestHeartPet(_petData, _petData.heart + 1, () =>
			{
				PetListCanvas.instance.currentCanvasPetActor.animator.Play("Heart");
				heartImageEffectObject.SetActive(true);
				BattleInstanceManager.instance.GetCachedObject(heartEffectPrefab, PetListCanvas.instance.currentCanvasPetActor.cachedTransform.position + new Vector3(0.0f, 0.0f, -0.5f), Quaternion.identity);
				RefreshHeart();
				MainCanvas.instance.RefreshPetAlarmObject();
			});
		});
	}

	float _prevCombatValue = 0.0f;
	public void OnClickStepUpButton()
	{
		if (_petData == null)
			return;

		if (_petData.step >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPetCountStep"))
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("PetUI_MaxStepReached"), 2.0f);
			return;
		}

		PetCountTableData nextPetCountTableData = TableDataManager.instance.FindPetCountTableData(_petData.cachedPetTableData.star, _petData.step + 1);
		if (CurrencyData.instance.gold < nextPetCountTableData.cost)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_NotEnoughGold"), 2.0f);
			return;
		}

		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			PetCountTableData petCountTableData = TableDataManager.instance.FindPetCountTableData(_petData.cachedPetTableData.star, _petData.step);

			string nextValue = string.Format("<color=#FF3300>{0}</color> -> {1}", petCountTableData.max, nextPetCountTableData.max);
			string message = string.Format("{0}\n\n{1}", UIString.instance.GetString("PetUI_ConfirmMaxCount"), nextValue);
			ConfirmSpendCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), message, CurrencyData.eCurrencyType.Gold, nextPetCountTableData.cost, false, () =>
			{
				_prevCombatValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
				PlayFabApiManager.instance.RequestPetMaxCount(_petData, _petData.step + 1, nextPetCountTableData.cost, () =>
				{
					ConfirmSpendCanvas.instance.gameObject.SetActive(false);
					OnRecvPetMaxCount();
				});
			});
		});
	}

	void OnRecvPetMaxCount()
	{
		RefreshInfo();
		currencySmallInfo.RefreshInfo();
		PetListCanvas.instance.RefreshGrid();
		PetInfoGround.instance.ShowMaxCountUpEffect();

		float nextValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);
		UIInstanceManager.instance.ShowCanvasAsync("ChangePowerCanvas", () =>
		{
			ChangePowerCanvas.instance.ShowInfo(_prevCombatValue, nextValue);
		});
	}

	public void OnClickButton()
	{
		if (_contains == false)
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("GameUI_MainPetDontHave"), 2.0f);
			return;
		}

		if (string.IsNullOrEmpty(PetManager.instance.activePetId) == false)
		{
			if (PetListCanvas.instance.selectedPetId == PetManager.instance.activePetId)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("PetUI_AlreadyPosition"), 2.0f);
				return;
			}
		}

		PlayFabApiManager.instance.RequestSelectActivePet(PetListCanvas.instance.selectedPetId, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("PetUI_SelectedToast"), 2.0f);
			PetListCanvas.instance.RefreshGrid();
			TeamManager.instance.SpawnActivePet();
		});
	}

	// 이거 PetShowCanvasBase 에 있는 코드인데 코드 중복을 피하기가 애매해서 그냥 복사해서 쓴다.
	#region Pet Sale
	Dictionary<string, GameObject> _dicAdditionalPrefab = new Dictionary<string, GameObject>();
	GameObject _additionalPrefab;
	int _additionalCount;
	bool _spawnAdditionalFlag = false;
	float _addRemainTime;
	List<GameObject> _listAdditionalObject = new List<GameObject>();
	void UpdateAdditionalObject()
	{
		if (_spawnAdditionalFlag == false)
			return;

		_addRemainTime -= Time.deltaTime;
		if (_addRemainTime < 0.0f)
		{
			Vector3 randomPosition = PetListCanvas.instance.rootOffsetPosition + new Vector3(-2.1f, 0.0f, -0.3f);
			Vector2 offset = UnityEngine.Random.insideUnitCircle;
			randomPosition.x += offset.x * 0.8f;
			randomPosition.z += offset.y * 0.8f;
			GameObject newObject = BattleInstanceManager.instance.GetCachedObject(_additionalPrefab, randomPosition, Quaternion.Euler(0.0f, UnityEngine.Random.Range(0.0f, 360.0f), 0.0f));
			newObject.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
			newObject.GetComponent<PetActor>().animator.Play("Idle", 0, UnityEngine.Random.Range(0.0f, 0.8f));
			_listAdditionalObject.Add(newObject);

			_additionalCount -= 1;
			if (_additionalCount > 0)
			{
				_addRemainTime += 0.05f;
			}
			else
				_spawnAdditionalFlag = false;
		}
	}

	void DisableAdditionalObjectList()
	{
		_spawnAdditionalFlag = false;
		_addRemainTime = 0.0f;

		// 지금까지 만들어진 오브젝트들 꺼놔야한다.
		for (int i = 0; i < _listAdditionalObject.Count; ++i)
			_listAdditionalObject[i].SetActive(false);
		_listAdditionalObject.Clear();
	}
	#endregion
}