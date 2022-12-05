using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PetInfoCanvas : MonoBehaviour
{
	public static PetInfoCanvas instance;

	public CurrencySmallInfo currencySmallInfo;
	public GameObject todayHeartObject;
	public GameObject dragRectObject;
	public RectTransform alarmRootTransform;

	public Text nameText;
	public GameObject starGridRootObject;
	public GameObject[] starObjectList;
	public GameObject fiveStarObject;

	public Text countText;
	public GameObject countLevelUpButtonObject;
	public Text heartText;
	public GameObject heartImageEffectObject;
	public Text atkText;

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

	}

	void Update()
	{
		PetListCanvas.instance.UpdateAdditionalObject();
	}

	bool _contains = false;
	PetData _petData = null;
	void RefreshInfo()
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
			if (petCountTableData != null && step < BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPetCountStep"))
				countLevelUpButtonObject.SetActive(true);
		}

		RefreshHeart();
	}

	void RefreshHeart()
	{
		bool showHeart = _contains && PetListCanvas.CheckTodayHeart();
		todayHeartObject.SetActive(showHeart);
		dragRectObject.SetActive(showHeart);
		if (showHeart)
			AlarmObject.Show(alarmRootTransform);
		else
			AlarmObject.Hide(alarmRootTransform);
	}

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

	bool _wait = false;
	public void OnHeartDragRect(BaseEventData baseEventData)
	{
		if (_wait)
			return;

		//Debug.Log("Heart Darg On");
		_wait = true;
		PlayFabApiManager.instance.RequestHeartPet(_petData, _petData.heart + 1, () =>
		{
			_wait = false;
			PetListCanvas.instance.currentCanvasPetActor.animator.Play("Heart");
			heartImageEffectObject.SetActive(true);
			RefreshHeart();
			MainCanvas.instance.RefreshPetAlarmObject();
		});
	}

	float _prevCombatValue = 0.0f;
	public void OnClickStepUpButton()
	{
		if (_petData == null)
			return;

		UIInstanceManager.instance.ShowCanvasAsync("ConfirmSpendCanvas", () =>
		{
			PetCountTableData petCountTableData = TableDataManager.instance.FindPetCountTableData(_petData.cachedPetTableData.star, _petData.step);
			PetCountTableData nextPetCountTableData = TableDataManager.instance.FindPetCountTableData(_petData.cachedPetTableData.star, _petData.step + 1);

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
}