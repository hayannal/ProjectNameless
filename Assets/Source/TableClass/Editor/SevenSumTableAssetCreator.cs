using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/SevenSumTable", false, 500)]
    public static void CreateSevenSumTableAssetFile()
    {
        SevenSumTable asset = CustomAssetUtility.CreateAsset<SevenSumTable>();
        asset.SheetName = "../Excel/SevenDays.xlsx";
        asset.WorksheetName = "SevenSumTable";
        EditorUtility.SetDirty(asset);        
    }
    
}