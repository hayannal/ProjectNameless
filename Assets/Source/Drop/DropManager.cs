﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;

public class DropManager : MonoBehaviour
{
	public static DropManager instance
	{
		get
		{
			if (_instance == null)
				_instance = (new GameObject("DropManager")).AddComponent<DropManager>();
			return _instance;
		}
	}
	static DropManager _instance = null;


	#region Drop Info
	ObscuredFloat _dropGold;
	ObscuredInt _dropSeal;
	List<ObscuredString> _listDropEquipId;

	public void AddDropGold(float gold)
	{
		// 이미 DropAdjustAffector.eDropAdjustType.GoldDropAmount 적용된채로 누적되어있는 값이다. 정산시 더해주면 끝이다.
		_dropGold += gold;

		// 인장과 골드는 중요도가 낮아서 간단하게 처리하기로 해서 DropObject의 획득 시점때만 기록해두려고 했는데
		// 골드의 경우 드랍오브젝트 개수가 너무 많이 분할되서 괜히 부하가 먹는거 같다.
		// 그래서 여기서 안하고 층 하나 넘어가는 시점 당 한번씩 기록하기로 한다.
		//if (ClientSaveData.instance.IsLoadingInProgressGame() == false)
		//	ClientSaveData.instance.OnChangedDropGold(_dropGold);
	}

	public int GetStackedDropGold()
	{
		return (int)_dropGold;
	}

	public float GetStackedFloatDropGold()
	{
		return _dropGold;
	}

	public void AddDropSeal(int amount)
	{
		_dropSeal += amount;

		/*
		// 인장과 골드는 중요도가 낮아서 간단하게 처리하기로 했다.
		if (ClientSaveData.instance.IsLoadingInProgressGame() == false)
			ClientSaveData.instance.OnChangedDropSeal(_dropSeal);
		*/
	}

	public int GetStackedDropSeal()
	{
		return _dropSeal;
	}

	public void AddDropItem(string equipId)
	{
		if (string.IsNullOrEmpty(equipId))
			return;

		if (_listDropEquipId == null)
			_listDropEquipId = new List<ObscuredString>();

		_listDropEquipId.Add(equipId);

		// 장비 획득이 되면 서버에 카운트를 증가시켜둔다. EndGame에서 검증하기 위함으로 하려다가 오히려 선량한 유저의 플레이를 방해할까봐 안하기로 한다.
		// 적어도 전설에 대해선 황금열쇠 체크를 하니 패스하기로 해본다.
		//PlayFabApiManager.instance.RequestAddDropEquipCount();
	}

	public List<ObscuredString> GetStackedDropEquipList()
	{
		return _listDropEquipId;
	}

	public int droppedStageItemCount { get; set; }

	#region Legend Key
	// 전설키를 DropItem과 달리 따로 체크해야한다.
	// 위 DropItem은 습득하고 난 아이템 리스트를 관리하는건데
	// 전설키의 개수를 가지고 weight를 조정하는건 드랍되는 시점에서 바로 카운트에 반영되어야하는거라
	// DropItem에 들어있는 전설로 하게되면 틀어질 수 있다.(전설키가 1개 남은 상황에서 2개의 전설이 드랍될 수 있다.)
	//
	// 그래서 차라리 별도의 드랍 카운트 변수를 만들고
	// 드랍이 결정될때마다 증가시켜서 관리하기로 한다.
	// 초기화는 신경쓸필요 없는게 전투끝나고 돌아올때 어차피 DropManager가 삭제되고 새로 만들어지기 때문에 신경쓰지 않아도 된다.
	public int droppedLengendItemCount { get; set; }
	#endregion


	#region
	// Seal이랑 거의 비슷한 구조로 추가해둔다.
	ObscuredInt _dropChaosFragment;

	public void AddDropChaosFragment(int amount)
	{
		_dropChaosFragment += amount;
		/*
		if (ClientSaveData.instance.IsLoadingInProgressGame() == false)
			ClientSaveData.instance.OnChangedDropChaosFragment(_dropChaosFragment);
		*/
	}

	public int GetStackedDropChaosFragment()
	{
		return _dropChaosFragment;
	}

	public int droppedChaosFragmentCount { get; set; }
	#endregion


	public int stackDropExp { get { return _stackDropExp; } }
	int _stackDropExp = 0;
	public void StackDropExp(int exp)
	{
		_stackDropExp += exp;
	}

	public void GetStackedDropExp()
	{
		/*
		// Stack된걸 적용하기 직전에 현재 맵의 보정치를 적용시킨다.
		_stackDropExp += StageManager.instance.addDropExp;
		_stackDropExp = (int)(_stackDropExp * StageManager.instance.currentStageTableData.dropExpAdjustment);
		*/

		//Debug.LogFormat("Drop Exp Add {0} / Get Exp : {1}", StageManager.instance.addDropExp, _stackDropExp);

		if (_stackDropExp < 0)
			Debug.LogError("Invalid Drop Exp : Negative Total Exp!");

		/*
		// 경험치 얻는 처리를 한다.
		// 이펙트가 먼저 나오고 곧바로 렙업창이 뜬다. 두번 이상 렙업 되는걸 처리하기 위해 업데이트 돌면서 스택에 쌓아둔채 꺼내쓰는 방법으로 해야할거다.
		StageManager.instance.AddExp(_stackDropExp);
		*/

		_stackDropExp = 0;
	}

	// 레벨팩이 드랍되면 체크해놨다가 먹어야 GatePillar가 나오게 해야한다.
	public int reservedLevelPackCount { get; set; }
	#endregion



	#region Drop Object
	List<DropObject> _listDropObject = new List<DropObject>();
	public void OnInitializeDropObject(DropObject dropObject)
	{
		_listDropObject.Add(dropObject);
	}

	public void OnFinalizeDropObject(DropObject dropObject)
	{
		_listDropObject.Remove(dropObject);
	}

	DropObject _reservedLastDropObject;
	public void ReserveLastDropObject(DropObject dropObject)
	{
		_reservedLastDropObject = dropObject;
	}

	public bool IsExistReservedLastDropObject()
	{
		return (_reservedLastDropObject != null);
	}

	public void ApplyLastDropObject()
	{
		if (_reservedLastDropObject != null)
		{
			_reservedLastDropObject.ApplyLastDropObject();
			_reservedLastDropObject = null;
		}
	}

	public void OnDropLastMonsterInStage()
	{
		for (int i = 0; i < _listDropObject.Count; ++i)
			_listDropObject[i].OnAfterBattle();
	}

	public void OnFinishLastDropAnimation()
	{
		for (int i = 0; i < _listDropObject.Count; ++i)
		{
			// 다음 스테이지에 드랍된 템들은 켜져있지 않을거다. 패스.
			if (_listDropObject[i].onAfterBattle == false)
				continue;

			_listDropObject[i].OnAfterAllDropAnimation();
		}
	}
	#endregion


	#region EndGame
	public bool IsExistAcquirableDropObject()
	{
		// 정산 직전에 쓰는 함수다. 획득할 수 있는 드랍 오브젝트가 하나도 없어야 정산이 가능하다.

		// 드랍 오브젝트가 생성되기 전이라서 드랍정보만 가지고는 알기 어렵기 때문에 DropProcessor가 하나라도 살아있다면 우선 기다린다.
		if (BattleInstanceManager.instance.IsAliveAnyDropProcessor())
			return true;

		// 생성되어있는 DropObject를 뒤져서 획득할 수 있는게 하나도 없어야 한다.
		for (int i = 0; i < _listDropObject.Count; ++i)
		{
			if (_listDropObject[i].IsAcquirableForEnd())
				return true;
		}
		return false;
	}
	#endregion





	/*
	/// ///////////////////////////////////////////////////////////////////////////////////////////////////////
	/// 여기서부터는 뽑기로직
	#region Stage Drop Equip
	class RandomDropEquipInfo
	{
		public EquipTableData equipTableData;
		public float sumWeight;
	}
	List<RandomDropEquipInfo> _listRandomDropEquipInfo = null;
	int _lastDropChapter = -1;
	int _lastDropStage = -1;
	int _lastLegendKey = -1;
	public string GetStageDropEquipId()
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby)
			return "";

		bool needRefresh = false;
		if (_lastDropChapter != StageManager.instance.playChapter || _lastDropStage != StageManager.instance.playStage || _lastLegendKey != GetRemainLegendKey())
		{
			needRefresh = true;
			_lastDropChapter = StageManager.instance.playChapter;
			_lastDropStage = StageManager.instance.playStage;
			_lastLegendKey = GetRemainLegendKey();
		}

		if (needRefresh)
		{
			if (_listRandomDropEquipInfo == null)
				_listRandomDropEquipInfo = new List<RandomDropEquipInfo>();
			_listRandomDropEquipInfo.Clear();

			float sumWeight = 0.0f;
			for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
			{
				float weight = TableDataManager.instance.equipTable.dataArray[i].stageDropWeight;
				if (weight <= 0.0f)
					continue;

				bool add = false;
				int playChapter = PlayerData.instance.currentChaosMode ? (StageManager.instance.playChapter - 1) : (int)StageManager.instance.playChapter;
				if (playChapter > TableDataManager.instance.equipTable.dataArray[i].startingDropChapter)
					add = true;
				// MonsterActor.OnDie 함수 안에서 드랍의 모든 정보가 결정되기때문에 StageManager.instance.playStage을 사용해도 괜찮다.
				// 다음 스테이지로 넘어가서 드랍아이템이 생성되더라도 이미 정보는 다 킬 시점에 결정되기 때문.
				if (add == false && playChapter == TableDataManager.instance.equipTable.dataArray[i].startingDropChapter && StageManager.instance.playStage >= TableDataManager.instance.equipTable.dataArray[i].startingDropStage)
					add = true;
				if (add == false)
					continue;

				if (EquipData.IsUseLegendKey(TableDataManager.instance.equipTable.dataArray[i]))
				{
					float adjustWeight = 0.0f;
					RemainTableData remainTableData = TableDataManager.instance.FindRemainTableData(GetRemainLegendKey());
					if (remainTableData != null)
						adjustWeight = remainTableData.adjustWeight;
					// adjustWeight 검증
					if (adjustWeight > 1.0f)
						CheatingListener.OnDetectCheatTable();
					weight *= adjustWeight;
					if (weight <= 0.0f)
						continue;
				}

				sumWeight += weight;
				RandomDropEquipInfo newInfo = new RandomDropEquipInfo();
				newInfo.equipTableData = TableDataManager.instance.equipTable.dataArray[i];
				newInfo.sumWeight = sumWeight;
				_listRandomDropEquipInfo.Add(newInfo);
			}
		}

		if (_listRandomDropEquipInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listRandomDropEquipInfo[_listRandomDropEquipInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomDropEquipInfo.Count; ++i)
		{
			if (random <= _listRandomDropEquipInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";

		++droppedStageItemCount;

		// 바로 감소시켜놔야 다음번 드랍될때 _lastLegendKey가 달라지면서 드랍 리스트를 리프레쉬 하게 된다.
		// 인게임에서만 적용되는 수치로 장비뽑기할때는 적용받지 않는다.
		if (EquipData.IsUseLegendKey(_listRandomDropEquipInfo[index].equipTableData))
			++droppedLengendItemCount;
		return _listRandomDropEquipInfo[index].equipTableData.equipId;
	}

	int GetRemainLegendKey()
	{
		return CurrencyData.instance.legendKey - droppedLengendItemCount * 10;
	}
	#endregion

	#region BossBattle
	List<RandomDropEquipInfo> _listBossDropEquipInfo = null;
	public string GetBossDropEquipId()
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby)
			return "";

		// 위에 있는 기본 스테이지 드랍인 GetStageDropEquipId와 비슷하지만 전설키 처리만 조금 다른 함수다.
		// 보스한테 한번 굴리는거라 _last 체크없이 항상 굴리기로 한다.

		if (_listBossDropEquipInfo == null)
			_listBossDropEquipInfo = new List<RandomDropEquipInfo>();
		_listBossDropEquipInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.equipTable.dataArray[i].stageDropWeight;
			if (weight <= 0.0f)
				continue;

			bool add = false;
			int playChapter = StageManager.instance.currentStageTableData.chapter;
			// Difficulty 1이어도 2인거처럼 해야 드랍템을 구할 수 있어서 예외처리.
			if (playChapter == 1) playChapter = 2;
			if (playChapter >= TableDataManager.instance.equipTable.dataArray[i].startingDropChapter)
				add = true;
			
			if (add == false)
				continue;

			if (EquipData.IsUseLegendKey(TableDataManager.instance.equipTable.dataArray[i]))
			{
				float adjustWeight = 0.0f;
				int tempKey = GetRemainLegendKey();
				if (tempKey > 10) tempKey = 10;
				RemainTableData remainTableData = TableDataManager.instance.FindRemainTableData(tempKey);
				if (remainTableData != null)
					adjustWeight = remainTableData.adjustWeight;
				// adjustWeight 검증
				if (adjustWeight > 1.0f)
					CheatingListener.OnDetectCheatTable();
				weight *= adjustWeight;
				if (weight <= 0.0f)
					continue;
			}

			sumWeight += weight;
			RandomDropEquipInfo newInfo = new RandomDropEquipInfo();
			newInfo.equipTableData = TableDataManager.instance.equipTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listBossDropEquipInfo.Add(newInfo);
		}

		if (_listBossDropEquipInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listBossDropEquipInfo[_listBossDropEquipInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listBossDropEquipInfo.Count; ++i)
		{
			if (random <= _listBossDropEquipInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";

		++droppedStageItemCount;

		// 바로 감소시켜놔야 다음번 드랍될때 _lastLegendKey가 달라지면서 드랍 리스트를 리프레쉬 하게 된다.
		// 인게임에서만 적용되는 수치로 장비뽑기할때는 적용받지 않는다.
		if (EquipData.IsUseLegendKey(_listBossDropEquipInfo[index].equipTableData))
			++droppedLengendItemCount;
		return _listBossDropEquipInfo[index].equipTableData.equipId;
	}

	// not streak에 영향주지 않는 보스 첫클리어 보상용이다.
	List<RandomDropEquipInfo> _listBossFirstDropEquipInfo = null;
	public string GetBossEquipIdByGrade(int grade = -1)
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby)
			return "";

		if (_listBossFirstDropEquipInfo == null)
			_listBossFirstDropEquipInfo = new List<RandomDropEquipInfo>();
		_listBossFirstDropEquipInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.equipTable.dataArray[i].equipGachaWeight;
			if (weight <= 0.0f)
				continue;

			// 보스드랍이기 때문에 캐시 장비들은 다 빼야한다.
			switch ((TimeSpaceData.eEquipSlotType)TableDataManager.instance.equipTable.dataArray[i].equipType)
			{
				case TimeSpaceData.eEquipSlotType.Gun:
				case TimeSpaceData.eEquipSlotType.Shield:
				case TimeSpaceData.eEquipSlotType.TwoHanded:
					continue;
			}

			if (grade == -1)
			{
				// 전설뽑기니 나머지 제외
				if (EquipData.IsUseLegendKey(TableDataManager.instance.equipTable.dataArray[i]) == false)
					continue;

				// equipGachaWeight 검증
				if (weight > 1.0f)
					CheatingListener.OnDetectCheatTable();
			}
			else
			{
				if (grade == 0 || grade == 1 || grade == 2 || grade == 3)
				{
					if (TableDataManager.instance.equipTable.dataArray[i].grade != grade)
						continue;
				}
				else
					continue;
			}

			sumWeight += weight;
			RandomDropEquipInfo newInfo = new RandomDropEquipInfo();
			newInfo.equipTableData = TableDataManager.instance.equipTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listBossFirstDropEquipInfo.Add(newInfo);
		}
		if (_listBossFirstDropEquipInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listBossFirstDropEquipInfo[_listBossFirstDropEquipInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listBossFirstDropEquipInfo.Count; ++i)
		{
			if (random <= _listBossFirstDropEquipInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listBossFirstDropEquipInfo[index].equipTableData.equipId;
	}
	#endregion

	List<RandomDropEquipInfo> _listFullChaosRevertDropEquipInfo = null;
	public string GetFullChaosRevertDropEquipId()
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		// Invasion 예외처리.
		if (lobby == false && BattleManager.instance != null && BattleManager.instance.IsInvasion())
			lobby = true;
		if (lobby == false)
			return "";

		if (_listFullChaosRevertDropEquipInfo == null)
			_listFullChaosRevertDropEquipInfo = new List<RandomDropEquipInfo>();
		_listFullChaosRevertDropEquipInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.equipTable.dataArray[i].stageDropWeight;
			if (weight <= 0.0f)
				continue;

			// 환원 보상은 Stage 0 으로 처리하기 때문에 챕터만 비교해서 이전 챕터꺼만 포함시키면 된다. 같은 챕터의 스테이지는 비교할 필요 없다.
			bool add = false;
			int playChapter = PlayerData.instance.currentChaosMode ? (StageManager.instance.playChapter - 1) : (int)StageManager.instance.playChapter;
			if (playChapter > TableDataManager.instance.equipTable.dataArray[i].startingDropChapter)
				add = true;
			if (add == false)
				continue;

			// 환원 보상에서는 전설 아이템이 나오지 않는다.
			if (EquipData.IsUseLegendKey(TableDataManager.instance.equipTable.dataArray[i]))
				continue;

			sumWeight += weight;
			RandomDropEquipInfo newInfo = new RandomDropEquipInfo();
			newInfo.equipTableData = TableDataManager.instance.equipTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listFullChaosRevertDropEquipInfo.Add(newInfo);
		}

		if (_listFullChaosRevertDropEquipInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listFullChaosRevertDropEquipInfo[_listFullChaosRevertDropEquipInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listFullChaosRevertDropEquipInfo.Count; ++i)
		{
			if (random <= _listFullChaosRevertDropEquipInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listFullChaosRevertDropEquipInfo[index].equipTableData.equipId;
	}


	/////////////////////////////////////////////////////////////////////////////////////////////////
	// 이 아래서부터는 Lobby에서 사용하는 가차용 뽑기 로직들이다.
	// 한번 드랍프로세서가 동작하고 나서는 패킷 주고받은 후 초기화를 해줘야한다.
	public void ClearLobbyDropInfo()
	{
		// 3연차 8연차 등등 하나의 연속가차 안에서 썼던 정보들이다.
		// 연차 중에는 이왕이면 pp를 각각 나눠서 뽑는다. 캐릭터는 동시에 같은 캐릭터를 뽑을 수 없다. 연속으로 전설템을 못뽑으면 확률이 증가한다. 등등의 조건을 처리하기 위해 사용하는 임시 변수들이다.
		_droppedNotStreakItemCount = 0;
		droppedNotStreakCharCount = 0;
		droppedNotStreakLegendCharCount = 0;
		_listDroppedActorId.Clear();
		_listDroppedPowerPointId.Clear();
		droppedAnalysisOriginCount = 0;

		// 위와 별개로 패킷으로 보낼때 쓴 정보도 초기화 해줘야한다.
		ClearLobbyDropPacketInfo();
	}

	#region Gacha Drop Equip
	float _lastNotStreakAdjustWeight = -1.0f;
	public string GetGachaEquipId()
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false)
			return "";
		bool needRefresh = false;
		float notStreakAdjustWeight = TableDataManager.instance.FindNotStreakAdjustWeight(GetCurrentNotSteakCount());
		if (_lastNotStreakAdjustWeight != notStreakAdjustWeight)
		{
			needRefresh = true;
			_lastNotStreakAdjustWeight = notStreakAdjustWeight;
		}

		if (needRefresh)
		{
			// AdjustWeight 검증
			if (notStreakAdjustWeight > 17.2f)
				CheatingListener.OnDetectCheatTable();

			if (_listRandomDropEquipInfo == null)
				_listRandomDropEquipInfo = new List<RandomDropEquipInfo>();
			_listRandomDropEquipInfo.Clear();

			float sumWeight = 0.0f;
			for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
			{
				float weight = TableDataManager.instance.equipTable.dataArray[i].equipGachaWeight;
				if (weight <= 0.0f)
					continue;

				// equipGachaWeight 검증
				if (EquipData.IsUseLegendKey(TableDataManager.instance.equipTable.dataArray[i]) && weight > 1.0f)
					CheatingListener.OnDetectCheatTable();

				if (EquipData.IsUseNotStreakGacha(TableDataManager.instance.equipTable.dataArray[i]))
				{
					weight *= EquipData.GetLegendAdjustWeightByCount();
					weight *= notStreakAdjustWeight;
				}

				sumWeight += weight;
				RandomDropEquipInfo newInfo = new RandomDropEquipInfo();
				newInfo.equipTableData = TableDataManager.instance.equipTable.dataArray[i];
				newInfo.sumWeight = sumWeight;
				_listRandomDropEquipInfo.Add(newInfo);
			}
		}

		if (_listRandomDropEquipInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listRandomDropEquipInfo[_listRandomDropEquipInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomDropEquipInfo.Count; ++i)
		{
			if (random <= _listRandomDropEquipInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";

		// 전설이 나오지 않으면 바로 누적시켜놔야 다음번 드랍될때 GetCurrentNotSteakCount()값이 달라지면서 체크할 수 있게된다.
		if (EquipData.IsUseLegendKey(_listRandomDropEquipInfo[index].equipTableData) == false)
			++_droppedNotStreakItemCount;
		else
			// 전설이 나오면 서버에 패킷 보내기 전이지만 리셋을 해놔야 다음번 계산할때 가중치 테이블을 초기화 할 수 있다.
			PlayerData.instance.notStreakCount = 0;
		return _listRandomDropEquipInfo[index].equipTableData.equipId;
	}
	int _droppedNotStreakItemCount = 0;
	public int GetCurrentNotSteakCount()
	{
		// 임시변수를 만들어서 계산하다가 서버에서 리턴받을때 적용해보자
		return PlayerData.instance.notStreakCount + _droppedNotStreakItemCount;
	}

	// not streak에 영향주지 않는 전설 뽑기다.
	// 우편 보상에 단독 등급 뽑기가 추가되면서 인자로 grade를 전달하기로 한다. -1일땐 전설.
	List<RandomDropEquipInfo> _listRandomDropLegendEquipInfo = null;
	public string GetGachaEquipIdByGrade(int grade = -1)
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false)
			return "";

		if (_listRandomDropLegendEquipInfo == null)
			_listRandomDropLegendEquipInfo = new List<RandomDropEquipInfo>();
		_listRandomDropLegendEquipInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.equipTable.dataArray[i].equipGachaWeight;
			if (weight <= 0.0f)
				continue;

			if (grade == -1)
			{
				// 전설뽑기니 나머지 제외
				if (EquipData.IsUseLegendKey(TableDataManager.instance.equipTable.dataArray[i]) == false)
					continue;

				// equipGachaWeight 검증
				if (weight > 1.0f)
					CheatingListener.OnDetectCheatTable();
			}
			else
			{
				if (grade == 1 || grade == 2 || grade == 3)
				{
					if (TableDataManager.instance.equipTable.dataArray[i].grade != grade)
						continue;
				}
				else
					continue;
			}

			sumWeight += weight;
			RandomDropEquipInfo newInfo = new RandomDropEquipInfo();
			newInfo.equipTableData = TableDataManager.instance.equipTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listRandomDropLegendEquipInfo.Add(newInfo);
		}
		if (_listRandomDropLegendEquipInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listRandomDropLegendEquipInfo[_listRandomDropLegendEquipInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomDropLegendEquipInfo.Count; ++i)
		{
			if (random <= _listRandomDropLegendEquipInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listRandomDropLegendEquipInfo[index].equipTableData.equipId;
	}

	List<RandomDropEquipInfo> _listRandomDropEquipInfoByType = null;
	public string GetGachaEquipIdByType(int exceptType)
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);

		// NodeWar 드랍은 lobby드랍처럼 처리해줘야한다.
		if (lobby == false && BattleManager.instance != null && BattleManager.instance.IsNodeWar())
			lobby = true;
		if (lobby == false)
			return "";

		if (_listRandomDropEquipInfoByType == null)
			_listRandomDropEquipInfoByType = new List<RandomDropEquipInfo>();
		_listRandomDropEquipInfoByType.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.equipTable.dataArray[i].equipGachaWeight;
			if (weight <= 0.0f)
				continue;

			// 인자로 들어오는 값은 제외될 타입이다.
			if (TableDataManager.instance.equipTable.dataArray[i].equipType == exceptType)
				continue;

			sumWeight += weight;
			RandomDropEquipInfo newInfo = new RandomDropEquipInfo();
			newInfo.equipTableData = TableDataManager.instance.equipTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listRandomDropEquipInfoByType.Add(newInfo);
		}
		if (_listRandomDropEquipInfoByType.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listRandomDropEquipInfoByType[_listRandomDropEquipInfoByType.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomDropEquipInfoByType.Count; ++i)
		{
			if (random <= _listRandomDropEquipInfoByType[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listRandomDropEquipInfoByType[index].equipTableData.equipId;
	}

	List<RandomDropEquipInfo> _listRandomInvasionEquipInfoByType = null;
	public string GetInvasionEquipIdByType(params int[] typeList)
	{
		// Invasion 전용 드랍이라서 Invasion 만 체크.
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false && BattleManager.instance != null && BattleManager.instance.IsInvasion()) { }
		else return "";

		if (_listRandomInvasionEquipInfoByType == null)
			_listRandomInvasionEquipInfoByType = new List<RandomDropEquipInfo>();
		_listRandomInvasionEquipInfoByType.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.equipTable.dataArray.Length; ++i)
		{
			// invasion은 gachaWeight를 사용
			float weight = TableDataManager.instance.equipTable.dataArray[i].equipGachaWeight;
			if (weight <= 0.0f)
				continue;

			// Invasion 보상에서는 전설 아이템이 나오지 않는다.
			if (EquipData.IsUseLegendKey(TableDataManager.instance.equipTable.dataArray[i]))
				continue;

			// 인자로 들어오는 값이 포함되는 타입이다.
			bool find = false;
			for (int j = 0; j < typeList.Length; ++j)
			{
				if (TableDataManager.instance.equipTable.dataArray[i].equipType == typeList[j])
				{
					find = true;
					break;
				}
			}
			if (find == false)
				continue;

			sumWeight += weight;
			RandomDropEquipInfo newInfo = new RandomDropEquipInfo();
			newInfo.equipTableData = TableDataManager.instance.equipTable.dataArray[i];
			newInfo.sumWeight = sumWeight;
			_listRandomInvasionEquipInfoByType.Add(newInfo);
		}
		if (_listRandomInvasionEquipInfoByType.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listRandomInvasionEquipInfoByType[_listRandomInvasionEquipInfoByType.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomInvasionEquipInfoByType.Count; ++i)
		{
			if (random <= _listRandomInvasionEquipInfoByType[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		return _listRandomInvasionEquipInfoByType[index].equipTableData.equipId;
	}
	#endregion



	#region Gacha Character
	class RandomGachaActorInfo
	{
		public string actorId;
		public float sumWeight;
	}
	List<RandomGachaActorInfo> _listRandomGachaActorInfo = null;
	public string GetGachaCharacterId(bool originDrop, bool characterBoxDrop, bool questCharacterBoxDrop, bool analysisDrop, int grade = -1, bool ignoreCheckLobby = false)
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		if (lobby == false && ignoreCheckLobby == false)
			return "";

		if (_listRandomGachaActorInfo == null)
			_listRandomGachaActorInfo = new List<RandomGachaActorInfo>();
		_listRandomGachaActorInfo.Clear();

		int fixedCharacterGroupIndex = -1;
		bool isCompleteFixedCharacterGroup = IsCompleteFixedCharacterGroup(ref fixedCharacterGroupIndex);
		float sumWeight = 0.0f;
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			float weight = TableDataManager.instance.actorTable.dataArray[i].charGachaWeight;
			if (weight <= 0.0f)
				continue;
			if (MercenaryData.IsMercenaryActor(TableDataManager.instance.actorTable.dataArray[i].actorId))
				continue;

			bool ignoreFixedCharacterGroup = false;
			if (grade == 0 || grade == 1)
			{
				if (TableDataManager.instance.actorTable.dataArray[i].grade != grade)
					continue;
				// grade 지정해서 뽑을땐 trp가 나오면 안된다.
				if (PlayerData.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId))
					continue;

				// grade 지정해서 뽑을땐 FixedCharacter 그룹 체크를 건너뛰어야한다.
				ignoreFixedCharacterGroup = true;
			}

			// 초기 필수캐릭 얻었는지 체크 후 얻었다면 원래대로 진행
			bool useAdjustWeight = false;
			if (isCompleteFixedCharacterGroup || ignoreFixedCharacterGroup)
			{
				// 획득가능한지 물어봐야한다.
				if (GetableOrigin(TableDataManager.instance.actorTable.dataArray[i].actorId, ref useAdjustWeight) == false)
					continue;

				// analysisDrop 예외처리.
				if (useAdjustWeight == false && analysisDrop)
					useAdjustWeight = true;
			}
			else
			{
				// 얻지 못했다면 필수 캐릭터 리스트인지 확인해서 이 캐릭들만 후보 리스트에 넣어야한다.
				bool getable = false;
				if (IsFixedCharacterIncompleteGroup(fixedCharacterGroupIndex, TableDataManager.instance.actorTable.dataArray[i].actorId))
					getable = true;
				// FixedCharTable 검증
				if (getable && TableDataManager.instance.actorTable.dataArray[i].grade > 0)
					CheatingListener.OnDetectCheatTable();
				// 필수캐릭이 아니더라도 이미 인벤에 들어있는 캐릭터라면(ganfaul, keepseries) 초월재료를 얻을 수 있게 해줘야한다.
				if (getable == false && PlayerData.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId) && GetableOrigin(TableDataManager.instance.actorTable.dataArray[i].actorId, ref useAdjustWeight))
					getable = true;
				if (getable == false)
					continue;
			}

			// 초월은 초반 굴림에선 나오지 않게 한다.
			if ((originDrop && PlayerData.instance.originOpenCount <= 10) || (characterBoxDrop && PlayerData.instance.characterBoxOpenCount <= 8) || (questCharacterBoxDrop && PlayerData.instance.questCharacterBoxOpenCount <= 8))
			{
				if (PlayerData.instance.ContainsActor(TableDataManager.instance.actorTable.dataArray[i].actorId))
					continue;
			}

			// charGachaWeight 검증
			if (CharacterData.IsUseLegendWeight(TableDataManager.instance.actorTable.dataArray[i]) && weight > 1.0f)
				CheatingListener.OnDetectCheatTable();

			float adjustWeight = (useAdjustWeight ? (weight * TableDataManager.instance.actorTable.dataArray[i].noHaveTimes) : weight);

			if (CharacterData.IsUseLegendWeight(TableDataManager.instance.actorTable.dataArray[i]))
			{
				adjustWeight *= DropManager.GetGradeAdjust(TableDataManager.instance.actorTable.dataArray[i]);

				// 전설 최대 가중치 합 보정.
				adjustWeight *= CharacterData.GetLegendAdjustWeightByCount();

				if (characterBoxDrop)
				{
					// 드랍 안될때 보너스 적용.
					float notStreakLegendAdjustWeight = TableDataManager.instance.FindNotLegendCharAdjustWeight(DropManager.instance.GetCurrentNotStreakLegendCharCount());
					// NotLegendCharTable Adjust Weight 검증
					if (notStreakLegendAdjustWeight > 9.99f)
						CheatingListener.OnDetectCheatTable();
					adjustWeight *= notStreakLegendAdjustWeight;
				}
			}
			else
			{
				// 전설이 아닐때는 미보유인지 아닌지를 구분해서 특별한 보정처리를 한다.
				if (useAdjustWeight)
				{
					// 미보유
					if (originDrop) adjustWeight *= 3.0f;
					else if (characterBoxDrop || questCharacterBoxDrop) adjustWeight *= 1.5f;
					else if (analysisDrop)
					{
						if (TableDataManager.instance.actorTable.dataArray[i].grade == 0)
							adjustWeight *= 2.5f;
						else if (TableDataManager.instance.actorTable.dataArray[i].grade == 1)
							adjustWeight *= 1.5f;
					}
					adjustWeight += TableDataManager.instance.actorTable.dataArray[i].charGachaWeight * (DropManager.GetGradeAdjust(TableDataManager.instance.actorTable.dataArray[i]) - 1.0f);
				}
				else
					adjustWeight *= DropManager.GetGradeAdjust(TableDataManager.instance.actorTable.dataArray[i]);
			}

			sumWeight += adjustWeight;
			RandomGachaActorInfo newInfo = new RandomGachaActorInfo();
			newInfo.actorId = TableDataManager.instance.actorTable.dataArray[i].actorId;
			newInfo.sumWeight = sumWeight;
			_listRandomGachaActorInfo.Add(newInfo);
		}

		if (_listRandomGachaActorInfo.Count == 0)
			return "";

		int index = -1;
		float random = Random.Range(0.0f, _listRandomGachaActorInfo[_listRandomGachaActorInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomGachaActorInfo.Count; ++i)
		{
			if (random <= _listRandomGachaActorInfo[i].sumWeight)
			{
				if (characterBoxDrop)
				{
					// 캐릭터 박스 드랍 굴림이었는데 전설이 안나온다면
					ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(_listRandomGachaActorInfo[i].actorId);
					if (CharacterData.IsUseLegendWeight(actorTableData) == false)
						++droppedNotStreakLegendCharCount;
					else
						// notStreakCount처럼 패킷 받기전에 리셋해준다.
						PlayerData.instance.notStreakLegendCharCount = 0;
				}
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		string result = _listRandomGachaActorInfo[index].actorId;
		_listRandomGachaActorInfo.Clear();
		_listDroppedActorId.Add(result);
		if (analysisDrop)
			++droppedAnalysisOriginCount;
		return result;
	}
	public int droppedNotStreakCharCount { get; set; }
	public int GetCurrentNotSteakCharCount()
	{
		// 임시변수를 만들어서 계산하다가 서버에서 리턴받을때 적용해보자
		return PlayerData.instance.notStreakCharCount + droppedNotStreakCharCount;
	}

	public int droppedNotStreakLegendCharCount { get; set; }
	public int GetCurrentNotStreakLegendCharCount()
	{
		// 위와 마찬가지로 전설용 Streak도 굴리는 중간에 개수 합산하는게 필요하다.
		return PlayerData.instance.notStreakLegendCharCount + droppedNotStreakLegendCharCount;
	}

	#region Analysis Origin Key
	public int droppedAnalysisOriginCount { get; set; }
	#endregion

	List<string> _listDroppedActorId = new List<string>();
	public bool GetableOrigin(string actorId, ref bool useAdjustWeight)
	{
		//if (actorId != "Actor0201")
		//	return false;

		if (MercenaryData.IsMercenaryActor(actorId))
			return false;

		if (_listDroppedActorId != null && _listDroppedActorId.Contains(actorId))
		{
			// 이번 드랍으로 결정된거면 두번 나오지는 않게 한다.
			return false;
		}

		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null && actorId == "Actor2103")
			return false;

		if (characterData == null)
		{
			useAdjustWeight = true;
			return true;
		}

		// 이제 한계돌파랑 초월이랑 상관없어지면서 limitBreak로 판단하지 않는다.
		//if (characterData.needLimitBreak && characterData.limitBreakPoint <= characterData.limitBreakLevel)
		//	return true;
		if (characterData.transcendPoint < CharacterData.GetTranscendPoint(CharacterData.TranscendLevelMax))
			return true;

		return false;
	}

	public static float GetGradeAdjust(ActorTableData actorTableData)
	{
		// 테이블에 있는 해당 등급의 캐릭터 수 / (테이블에 있는 해당 등급의 캐릭터 수 - 해당 등급에서 내가 초월까지 완료해 더이상 얻을 수 없는 캐릭터 수)
		int totalCharacterCountByGrade = DropManager.instance.GetTotalCharacterCountByGrade(actorTableData.grade);
		int cantGetCount = 0;
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			if (TableDataManager.instance.actorTable.dataArray[i].grade != actorTableData.grade)
				continue;
			if (MercenaryData.IsMercenaryActor(TableDataManager.instance.actorTable.dataArray[i].actorId))
				continue;

			bool useAdjustWeight = false;
			if (DropManager.instance.GetableOrigin(TableDataManager.instance.actorTable.dataArray[i].actorId, ref useAdjustWeight) == false)
				cantGetCount += 1;
		}
		if (totalCharacterCountByGrade == cantGetCount)
			return 0.0f;
		return (float)totalCharacterCountByGrade / (float)(totalCharacterCountByGrade - cantGetCount);
	}

	Dictionary<int, int> _dicTotalCharacterCountByGrade = null;
	public int GetTotalCharacterCountByGrade(int grade)
	{
		if (_dicTotalCharacterCountByGrade == null)
			_dicTotalCharacterCountByGrade = new Dictionary<int, int>();

		// 테이블에 있는 해당 등급의 캐릭터 수
		// 고정이기 때문에 캐싱해서 쓴다.
		if (_dicTotalCharacterCountByGrade.ContainsKey(grade))
			return _dicTotalCharacterCountByGrade[grade];

		int count = 0;
		for (int i = 0; i < TableDataManager.instance.actorTable.dataArray.Length; ++i)
		{
			if (MercenaryData.IsMercenaryActor(TableDataManager.instance.actorTable.dataArray[i].actorId))
				continue;

			if (TableDataManager.instance.actorTable.dataArray[i].grade == grade)
				++count;
		}
		_dicTotalCharacterCountByGrade.Add(grade, count);
		return count;
	}

	#region Sub-Region Fixed Character Group
	bool IsCompleteFixedCharacterGroup(ref int index)
	{
		for (int i = 0; i < TableDataManager.instance.fixedCharTable.dataArray.Length; ++i)
		{
			bool contains = false;
			for (int j = 0; j < TableDataManager.instance.fixedCharTable.dataArray[i].actorId.Length; ++j)
			{
				if (PlayerData.instance.ContainsActor(TableDataManager.instance.fixedCharTable.dataArray[i].actorId[j]))
				{
					contains = true;
					break;
				}
				if (_listDroppedActorId != null && _listDroppedActorId.Contains(TableDataManager.instance.fixedCharTable.dataArray[i].actorId[j]))
				{
					// 이번 드랍으로 결정된거면 얻었다고 판단해야한다.
					contains = true;
					break;
				}
			}

			if (contains == false)
			{
				index = i;
				return false;
			}
		}
		return true;
	}

	bool IsFixedCharacterIncompleteGroup(int fixedCharacterGroupIndex, string actorId)
	{
		if (fixedCharacterGroupIndex == -1)
			return false;
		if (fixedCharacterGroupIndex >= TableDataManager.instance.fixedCharTable.dataArray.Length)
			return false;

		string[] actorIdList = TableDataManager.instance.fixedCharTable.dataArray[fixedCharacterGroupIndex].actorId;
		for (int i = 0; i < actorIdList.Length; ++i)
		{
			if (actorIdList[i] == actorId)
				return true;
		}
		return false;
	}
	#endregion

	#endregion

	#region Gacha PowerPoint
	List<string> _listDroppedPowerPointId = new List<string>();
	const float _maxPowerPointRate = 1.5f;
	public string GetGachaPowerPointId(bool originDrop, bool characterBoxDrop, bool questCharacterBoxDrop, int grade = -1, bool ignoreCheckLobby = false)
	{
		bool lobby = (MainSceneBuilder.instance != null && MainSceneBuilder.instance.lobby);
		// Invasion 예외처리.
		if (lobby == false && BattleManager.instance != null)
		{
			if (BattleManager.instance.IsInvasion())
				lobby = true;
		}
		if (lobby == false && ignoreCheckLobby == false)
			return "";

		if (_listRandomGachaActorInfo == null)
			_listRandomGachaActorInfo = new List<RandomGachaActorInfo>();
		_listRandomGachaActorInfo.Clear();

		float maxPp = 0.0f;
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
			maxPp = Mathf.Max(maxPp, PlayerData.instance.listCharacterData[i].pp);

		// 최초에는 pp가 없기때문에 예외처리 해줘야한다.
		if (maxPp == 0.0f) maxPp = 10.0f;

		float baseWeight = maxPp * _maxPowerPointRate;
		float sumWeight = 0.0f;
		for (int i = 0; i < PlayerData.instance.listCharacterData.Count; ++i)
		{
			string actorId = PlayerData.instance.listCharacterData[i].actorId;
			// 원래는 중복해서 나오면 안되지만 캐릭터가 2개밖에 없는 상황에서 5개를 뽑아야한다면 중복을 허용한다.
			if (_listDroppedPowerPointId != null && PlayerData.instance.listCharacterData.Count == _listDroppedPowerPointId.Count)
				_listDroppedPowerPointId.Clear();
			if (_listDroppedPowerPointId != null && _listDroppedPowerPointId.Contains(actorId))
			{
				// 이번 드랍으로 결정된거면 두번 나오지는 않게 한다.
				continue;
			}

			if (grade == 0 || grade == 1)
			{
				ActorTableData actorTableData = TableDataManager.instance.FindActorTableData(actorId);
				if (actorTableData.grade != grade)
					continue;
			}

			// 초반 플레이 예외처리 두번째. 중복해서 뽑는걸 막는 로직때문에 이렇게 그냥 continue 하면 하나만 남은 상태에서도 continue하게 되면서 뽑을게 없어져버린다.
			// 
			const int Actor0201LimitPp = 49;
			if (originDrop || characterBoxDrop || questCharacterBoxDrop)
			{
				int sum = PlayerData.instance.originOpenCount + PlayerData.instance.characterBoxOpenCount + PlayerData.instance.questCharacterBoxOpenCount;
				if (sum <= 6 && actorId == "Actor0201")
				{
					bool needContinue = false;
					if (PlayerData.instance.listCharacterData[i].pp >= Actor0201LimitPp)
						needContinue = true;
					if (needContinue == false)
					{
						// pp는 여러개로 쪼개져서 드랍을 굴리기때문에 굴려둔거중에 이미 개수를 초과한다면 더는 굴리지 않도록 해줘야한다.
						int currentRequestPp = 0;
						for (int j = 0; j < _listCharacterPpRequest.Count; ++j)
						{
							if (_listCharacterPpRequest[j].ChrId == PlayerData.instance.listCharacterData[i].entityKey.Id)
							{
								currentRequestPp = _listCharacterPpRequest[j].pp;
								break;
							}
						}
						if (currentRequestPp >= Actor0201LimitPp)
							needContinue = true;
					}
					if (needContinue)
					{
						// 그래서 간파울 하나 남은 상태에서 continue하는거라 판단될땐 _listDroppedPowerPointId를 초기화 시켜놓고 루프를 다시 돌게해서
						// 간파울을 제외한 나머지 캐릭들 중에 하나가 나오게 한다.
						// 이런 상황이라면 _listRandomGachaActorInfo에 아무것도 들어있지 않을거다. 확인할 겸 검사하자.
						if (PlayerData.instance.listCharacterData.Count == (_listDroppedPowerPointId.Count + 1) && _listRandomGachaActorInfo.Count == 0)
						{
							i = -1;
							_listDroppedPowerPointId.Clear();
						}
						continue;
					}
				}
			}

			float weight = baseWeight - PlayerData.instance.listCharacterData[i].pp;
			// 초반 플레이 예외처리.
			bool newbAdjust = false;
			if (originDrop || questCharacterBoxDrop)
			{
				if (PlayerData.instance.originOpenCount + PlayerData.instance.questCharacterBoxOpenCount <= 3)
					newbAdjust = true;
			}
			if (newbAdjust)
			{
				if (actorId == "Actor1002" || actorId == "Actor2103") { }
				else
					weight *= 0.001f;
			}
			sumWeight += weight;
			RandomGachaActorInfo newInfo = new RandomGachaActorInfo();
			newInfo.actorId = PlayerData.instance.listCharacterData[i].actorId;
			newInfo.sumWeight = sumWeight;
			_listRandomGachaActorInfo.Add(newInfo);
		}

		if (_listRandomGachaActorInfo.Count == 0)
		{
			if (originDrop || characterBoxDrop || questCharacterBoxDrop)
				Debug.LogError("Invalid Gacha PowerPoint. Nothing has been selected.");
			return "";
		}

		int index = -1;
		float random = Random.Range(0.0f, _listRandomGachaActorInfo[_listRandomGachaActorInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listRandomGachaActorInfo.Count; ++i)
		{
			if (random <= _listRandomGachaActorInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return "";
		string result = _listRandomGachaActorInfo[index].actorId;
		_listRandomGachaActorInfo.Clear();
		_listDroppedPowerPointId.Add(result);
		return result;
	}
	#endregion




	#region Lobby Drop Packet
	// 로비 가차 드랍은 따로 모아놔야 패킷 만들때 편하다.
	// 이 아래부터는 패킷 정보들이다.
	// 패킷 보낼때만 잠시 쓰는거라 Obscured안해놔도 될텐데 그냥 해둔다.
	// Drop과 동시에 계산되서 여기에 다 쌓여있게 되니 바로 서버로 보내면 된다.
	ObscuredInt _lobbyDia = 0;
	ObscuredFloat _lobbyGold = 0.0f;
	ObscuredInt _lobbyBalancePp = 0;

	void ClearLobbyDropPacketInfo()
	{
		_lobbyDia = 0;
		_lobbyGold = 0.0f;
		_lobbyBalancePp = 0;
		_listCharacterPpRequest.Clear();
		_listGrantCharacterRequest.Clear();		
		_listCharacterTrpRequest.Clear();
		_listEquipIdRequest.Clear();
	}

	public void AddLobbyDia(int dia)
	{
		_lobbyDia += dia;
	}
	public int GetLobbyDiaAmount()
	{
		return _lobbyDia;
	}

	public void AddLobbyGold(float gold)
	{
		_lobbyGold += gold;
	}
	public int GetLobbyGoldAmount()
	{
		float lobbyGold = _lobbyGold;
		return (int)lobbyGold;
	}

	public class CharacterPpRequest
	{
		public string ChrId;
		public int pp;
		public int add;

		[System.NonSerialized]
		public string actorId;
	}
	List<CharacterPpRequest> _listCharacterPpRequest = new List<CharacterPpRequest>();
	public void AddPowerPoint(string actorId, int amount)
	{
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null)
			return;

		// 캐릭터가 둘밖에 없을때 5인분의 pp를 뽑으려면 같은 캐릭터에 두어개씩 들어오게 된다. 합산해줘야한다.
		bool find = false;
		for (int i = 0; i < _listCharacterPpRequest.Count; ++i)
		{
			if (_listCharacterPpRequest[i].ChrId == characterData.entityKey.Id)
			{
				_listCharacterPpRequest[i].pp += amount;
				_listCharacterPpRequest[i].add += amount;
				find = true;
				break;
			}
		}

		if (find == false)
		{
			// pp는 Add 대신 Set을 쓸거기 때문에 처음 찾아질때 기존의 값에 더하는 형태로 기록해둔다.
			CharacterPpRequest newInfo = new CharacterPpRequest();
			newInfo.actorId = characterData.actorId;
			newInfo.ChrId = characterData.entityKey.Id;
			newInfo.pp = characterData.pp + amount;
			newInfo.add = amount;
			_listCharacterPpRequest.Add(newInfo);
		}
	}
	public List<CharacterPpRequest> GetPowerPointInfo()
	{
		return _listCharacterPpRequest;
	}

	public void AddLobbyBalancePp(int amount)
	{
		_lobbyBalancePp += amount;
	}

	public int GetLobbyBalancePpAmount()
	{
		return _lobbyBalancePp;
	}

	List<string> _listGrantCharacterRequest = new List<string>();
	public class CharacterTrpRequest
	{
		public string ChrId;
		public int trp;

		[System.NonSerialized]
		public string actorId;
	}
	List<CharacterTrpRequest> _listCharacterTrpRequest = new List<CharacterTrpRequest>();
	public void AddOrigin(string actorId)
	{
		CharacterData characterData = PlayerData.instance.GetCharacterData(actorId);
		if (characterData == null)
		{
			if (_listGrantCharacterRequest.Contains(actorId) == false)
				_listGrantCharacterRequest.Add(actorId);
		}
		else
		{
			// 두개 이상 뽑힐리 없으니 기존값 구해와서 1 증가시키면 된다.
			CharacterTrpRequest newInfo = new CharacterTrpRequest();
			newInfo.actorId = characterData.actorId;
			newInfo.ChrId = characterData.entityKey.Id;
			newInfo.trp = characterData.transcendPoint + 1;
			_listCharacterTrpRequest.Add(newInfo);
		}

		// 이 함수에 들어왔다는거 자체가 캐릭터를 뽑고있다는걸 의미하니 연출 끝나고 나올 결과창에서 보여줄 캐릭터를 미리 로딩해두기로 한다.
		// 장비 아이콘의 경우엔 크기가 작기도 하고 그래서 패킷 받는 부분에서 했었는데
		// 캐릭터의 경우엔 보내기 전부터 하기로 한다.
		AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(actorId));
	}
	public List<string> GetGrantCharacterInfo()
	{
		return _listGrantCharacterRequest;
	}
	public List<CharacterTrpRequest> GetTranscendPointInfo()
	{
		return _listCharacterTrpRequest;
	}

	List<ObscuredString> _listEquipIdRequest = new List<ObscuredString>();
	public void AddLobbyDropItemId(string equipId)
	{
		_listEquipIdRequest.Add(equipId);
	}
	public List<ObscuredString> GetLobbyDropItemInfo()
	{
		return _listEquipIdRequest;
	}
	#endregion
	*/
}