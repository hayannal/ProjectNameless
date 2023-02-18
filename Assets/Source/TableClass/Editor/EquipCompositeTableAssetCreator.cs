using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/EquipCompositeTable", false, 500)]
    public static void CreateEquipCompositeTableAssetFile()
    {
        EquipCompositeTable asset = CustomAssetUtility.CreateAsset<EquipCompositeTable>();
        asset.SheetName = "../Excel/Equip.xlsx";
        asset.WorksheetName = "EquipCompositeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}