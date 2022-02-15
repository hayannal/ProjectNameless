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

	public void InitializeGround(StageTableData stageTableData, bool repeat)
	{
		Timing.RunCoroutine(LoadStageProcess(stageTableData, repeat));
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
	void InstantiateMap(StageTableData stageTableData, bool repeat)
	{
		if (_currentPlaneObject != null)
			_currentPlaneObject.SetActive(false);
		_currentPlaneObject = BattleInstanceManager.instance.GetCachedObject(_stagePlanePrefab, StageManager.instance.GetSafeWorldOffset(), Quaternion.identity);
		BattleInstanceManager.instance.planeCollider = _currentPlaneObject.GetComponent<Collider>();

		if (_currentGroundObject != null)
			_currentGroundObject.SetActive(false);
		_currentGroundObject = BattleInstanceManager.instance.GetCachedObject(_stageGroundPrefab, StageManager.instance.GetSafeWorldOffset(), Quaternion.identity);

		if (_currentWallObject != null)
			_currentWallObject.SetActive(false);
		_currentWallObject = BattleInstanceManager.instance.GetCachedObject(_stageWallPrefab, StageManager.instance.GetSafeWorldOffset(), Quaternion.identity);

		if (_currentEnvironmentSettingObject != null)
			_currentEnvironmentSettingObject.SetActive(false);
		_currentEnvironmentSettingObject = BattleInstanceManager.instance.GetCachedObject(_stageEnvPrefab, null);

		if (_monsterSpawnPortalObject != null)
			_monsterSpawnPortalObject.SetActive(false);
		_monsterSpawnPortalObject = BattleInstanceManager.instance.GetCachedObject(monsterSpawnPortalPrefab, new Vector3(stageTableData.monsterSpawnx, 0.0f, stageTableData.monsterSpawnz) + StageManager.instance.GetSafeWorldOffset(), Quaternion.identity);

		if (_endLineObject != null)
			_endLineObject.SetActive(false);
		if (repeat == false)
			_endLineObject = BattleInstanceManager.instance.GetCachedObject(endLinePrefab, new Vector3(stageTableData.redLinex, 0.0f, stageTableData.redLinez) + StageManager.instance.GetSafeWorldOffset(), Quaternion.identity);

		// create callback
		if (StageManager.instance != null)
			StageManager.instance.OnInstantiateMap(stageTableData);
		if (MainSceneBuilder.instance != null && MainSceneBuilder.instance.mainSceneBuilding)
			MainSceneBuilder.instance.waitSpawnFlag = true;
	}

	bool _processing = false;
	public bool processing { get { return _processing; } }
	IEnumerator<float> LoadStageProcess(StageTableData stageTableData, bool repeat)
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

		InstantiateMap(stageTableData, repeat);

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
	}
}