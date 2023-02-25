using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class EquipBoxResultCanvasListItem : MonoBehaviour
{
	public EquipCanvasListItem equipCanvasListItem;

	public void Initialize(ItemInstance itemInstance, EquipTableData equipTableData)
	{
		EquipData equipData = EquipManager.instance.FindEquipData(itemInstance.ItemInstanceId, (EquipManager.eEquipSlotType)equipTableData.equipType);
		equipCanvasListItem.Initialize(equipData, OnClickListItem);
	}

	public void OnClickListItem(EquipData equipData)
	{
		EquipBoxResultCanvas.instance.ShowSmallEquipInfo(equipData);
	}
}