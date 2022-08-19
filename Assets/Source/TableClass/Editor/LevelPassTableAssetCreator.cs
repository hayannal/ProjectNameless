using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/LevelPassTable", false, 500)]
    public static void CreateLevelPassTableAssetFile()
    {
        LevelPassTable asset = CustomAssetUtility.CreateAsset<LevelPassTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "LevelPassTable";
        EditorUtility.SetDirty(asset);        
    }
    
}