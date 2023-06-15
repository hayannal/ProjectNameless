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
[CustomEditor(typeof(BossBattleRewardTable))]
public class BossBattleRewardTableEditor : BaseExcelEditor<BossBattleRewardTable>
{	    
    public override bool Load()
    {
        BossBattleRewardTable targetData = target as BossBattleRewardTable;

        string path = targetData.SheetName;

		path = ExcelMachineEditor.CheckRootPath(path);

        if (!File.Exists(path))
            return false;

        string sheet = targetData.WorksheetName;

        ExcelQuery query = new ExcelQuery(path, sheet);
        if (query != null && query.IsValid())
        {
            targetData.dataArray = query.Deserialize<BossBattleRewardTableData>().ToArray();
            EditorUtility.SetDirty(targetData);
            AssetDatabase.SaveAssets();
            return true;
        }
        else
            return false;
    }
}
