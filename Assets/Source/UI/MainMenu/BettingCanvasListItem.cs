using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BettingCanvasListItem : MonoBehaviour
{
	public Image slotImage;
	public Image slotBlurImage;

	public int slotImageId { get; set; }
	public void Initialize(int slotImageId)
	{
		this.slotImageId = slotImageId;

		if (BettingCanvas.instance != null)
			slotImage.sprite = BettingCanvas.instance.slotSpriteList[slotImageId];
	}

	public void SwitchBlurImage(bool blurImage)
	{
		if (BettingCanvas.instance != null)
			slotImage.sprite = blurImage ? BettingCanvas.instance.slotBlurSpriteList[slotImageId] : BettingCanvas.instance.slotSpriteList[slotImageId];
	}
}