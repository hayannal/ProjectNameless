using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/SpellGradeLevelTable", false, 500)]
    public static void CreateSpellGradeLevelTableAssetFile()
    {
        SpellGradeLevelTable asset = CustomAssetUtility.CreateAsset<SpellGradeLevelTable>();
        asset.SheetName = "../Excel/Spell.xlsx";
        asset.WorksheetName = "SpellGradeLevelTable";
        EditorUtility.SetDirty(asset);        
    }
    
}