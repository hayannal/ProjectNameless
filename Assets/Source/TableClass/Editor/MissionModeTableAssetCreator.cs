using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/MissionModeTable", false, 500)]
    public static void CreateMissionModeTableAssetFile()
    {
        MissionModeTable asset = CustomAssetUtility.CreateAsset<MissionModeTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "MissionModeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}