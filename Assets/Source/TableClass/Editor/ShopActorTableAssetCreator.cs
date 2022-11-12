using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ShopActorTable", false, 500)]
    public static void CreateShopActorTableAssetFile()
    {
        ShopActorTable asset = CustomAssetUtility.CreateAsset<ShopActorTable>();
        asset.SheetName = "../Excel/Gacha.xlsx";
        asset.WorksheetName = "ShopActorTable";
        EditorUtility.SetDirty(asset);        
    }
    
}