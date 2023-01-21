using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/BrokenEnergyTable", false, 500)]
    public static void CreateBrokenEnergyTableAssetFile()
    {
        BrokenEnergyTable asset = CustomAssetUtility.CreateAsset<BrokenEnergyTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "BrokenEnergyTable";
        EditorUtility.SetDirty(asset);        
    }
    
}