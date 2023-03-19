using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class TeamManager : MonoBehaviour
{
	public static TeamManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("TeamManager")).AddComponent<TeamManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static TeamManager _instance = null;

	public enum ePosition
	{
		Top,
		Mid,
		Bottom,
		Left,
		Right,

		Amount,
	}

	List<PlayerActor> _listPlayerActor = new List<PlayerActor>();

	public void InitializeTeamMember(bool clear)
	{
		// 미션 진입할때 이 리스트를 초기화 하고 n개 이상 Add하는 경우가 있어서 씬 초기화할때는 clear시켜놓고 다시 셋팅하기로 한다.
		if (clear)
		{
			_listPlayerActor.Clear();
			_hideFlag = false;
		}

		if (_listPlayerActor.Count == 0)
		{
			for (int i = 0; i < (int)ePosition.Amount; ++i)
				_listPlayerActor.Add(null);
		}

		for (int i = 0; i < _listPlayerActor.Count; ++i)
		{
			if (_listPlayerActor[i] == null)
				continue;
			_listPlayerActor[i].gameObject.SetActive(false);
			_listPlayerActor[i] = null;
		}

		// 서버에 저장되어있는거 보유했는지 확인 후
		for (int i = 0; i < (int)ePosition.Amount; ++i)
		{
			string actorId = CharacterManager.instance.listTeamPositionId[i];
			if (string.IsNullOrEmpty(actorId) == false && CharacterManager.instance.ContainsActor(actorId))
			{
				#region TeamPass
				if (TeamPositionCanvas.IsApplySlotByPass(i) && CharacterManager.instance.IsTeamPass() == false)
					continue;
				#endregion

				SpawnTeamMember((ePosition)i, actorId);
			}
		}

		#region Pet
		SpawnActivePet();
		#endregion
	}

	Dictionary<string, int> _dicPositionInfo = new Dictionary<string, int>();
	void SpawnTeamMember(ePosition positionType, string memberActorId)
	{
		// 캐릭터 교체는 이 캔버스 담당이다.
		// 액터가 혹시나 미리 만들어져있다면 등록되어있을거니 가져다쓴다.
		PlayerActor playerActor = BattleInstanceManager.instance.GetCachedPlayerActor(memberActorId);
		if (playerActor != null)
		{
			if (playerActor != _listPlayerActor[(int)positionType])
			{
				if (_listPlayerActor[(int)positionType] != null)
					_listPlayerActor[(int)positionType].gameObject.SetActive(false);
				_listPlayerActor[(int)positionType] = playerActor;
				_listPlayerActor[(int)positionType].gameObject.SetActive(true);
			}
			OnFinishLoadedPlayerActor(positionType, playerActor);
		}
		else
		{
			// 위치정보를 Dictionary에 넣어두고 어드레서블 로딩을 시작한다.
			if (_dicPositionInfo.ContainsKey(memberActorId))
				_dicPositionInfo[memberActorId] = (int)positionType;
			else
				_dicPositionInfo.Add(memberActorId, (int)positionType);

			AddressableAssetLoadManager.GetAddressableGameObject(CharacterData.GetAddressByActorId(memberActorId), "", OnLoadedPlayerActor);
		}
	}

	void OnLoadedPlayerActor(GameObject prefab)
	{
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(prefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(prefab);
#endif

		PlayerActor playerActor = newObject.GetComponent<PlayerActor>();
		if (playerActor == null)
			return;
		playerActor.playerAI.useTeamMemberAI = true;
		BattleInstanceManager.instance.OnInitializePlayerActor(playerActor, playerActor.actorId);

		ePosition positionType = ePosition.Left;
		if (_dicPositionInfo.ContainsKey(playerActor.actorId))
			positionType = (ePosition)_dicPositionInfo[playerActor.actorId];

		if (playerActor != _listPlayerActor[(int)positionType])
		{
			if (_listPlayerActor[(int)positionType] != null)
				_listPlayerActor[(int)positionType].gameObject.SetActive(false);
			_listPlayerActor[(int)positionType] = playerActor;
			_listPlayerActor[(int)positionType].gameObject.SetActive(true);
		}
		OnFinishLoadedPlayerActor(positionType, playerActor);
	}

	void OnFinishLoadedPlayerActor(ePosition positionType, PlayerActor playerActor)
	{
		// 위치 설정
		if (BattleInstanceManager.instance.playerActor == null)
			return;
		if (playerActor == null)
			return;

		Vector3 basePosition = BattleInstanceManager.instance.playerActor.cachedTransform.position;
		Vector3 offset = Vector3.zero;
		switch  (positionType)
		{
			case ePosition.Top: offset = new Vector3(1.7f, 0.0f, 7.75f); break;
			case ePosition.Mid: offset = new Vector3(-1.7f, 0.0f, 4.9f); break;
			case ePosition.Bottom: offset = new Vector3(1.7f, 0.0f, 2.22f); break;
			case ePosition.Left: offset = new Vector3(-1.5f, 0.0f, 0.3f); break;
			case ePosition.Right: offset = new Vector3(1.5f, 0.0f, 0.3f); break;
		}
		playerActor.cachedTransform.position = basePosition + offset;
		TailAnimatorUpdater.UpdateAnimator(playerActor.cachedTransform, 15);
	}

	// 위 루틴을 다 따르는게 괜히 무거워서 이미 로딩이 되어있고 씬 이동을 할때는 그냥 껐다가 켜는 형태로만 캐릭터를 옮기기로 한다.
	bool _hideFlag = false;
	public void HideForMoveMap(bool hide)
	{
		if (hide)
		{
			if (_hideFlag)
				return;

			for (int i = 0; i < _listPlayerActor.Count; ++i)
			{
				if (_listPlayerActor[i] == null)
					continue;
				_listPlayerActor[i].gameObject.SetActive(false);

				#region TeamPass
				if (TeamPositionCanvas.IsApplySlotByPass(i) && CharacterManager.instance.IsTeamPass() == false)
					_listPlayerActor[i] = null;
				#endregion
			}

			#region Pet
			if (_petActor != null)
				_petActor.gameObject.SetActive(false);
			#endregion
		}
		else
		{
			// 맵 이동한 곳에 복구
			if (_hideFlag == false)
				return;

			for (int i = 0; i < _listPlayerActor.Count; ++i)
			{
				if (_listPlayerActor[i] == null)
					continue;
				OnFinishLoadedPlayerActor((ePosition)i, _listPlayerActor[i]);
				_listPlayerActor[i].gameObject.SetActive(true);
			}

			#region Pet
			if (_petActor != null)
			{
				OnFinishLoadedPetActor(_petActor);
				_petActor.gameObject.SetActive(true);
			}
			#endregion
		}
		_hideFlag = hide;
	}

	public void InitializeActorStatus()
	{
		for (int i = 0; i < _listPlayerActor.Count; ++i)
		{
			if (_listPlayerActor[i] == null)
				continue;
			_listPlayerActor[i].actorStatus.InitializeActorStatus();
		}
	}

	public void ExecuteAffectorValueTeamMember(eAffectorType affectorType, AffectorValueLevelTableData levelData)
	{
		for (int i = 0; i < _listPlayerActor.Count; ++i)
		{
			if (_listPlayerActor[i] == null)
				continue;
			_listPlayerActor[i].affectorProcessor.ExecuteAffectorValueWithoutTable(affectorType, levelData, _listPlayerActor[i], false);
		}
	}

	#region Mission
	public void ClearTeamPlayerActorForMission()
	{
		_listPlayerActor.Clear();
	}

	public void AddTeamPlayerActorForMission(PlayerActor playerActor)
	{
		// 미션에서도 스펠을 사용해야하는데 ExecuteAffectorValueTeamMember 함수가 제대로 동작하려면
		// 현재 미션에서 사용하는 캐릭터들이 _listPlayerActor 리스트안에 들어있어야한다.
		// 그래서 이렇게 전용함수로 만들어서 클리어하고 외부에서 Add할 수 있게 임시 처리 해둔다.
		_listPlayerActor.Add(playerActor);
	}
	#endregion

	#region Pet
	PetActor _petActor;
	public void SpawnActivePet()
	{
		if (string.IsNullOrEmpty(PetManager.instance.activePetId))
			return;

		// 캐릭터 교체는 이 캔버스 담당이다.
		// 액터가 혹시나 미리 만들어져있다면 등록되어있을거니 가져다쓴다.
		PetActor petActor = BattleInstanceManager.instance.GetCachedPetActor(PetManager.instance.activePetId);
		if (petActor != null)
		{
			if (petActor != _petActor)
			{
				if (_petActor != null)
					_petActor.gameObject.SetActive(false);
				_petActor = petActor;
				_petActor.gameObject.SetActive(true);
			}
			OnFinishLoadedPetActor(petActor);
		}
		else
		{
			AddressableAssetLoadManager.GetAddressableGameObject(PetData.GetAddressByPetId(PetManager.instance.activePetId), "", OnLoadedPetActor);
		}
	}

	void OnLoadedPetActor(GameObject prefab)
	{
#if UNITY_EDITOR
		GameObject newObject = Instantiate<GameObject>(prefab);
		AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
		if (settings.ActivePlayModeDataBuilderIndex == 2)
			ObjectUtil.ReloadShader(newObject);
#else
		GameObject newObject = Instantiate<GameObject>(prefab);
#endif

		PetActor petActor = newObject.GetComponent<PetActor>();
		if (petActor == null)
			return;
		BattleInstanceManager.instance.OnInitializePetActor(petActor, petActor.actorId);

		if (petActor != _petActor)
		{
			if (_petActor != null)
				_petActor.gameObject.SetActive(false);
			_petActor = petActor;
			_petActor.gameObject.SetActive(true);
		}
		OnFinishLoadedPetActor(petActor);
	}

	void OnFinishLoadedPetActor(PetActor petActor)
	{
		// 위치 설정
		if (BattleInstanceManager.instance.playerActor == null)
			return;
		if (petActor == null)
			return;

		petActor.cachedTransform.position = BattleInstanceManager.instance.playerActor.cachedTransform.position + new Vector3(-0.48f, 0.0f, -0.3f);
		petActor.cachedTransform.rotation = Quaternion.Euler(0.0f, 25.0f, 0.0f);
	}
	#endregion
}