using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class LevelPackTableData
{
  [SerializeField]
  string _levelPackId;
  public string levelPackId { get { return _levelPackId; } set { _levelPackId = value; } }
  
  [SerializeField]
  string _iconAddress;
  public string iconAddress { get { return _iconAddress; } set { _iconAddress = value; } }
  
  [SerializeField]
  bool _exclusive;
  public bool exclusive { get { return _exclusive; } set { _exclusive = value; } }
  
  [SerializeField]
  bool _noHit;
  public bool noHit { get { return _noHit; } set { _noHit = value; } }
  
  [SerializeField]
  bool _colored;
  public bool colored { get { return _colored; } set { _colored = value; } }
  
  [SerializeField]
  float _dropWeight;
  public float dropWeight { get { return _dropWeight; } set { _dropWeight = value; } }
  
  [SerializeField]
  string[] _affectorValueId = new string[0];
  public string[] affectorValueId { get { return _affectorValueId; } set { _affectorValueId = value; } }
  
  [SerializeField]
  string _nameId;
  public string nameId { get { return _nameId; } set { _nameId = value; } }
  
  [SerializeField]
  string _descriptionId;
  public string descriptionId { get { return _descriptionId; } set { _descriptionId = value; } }
  
  [SerializeField]
  int _max;
  public int max { get { return _max; } set { _max = value; } }
  
  [SerializeField]
  bool _useAffectorValueIdOverriding;
  public bool useAffectorValueIdOverriding { get { return _useAffectorValueIdOverriding; } set { _useAffectorValueIdOverriding = value; } }
  
  [SerializeField]
  string[] _effectAddress = new string[0];
  public string[] effectAddress { get { return _effectAddress; } set { _effectAddress = value; } }
  
  [SerializeField]
  int _openChapter;
  public int openChapter { get { return _openChapter; } set { _openChapter = value; } }
  
}