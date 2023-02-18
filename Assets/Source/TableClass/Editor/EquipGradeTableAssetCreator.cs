using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/EquipGradeTable", false, 500)]
    public static void CreateEquipGradeTableAssetFile()
    {
        EquipGradeTable asset = CustomAssetUtility.CreateAsset<EquipGradeTable>();
        asset.SheetName = "../Excel/Equip.xlsx";
        asset.WorksheetName = "EquipGradeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}