using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class GachaActorTableData
{
  [SerializeField]
  int _grade;
  public int grade { get { return _grade; } set { _grade = value; } }
  
  [SerializeField]
  float _prob;
  public float prob { get { return _prob; } set { _prob = value; } }
  
  [SerializeField]
  float[] _adjustProbs = new float[0];
  public float[] adjustProbs { get { return _adjustProbs; } set { _adjustProbs = value; } }
  
}