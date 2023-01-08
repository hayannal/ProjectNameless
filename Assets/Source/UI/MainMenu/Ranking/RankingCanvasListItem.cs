using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankingCanvasListItem : MonoBehaviour
{
	public Text rankText;
	public Text nameText;
	public Text valueText;

	int _defaultFontSize;
	Color _defaultFontColor;
	void Awake()
	{
		_defaultFontSize = rankText.fontSize;
		_defaultFontColor = rankText.color;
	}

	public void Initialize(int ranking, string displayName, int value)
	{
		rankText.text = ranking.ToString();

		int fontSize = _defaultFontSize;
		Color fontColor = _defaultFontColor; 
		switch (ranking)
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
		rankText.fontSize = fontSize;
		rankText.color = fontColor;

		nameText.text = displayName;

		valueText.text = string.Format("{0:N0}", value);
		valueText.color = Color.white;
	}
}