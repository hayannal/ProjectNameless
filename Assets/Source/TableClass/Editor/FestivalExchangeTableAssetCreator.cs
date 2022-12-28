using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/FestivalExchangeTable", false, 500)]
    public static void CreateFestivalExchangeTableAssetFile()
    {
        FestivalExchangeTable asset = CustomAssetUtility.CreateAsset<FestivalExchangeTable>();
        asset.SheetName = "../Excel/SevenDays.xlsx";
        asset.WorksheetName = "FestivalExchangeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}