using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class EquipLevelTableData
{
  [SerializeField]
  string _equipId;
  public string equipId { get { return _equipId; } set { _equipId = value; } }
  
  [SerializeField]
  string _equipGroup;
  public string equipGroup { get { return _equipGroup; } set { _equipGroup = value; } }
  
  [SerializeField]
  int _grade;
  public int grade { get { return _grade; } set { _grade = value; } }
  
  [SerializeField]
  int[] _atk = new int[0];
  public int[] atk { get { return _atk; } set { _atk = value; } }
  
}