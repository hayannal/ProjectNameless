using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/EnergyUsePaybackTable", false, 500)]
    public static void CreateEnergyUsePaybackTableAssetFile()
    {
        EnergyUsePaybackTable asset = CustomAssetUtility.CreateAsset<EnergyUsePaybackTable>();
        asset.SheetName = "../Excel/Event.xlsx";
        asset.WorksheetName = "EnergyUsePaybackTable";
        EditorUtility.SetDirty(asset);        
    }
    
}