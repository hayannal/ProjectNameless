using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/StageIdTable", false, 500)]
    public static void CreateStageIdTableAssetFile()
    {
        StageIdTable asset = CustomAssetUtility.CreateAsset<StageIdTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "StageIdTable";
        EditorUtility.SetDirty(asset);        
    }
    
}