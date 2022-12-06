using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/PetSaleTable", false, 500)]
    public static void CreatePetSaleTableAssetFile()
    {
        PetSaleTable asset = CustomAssetUtility.CreateAsset<PetSaleTable>();
        asset.SheetName = "../Excel/Pet.xlsx";
        asset.WorksheetName = "PetSaleTable";
        EditorUtility.SetDirty(asset);        
    }
    
}