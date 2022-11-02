using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GachaSpellInfoCanvasListItem : MonoBehaviour
{
	public SkillIcon skillIcon;
	public Text rateText;

	public void Initialize(GachaSpellTableData gachaSpellTableData, int probIndex)
	{
		skillIcon.SetGradeStar(gachaSpellTableData.grade, gachaSpellTableData.star);
		rateText.text = string.Format("{0:0.0}%", (gachaSpellTableData.probs[probIndex] * 100.0f));
	}
}