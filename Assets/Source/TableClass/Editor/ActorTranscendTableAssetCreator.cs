using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ActorTranscendTable", false, 500)]
    public static void CreateActorTranscendTableAssetFile()
    {
        ActorTranscendTable asset = CustomAssetUtility.CreateAsset<ActorTranscendTable>();
        asset.SheetName = "../Excel/Actor.xlsx";
        asset.WorksheetName = "ActorTranscendTable";
        EditorUtility.SetDirty(asset);        
    }
    
}