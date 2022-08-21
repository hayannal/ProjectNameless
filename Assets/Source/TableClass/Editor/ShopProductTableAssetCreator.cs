using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ShopProductTable", false, 500)]
    public static void CreateShopProductTableAssetFile()
    {
        ShopProductTable asset = CustomAssetUtility.CreateAsset<ShopProductTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "ShopProductTable";
        EditorUtility.SetDirty(asset);        
    }
    
}