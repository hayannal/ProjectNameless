using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/PickOneCharacterTable", false, 500)]
    public static void CreatePickOneCharacterTableAssetFile()
    {
        PickOneCharacterTable asset = CustomAssetUtility.CreateAsset<PickOneCharacterTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "PickOneCharacterTable";
        EditorUtility.SetDirty(asset);        
    }
    
}