using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaCharacterInfoCanvasListItem : MonoBehaviour
{
	public RectTransform offsetRectTransform;
	public CharacterCanvasListItem characterCanvasListItem;
	public Text rateText;
	public GameObject nextObject;
	public Text nextRateText;

	public void Initialize(GachaActorTableData gachaActorTableData, bool applyHalf)
	{
		float prob = gachaActorTableData.prob;
		if (applyHalf)
			prob *= 0.5f;

		characterCanvasListItem.InitializeGrade(gachaActorTableData.grade, true);
		rateText.text = string.Format("{0:0.##}%", (prob * 100.0f));
		nextObject.SetActive(false);

		offsetRectTransform.anchoredPosition = new Vector2(offsetRectTransform.anchoredPosition.x, 0.0f);
		characterCanvasListItem.cachedRectTransform.localScale = Vector3.one;
		rateText.transform.localScale = Vector3.one;
		rateText.color = Color.white;
	}

	public void Initialize(string actorId, float prob)
	{
		characterCanvasListItem.Initialize(actorId, 0, 0, true, 0, null, null, null);
		rateText.text = string.Format("{0:0.##}%", (prob * 100.0f));
		nextObject.SetActive(false);

		offsetRectTransform.anchoredPosition = new Vector2(offsetRectTransform.anchoredPosition.x, 30.0f);
		characterCanvasListItem.cachedRectTransform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
		rateText.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
		rateText.color = new Color(0.1f, 1.0f, 0.1f);
	}
}