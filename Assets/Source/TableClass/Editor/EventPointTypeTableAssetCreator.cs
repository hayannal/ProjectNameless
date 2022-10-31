using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/EventPointTypeTable", false, 500)]
    public static void CreateEventPointTypeTableAssetFile()
    {
        EventPointTypeTable asset = CustomAssetUtility.CreateAsset<EventPointTypeTable>();
        asset.SheetName = "../Excel/Summon.xlsx";
        asset.WorksheetName = "EventPointTypeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}