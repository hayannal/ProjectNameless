using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
///
[CustomEditor(typeof(SevenDaysTypeTable))]
public class SevenDaysTypeTableEditor : BaseExcelEditor<SevenDaysTypeTable>
{	    
    public override bool Load()
    {
        SevenDaysTypeTable targetData = target as SevenDaysTypeTable;

        string path = targetData.SheetName;

		path = ExcelMachineEditor.CheckRootPath(path);

        if (!File.Exists(path))
            return false;

        string sheet = targetData.WorksheetName;

        ExcelQuery query = new ExcelQuery(path, sheet);
        if (query != null && query.IsValid())
        {
            targetData.dataArray = query.Deserialize<SevenDaysTypeTableData>().ToArray();
            EditorUtility.SetDirty(targetData);
            AssetDatabase.SaveAssets();
            return true;
        }
        else
            return false;
    }
}
