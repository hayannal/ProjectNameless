using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/EquipLevelTable", false, 500)]
    public static void CreateEquipLevelTableAssetFile()
    {
        EquipLevelTable asset = CustomAssetUtility.CreateAsset<EquipLevelTable>();
        asset.SheetName = "../Excel/Equip.xlsx";
        asset.WorksheetName = "EquipLevelTable";
        EditorUtility.SetDirty(asset);        
    }
    
}