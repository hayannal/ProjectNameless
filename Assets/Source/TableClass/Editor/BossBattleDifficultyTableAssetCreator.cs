using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/BossBattleDifficultyTable", false, 500)]
    public static void CreateBossBattleDifficultyTableAssetFile()
    {
        BossBattleDifficultyTable asset = CustomAssetUtility.CreateAsset<BossBattleDifficultyTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "BossBattleDifficultyTable";
        EditorUtility.SetDirty(asset);        
    }
    
}