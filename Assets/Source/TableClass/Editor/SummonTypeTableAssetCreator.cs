using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/SummonTypeTable", false, 500)]
    public static void CreateSummonTypeTableAssetFile()
    {
        SummonTypeTable asset = CustomAssetUtility.CreateAsset<SummonTypeTable>();
        asset.SheetName = "../Excel/Summon.xlsx";
        asset.WorksheetName = "SummonTypeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}