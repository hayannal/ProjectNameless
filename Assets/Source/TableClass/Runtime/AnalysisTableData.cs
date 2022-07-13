using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class AnalysisTableData
{
  [SerializeField]
  int _level;
  public int level { get { return _level; } set { _level = value; } }
  
  [SerializeField]
  int _requiredTime;
  public int requiredTime { get { return _requiredTime; } set { _requiredTime = value; } }
  
  [SerializeField]
  int _requiredAccumulatedTime;
  public int requiredAccumulatedTime { get { return _requiredAccumulatedTime; } set { _requiredAccumulatedTime = value; } }
  
  [SerializeField]
  int _maxTime;
  public int maxTime { get { return _maxTime; } set { _maxTime = value; } }
  
  [SerializeField]
  int _forceLeveling;
  public int forceLeveling { get { return _forceLeveling; } set { _forceLeveling = value; } }
  
  [SerializeField]
  float _goldPerTime;
  public float goldPerTime { get { return _goldPerTime; } set { _goldPerTime = value; } }
  
  [SerializeField]
  int _accumulatedAtk;
  public int accumulatedAtk { get { return _accumulatedAtk; } set { _accumulatedAtk = value; } }
  
}