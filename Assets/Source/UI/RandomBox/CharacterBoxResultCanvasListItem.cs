using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class CharacterBoxResultCanvasListItem : MonoBehaviour
{
	public CharacterCanvasListItem characterCanvasListItem;
	public Text countText;
	public Text newText;

	ActorTableData _actorTableData;
	public void InitializeForNewCharacter(bool newCharacter, ActorTableData actorTableData)
	{
		_actorTableData = actorTableData;

		int level = 0;
		int transcend = 0;
		CharacterData characterData = CharacterManager.instance.GetCharacterData(actorTableData.actorId);
		if (characterData != null)
		{
			level = characterData.level;
			transcend = characterData.transcend;
		}
		characterCanvasListItem.Initialize(actorTableData.actorId, level, transcend, true, 0, null, null, null);

		countText.gameObject.SetActive(false);
		newText.SetLocalizedText(UIString.instance.GetString(newCharacter ? "ShopUI_NewCharacter" : "ShopUI_TranscendReward"));
		newText.gameObject.SetActive(true);
	}

	public void InitializeForCharacterPp(ItemInstance itemInstance, ActorTableData actorTableData)
	{
		_actorTableData = actorTableData;

		int level = 0;
		int transcend = 0;
		CharacterData characterData = CharacterManager.instance.GetCharacterData(actorTableData.actorId);
		if (characterData != null)
		{
			level = characterData.level;
			transcend = characterData.transcend;
		}
		characterCanvasListItem.Initialize(actorTableData.actorId, level, transcend, false, 0, null, null, null);

		newText.gameObject.SetActive(false);
		countText.text = string.Format("+ {0:N0}", itemInstance.UsesIncrementedBy);
		countText.gameObject.SetActive(true);
	}
}