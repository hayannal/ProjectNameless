using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/BossBattleRewardTable", false, 500)]
    public static void CreateBossBattleRewardTableAssetFile()
    {
        BossBattleRewardTable asset = CustomAssetUtility.CreateAsset<BossBattleRewardTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "BossBattleRewardTable";
        EditorUtility.SetDirty(asset);        
    }
    
}