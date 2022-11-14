using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/GachaActorTable", false, 500)]
    public static void CreateGachaActorTableAssetFile()
    {
        GachaActorTable asset = CustomAssetUtility.CreateAsset<GachaActorTable>();
        asset.SheetName = "../Excel/Gacha.xlsx";
        asset.WorksheetName = "GachaActorTable";
        EditorUtility.SetDirty(asset);        
    }
    
}