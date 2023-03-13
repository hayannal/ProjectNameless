using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;

public class GuideQuestData : MonoBehaviour
{
	public static GuideQuestData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("GuideQuestData")).AddComponent<GuideQuestData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static GuideQuestData _instance = null;

	// QuestData에 쓰던거 그대로 가져와서 더 많이 추가해서 쓴다. Swap같은건 완전 겹치는거라 안쓰기로 한다. 대신 인덱스 밀리면 안되서 지우진 않는다.
	public enum eQuestClearType
	{
		KillMonster = 1,
		KillBossMonster = 2,
		UseSkill = 3,

		EnhancePlayer = 11,		// 서브레벨업 하기
		LevelUpPlayer = 12,		// 특정 레벨업 하기. 세븐데이즈에서도 마찬가지. 페스티벌에선 사용 못한다.
		ClearStage = 13,		// 특정 층을 도달
		Analysis = 14,			// 분석 하기
		SpinChargeAlarm = 15,
		FreeFortuneWheel = 16,
		SpellGacha = 17,
		CharacterGacha = 18,
		EquipGacha = 19,
		UseEnergy = 20,         // 에너지 소모하기

		LevelUpSpellTotal = 21,	// 토탈 스펠레벨 도달하기
		GatherCharacter = 22,   // 동료 n명 모으기
		LevelUpCharacter = 23,	// 동료 한명 아무나 n레벨 도달하기
		GatherPet = 24,			// 펫 n마리 모으기
		GatherPetCount = 25,	// 펫 아무나 하나 n마리 모으기
		GradeUpEquip = 26,		// 임의의 장비 하나 특정 등급 도달하기
		UseTicket = 27,			// 티켓 소모하기
		ClearRushDefense = 28,	// 침공방어 플레이하기
		ClearBossDefense = 29,	// 보스저지 플레이하기
	}

	// 이거 서버에도 둬서 완료 체크할때 수량 체크를 느슨하게 하니 변경된다면 서버에도 등록해야한다.
	public static bool IsUseClientCache(eQuestClearType questClearType)
	{
		switch (questClearType)
		{
			case eQuestClearType.KillMonster:
			case eQuestClearType.UseSkill:
				return true;
		}
		return false;
	}

	// 현재 진행중인 퀘스트.
	public ObscuredInt currentGuideQuestIndex { get; set; }
	public ObscuredInt currentGuideQuestProceedingCount { get; set; }
	public ObscuredInt clientCacheIndex { get; set; }
	public ObscuredInt clientCacheCount { get; set; }

	public int GetProceedingCount()
	{
		int result = currentGuideQuestProceedingCount;
		if (currentGuideQuestIndex == clientCacheIndex)
			result += clientCacheCount;
		return result;
	}

	public void OnRecvGuideQuestData(Dictionary<string, UserDataRecord> userReadOnlyData, List<StatisticValue> playerStatistics)
	{
		// 현재 진행중인 퀘스트의 상태. 동시에 1개만 진행가능하다.
		currentGuideQuestIndex = 0;
		for (int i = 0; i < playerStatistics.Count; ++i)
		{
			if (playerStatistics[i].StatisticName == "guideQuestIndex")
			{
				currentGuideQuestIndex = playerStatistics[i].Value;
				break;
			}
		}

		currentGuideQuestProceedingCount = 0;
		if (userReadOnlyData.ContainsKey("gQstPrcdCnt"))
		{
			int intValue = 0;
			if (int.TryParse(userReadOnlyData["gQstPrcdCnt"].Value, out intValue))
				currentGuideQuestProceedingCount = intValue;
		}

		clientCacheIndex = ObscuredPrefs.GetInt("cachedGuideQuestIndex");
		clientCacheCount = 0;
		if (currentGuideQuestIndex == clientCacheIndex)
			clientCacheCount = ObscuredPrefs.GetInt("cachedGuideQuestCount");

		_lastCachedGuideQuestIndex = -1;

		// 마지막 동기화 타임을 현재보다 과거로 돌려놓으면 최초 1회는 바로 보낼 수 있게 된다.
		_lastSendTime = ServerTime.UtcNow - TimeSpan.FromSeconds(30);
	}

	int _lastCachedGuideQuestIndex = -1;
	GuideQuestTableData _cachedTableData = null;
	public GuideQuestTableData GetCurrentGuideQuestTableData()
	{
		// 자주 호출되는 부분이라서 캐싱을 사용한다.
		if (_lastCachedGuideQuestIndex == currentGuideQuestIndex && _cachedTableData != null)
			return _cachedTableData;

		_lastCachedGuideQuestIndex = currentGuideQuestIndex;
		_cachedTableData = TableDataManager.instance.FindGuideQuestTableData(currentGuideQuestIndex);
		return _cachedTableData;
	}

	public bool IsCompleteQuest()
	{
		GuideQuestTableData guideQuestTableData = GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return false;
		return (GetProceedingCount() >= guideQuestTableData.needCount);
	}

	public void OnQuestEvent(eQuestClearType questClearType, int addValue = 1)
	{
		// 호출은 가이드가 받아서 전달하기로 한다.
		MissionData.instance.OnQuestEvent(questClearType, addValue);
		FestivalData.instance.OnQuestEvent(questClearType, addValue);

		/*
		if (ContentsManager.IsTutorialChapter())
			return;
		*/

		if (currentGuideQuestIndex > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxGuideQuestId"))
			return;

		GuideQuestTableData guideQuestTableData = GetCurrentGuideQuestTableData();
		if (guideQuestTableData == null)
			return;

		// 이미 완료한 상태라면 더 할 필요도 없다.
		if (GetProceedingCount() >= guideQuestTableData.needCount)
		{
			#region Sync Max Count
			// 완료한건데 아직 서버로 못보내고 있는거였다면
			// 마지막 동기화 타임 체크해서 강제로 동기화 시켜둔다.
			if (currentGuideQuestIndex == clientCacheIndex && clientCacheCount > 0)
			{
				if (ServerTime.UtcNow > _lastSendTime + TimeSpan.FromSeconds(15))
				{
					_lastSendTime = ServerTime.UtcNow;

					// UI 갱신은 할필요 없을거다.
					PlayFabApiManager.instance.RequestGuideQuestProceedingCount(currentGuideQuestIndex, clientCacheCount, currentGuideQuestProceedingCount + clientCacheCount, guideQuestTableData.key, () =>
					{
						clientCacheCount = 0;
						ObscuredPrefs.SetInt("cachedGuideQuestCount", clientCacheCount);
					});
				}
			}
			#endregion
			return;
		}

		// 조건들 체크
		if ((eQuestClearType)guideQuestTableData.typeId != questClearType)
			return;

		// 너무 변화량이 빨라서 매번 보내지 않을 항목들은 클라이언트 캐싱을 함께 사용하기로 한다.
		bool ignorePacket = false;
		if (IsUseClientCache(questClearType))
		{
			if (CheckLastSendDelay(questClearType))
				ignorePacket = true;
		}

		if (ignorePacket)
		{
			clientCacheIndex = currentGuideQuestIndex;
			++clientCacheCount;
			ObscuredPrefs.SetInt("cachedGuideQuestIndex", clientCacheIndex);
			ObscuredPrefs.SetInt("cachedGuideQuestCount", clientCacheCount);

			// 패킷 보내는걸 패스할땐 그냥 UI 처리만 해두고 넘어가야한다.
			OnQuestProceedingCount();
		}
		else
		{
			// 나머지 항목들은 바로 보내는게 맞다.
			if (currentGuideQuestIndex == clientCacheIndex)
				addValue += clientCacheCount;

			PlayFabApiManager.instance.RequestGuideQuestProceedingCount(currentGuideQuestIndex, addValue, currentGuideQuestProceedingCount + addValue, guideQuestTableData.key,() =>
			{
				clientCacheCount = 0;
				ObscuredPrefs.SetInt("cachedGuideQuestCount", clientCacheCount);

				OnQuestProceedingCount();
			});
		}
	}

	// 30초에 한번씩만 보내도 문제없지 않을까
	DateTime _lastSendTime;
	bool CheckLastSendDelay(eQuestClearType questClearType)
	{
		// 마지막으로 보낸 시간으로부터 30초가 지나지 않았다면
		if (_lastSendTime + TimeSpan.FromSeconds(30) > ServerTime.UtcNow)
			return true;

		// 지났으면 보내도 된다. 마지막 시간을 기록해둔다.
		_lastSendTime = ServerTime.UtcNow;
		return false;
	}

	void OnQuestProceedingCount()
	{
		GuideQuestInfo.instance.RefreshCountInfo();
		if (IsCompleteQuest())
			GuideQuestInfo.instance.RefreshAlarmObject();
	}
	
	public int CheckNextInitialProceedingCount()
	{
		int nextGuideQuestIndex = currentGuideQuestIndex + 1;
		if (nextGuideQuestIndex > BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxGuideQuestId"))
			return 0;

		GuideQuestTableData nextGuideQuestTableData = TableDataManager.instance.FindGuideQuestTableData(nextGuideQuestIndex);
		if (nextGuideQuestTableData == null)
			return 0;

		// 이미 완료처리 해야할게 있는지 확인
		bool complete = false;
		eQuestClearType questClearType = (eQuestClearType)nextGuideQuestTableData.typeId;
		switch (questClearType)
		{
			case eQuestClearType.EnhancePlayer:
				// 최대 레벨에 막히는거면 어떻게 통과시켜주지? 우선 1개씩만 해서 못찍는 상황을 만들지 않기로 해본다.
				if (PlayerData.instance.playerLevel >= BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxPlayerLevel"))
					complete = true;
				break;
			case eQuestClearType.SpinChargeAlarm:
				if (OptionManager.instance.energyAlarm == 1)
					complete = true;
				break;
		}
		if (complete)
			return nextGuideQuestTableData.needCount;

		// 카운트를 채워서 시작해야할게 있는지 확인
		int nextInitialProceedingCount = GetNextInitialProceedingCount(questClearType);
		return Mathf.Min(nextInitialProceedingCount, nextGuideQuestTableData.needCount);
	}

	public static int GetNextInitialProceedingCount(eQuestClearType questClearType)
	{
		switch (questClearType)
		{
			case eQuestClearType.LevelUpPlayer: return PlayerData.instance.playerLevel;
			case eQuestClearType.ClearStage: return PlayerData.instance.highestClearStage;
			case eQuestClearType.LevelUpSpellTotal: return SpellManager.instance.spellTotalLevel;
			case eQuestClearType.GatherCharacter: return CharacterManager.instance.listCharacterData.Count;
			case eQuestClearType.LevelUpCharacter: return CharacterManager.instance.GetHighestCharacterLevel();
			case eQuestClearType.GatherPet: return PetManager.instance.listPetData.Count;
			case eQuestClearType.GatherPetCount: return PetManager.instance.GetHighestPetCount();
		}
		return 0;
	}
}