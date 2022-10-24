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
		Left,
		Right,

		Amount,
	}

	List<PlayerActor> _listPlayerActor = new List<PlayerActor>();

	public void InitializeTeamMember()
	{
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
		}

		// 서버에 저장되어있는거 보유했는지 확인 후
		string actorId = PlayerData.instance.leftCharacterId;
		if (string.IsNullOrEmpty(actorId) && PlayerData.instance.ContainsActor(actorId))
		{
			SpawnTeamMember(ePosition.Left, actorId);
		}

		actorId = PlayerData.instance.rightCharacterId;
		if (string.IsNullOrEmpty(actorId) && PlayerData.instance.ContainsActor(actorId))
		{
			SpawnTeamMember(ePosition.Right, actorId);
		}
	}

	Dictionary<string, int> _dicPositionInfo = new Dictionary<string, int>();
	public void SpawnTeamMember(ePosition positionType, string memberActorId)
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
			case ePosition.Left: offset = new Vector3(-1.5f, 0.0f, 0.3f); break;
			case ePosition.Right: offset = new Vector3(1.5f, 0.0f, 0.3f); break;
		}
		playerActor.cachedTransform.position = basePosition + offset;
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
			}
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
}