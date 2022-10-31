using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class GachaSpellTableData
{
  [SerializeField]
  int _grade;
  public int grade { get { return _grade; } set { _grade = value; } }
  
  [SerializeField]
  int _star;
  public int star { get { return _star; } set { _star = value; } }
  
  [SerializeField]
  float[] _probs = new float[0];
  public float[] probs { get { return _probs; } set { _probs = value; } }
  
}