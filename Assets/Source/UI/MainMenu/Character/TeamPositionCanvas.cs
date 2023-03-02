using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamPositionCanvas : MonoBehaviour
{
	public CharacterCanvasListItem[] characterCanvasListItemList;
	public GameObject[] over100StagePositionArrowObjectList;

	void OnEnable()
	{
		RefreshSlot();
	}

	void RefreshSlot()
	{
		for (int i = 0; i < characterCanvasListItemList.Length; ++i)
		{
			characterCanvasListItemList[i].gameObject.SetActive(false);

			if (string.IsNullOrEmpty(CharacterManager.instance.listTeamPositionId[i]) == false)
			{
				CharacterData characterData = CharacterManager.instance.GetCharacterData(CharacterManager.instance.listTeamPositionId[i]);
				if (characterData != null)
				{
					characterCanvasListItemList[i].Initialize(characterData.actorId, characterData.level, characterData.transcend, false, 0, null, null, null);
					characterCanvasListItemList[i].equippedObject.SetActive(false);
					characterCanvasListItemList[i].gameObject.SetActive(true);
				}
			}
		}
	}

	public void OnClickButton(int index)
	{
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
}