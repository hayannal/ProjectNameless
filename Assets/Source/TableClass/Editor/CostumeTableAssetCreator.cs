using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/CostumeTable", false, 500)]
    public static void CreateCostumeTableAssetFile()
    {
        CostumeTable asset = CustomAssetUtility.CreateAsset<CostumeTable>();
        asset.SheetName = "../Excel/Costume.xlsx";
        asset.WorksheetName = "CostumeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}