using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/EventTypeTable", false, 500)]
    public static void CreateEventTypeTableAssetFile()
    {
        EventTypeTable asset = CustomAssetUtility.CreateAsset<EventTypeTable>();
        asset.SheetName = "../Excel/Event.xlsx";
        asset.WorksheetName = "EventTypeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}