using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaCharacterInfoCanvasListItem : MonoBehaviour
{
	public CharacterCanvasListItem characterCanvasListItem;
	public Text rateText;
	public GameObject nextObject;
	public Text nextRateText;

	public void Initialize(GachaActorTableData gachaActorTableData)
	{
		characterCanvasListItem.InitializeGrade(gachaActorTableData.grade, true);
		rateText.text = string.Format("{0:0.0}%", (gachaActorTableData.prob * 100.0f));
		nextObject.SetActive(false);
	}
}