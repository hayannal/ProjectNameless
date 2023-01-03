using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/StageClearTable", false, 500)]
    public static void CreateStageClearTableAssetFile()
    {
        StageClearTable asset = CustomAssetUtility.CreateAsset<StageClearTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "StageClearTable";
        EditorUtility.SetDirty(asset);        
    }
    
}