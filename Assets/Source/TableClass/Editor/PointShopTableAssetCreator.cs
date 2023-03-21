using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/PointShopTable", false, 500)]
    public static void CreatePointShopTableAssetFile()
    {
        PointShopTable asset = CustomAssetUtility.CreateAsset<PointShopTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "PointShopTable";
        EditorUtility.SetDirty(asset);        
    }
    
}