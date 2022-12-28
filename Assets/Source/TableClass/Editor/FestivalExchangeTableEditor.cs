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
[CustomEditor(typeof(FestivalExchangeTable))]
public class FestivalExchangeTableEditor : BaseExcelEditor<FestivalExchangeTable>
{	    
    public override bool Load()
    {
        FestivalExchangeTable targetData = target as FestivalExchangeTable;

        string path = targetData.SheetName;

		path = ExcelMachineEditor.CheckRootPath(path);

        if (!File.Exists(path))
            return false;

        string sheet = targetData.WorksheetName;

        ExcelQuery query = new ExcelQuery(path, sheet);
        if (query != null && query.IsValid())
        {
            targetData.dataArray = query.Deserialize<FestivalExchangeTableData>().ToArray();
            EditorUtility.SetDirty(targetData);
            AssetDatabase.SaveAssets();
            return true;
        }
        else
            return false;
    }
}
