using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterToggleButton : MonoBehaviour
{
	public Image iconImage;
	public GameObject selectObject;
	public Text nameText;

	public string actorId { get; private set; }
	public void RefreshInfo(string actorId)
	{
		this.actorId = actorId;
		ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
		nameText.SetLocalizedText(UIString.instance.GetString(actorTableData.nameId));
		AddressableAssetLoadManager.GetAddressableSprite(actorTableData.portraitAddress, "Icon", (sprite) =>
		{
			iconImage.sprite = null;
			iconImage.sprite = sprite;
		});
	}

	public void OnSelect(bool select)
	{
		selectObject.SetActive(select);
		nameText.gameObject.SetActive(select);
	}

	public void OnClickButton()
	{
		if (RushDefenseMissionCanvas.instance != null && RushDefenseMissionCanvas.instance.gameObject.activeSelf)
		{
			if (RushDefenseMissionCanvas.instance.autoPositionProcessed)
				return;
			RushDefenseMissionCanvas.instance.OnValueChangedToggle(actorId);
		}

		if (BossDefenseMissionCanvas.instance != null && BossDefenseMissionCanvas.instance.gameObject.activeSelf)
		{
			if (BossDefenseMissionCanvas.instance.autoPositionProcessed)
				return;
			BossDefenseMissionCanvas.instance.OnValueChangedToggle(actorId);
		}
	}
}