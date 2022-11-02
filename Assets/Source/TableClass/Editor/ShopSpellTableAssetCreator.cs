using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ShopSpellTable", false, 500)]
    public static void CreateShopSpellTableAssetFile()
    {
        ShopSpellTable asset = CustomAssetUtility.CreateAsset<ShopSpellTable>();
        asset.SheetName = "../Excel/Gacha.xlsx";
        asset.WorksheetName = "ShopSpellTable";
        EditorUtility.SetDirty(asset);        
    }
    
}