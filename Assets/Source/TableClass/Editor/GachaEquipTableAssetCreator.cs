using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/GachaEquipTable", false, 500)]
    public static void CreateGachaEquipTableAssetFile()
    {
        GachaEquipTable asset = CustomAssetUtility.CreateAsset<GachaEquipTable>();
        asset.SheetName = "../Excel/Gacha.xlsx";
        asset.WorksheetName = "GachaEquipTable";
        EditorUtility.SetDirty(asset);        
    }
    
}