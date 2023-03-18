using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChangeDifficultyCanvasListItem : MonoBehaviour
{
	public Button difficultyButton;
	public Text difficultyText;
	public GameObject clearObject;

	int _difficulty;
	public void Initialize(int difficulty, bool showClear, bool selected)
	{
		_difficulty = difficulty;
		difficultyText.text = string.Format("<size=14>DIFFICULTY</size> {0}", _difficulty);
		difficultyButton.interactable = !selected;
		clearObject.SetActive(showClear);
	}

	public void OnClickButton()
	{
		int result = CheckSelectable(_difficulty);
		switch (result)
		{
			case -1:
				ToastCanvas.instance.ShowToast(UIString.instance.GetString("BossUI_NotDeveloped"), 2.0f);
				return;
		}

		ChangeDifficultyCanvas.instance.gameObject.SetActive(false);
		BossBattleEnterCanvas.instance.OnChangeDifficulty(_difficulty);
	}

	public static int CheckSelectable(int targetDifficulty)
	{
		if (targetDifficulty > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattleDifficulty"))
			return -1;
		return 0;
	}
}