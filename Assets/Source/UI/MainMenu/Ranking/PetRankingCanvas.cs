using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PetRankingCanvas : MonoBehaviour
{
	public static PetRankingCanvas instance;

	public GameObject toggleContentItemPrefab;
	public RectTransform toggleRootRectTransform;

	public Text topTitleText;
	public GameObject emptyRankingObject;

	public Text myNameText;
	public Text myRankText;
	public GameObject myOutOfRankTextObject;
	public GameObject rankSusTextObject;

	public GameObject editButtonObject;
	public RectTransform alarmRootTransform;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<RankingCanvasListItem>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	int _defaultFontSize;
	Color _defaultFontColor;
	void Awake()
	{
		instance = this;
		_defaultFontSize = myRankText.fontSize;
		_defaultFontColor = myRankText.color;
	}

	void Start()
	{
		toggleContentItemPrefab.SetActive(false);
		contentItemPrefab.SetActive(false);

		InitializeToggleButton();
	}

	void OnEnable()
	{
		StackCanvas.Push(gameObject);

		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ApplyUIDragThreshold();

		RefreshGrid();
		RefreshMyRank();
	}

	void OnDisable()
	{
		if (DragThresholdController.instance != null)
			DragThresholdController.instance.ResetUIDragThreshold();

		StackCanvas.Pop(gameObject);
	}

	List<PetTableData> _listTempTableData = new List<PetTableData>();
	List<RankingToggleButton> _listRankingToggleButton = new List<RankingToggleButton>();
	void InitializeToggleButton()
	{
		_listTempTableData.Clear();
		for (int i = 0; i < TableDataManager.instance.petTable.dataArray.Length; ++i)
			_listTempTableData.Add(TableDataManager.instance.petTable.dataArray[i]);

		_listTempTableData.Sort(delegate (PetTableData x, PetTableData y)
		{
			if (x.orderIndex > y.orderIndex) return 1;
			else if (x.orderIndex < y.orderIndex) return -1;
			return 0;
		});

		for (int i = 0; i < _listTempTableData.Count; ++i)
		{
			GameObject newObject = Instantiate<GameObject>(toggleContentItemPrefab, toggleRootRectTransform);
			RankingToggleButton rankingToggleButton = newObject.GetComponent<RankingToggleButton>();
			rankingToggleButton.RefreshInfo(_listTempTableData[i]);
			newObject.SetActive(true);
			_listRankingToggleButton.Add(rankingToggleButton);
		}

		// 항상 게임을 처음 켤땐 0번탭을 보게 해준다.
		OnValueChangedToggle(_listTempTableData[0].petId);
	}

	public void OnValueChangedToggle(string petId)
	{
		for (int i = 0; i < _listRankingToggleButton.Count; ++i)
			_listRankingToggleButton[i].OnSelect(_listRankingToggleButton[i].petId == petId);

		RankingData.instance.RequestPetRankingData(petId, (listDisplayRankingDataInfo) =>
		{
			if (gameObject.activeSelf)
			{
				_listDisplayRankingInfo = listDisplayRankingDataInfo;

				RefreshGrid();
				RefreshMyRank();
			}
		});
	}

	List<RankingData.DisplayRankingInfo> _listDisplayRankingInfo;
	List<RankingCanvasListItem> _listRankingCanvasListItem = new List<RankingCanvasListItem>();
	void RefreshGrid()
	{
		for (int i = 0; i < _listRankingCanvasListItem.Count; ++i)
			_listRankingCanvasListItem[i].gameObject.SetActive(false);
		_listRankingCanvasListItem.Clear();

		if (_listDisplayRankingInfo == null || _listDisplayRankingInfo.Count == 0)
		{
			emptyRankingObject.SetActive(true);
			return;
		}

		emptyRankingObject.SetActive(false);
		for (int i = 0; i < _listDisplayRankingInfo.Count; ++i)
		{
			RankingCanvasListItem rankingCanvasListItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			rankingCanvasListItem.Initialize(_listDisplayRankingInfo[i].ranking, _listDisplayRankingInfo[i].displayName, _listDisplayRankingInfo[i].value);
			_listRankingCanvasListItem.Add(rankingCanvasListItem);
		}
	}

	void RefreshMyRank()
	{
		#region Name
		bool noName = string.IsNullOrEmpty(PlayerData.instance.displayName);
		//noName = true;
		editButtonObject.SetActive(noName);
		if (noName)
		{
			myNameText.text = string.Format("Nameless_{0}", PlayFabApiManager.instance.playFabId.Substring(0, 5));
			AlarmObject.Show(alarmRootTransform);
		}
		else
		{
			myNameText.text = PlayerData.instance.displayName;
			AlarmObject.Hide(alarmRootTransform);
		}
		#endregion

		if (_listDisplayRankingInfo == null || _listDisplayRankingInfo.Count == 0)
		{
			myRankText.fontSize = _defaultFontSize;
			myRankText.color = _defaultFontColor;
			myRankText.text = "-";
			myOutOfRankTextObject.SetActive(false);
			rankSusTextObject.SetActive(false);
			return;
		}

		if (PlayerData.instance.cheatRankSus > 0)
		{
			myRankText.text = "";
			myOutOfRankTextObject.SetActive(false);
			rankSusTextObject.SetActive(true);
			return;
		}

		int myRanking = 0;
		for (int i = 0; i < _listDisplayRankingInfo.Count; ++i)
		{
			if (_listDisplayRankingInfo[i].playFabId != PlayFabApiManager.instance.playFabId)
				continue;

			myRanking = _listDisplayRankingInfo[i].ranking;
			break;
		}
		if (myRanking == 0)
		{
			myRankText.text = "";
			myOutOfRankTextObject.SetActive(true);
			rankSusTextObject.SetActive(false);
			return;
		}
		myRankText.text = myRanking.ToString();
		myOutOfRankTextObject.SetActive(false);
		rankSusTextObject.SetActive(false);

		int fontSize = _defaultFontSize;
		Color fontColor = _defaultFontColor;
		switch (myRanking)
		{
			case 1:
				fontSize = 30;
				fontColor = new Color(1.0f, 0.95f, 0.0f);
				break;
			case 2:
				fontSize = 27;
				fontColor = new Color(1.0f, 0.95f, 0.0f);
				break;
			case 3:
				fontSize = 24;
				fontColor = new Color(1.0f, 0.95f, 0.0f);
				break;
		}
		myRankText.fontSize = fontSize;
		myRankText.color = fontColor;
	}

	public void OnClickEditButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("InputNameCanvas", null);
	}
}