using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/FestivalCollectTable", false, 500)]
    public static void CreateFestivalCollectTableAssetFile()
    {
        FestivalCollectTable asset = CustomAssetUtility.CreateAsset<FestivalCollectTable>();
        asset.SheetName = "../Excel/SevenDays.xlsx";
        asset.WorksheetName = "FestivalCollectTable";
        EditorUtility.SetDirty(asset);        
    }
    
}