using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/RelayPackTable", false, 500)]
    public static void CreateRelayPackTableAssetFile()
    {
        RelayPackTable asset = CustomAssetUtility.CreateAsset<RelayPackTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "RelayPackTable";
        EditorUtility.SetDirty(asset);        
    }
    
}