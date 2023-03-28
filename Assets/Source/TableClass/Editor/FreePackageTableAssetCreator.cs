using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/FreePackageTable", false, 500)]
    public static void CreateFreePackageTableAssetFile()
    {
        FreePackageTable asset = CustomAssetUtility.CreateAsset<FreePackageTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "FreePackageTable";
        EditorUtility.SetDirty(asset);        
    }
    
}