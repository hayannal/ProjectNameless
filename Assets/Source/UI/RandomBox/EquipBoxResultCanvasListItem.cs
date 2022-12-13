using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlayFab.ClientModels;

public class EquipBoxResultCanvasListItem : MonoBehaviour
{
	public EquipCanvasListItem equipCanvasListItem;

	EquipTableData _equipTableData;
	public void Initialize(ItemInstance itemInstance, EquipTableData equipTableData)
	{
		_equipTableData = equipTableData;

		EquipData equipData = EquipManager.instance.FindEquipData(itemInstance.ItemInstanceId, (EquipManager.eEquipSlotType)equipTableData.equipType);
		equipCanvasListItem.Initialize(equipData, null);
	}
}