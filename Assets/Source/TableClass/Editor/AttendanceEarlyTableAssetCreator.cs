using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/AttendanceEarlyTable", false, 500)]
    public static void CreateAttendanceEarlyTableAssetFile()
    {
        AttendanceEarlyTable asset = CustomAssetUtility.CreateAsset<AttendanceEarlyTable>();
        asset.SheetName = "../Excel/Attendance.xlsx";
        asset.WorksheetName = "AttendanceEarlyTable";
        EditorUtility.SetDirty(asset);        
    }
    
}