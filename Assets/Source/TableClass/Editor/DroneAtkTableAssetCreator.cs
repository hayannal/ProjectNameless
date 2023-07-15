using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/DroneAtkTable", false, 500)]
    public static void CreateDroneAtkTableAssetFile()
    {
        DroneAtkTable asset = CustomAssetUtility.CreateAsset<DroneAtkTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "DroneAtkTable";
        EditorUtility.SetDirty(asset);        
    }
    
}