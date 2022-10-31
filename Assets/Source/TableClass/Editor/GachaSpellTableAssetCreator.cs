using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/GachaSpellTable", false, 500)]
    public static void CreateGachaSpellTableAssetFile()
    {
        GachaSpellTable asset = CustomAssetUtility.CreateAsset<GachaSpellTable>();
        asset.SheetName = "../Excel/Gacha.xlsx";
        asset.WorksheetName = "GachaSpellTable";
        EditorUtility.SetDirty(asset);        
    }
    
}