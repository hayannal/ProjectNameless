using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeamPositionCanvas : MonoBehaviour
{
	public CharacterCanvasListItem leftCharacterCanvasListItem;
	public CharacterCanvasListItem rightCharacterCanvasListItem;

	void OnEnable()
	{
		RefreshSlot();
	}

	void RefreshSlot()
	{
		leftCharacterCanvasListItem.gameObject.SetActive(false);
		if (string.IsNullOrEmpty(CharacterManager.instance.leftCharacterId) == false)
		{
			CharacterData characterData = CharacterManager.instance.GetCharacterData(CharacterManager.instance.leftCharacterId);
			if (characterData != null)
			{
				leftCharacterCanvasListItem.Initialize(characterData.actorId, characterData.level, characterData.transcend, false, 0, null, null, null);
				leftCharacterCanvasListItem.equippedObject.SetActive(false);
				leftCharacterCanvasListItem.gameObject.SetActive(true);
			}
		}

		rightCharacterCanvasListItem.gameObject.SetActive(false);
		if (string.IsNullOrEmpty(CharacterManager.instance.rightCharacterId) == false)
		{
			CharacterData characterData = CharacterManager.instance.GetCharacterData(CharacterManager.instance.rightCharacterId);
			if (characterData != null)
			{
				rightCharacterCanvasListItem.Initialize(characterData.actorId, characterData.level, characterData.transcend, false, 0, null, null, null);
				rightCharacterCanvasListItem.equippedObject.SetActive(false);
				rightCharacterCanvasListItem.gameObject.SetActive(true);
			}
		}
	}

	public void OnClickLeftButton()
	{
		if (string.IsNullOrEmpty(CharacterManager.instance.leftCharacterId) == false)
		{
			if (CharacterListCanvas.instance.selectedActorId == CharacterManager.instance.leftCharacterId)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("CharacterUI_AlreadyPosition"), 2.0f);
				return;
			}
		}

		bool swap = false;
		if (string.IsNullOrEmpty(CharacterManager.instance.rightCharacterId) == false)
		{
			if (CharacterListCanvas.instance.selectedActorId == CharacterManager.instance.rightCharacterId)
				swap = true;
		}

		PlayFabApiManager.instance.RequestSelectTeamPosition(CharacterListCanvas.instance.selectedActorId, true, swap, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("CharacterUI_SelectedToast"), 2.0f);
			CharacterListCanvas.instance.RefreshGrid();
			TeamManager.instance.InitializeTeamMember();
			gameObject.SetActive(false);
		});
	}

	public void OnClickRightButton()
	{
		if (string.IsNullOrEmpty(CharacterManager.instance.rightCharacterId) == false)
		{
			if (CharacterListCanvas.instance.selectedActorId == CharacterManager.instance.rightCharacterId)
			{
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("CharacterUI_AlreadyPosition"), 2.0f);
				return;
			}
		}

		bool swap = false;
		if (string.IsNullOrEmpty(CharacterManager.instance.leftCharacterId) == false)
		{
			if (CharacterListCanvas.instance.selectedActorId == CharacterManager.instance.leftCharacterId)
				swap = true;
		}

		PlayFabApiManager.instance.RequestSelectTeamPosition(CharacterListCanvas.instance.selectedActorId, false, swap, () =>
		{
			ToastCanvas.instance.ShowToast(UIString.instance.GetString("CharacterUI_SelectedToast"), 2.0f);
			CharacterListCanvas.instance.RefreshGrid();
			TeamManager.instance.InitializeTeamMember();
			gameObject.SetActive(false);
		});
	}
}