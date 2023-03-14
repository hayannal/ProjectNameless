using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MEC;

public class StageGround : MonoBehaviour
{
	public static StageGround instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = Instantiate<GameObject>(StageManager.instance.stageGroundPrefab).GetComponent<StageGround>();
			}
			return _instance;
		}
	}
	static StageGround _instance = null;

	public GameObject monsterSpawnPortalPrefab;
	public GameObject endLinePrefab;
	public GameObject stageFloorInfoCanvasPrefab;

	public void InitializeGround(StageTableData stageTableData, bool repeat, bool missionMode)
	{
		Timing.RunCoroutine(LoadStageProcess(stageTableData, repeat, missionMode));
	}

	GameObject _stagePlanePrefab = null;
	GameObject _stageGroundPrefab = null;
	GameObject _stageWallPrefab = null;
	GameObject _stageEnvPrefab = null;
	void PrepareStage(StageTableData stageTableData)
	{
		AddressableAssetLoadManager.GetAddressableGameObject(stageTableData.plane, "Map", (prefab) =>
		{
			_stagePlanePrefab = prefab;
		});

		AddressableAssetLoadManager.GetAddressableGameObject(stageTableData.ground, "Map", (prefab) =>
		{
			_stageGroundPrefab = prefab;
		});

		AddressableAssetLoadManager.GetAddressableGameObject(stageTableData.wall, "Map", (prefab) =>
		{
			_stageWallPrefab = prefab;
		});

		string environmentSetting = stageTableData.environmentSetting[Random.Range(0, stageTableData.environmentSetting.Length)];
		AddressableAssetLoadManager.GetAddressableGameObject(environmentSetting, "Map", (prefab) =>
		{
			_stageEnvPrefab = prefab;
		});
	}

	GameObject _currentPlaneObject;
	GameObject _currentGroundObject;
	GameObject _currentWallObject;
	GameObject _currentEnvironmentSettingObject;
	GameObject _monsterSpawnPortalObject;
	GameObject _endLineObject;
	public Vector3 endLinePosition { get; set; }
	void InstantiateMap(StageTableData stageTableData, bool repeat, bool missionMode)
	{
		Debug.LogWarning("1111");

		if (_currentPlaneObject != null)
			_currentPlaneObject.SetActive(false);
		_currentPlaneObject = BattleInstanceManager.instance.GetCachedObject(_stagePlanePrefab, StageManager.instance.GetSafeWorldOffset(), Quaternion.identity);
		BattleInstanceManager.instance.planeCollider = _currentPlaneObject.GetComponent<Collider>();

		Debug.LogWarning("2222");

		if (_currentGroundObject != null)
			_currentGroundObject.SetActive(false);
		_currentGroundObject = BattleInstanceManager.instance.GetCachedObject(_stageGroundPrefab, StageManager.instance.GetSafeWorldOffset(), Quaternion.identity);

		Debug.LogWarning("3333");

		if (_currentWallObject != null)
			_currentWallObject.SetActive(false);
		_currentWallObject = BattleInstanceManager.instance.GetCachedObject(_stageWallPrefab, StageManager.instance.GetSafeWorldOffset(), Quaternion.identity);

		Debug.LogWarning("4444");

		if (_currentEnvironmentSettingObject != null)
			_currentEnvironmentSettingObject.SetActive(false);
		_currentEnvironmentSettingObject = BattleInstanceManager.instance.GetCachedObject(_stageEnvPrefab, null);

		Debug.LogWarning("5555");

		if (_monsterSpawnPortalObject != null)
			_monsterSpawnPortalObject.SetActive(false);
		if (missionMode == false)
			_monsterSpawnPortalObject = BattleInstanceManager.instance.GetCachedObject(monsterSpawnPortalPrefab, new Vector3(stageTableData.monsterSpawnx, 0.0f, stageTableData.monsterSpawnz) + StageManager.instance.GetSafeWorldOffset(), Quaternion.identity);

		Debug.LogWarning("6666");

		endLinePosition = new Vector3(stageTableData.redLinex, 0.0f, stageTableData.redLinez);
		if (_endLineObject != null)
			_endLineObject.SetActive(false);
		if (repeat == false)
			_endLineObject = BattleInstanceManager.instance.GetCachedObject(endLinePrefab, new Vector3(stageTableData.redLinex, 0.0f, stageTableData.redLinez) + StageManager.instance.GetSafeWorldOffset(), Quaternion.identity);

		Debug.LogWarning("7777");

		// create callback
		if (StageManager.instance != null)
			StageManager.instance.OnInstantiateMap(stageTableData, missionMode);
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.mainSceneBuilding)
			MainSceneBuilder.instance.waitSpawnFlag = true;

		Debug.LogWarning("8888");
	}

	public bool IsShowEndLineObject()
	{
		if (_endLineObject != null && _endLineObject.activeSelf)
			return true;
		return false;
	}

	bool _processing = false;
	public bool processing { get { return _processing; } }
	IEnumerator<float> LoadStageProcess(StageTableData stageTableData, bool repeat, bool missionMode)
	{
		if (_processing)
			yield break;
		_processing = true;

		_stagePlanePrefab = null;
		_stageGroundPrefab = null;
		_stageWallPrefab = null;
		_stageEnvPrefab = null;
		PrepareStage(stageTableData);

		while (_stagePlanePrefab == null || _stageGroundPrefab == null || _stageWallPrefab == null || _stageEnvPrefab == null)
			yield return Timing.WaitForOneFrame;

		InstantiateMap(stageTableData, repeat, missionMode);

		// 미션에선 할필요 없다.
		if (missionMode == false)
			ShowStageFloorInfoCanvas(true);

		_processing = false;
	}

	public void FinalizeGround()
	{
		if (_currentPlaneObject != null)
			_currentPlaneObject.SetActive(false);
		if (_currentGroundObject != null)
			_currentGroundObject.SetActive(false);
		if (_currentWallObject != null)
			_currentWallObject.SetActive(false);
		if (_currentEnvironmentSettingObject != null)
			_currentEnvironmentSettingObject.SetActive(false);
		if (_monsterSpawnPortalObject != null)
			_monsterSpawnPortalObject.SetActive(false);
		if (_endLineObject != null)
			_endLineObject.SetActive(false);

		ShowStageFloorInfoCanvas(false);
	}


	#region World Canvas
	void ShowStageFloorInfoCanvas(bool show)
	{
		if (show)
		{
			if (StageFloorInfoCanvas.instance == null)
				Instantiate<GameObject>(stageFloorInfoCanvasPrefab, null);
			else
				StageFloorInfoCanvas.instance.gameObject.SetActive(true);

			StageFloorInfoCanvas.instance.cachedTransform.position = StageManager.instance.GetSafeWorldOffset();
			StageFloorInfoCanvas.instance.RefreshStageInfo(StageManager.instance.currentFloor, StageManager.instance.repeatMode);
			StageFloorInfoCanvas.instance.RefreshCombatPower();
		}
		else
		{
			if (StageFloorInfoCanvas.instance != null)
				StageFloorInfoCanvas.instance.gameObject.SetActive(false);
		}
	}
	#endregion


	#region For Canvas EnvironmentSetting
	public GameObject currentEnvironmentSettingObjectForCanvas { get; set; }
	public void OnEnableEnvironmentSetting(GameObject newObject)
	{
		// 필드로 쓰려는게 올땐 아무것도 할 필요 없다.
		if (_currentEnvironmentSettingObject == newObject)
			return;
		// 혹은 필드에서 교체시 _currentEnvironmentSettingObject 이 null로 셋팅하고 교체하기 때문에 이때도 그냥 리턴한다.
		if (_currentEnvironmentSettingObject == null)
			return;

		// 캔버스가 독자적으로 가지고 있는거라면 현재 지형이 사용하는 환경셋팅과 분명 다를거다.
		// 이때만 임시 변수에 등록하면 된다.
		// 각각의 연출 캔버스-환경셋팅을 가지고 있는 캔버스-들이 이전 오브젝트를 기억하는 구조라 여기서는 마지막꺼 하나만 기억해두면 된다.
		currentEnvironmentSettingObjectForCanvas = newObject;
	}

	public GameObject DisableCurrentEnvironmentSetting()
	{
		// 시공간에서 캐릭터 창을 열때처럼 로비로 돌아가는게 아니라 별도의 환경셋팅을 사용하는 창끼리 넘어다닐때를 위해 이렇게 체크한다.
		if (currentEnvironmentSettingObjectForCanvas != null && currentEnvironmentSettingObjectForCanvas.activeInHierarchy)
		{
			currentEnvironmentSettingObjectForCanvas.SetActive(false);
			return currentEnvironmentSettingObjectForCanvas;
		}

		// 이게 아니라면 로비에 있는 메인 환경셋팅을 끄면 될거다.
		// 한번에 하나의 환경셋팅만 켜있을거기 때문에 위에 있는 임시값 아니면 이 아래있는 진짜 월드값 둘중 하나다.
		if (_currentEnvironmentSettingObject != null)
			_currentEnvironmentSettingObject.SetActive(false);
		return _currentEnvironmentSettingObject;
	}
	#endregion
}