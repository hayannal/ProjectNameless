using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/PointShopAtkTable", false, 500)]
    public static void CreatePointShopAtkTableAssetFile()
    {
        PointShopAtkTable asset = CustomAssetUtility.CreateAsset<PointShopAtkTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "PointShopAtkTable";
        EditorUtility.SetDirty(asset);        
    }
    
}