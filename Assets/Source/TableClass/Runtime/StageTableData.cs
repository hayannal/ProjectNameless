using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class StageTableData
{
  [SerializeField]
  int _type;
  public int type { get { return _type; } set { _type = value; } }
  
  [SerializeField]
  int _stage;
  public int stage { get { return _stage; } set { _stage = value; } }
  
  [SerializeField]
  float _standardHp;
  public float standardHp { get { return _standardHp; } set { _standardHp = value; } }
  
  [SerializeField]
  float _standardAtk;
  public float standardAtk { get { return _standardAtk; } set { _standardAtk = value; } }
  
  [SerializeField]
  string[] _environmentSetting = new string[0];
  public string[] environmentSetting { get { return _environmentSetting; } set { _environmentSetting = value; } }
  
  [SerializeField]
  string _plane;
  public string plane { get { return _plane; } set { _plane = value; } }
  
  [SerializeField]
  string _ground;
  public string ground { get { return _ground; } set { _ground = value; } }
  
  [SerializeField]
  string _wall;
  public string wall { get { return _wall; } set { _wall = value; } }
  
  [SerializeField]
  float _playerSpawnx;
  public float playerSpawnx { get { return _playerSpawnx; } set { _playerSpawnx = value; } }
  
  [SerializeField]
  float _playerSpawnz;
  public float playerSpawnz { get { return _playerSpawnz; } set { _playerSpawnz = value; } }
  
  [SerializeField]
  float _monsterSpawnx;
  public float monsterSpawnx { get { return _monsterSpawnx; } set { _monsterSpawnx = value; } }
  
  [SerializeField]
  float _monsterSpawnz;
  public float monsterSpawnz { get { return _monsterSpawnz; } set { _monsterSpawnz = value; } }
  
  [SerializeField]
  float _monsterTargetx;
  public float monsterTargetx { get { return _monsterTargetx; } set { _monsterTargetx = value; } }
  
  [SerializeField]
  float _monsterTargetz;
  public float monsterTargetz { get { return _monsterTargetz; } set { _monsterTargetz = value; } }
  
  [SerializeField]
  float _redLinex;
  public float redLinex { get { return _redLinex; } set { _redLinex = value; } }
  
  [SerializeField]
  float _redLinez;
  public float redLinez { get { return _redLinez; } set { _redLinez = value; } }
  
  [SerializeField]
  string _spawnInfo;
  public string spawnInfo { get { return _spawnInfo; } set { _spawnInfo = value; } }
  
}