using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/GachaTypeTable", false, 500)]
    public static void CreateGachaTypeTableAssetFile()
    {
        GachaTypeTable asset = CustomAssetUtility.CreateAsset<GachaTypeTable>();
        asset.SheetName = "../Excel/Gacha.xlsx";
        asset.WorksheetName = "GachaTypeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}