using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ConsumeItemTable", false, 500)]
    public static void CreateConsumeItemTableAssetFile()
    {
        ConsumeItemTable asset = CustomAssetUtility.CreateAsset<ConsumeItemTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "ConsumeItemTable";
        EditorUtility.SetDirty(asset);        
    }
    
}