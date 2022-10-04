using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RankingCanvasListItem : MonoBehaviour
{
	public Text rankText;
	public Text nameText;
	public Text stageText;

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

		int stage = value;
		if (stage > PlayerData.instance.highestClearStage)
		{
			stageText.text = "???";
			stageText.color = new Color(0.6f, 0.6f, 0.6f);
		}
		else
		{
			stageText.text = string.Format("{0:N0}", stage.ToString());
			stageText.color = Color.white;
		}
	}
}