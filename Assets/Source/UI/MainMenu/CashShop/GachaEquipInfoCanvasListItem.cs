using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaEquipInfoCanvasListItem : MonoBehaviour
{
	public RectTransform offsetRectTransform;
	public EquipCanvasListItem equipCanvasListItem;
	public Text rateText;
	public GameObject nextObject;
	public Text nextRateText;

	public void Initialize(GachaEquipTableData gachaEquipTableData)
	{
		equipCanvasListItem.InitializeGrade(gachaEquipTableData.grade, true);
		rateText.text = string.Format("{0:0.##}%", (gachaEquipTableData.prob * 100.0f));
		nextObject.SetActive(false);

		offsetRectTransform.anchoredPosition = new Vector2(offsetRectTransform.anchoredPosition.x, 0.0f);
		equipCanvasListItem.cachedRectTransform.localScale = Vector3.one;
		rateText.transform.localScale = Vector3.one;
		rateText.color = Color.white;
	}

	public void Initialize(string equipId, float prob)
	{
		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(equipId);
		equipCanvasListItem.Initialize(equipTableData);
		rateText.text = string.Format("{0:0.##}%", (prob * 100.0f));
		nextObject.SetActive(false);

		offsetRectTransform.anchoredPosition = new Vector2(offsetRectTransform.anchoredPosition.x, 20.0f);
		equipCanvasListItem.cachedRectTransform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
		rateText.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
		rateText.color = new Color(0.1f, 1.0f, 0.1f);
	}
}