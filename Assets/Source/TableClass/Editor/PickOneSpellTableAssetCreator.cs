using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/PickOneSpellTable", false, 500)]
    public static void CreatePickOneSpellTableAssetFile()
    {
        PickOneSpellTable asset = CustomAssetUtility.CreateAsset<PickOneSpellTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "PickOneSpellTable";
        EditorUtility.SetDirty(asset);        
    }
    
}