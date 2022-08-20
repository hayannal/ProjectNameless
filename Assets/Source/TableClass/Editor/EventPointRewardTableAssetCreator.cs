using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/EventPointRewardTable", false, 500)]
    public static void CreateEventPointRewardTableAssetFile()
    {
        EventPointRewardTable asset = CustomAssetUtility.CreateAsset<EventPointRewardTable>();
        asset.SheetName = "../Excel/Gacha.xlsx";
        asset.WorksheetName = "EventPointRewardTable";
        EditorUtility.SetDirty(asset);        
    }
    
}