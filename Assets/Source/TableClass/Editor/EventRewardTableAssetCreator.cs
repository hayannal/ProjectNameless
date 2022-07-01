using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/EventRewardTable", false, 500)]
    public static void CreateEventRewardTableAssetFile()
    {
        EventRewardTable asset = CustomAssetUtility.CreateAsset<EventRewardTable>();
        asset.SheetName = "../Excel/Event.xlsx";
        asset.WorksheetName = "EventRewardTable";
        EditorUtility.SetDirty(asset);        
    }
    
}