using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/SpellTotalTable", false, 500)]
    public static void CreateSpellTotalTableAssetFile()
    {
        SpellTotalTable asset = CustomAssetUtility.CreateAsset<SpellTotalTable>();
        asset.SheetName = "../Excel/Spell.xlsx";
        asset.WorksheetName = "SpellTotalTable";
        EditorUtility.SetDirty(asset);        
    }
    
}