using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MissionCanvasCharacterListItem : MonoBehaviour
{
	public CharacterCanvasListItem characterCanvasListItem;
	public GameObject blackImageObject;
	public GameObject textRootObject;
	public Text numberText;
	public GameObject redTextRootObject;
	public Text redNumberText;

	public void Initialize(string actorId, int level, int transcendLevel, bool shopItem, Action<string> clickCallback)
	{
		characterCanvasListItem.Initialize(actorId, level, transcendLevel, shopItem, 0, null, null, clickCallback);

		blackImageObject.SetActive(false);
		textRootObject.SetActive(false);
		redTextRootObject.SetActive(false);
	}

	int _number;
	public void SetNumber(bool show, int number, bool useRedColor = false)
	{
		if (show)
		{
			if (_number != number)
				textRootObject.SetActive(!show);

			numberText.text = redNumberText.text = number.ToString("N0");
			_number = number;

			blackImageObject.SetActive(true);
			textRootObject.SetActive(useRedColor == false);
			redTextRootObject.SetActive(useRedColor);
		}
		else
		{
			blackImageObject.SetActive(false);
			textRootObject.SetActive(false);
			redTextRootObject.SetActive(false);
		}
	}
}