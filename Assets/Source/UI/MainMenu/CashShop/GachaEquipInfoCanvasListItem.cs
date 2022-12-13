using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaEquipInfoCanvasListItem : MonoBehaviour
{
	public EquipCanvasListItem equipCanvasListItem;
	public Text rateText;
	public GameObject nextObject;
	public Text nextRateText;

	public void Initialize(GachaEquipTableData gachaEquipTableData)
	{
		equipCanvasListItem.InitializeGrade(gachaEquipTableData.grade, true);
		rateText.text = string.Format("{0:0.0}%", (gachaEquipTableData.prob * 100.0f));
		nextObject.SetActive(false);
	}
}