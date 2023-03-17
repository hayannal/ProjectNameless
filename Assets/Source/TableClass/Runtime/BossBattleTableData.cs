using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class BossBattleTableData
{
  [SerializeField]
  int _num;
  public int num { get { return _num; } set { _num = value; } }
  
  [SerializeField]
  int _stage;
  public int stage { get { return _stage; } set { _stage = value; } }
  
  [SerializeField]
  string _bossAddress;
  public string bossAddress { get { return _bossAddress; } set { _bossAddress = value; } }
  
  [SerializeField]
  string _nameId;
  public string nameId { get { return _nameId; } set { _nameId = value; } }
  
  [SerializeField]
  string _descriptionId;
  public string descriptionId { get { return _descriptionId; } set { _descriptionId = value; } }
  
  [SerializeField]
  string[] _suggestedActorId = new string[0];
  public string[] suggestedActorId { get { return _suggestedActorId; } set { _suggestedActorId = value; } }
  
  [SerializeField]
  int _startDifficulty;
  public int startDifficulty { get { return _startDifficulty; } set { _startDifficulty = value; } }
  
  [SerializeField]
  int _defaultHave;
  public int defaultHave { get { return _defaultHave; } set { _defaultHave = value; } }
  
}