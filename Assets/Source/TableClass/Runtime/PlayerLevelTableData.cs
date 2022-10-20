using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class PlayerLevelTableData
{
  [SerializeField]
  int _playerLevel;
  public int playerLevel { get { return _playerLevel; } set { _playerLevel = value; } }
  
  [SerializeField]
  int _accumulatedAtk;
  public int accumulatedAtk { get { return _accumulatedAtk; } set { _accumulatedAtk = value; } }
  
  [SerializeField]
  int _subLevelCount;
  public int subLevelCount { get { return _subLevelCount; } set { _subLevelCount = value; } }
  
  [SerializeField]
  string _subGold;
  public string subGold { get { return _subGold; } set { _subGold = value; } }
  
  [SerializeField]
  string _subGoldSale;
  public string subGoldSale { get { return _subGoldSale; } set { _subGoldSale = value; } }
  
  [SerializeField]
  int _addedAtk;
  public int addedAtk { get { return _addedAtk; } set { _addedAtk = value; } }
  
  [SerializeField]
  int _requiredGold;
  public int requiredGold { get { return _requiredGold; } set { _requiredGold = value; } }
  
  [SerializeField]
  int _saleRequiredGold;
  public int saleRequiredGold { get { return _saleRequiredGold; } set { _saleRequiredGold = value; } }
  
}