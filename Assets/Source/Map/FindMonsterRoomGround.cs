using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class FindMonsterRoomGround : MonoBehaviour
{
	public static FindMonsterRoomGround instance;

	public GameObject monsterPrefab;
	public GameObject transportEffectPrefab;
	public GameObject attackEffectPrefab;
	public GameObject goldEffectPrefab;

	public Canvas worldCanvas;
	public Transform[] worldButtonTransformList;

	void Awake()
	{
		instance = this;
	}

	Vector3[] _defaultOpenTransformRotation;
	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();
	}

	void OnEnable()
	{
		Timing.RunCoroutine(PickProcess());
	}

	void OnDisable()
	{
		if (_centerObject != null)
			_centerObject.SetActive(false);

		for (int i = 0; i < _listRandomObject.Count; ++i)
		{
			if (_listRandomObject[i] != null)
				_listRandomObject[i].SetActive(false);
		}
	}

	bool _waitSelect = false;
	GameObject _centerObject;
	List<GameObject> _listRandomObject = new List<GameObject>();
	List<int> _listCreateIndex = new List<int>();
	IEnumerator<float> PickProcess()
	{
		_waitSelect = false;

		Vector3 rootOffset = FindMonsterRoomCanvas.instance.rootOffsetPosition;

		// 최초 진입대기
		yield return Timing.WaitForSeconds(0.05f);

		// 가운데 미리 만들어두고
		_centerObject = BattleInstanceManager.instance.GetCachedObject(monsterPrefab, rootOffset + Vector3.zero, Quaternion.Euler(0.0f, 180.0f, 0.0f), cachedTransform);

		// 잠깐 대기 후
		yield return Timing.WaitForSeconds(0.3f);

		// 펑
		_centerObject.SetActive(false);
		BattleInstanceManager.instance.GetCachedObject(transportEffectPrefab, rootOffset + Vector3.up, Quaternion.identity, cachedTransform);
		yield return Timing.WaitForSeconds(0.8f);

		// 랜덤 순차로 위치도 랜덤이고 사분면도 랜덤이다.
		if (_listCreateIndex.Count == 0)
		{
			for (int i = 0; i < worldButtonTransformList.Length; ++i)
			{
				_listCreateIndex.Add(i);
				_listRandomObject.Add(null);
			}
		}
		ObjectUtil.Shuffle<int>(_listCreateIndex);
		for (int i = 0; i < _listCreateIndex.Count; ++i)
		{
			Vector3 randomPosition = GetRandomPosition(_listCreateIndex[i]);
			_listRandomObject[i] = BattleInstanceManager.instance.GetCachedObject(monsterPrefab, rootOffset + randomPosition, Quaternion.Euler(0.0f, Random.Range(150, 210.0f), 0.0f), cachedTransform);
			BattleInstanceManager.instance.GetCachedObject(transportEffectPrefab, rootOffset + randomPosition + Vector3.up, Quaternion.identity, cachedTransform);
			yield return Timing.WaitForSeconds(0.2f);
		}

		for (int i = 0; i < worldButtonTransformList.Length; ++i)
			worldButtonTransformList[i].position = new Vector3(_listRandomObject[i].transform.position.x, worldButtonTransformList[i].position.y, _listRandomObject[i].transform.position.z);

		_waitSelect = true;
	}

	Vector3 GetRandomPosition(int i)
	{
		Vector3 randomPosition = Vector3.zero;
		if (i == 0)
		{
			randomPosition.x = Random.Range(-3.0f, -0.75f);
			randomPosition.z = Random.Range(0.75f, 5.0f);
		}
		else if (i == 1)
		{
			randomPosition.x = Random.Range(0.75f, 3.0f);
			randomPosition.z = Random.Range(0.75f, 5.0f);
		}
		else if (i == 2)
		{
			randomPosition.x = Random.Range(-3.0f, -0.75f);
			randomPosition.z = Random.Range(-5.0f, -0.75f);
		}
		else if (i == 3)
		{
			randomPosition.x = Random.Range(0.75f, 3.0f);
			randomPosition.z = Random.Range(-5.0f, -0.75f);
		}
		return randomPosition;
	}

	public void OnClickBox1()
	{
		OnClickBox(0);
	}

	public void OnClickBox2()
	{
		OnClickBox(1);
	}

	public void OnClickBox3()
	{
		OnClickBox(2);
	}

	public void OnClickBox4()
	{
		OnClickBox(3);
	}

	void OnClickBox(int index)
	{
		if (_waitSelect == false)
			return;

		Debug.LogFormat("World Canvas Button Input : {0}", index);

		// 확률은 반반이라고 한다.
		// 클릭과 동시에 공격 이펙트 하나를 플레이 하고
		Vector3 targetPosition = worldButtonTransformList[index].position;
		targetPosition.y = 0.0f;
		BattleInstanceManager.instance.GetCachedObject(attackEffectPrefab, targetPosition, Quaternion.identity, cachedTransform);

		StageBetTableData stageBetTableData = TableDataManager.instance.FindStageBetTableData(PlayerData.instance.currentRewardStage);
		if (stageBetTableData == null)
		{
			Debug.LogErrorFormat("Not found StageBetTable! currentHighest = {0} / selected = {1}", PlayerData.instance.highestClearStage, PlayerData.instance.selectedStage);
			return;
		}

		Timing.RunCoroutine(ReactionProcess(index, stageBetTableData));
	}

	IEnumerator<float> ReactionProcess(int index, StageBetTableData stageBetTableData)
	{
		yield return Timing.WaitForSeconds(0.15f);

		// 성공이면 다이모션으로 넘어가면 되려나
		bool success = (Random.value > 0.5f);
		if (success)
		{
			_listRandomObject[index].GetComponentInChildren<Animator>().Play("Die");
			for (int i = 0; i < _listRandomObject.Count; ++i)
			{
				if (i == index)
				{
					BattleInstanceManager.instance.GetCachedObject(goldEffectPrefab, _listRandomObject[i].transform.position + Vector3.up, goldEffectPrefab.transform.rotation, cachedTransform);
					continue;
				}

				_listRandomObject[i].SetActive(false);
				BattleInstanceManager.instance.GetCachedObject(transportEffectPrefab, _listRandomObject[i].transform.position + Vector3.up, Quaternion.identity, cachedTransform);
			}

			// 성공에 대한 메세지 처리?
			ToastNumberCanvas.instance.ShowToast("SUCCESS!", 2.0f);
			FindMonsterRoomCanvas.instance.ShowSuccess(true);
			FindMonsterRoomCanvas.instance.SetStoleValue(stageBetTableData.goblinSuccess);
		}
		else
		{
			// 실패면 그냥 펑 하고 사라지면 된다.
			_listRandomObject[index].SetActive(false);
			BattleInstanceManager.instance.GetCachedObject(transportEffectPrefab, _listRandomObject[index].transform.position + Vector3.up, Quaternion.identity, cachedTransform);

			// 실패에 대한 메세지 처리?
			ToastNumberCanvas.instance.ShowToast("MISS!", 2.0f);
			FindMonsterRoomCanvas.instance.ShowSuccess(false);
			FindMonsterRoomCanvas.instance.SetStoleValue(stageBetTableData.goblinFailure);
		}

		_waitSelect = false;

		int betRate = GachaInfoCanvas.instance.GetBetRate();

		// 3번째 클릭하자마자 패킷을 보내서 결과 제대로 저장되면
		_resultValue = success ? stageBetTableData.goblinSuccess * betRate : stageBetTableData.goblinFailure * betRate;
		PlayFabApiManager.instance.RequestEndBettingRoom(_resultValue, () =>
		{
			Timing.RunCoroutine(ReturnProcess());
		});
	}

	ObscuredInt _resultValue;
	IEnumerator<float> ReturnProcess()
	{
		int betRate = GachaInfoCanvas.instance.GetBetRate();
		if (betRate == 1)
		{
			// 마지막 이펙트가 나올 타이밍 정도는 기다려준다.
			yield return Timing.WaitForSeconds(2.0f);
		}
		else
		{
			yield return Timing.WaitForSeconds(1.5f);
			FindMonsterRoomCanvas.instance.PunchAnimateWinText();
			yield return Timing.WaitForSeconds(0.8f);

			FindMonsterRoomCanvas.instance.ScaleZeroWinText();
			FindMonsterRoomCanvas.instance.SetStoleValue(_resultValue);
			yield return Timing.WaitForSeconds(1.5f);
		}

		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		FindMonsterRoomCanvas.instance.gameObject.SetActive(false);

		yield return Timing.WaitForSeconds(0.1f);

		// 가차 캔버스로 돌아가야한다.
		GachaCanvas.instance.gameObject.SetActive(true);

		// 
		FadeCanvas.instance.FadeIn(0.5f);

		yield return Timing.WaitForSeconds(0.3f);

		// FindMonster에서는 GoldBox 보상을 바꾸지 않는다.
		// 그렇지만 마지막 1턴 남겨두고 Find Monster Room 들어왔을땐 되돌아오면서 바뀌어야하니까
		// 호출을 건너뛸 순 없다.
		GachaInfoCanvas.instance.OnPostProcess();
	}





	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}