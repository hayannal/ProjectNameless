using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/AttendanceTypeTable", false, 500)]
    public static void CreateAttendanceTypeTableAssetFile()
    {
        AttendanceTypeTable asset = CustomAssetUtility.CreateAsset<AttendanceTypeTable>();
        asset.SheetName = "../Excel/Attendance.xlsx";
        asset.WorksheetName = "AttendanceTypeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}