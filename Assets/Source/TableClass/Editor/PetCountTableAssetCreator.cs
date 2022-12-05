using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/PetCountTable", false, 500)]
    public static void CreatePetCountTableAssetFile()
    {
        PetCountTable asset = CustomAssetUtility.CreateAsset<PetCountTable>();
        asset.SheetName = "../Excel/Pet.xlsx";
        asset.WorksheetName = "PetCountTable";
        EditorUtility.SetDirty(asset);        
    }
    
}