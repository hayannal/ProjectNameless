using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/MonsterGroupTable", false, 500)]
    public static void CreateMonsterGroupTableAssetFile()
    {
        MonsterGroupTable asset = CustomAssetUtility.CreateAsset<MonsterGroupTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "MonsterGroupTable";
        EditorUtility.SetDirty(asset);        
    }
    
}