using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/PetTable", false, 500)]
    public static void CreatePetTableAssetFile()
    {
        PetTable asset = CustomAssetUtility.CreateAsset<PetTable>();
        asset.SheetName = "../Excel/Pet.xlsx";
        asset.WorksheetName = "PetTable";
        EditorUtility.SetDirty(asset);        
    }
    
}