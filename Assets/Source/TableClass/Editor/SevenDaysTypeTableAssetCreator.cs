using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/SevenDaysTypeTable", false, 500)]
    public static void CreateSevenDaysTypeTableAssetFile()
    {
        SevenDaysTypeTable asset = CustomAssetUtility.CreateAsset<SevenDaysTypeTable>();
        asset.SheetName = "../Excel/SevenDays.xlsx";
        asset.WorksheetName = "SevenDaysTypeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}