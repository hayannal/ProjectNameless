using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/AttendanceRewardTable", false, 500)]
    public static void CreateAttendanceRewardTableAssetFile()
    {
        AttendanceRewardTable asset = CustomAssetUtility.CreateAsset<AttendanceRewardTable>();
        asset.SheetName = "../Excel/Attendance.xlsx";
        asset.WorksheetName = "AttendanceRewardTable";
        EditorUtility.SetDirty(asset);        
    }
    
}