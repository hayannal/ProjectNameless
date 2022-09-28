using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/SevenDaysRewardTable", false, 500)]
    public static void CreateSevenDaysRewardTableAssetFile()
    {
        SevenDaysRewardTable asset = CustomAssetUtility.CreateAsset<SevenDaysRewardTable>();
        asset.SheetName = "../Excel/SevenDays.xlsx";
        asset.WorksheetName = "SevenDaysRewardTable";
        EditorUtility.SetDirty(asset);        
    }
    
}