using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/AnalysisBoostTable", false, 500)]
    public static void CreateAnalysisBoostTableAssetFile()
    {
        AnalysisBoostTable asset = CustomAssetUtility.CreateAsset<AnalysisBoostTable>();
        asset.SheetName = "../Excel/Analysis.xlsx";
        asset.WorksheetName = "AnalysisBoostTable";
        EditorUtility.SetDirty(asset);        
    }
    
}