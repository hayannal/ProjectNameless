using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TeamPositionCanvas : MonoBehaviour
{
	public static TeamPositionCanvas instance;

	public const int PassLimitStage = 100;

	public CharacterCanvasListItem[] characterCanvasListItemList;
	public GameObject[] arrowObjectList;

	#region PetPass
	public GameObject passButtonObject;
	public Text teamPassPriceText;
	public Text teamPassPurchasedText;
	public Text teamPassRemainTimeText;
	#endregion

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		#region PetPass
		bool over100Stage = IsOverPassStage();
		passButtonObject.SetActive(over100Stage);
		if (over100Stage)
			RefreshTeamPass();
		#endregion

		RefreshSlot();
	}

	void Update()
	{
		UpdateTeamPassRemainTime();
	}

	#region PetPass
	// 패스에 영향받는 슬롯은 3번 4번 슬롯이다.
	public static bool IsApplySlotByPass(int index)
	{
		if (index == 3 || index == 4)
			return true;
		return false;
	}

	bool IsOverPassStage()
	{
		return PlayerData.instance.highestClearStage >= PassLimitStage;
	}
	#endregion

	bool _over100Stage;
	public void RefreshSlot()
	{
		_over100Stage = IsOverPassStage();

		for (int i = 0; i < characterCanvasListItemList.Length; ++i)
		{
			#region PetPass
			bool passSlot = IsApplySlotByPass(i);
			if (passSlot)
			{
				if (_over100Stage && CharacterManager.instance.IsTeamPass())
					arrowObjectList[i].SetActive(true);
				else
					arrowObjectList[i].SetActive(false);
			}
			#endregion

			characterCanvasListItemList[i].gameObject.SetActive(false);

			if (string.IsNullOrEmpty(CharacterManager.instance.listTeamPositionId[i]) == false)
			{
				CharacterData characterData = CharacterManager.instance.GetCharacterData(CharacterManager.instance.listTeamPositionId[i]);
				if (characterData != null)
				{
					characterCanvasListItemList[i].Initialize(characterData.actorId, characterData.level, characterData.transcend, false, 0, null, null, null);
					characterCanvasListItemList[i].equippedObject.SetActive(false);
					characterCanvasListItemList[i].gameObject.SetActive(true);

					#region PetPass
					if (passSlot)
						characterCanvasListItemList[i].blackObject.SetActive(arrowObjectList[i].activeSelf == false);
					#endregion
				}
			}
		}
	}

	public void OnClickButton(int index)
	{
		#region PetPass
		if (IsApplySlotByPass(index))
		{
			if (arrowObjectList[index].activeSelf == false)
			{
				if (_over100Stage == false)
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("CharacterUI_NotYetStage"), 2.0f);
				else if (CharacterManager.instance.IsTeamPass() == false)
					ToastCanvas.instance.ShowToast(UIString.instance.GetString("CharacterUI_NoHaveTeamPass"), 2.0f);
				return;
			}
		}
		#endregion

		if (string.IsNullOrEmpty(CharacterManager.instance.listTeamPositionId[index]) == false)
		{
			if (CharacterListCanvas.instance.selectedActorId == CharacterManager.instance.listTeamPositionId[index])
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("CharacterUI_AlreadyPosition"), 2.0f);
				return;
			}
		}

		int prevSwapIndex = -1;
		for (int i = 0; i < (int)TeamManager.ePosition.Amount; ++i)
		{
			if (i == index)
				continue;

			if (string.IsNullOrEmpty(CharacterManager.instance.listTeamPositionId[i]) == false)
			{
				if (CharacterListCanvas.instance.selectedActorId == CharacterManager.instance.listTeamPositionId[i])
					prevSwapIndex = i;
			}
		}
		
		PlayFabApiManager.instance.RequestSelectTeamPosition(CharacterListCanvas.instance.selectedActorId, index, prevSwapIndex, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("CharacterUI_SelectedToast"), 2.0f);
			CharacterListCanvas.instance.RefreshGrid();
			TeamManager.instance.InitializeTeamMember();
			gameObject.SetActive(false);
		});
	}



	#region TeamPass
	public void RefreshTeamPass()
	{
		bool actived = CharacterManager.instance.IsTeamPass();
		teamPassPriceText.gameObject.SetActive(!actived);
		teamPassPurchasedText.gameObject.SetActive(actived);
		teamPassRemainTimeText.gameObject.SetActive(actived);

		if (actived)
			return;
		ShopProductTableData shopProductTableData = TableDataManager.instance.FindShopProductTableData("teampass");
		if (shopProductTableData == null)
			return;
		SimpleCashCanvas.RefreshCashPrice(teamPassPriceText, shopProductTableData.serverItemId, shopProductTableData.kor, shopProductTableData.eng);
	}

	int _lastTeamRemainTimeSecond;
	void UpdateTeamPassRemainTime()
	{
		if (teamPassRemainTimeText.gameObject.activeSelf == false)
			return;

		if (ServerTime.UtcNow < CharacterManager.instance.teamPassExpireTime)
		{
			System.TimeSpan remainTime = CharacterManager.instance.teamPassExpireTime - ServerTime.UtcNow;
			if (_lastTeamRemainTimeSecond != (int)remainTime.TotalSeconds)
			{
				if (remainTime.Days > 0)
					teamPassRemainTimeText.text = string.Format("{0}d {1:00}:{2:00}:{3:00}", remainTime.Days, remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				else
					teamPassRemainTimeText.text = string.Format("{0:00}:{1:00}:{2:00}", remainTime.Hours, remainTime.Minutes, remainTime.Seconds);
				_lastTeamRemainTimeSecond = (int)remainTime.TotalSeconds;
			}
		}
		else
		{
			teamPassRemainTimeText.text = "";
			RefreshTeamPass();
			RefreshSlot();

			if (TeamPassCanvas.instance != null && TeamPassCanvas.instance.gameObject.activeSelf)
				TeamPassCanvas.instance.gameObject.SetActive(false);
		}
	}

	public void OnClickTeamPassButton()
	{
		UIInstanceManager.instance.ShowCanvasAsync("TeamPassCanvas", null);
	}
	#endregion
}