using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CashShopTabCanvas : MonoBehaviour
{
	public static CashShopTabCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public RectTransform alarmRootTransform;
	public RectTransform freeAlarmRootTransform;

	#region Tab Button
	public GameObject[] innerMenuPrefabList;
	public Transform innerMenuRootTransform;
	public TabButton[] tabButtonList;
	#endregion

	void Awake()
	{
		instance = this;
	}

	#region CashShop
	float _canvasMatchWidthOrHeightSize;
	float _lineLengthRatio;
	public float lineLengthRatio { get { return _lineLengthRatio; } }
	#endregion
	void Start()
	{
		#region Tab Button
		// 항상 게임을 처음 켤땐 0번탭을 보게 해준다.
		OnValueChangedToggle(0);
		#endregion

		#region CashShop
		// 캐시샵이 열리고나서부터는 직접 IAP Button에서 결과 처리를 하면 된다. 그러니 Listener 꺼둔다.
		IAPListenerWrapper.instance.EnableListener(false);

		CanvasScaler parentCanvasScaler = GetComponentInParent<CanvasScaler>();
		if (parentCanvasScaler == null)
			return;

		if (parentCanvasScaler.matchWidthOrHeight == 0.0f)
		{
			_canvasMatchWidthOrHeightSize = parentCanvasScaler.referenceResolution.x;
			_lineLengthRatio = _canvasMatchWidthOrHeightSize / Screen.width;
		}
		else
		{
			_canvasMatchWidthOrHeightSize = parentCanvasScaler.referenceResolution.y;
			_lineLengthRatio = _canvasMatchWidthOrHeightSize / Screen.height;
		}
		#endregion
	}

	void OnEnable()
	{
		RefreshAlarmObject();

		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (restore)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(true);
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	#region CashShop
	public bool ignoreStartEventFlag { get; set; }
	#endregion
	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;

		#region CashShop
		if (ignoreStartEventFlag)
		{
			ignoreStartEventFlag = false;
			MainCanvas.instance.OnEnterCharacterMenu(false, true);
			return;
		}
		MainCanvas.instance.OnEnterCharacterMenu(false);
		#endregion
	}

	public static bool CheckDailyDiamond()
	{
		if (CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.DailyDiamond) > 0 && CashShopData.instance.dailyDiamondReceived == false)
			return true;

		/*
		if (DailyShopData.instance.GetTodayFreeItemData() != null && DailyShopData.instance.dailyFreeItemReceived == false)
			result = true;
		if (PlayerData.instance.chaosFragmentCount >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("ChaosPowerPointsCost"))
		{
			for (int i = 0; i <= DailyShopData.ChaosSlotMax; ++i)
			{
				if (i <= DailyShopData.instance.chaosSlotUnlockLevel && DailyShopData.instance.IsPurchasedTodayChaosData(i) == false)
					return true;
			}
		}
		*/
		return false;
	}

	public static bool CheckGetFreePackage()
	{
		for (int i = 0; i < TableDataManager.instance.freePackageTable.dataArray.Length; ++i)
		{
			int conValue = TableDataManager.instance.freePackageTable.dataArray[i].conValue;
			if (TableDataManager.instance.freePackageTable.dataArray[i].type == (int)FreePackageGroupInfo.eFreeType.Level)
			{
				if (PlayerData.instance.playerLevel >= conValue && CashShopData.instance.IsRewardedFreeLevelPackage(conValue) == false)
					return true;
			}
			else if (TableDataManager.instance.freePackageTable.dataArray[i].type == (int)FreePackageGroupInfo.eFreeType.Stage)
			{
				if (PlayerData.instance.highestClearStage >= conValue && CashShopData.instance.IsRewardedFreeStagePackage(conValue) == false)
					return true;
			}
		}
		return false;
	}

	public void RefreshAlarmObject()
	{
		AlarmObject.Hide(alarmRootTransform);
		if (CheckDailyDiamond())
			AlarmObject.Show(alarmRootTransform);

		AlarmObject.Hide(freeAlarmRootTransform);
		if (CheckGetFreePackage())
			AlarmObject.Show(freeAlarmRootTransform);
	}



	#region Tab Button
	public void OnClickTabButton1() { OnValueChangedToggle(0); }
	public void OnClickTabButton2() { OnValueChangedToggle(1); }
	public void OnClickTabButton3() { OnValueChangedToggle(2); }

	List<Transform> _listMenuTransform = new List<Transform>();
	int _lastIndex = -1;
	void OnValueChangedToggle(int index)
	{
		if (index == _lastIndex)
			return;

		if (_listMenuTransform.Count == 0)
		{
			for (int i = 0; i < tabButtonList.Length; ++i)
				_listMenuTransform.Add(null);
		}

		if (_listMenuTransform[index] == null && innerMenuPrefabList[index] != null)
		{
			GameObject newObject = Instantiate<GameObject>(innerMenuPrefabList[index], innerMenuRootTransform);
			_listMenuTransform[index] = newObject.transform;
		}

		for (int i = 0; i < _listMenuTransform.Count; ++i)
		{
			tabButtonList[i].isOn = (index == i);
			if (_listMenuTransform[i] == null)
				continue;
			_listMenuTransform[i].gameObject.SetActive(index == i);
		}

		_lastIndex = index;
	}
	#endregion
}