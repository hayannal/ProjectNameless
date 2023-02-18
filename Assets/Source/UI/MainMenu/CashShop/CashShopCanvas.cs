using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class CashShopCanvas : MonoBehaviour
{
	public static CashShopCanvas instance;

	public CurrencySmallInfo currencySmallInfo;

	public GameObject iapInitializeFailedRectObject;

	public PickUpCharacterListItem pickUpCharacterListItem;
	public PickUpEquipListItem pickUpEquipListItem;

	public GameObject equipBoxRectObject;
	public Text equipBox1NameText;
	public Text equipBox1PriceText;
	public Image equipBox1IconImage;
	public RectTransform equipBox1IconRectTransform;
	public Text equipBox8NameText;
	public Text equipBox8PriceText;
	public Image equipBox8IconImage;
	public RectTransform equipBox8IconRectTransform;
	public Text equipBox8AddText;

	public GameObject equipBox45GroupObject;
	public Text equipBox45NameText;
	public Text equipBox45PriceText;
	public Image equipBox45IconImage;
	public RectTransform equipBox45IconRectTransform;
	public Text equipBox45AddText;
	
	public GameObject diaRectObject;
	//public DiaListItem[] diaListItemList;
	//public GoldListItem[] goldListItemList;
	
	public GameObject termsGroupObject;
	public GameObject emptyTermsGroupObject;

	void Awake()
	{
		instance = this;
	}

	float _canvasMatchWidthOrHeightSize;
	float _lineLengthRatio;
	public float lineLengthRatio { get { return _lineLengthRatio; } }
	void Start()
	{
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
	}

	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		if (restore)
			return;

		MainCanvas.instance.OnEnterCharacterMenu(true);

		RefreshInfo();

		termsGroupObject.SetActive(OptionManager.instance.language == "KOR");
		emptyTermsGroupObject.SetActive(OptionManager.instance.language != "KOR");

		// 자동 복구 코드 호출은 밖에서 했을거다. 여기선 하지 않는다.
		//CheckIAPListener();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		if (StackCanvas.Pop(gameObject))
			return;

		OnPopStack();
	}

	public bool ignoreStartEventFlag { get; set; }
	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;
		if (MainCanvas.instance == null)
			return;

		if (ignoreStartEventFlag)
		{
			ignoreStartEventFlag = false;
			MainCanvas.instance.OnEnterCharacterMenu(false, true);
			return;
		}
		MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	void RefreshInfo()
	{
		iapInitializeFailedRectObject.SetActive(!CodelessIAPStoreListener.initializationComplete);
		
		diaRectObject.SetActive(CodelessIAPStoreListener.initializationComplete);

		RefreshPickUpCharacterRect();
		RefreshPickUpEquipRect();
	}

	public void RefreshPickUpCharacterRect()
	{
		// 캐릭터의 경우엔 장비랑 달리 다 뽑았는지도 판단해야한다. 이런 상황에선 굴려봤자 의미없으니 하이드 시킨다.
		CashShopData.PickUpCharacterInfo characterInfo = CashShopData.instance.GetCurrentPickUpCharacterInfo();
		bool maxReached = false;
		if (characterInfo != null)
		{
			CharacterData characterData = CharacterManager.instance.GetCharacterData(characterInfo.id);
			if (characterData != null && characterData.transcendPoint >= TableDataManager.instance.GetGlobalConstantInt("GachaActorMaxTrp"))
				maxReached = true;
		}
		pickUpCharacterListItem.gameObject.SetActive(PlayerData.instance.downloadConfirmed && characterInfo != null && maxReached == false);
		pickUpCharacterListItem.RefreshInfo(characterInfo);
	}

	public void RefreshPickUpEquipRect()
	{
		CashShopData.PickUpEquipInfo equipInfo = CashShopData.instance.GetCurrentPickUpEquipInfo();
		pickUpEquipListItem.gameObject.SetActive(PlayerData.instance.downloadConfirmed && equipInfo != null);
		pickUpEquipListItem.RefreshInfo(equipInfo);
	}
}