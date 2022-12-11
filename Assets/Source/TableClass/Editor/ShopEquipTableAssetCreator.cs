using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ShopEquipTable", false, 500)]
    public static void CreateShopEquipTableAssetFile()
    {
        ShopEquipTable asset = CustomAssetUtility.CreateAsset<ShopEquipTable>();
        asset.SheetName = "../Excel/Gacha.xlsx";
        asset.WorksheetName = "ShopEquipTable";
        EditorUtility.SetDirty(asset);        
    }
    
}