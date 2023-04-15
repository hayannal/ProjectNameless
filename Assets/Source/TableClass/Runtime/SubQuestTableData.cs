using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class SubQuestTableData
{
  [SerializeField]
  int _type;
  public int type { get { return _type; } set { _type = value; } }
  
  [SerializeField]
  string _descriptionId;
  public string descriptionId { get { return _descriptionId; } set { _descriptionId = value; } }
  
  [SerializeField]
  string _shortDescriptionId;
  public string shortDescriptionId { get { return _shortDescriptionId; } set { _shortDescriptionId = value; } }
  
  [SerializeField]
  int[] _needCount = new int[0];
  public int[] needCount { get { return _needCount; } set { _needCount = value; } }
  
  [SerializeField]
  int[] _rewardEnergy = new int[0];
  public int[] rewardEnergy { get { return _rewardEnergy; } set { _rewardEnergy = value; } }
  
}