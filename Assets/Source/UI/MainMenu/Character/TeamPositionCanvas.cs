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
		if (string.IsNullOrEmpty(CharacterManager.instance.leftCharacterId) == false)
		{
			CharacterData characterData = CharacterManager.instance.GetCharacterData(CharacterManager.instance.leftCharacterId);
			if (characterData != null)
			{
				leftCharacterCanvasListItem.Initialize(characterData.actorId, characterData.level, characterData.transcend, 0, null, null, null);
				leftCharacterCanvasListItem.equippedObject.SetActive(false);
				leftCharacterCanvasListItem.gameObject.SetActive(true);
			}
		}

		if (string.IsNullOrEmpty(CharacterManager.instance.rightCharacterId) == false)
		{
			CharacterData characterData = CharacterManager.instance.GetCharacterData(CharacterManager.instance.rightCharacterId);
			if (characterData != null)
			{
				rightCharacterCanvasListItem.Initialize(characterData.actorId, characterData.level, characterData.transcend, 0, null, null, null);
				rightCharacterCanvasListItem.equippedObject.SetActive(false);
				rightCharacterCanvasListItem.gameObject.SetActive(true);
			}
		}
	}

	public void OnClickLeftButton()
	{

	}

	public void OnClickRightButton()
	{

	}
}