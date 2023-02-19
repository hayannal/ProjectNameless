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

	public void Initialize(GachaEquipTableData gachaEquipTableData, float tableOverrideProb, bool leftPosition, bool showNextRate, float nextProb)
	{
		float prob = gachaEquipTableData.prob;
		if (tableOverrideProb != 0.0f)
			prob = tableOverrideProb;

		equipCanvasListItem.InitializeGrade(gachaEquipTableData.grade, true);
		EquipCanvasListItem.RefreshRarity(gachaEquipTableData.rarity, equipCanvasListItem.rarityText, equipCanvasListItem.rarityGradient);
		rateText.text = string.Format("{0:0.##}%", (prob * 100.0f));
		nextObject.SetActive(false);

		offsetRectTransform.anchoredPosition = new Vector2(leftPosition ? -60.0f : 0.0f, 0.0f);
		equipCanvasListItem.cachedRectTransform.localScale = Vector3.one;
		rateText.transform.localScale = Vector3.one;
		rateText.color = Color.white;

		nextObject.SetActive(showNextRate);
		nextRateText.text = string.Format("{0:0.##}%", (nextProb * 100.0f));
		nextRateText.color = new Color(0.1f, 1.0f, 0.1f);
	}

	public void Initialize(string equipId, float prob, float nextProb)
	{
		bool leftPosition = (nextProb > 0.0f);

		EquipTableData equipTableData = TableDataManager.instance.FindEquipTableData(equipId);
		equipCanvasListItem.Initialize(equipTableData);
		EquipCanvasListItem.RefreshRarity(equipTableData.rarity, equipCanvasListItem.rarityText, equipCanvasListItem.rarityGradient);
		rateText.text = string.Format("{0:0.##}%", (prob * 100.0f));
		nextObject.SetActive(false);

		offsetRectTransform.anchoredPosition = new Vector2(leftPosition ? -60.0f : 0.0f, 25.0f);
		equipCanvasListItem.cachedRectTransform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
		rateText.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
		rateText.color = leftPosition ? Color.white : new Color(0.1f, 1.0f, 0.1f);

		nextObject.SetActive(leftPosition);
		nextRateText.text = string.Format("{0:0.##}%", (nextProb * 100.0f));
		nextRateText.color = new Color(0.1f, 1.0f, 0.1f);
	}
}