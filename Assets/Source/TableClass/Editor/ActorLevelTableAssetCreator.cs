using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ActorLevelTable", false, 500)]
    public static void CreateActorLevelTableAssetFile()
    {
        ActorLevelTable asset = CustomAssetUtility.CreateAsset<ActorLevelTable>();
        asset.SheetName = "../Excel/Actor.xlsx";
        asset.WorksheetName = "ActorLevelTable";
        EditorUtility.SetDirty(asset);        
    }
    
}