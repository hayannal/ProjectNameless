using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/AnalysisDropTable", false, 500)]
    public static void CreateAnalysisDropTableAssetFile()
    {
        AnalysisDropTable asset = CustomAssetUtility.CreateAsset<AnalysisDropTable>();
        asset.SheetName = "../Excel/Analysis.xlsx";
        asset.WorksheetName = "AnalysisDropTable";
        EditorUtility.SetDirty(asset);        
    }
    
}