using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/RobotDefenseStepTable", false, 500)]
    public static void CreateRobotDefenseStepTableAssetFile()
    {
        RobotDefenseStepTable asset = CustomAssetUtility.CreateAsset<RobotDefenseStepTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "RobotDefenseStepTable";
        EditorUtility.SetDirty(asset);        
    }
    
}