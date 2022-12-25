using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/FestivalTypeTable", false, 500)]
    public static void CreateFestivalTypeTableAssetFile()
    {
        FestivalTypeTable asset = CustomAssetUtility.CreateAsset<FestivalTypeTable>();
        asset.SheetName = "../Excel/SevenDays.xlsx";
        asset.WorksheetName = "FestivalTypeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}