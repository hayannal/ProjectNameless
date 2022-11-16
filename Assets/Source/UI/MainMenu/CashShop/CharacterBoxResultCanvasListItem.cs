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
	public void Initialize(ItemInstance itemInstance, ActorTableData actorTableData)
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
		characterCanvasListItem.Initialize(actorTableData.actorId, level, transcend, 0, null, null, null);

		countText.text = string.Format("+ {0:N0}", itemInstance.UsesIncrementedBy);
		newText.gameObject.SetActive(false);

		if (itemInstance.UsesIncrementedBy == itemInstance.RemainingUses)
		{
			newText.SetLocalizedText(UIString.instance.GetString("ShopUI_NewCharacter"));
			newText.gameObject.SetActive(true);
		}
	}
}