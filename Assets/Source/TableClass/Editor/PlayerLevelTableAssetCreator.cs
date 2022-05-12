using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/PlayerLevelTable", false, 500)]
    public static void CreatePlayerLevelTableAssetFile()
    {
        PlayerLevelTable asset = CustomAssetUtility.CreateAsset<PlayerLevelTable>();
        asset.SheetName = "../Excel/Actor.xlsx";
        asset.WorksheetName = "PlayerLevelTable";
        EditorUtility.SetDirty(asset);        
    }
    
}