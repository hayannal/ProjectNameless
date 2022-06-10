using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/StageBetTable", false, 500)]
    public static void CreateStageBetTableAssetFile()
    {
        StageBetTable asset = CustomAssetUtility.CreateAsset<StageBetTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "StageBetTable";
        EditorUtility.SetDirty(asset);        
    }
    
}