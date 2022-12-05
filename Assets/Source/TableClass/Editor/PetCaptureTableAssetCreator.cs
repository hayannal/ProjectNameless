using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/PetCaptureTable", false, 500)]
    public static void CreatePetCaptureTableAssetFile()
    {
        PetCaptureTable asset = CustomAssetUtility.CreateAsset<PetCaptureTable>();
        asset.SheetName = "../Excel/Pet.xlsx";
        asset.WorksheetName = "PetCaptureTable";
        EditorUtility.SetDirty(asset);        
    }
    
}