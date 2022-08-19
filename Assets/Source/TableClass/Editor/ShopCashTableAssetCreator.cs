using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ShopCashTable", false, 500)]
    public static void CreateShopCashTableAssetFile()
    {
        ShopCashTable asset = CustomAssetUtility.CreateAsset<ShopCashTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "ShopCashTable";
        EditorUtility.SetDirty(asset);        
    }
    
}