using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using PlayFab.ClientModels;
using MEC;
using CodeStage.AntiCheat.ObscuredTypes;
#if UNITY_EDITOR
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

public class PetSearchGround : MonoBehaviour
{
	public static PetSearchGround instance;

	public GameObject dropLevelPackPrefab;
	public GameObject appearEffectPrefab;

	public const float CameraRotateRange = 2.7f;

	public Transform playerPositionTransform;
	public Transform enemy1PositionTransform;
	public Transform enemy2PositionTransform;
	public Transform questionPositionTransform;
	public Transform extraGain1PositionTransform;
	public Transform extraGain2PositionTransform;
	public Transform extraGain3PositionTransform;

	public Canvas worldCanvas;
	public Transform firstGetButtonTransform;
	public PetBattleInfo firstGetPetBattleInfo;
	public PetBattleInfo enemy1PetBattleInfo;
	public PetBattleInfo enemy2PetBattleInfo;
	public PetBattleInfo extraGain1PetBattleInfo;
	public PetBattleInfo extraGain2PetBattleInfo;
	public PetBattleInfo extraGain3PetBattleInfo;

	public ParticleSystem[] heartParticleSystemList;

	public GameObject[] enemy1CaptureEffectObjectList;
	public GameObject[] enemy2CaptureEffectObjectList;
	public GameObject enemy1CaptureFailEffectObject;
	public GameObject enemy2CaptureFailEffectObject;

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		worldCanvas.worldCamera = UIInstanceManager.instance.GetCachedCameraMain();

		OnOffHeartParticleSystem(false);
	}

	void OnEnable()
	{
		InitializePhase();
	}

	GameObject _activePetObject;
	void InitializePhase()
	{
		if (string.IsNullOrEmpty(PetManager.instance.activePetId))
		{
			// 다른 곳에서 펫을 얻을 수 있게 되면서 펫이 하나도 없을땐 최초 획득 로직을 수행하는건 조건이 안맞게 되었다.
			// 액티브가 없을 때로 판단해서 FirstGet 처리를 하기로 한다.
			InitializeFirstGet();
			return;
		}

		// 메인 펫을 생성하고 탐색 대기 상태로 넘어간다.
		SpawnPetActor(PetManager.instance.activePetId, (newPetActor) =>
		{
			_activePetObject = newPetActor.gameObject;
			newPetActor.cachedTransform.position = playerPositionTransform.position;
			newPetActor.cachedTransform.rotation = playerPositionTransform.rotation;
		});

		firstGetButtonTransform.gameObject.SetActive(false);

		// 만약 진행중인 전투가 있다면
		if (PetManager.instance.IsCachedInProgressGame())
		{
			Timing.RunCoroutine(SearchProcess(false));
			return;
		}

		// 카메라 
		_rotateCamera = true;
		_rightDirection = (Random.value > 0.5f);
		_waitRemainTime = Random.Range(1.0f, 2.0f);
	}

	GameObject _firstGetQuestionObject;
	void InitializeFirstGet()
	{
		_firstGetQuestionObject = Instantiate<GameObject>(dropLevelPackPrefab, questionPositionTransform.position, Quaternion.identity);
		_firstGetQuestionObject.transform.localScale = questionPositionTransform.localScale;
		//firstGetButtonTransform.position = questionPositionTransform.position;

	}

	void OnDisable()
	{
		_rotateCamera = _rightDirection = false;
	}

	void Update()
	{
		UpdateRotateCamera();
		UpdateHeartParticleSystem();
	}

	float _waitRemainTime;
	bool _rotateCamera = false;
	bool _rightDirection = false;
	void UpdateRotateCamera()
	{
		if (_rotateCamera == false)
			return;

		if (_waitRemainTime > 0.0f)
		{
			_waitRemainTime -= Time.deltaTime;
			if (_waitRemainTime <= 0.0f)
			{
				_waitRemainTime = 0.0f;
			}
			return;
		}

		if (_rightDirection)
		{
			CustomFollowCamera.instance.cachedTransform.Rotate(0.0f, CameraRotateRange * 0.25f * Time.deltaTime, 0.0f, Space.World);
			if (Quaternion.Angle(CustomFollowCamera.instance.cachedTransform.rotation, Quaternion.Euler(CustomFollowCamera.instance.cachedTransform.eulerAngles.x, 0.0f, 0.0f)) > CameraRotateRange)
			{
				CustomFollowCamera.instance.cachedTransform.rotation = Quaternion.Euler(CustomFollowCamera.instance.cachedTransform.eulerAngles.x, CameraRotateRange, 0.0f);
				_waitRemainTime = Random.Range(1.0f, 2.0f);
				_rightDirection = false;
			}
		}
		else
		{
			CustomFollowCamera.instance.cachedTransform.Rotate(0.0f, -CameraRotateRange * 0.25f * Time.deltaTime, 0.0f, Space.World);
			if (Quaternion.Angle(CustomFollowCamera.instance.cachedTransform.rotation, Quaternion.Euler(CustomFollowCamera.instance.cachedTransform.eulerAngles.x, 0.0f, 0.0f)) > CameraRotateRange)
			{
				CustomFollowCamera.instance.cachedTransform.rotation = Quaternion.Euler(CustomFollowCamera.instance.cachedTransform.eulerAngles.x, -CameraRotateRange, 0.0f);
				_waitRemainTime = Random.Range(1.0f, 2.0f);
				_rightDirection = true;
			}
		}
	}


	public void ForceDirectionCamera(float eulerY, float duration)
	{
		_rotateCamera = false;
		CustomFollowCamera.instance.cachedTransform.DORotate(new Vector3(CustomFollowCamera.instance.cachedTransform.eulerAngles.x, eulerY, 0.0f), duration);
	}

	public void OnClickFirstGetButton()
	{
		Debug.Log("First Get Button");
		PlayFabApiManager.instance.RequestGetFirstPet(PetManager.instance.GetFirstPetId(), OnRecvFirstPet);
	}

	void OnRecvFirstPet(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		List<ItemInstance> listItemInstance = PetManager.instance.OnRecvItemGrantResult(itemGrantString, 1);
		if (listItemInstance == null)
			return;

		MainCanvas.instance.RefreshMenuButton();

		// preload
		AddressableAssetLoadManager.GetAddressableGameObject(PetData.GetAddressByPetId(PetManager.instance.activePetId), "Pet");

		Timing.RunCoroutine(GetFirstPetProcess());
	}

	Transform _firstPetTransform;
	IEnumerator<float> GetFirstPetProcess()
	{
		firstGetButtonTransform.gameObject.SetActive(false);
		PetSearchCanvas.instance.backButton.interactable = false;

		BattleInstanceManager.instance.GetCachedObject(appearEffectPrefab, questionPositionTransform.position, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(0.01f);

		_firstGetQuestionObject.SetActive(false);
		yield return Timing.WaitForSeconds(0.01f);

		SpawnPetActor(PetManager.instance.activePetId, (newPetActor) =>
		{
			_firstPetTransform = newPetActor.cachedTransform;
			newPetActor.cachedTransform.position = questionPositionTransform.position;
			newPetActor.cachedTransform.rotation = Quaternion.Euler(0.0f, 200.0f, 0.0f);
			newPetActor.cachedTransform.DOLocalMoveY(0.0f, 0.4f).SetEase(Ease.InCirc);

			firstGetPetBattleInfo.SetInfo(PetManager.instance.activePetId);
			firstGetPetBattleInfo.gameObject.SetActive(true);
		});

		yield return Timing.WaitForSeconds(1.0f);
		string message = string.Format("<color=#00FFFF>{0}</color>\n\n{1}", PetData.GetNameByPetId(PetManager.instance.activePetId), UIString.instance.GetString("PetUI_GetFirstPet"));
		OkCanvas.instance.ShowCanvas(true, UIString.instance.GetString("SystemUI_Info"), message, () =>
		{
			Timing.RunCoroutine(ResetMenuProcess());
		}, -1, true);
	}

	IEnumerator<float> ResetMenuProcess()
	{
		FadeCanvas.instance.FadeOut(1.0f, 1.0f);
		yield return Timing.WaitForSeconds(1.0f);

		_firstPetTransform.gameObject.SetActive(false);
		firstGetPetBattleInfo.gameObject.SetActive(false);

		InitializePhase();
		PetSearchCanvas.instance.InitializePhase();

		PetSearchCanvas.instance.backButton.interactable = true;
		FadeCanvas.instance.FadeIn(0.5f);
	}

	public void StartSearch()
	{
		Timing.RunCoroutine(SearchProcess(true));
	}

	PetActor _enemy1PetActor;
	PetActor _enemy2PetActor;
	IEnumerator<float> SearchProcess(bool search)
	{
		// 두마리 소환해서 전투를 준비해야한다. 프리로드를 위해 제일 먼저 굴린다.
		List<ObscuredString> listSearchPetId = PetManager.instance.GetInProgressSearchIdList();
		if (listSearchPetId.Count != 2)
			yield break;

		// preload
		AddressableAssetLoadManager.GetAddressableGameObject(PetData.GetAddressByPetId(listSearchPetId[0]), "Pet");
		AddressableAssetLoadManager.GetAddressableGameObject(PetData.GetAddressByPetId(listSearchPetId[1]), "Pet");

		if (search)
		{
			PetSearchCanvas.instance.backButton.interactable = false;

			ForceDirectionCamera(0.0f, 0.3f);
			yield return Timing.WaitForSeconds(0.3f);

			// 흔들흔들
			CustomFollowCamera.instance.cachedTransform.DOShakePosition(0.5f, 0.04f, 30, 90, false, false);
			yield return Timing.WaitForSeconds(1.0f);
		}

		// 왼쪽부터
		BattleInstanceManager.instance.GetCachedObject(appearEffectPrefab, enemy1PositionTransform.position, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(0.01f);

		bool spawnedFirstEnemy = false;
		SpawnPetActor(listSearchPetId[0], (newPetActor) =>
		{
			_enemy1PetActor = newPetActor;
			newPetActor.cachedTransform.position = enemy1PositionTransform.position + new Vector3(0.0f, 0.5f, 0.0f);
			newPetActor.cachedTransform.rotation = enemy1PositionTransform.rotation;
			newPetActor.cachedTransform.DOLocalMoveY(0.0f, 0.4f).SetEase(Ease.InCirc);
			spawnedFirstEnemy = true;

			enemy1PetBattleInfo.cachedTransform.position = enemy1PositionTransform.position;
			enemy1PetBattleInfo.SetInfo(listSearchPetId[0]);
			enemy1PetBattleInfo.gameObject.SetActive(true);
		});

		while (spawnedFirstEnemy == false)
			yield return Timing.WaitForOneFrame;

		BattleInstanceManager.instance.GetCachedObject(appearEffectPrefab, enemy2PositionTransform.position, Quaternion.identity, null);
		yield return Timing.WaitForSeconds(0.01f);

		SpawnPetActor(listSearchPetId[1], (newPetActor) =>
		{
			_enemy2PetActor = newPetActor;
			newPetActor.cachedTransform.position = enemy2PositionTransform.position + new Vector3(0.0f, 0.5f, 0.0f);
			newPetActor.cachedTransform.rotation = enemy2PositionTransform.rotation;
			newPetActor.cachedTransform.DOLocalMoveY(0.0f, 0.4f).SetEase(Ease.InCirc);

			enemy2PetBattleInfo.cachedTransform.position = enemy2PositionTransform.position;
			enemy2PetBattleInfo.SetInfo(listSearchPetId[1]);
			enemy2PetBattleInfo.gameObject.SetActive(true);
		});

		if (search)
		{
			// 두마리 다 소환했으면 1초 딜레이 후 전투 페이즈로 넘어가야한다. search 일때만 자동으로 넘어가고 inProgress일때는 눌러서 넘어가야하니 바로 Start 시키지 않는다.
			yield return Timing.WaitForSeconds(1.0f);
			PetSearchCanvas.instance.StartAttackPhase();
		}
	}

	public void ShowPetGauge()
	{
		enemy1PetBattleInfo.ShowGaugeObject(true);
		enemy2PetBattleInfo.ShowGaugeObject(true);
	}

	public void RefreshHeartParticleSystem()
	{
		OnOffHeartParticleSystem(true);
		_heartParticleSystemRemainTime = 1.0f;
	}

	void OnOffHeartParticleSystem(bool on)
	{
		for (int i = 0; i < heartParticleSystemList.Length; ++i)
		{
			ParticleSystem.EmissionModule emissionModule = heartParticleSystemList[i].emission;
			emissionModule.enabled = on;
		}
	}

	float _heartParticleSystemRemainTime;
	void UpdateHeartParticleSystem()
	{
		if (_heartParticleSystemRemainTime > 0.0f)
		{
			_heartParticleSystemRemainTime -= Time.deltaTime;
			if (_heartParticleSystemRemainTime <= 0.0f)
			{
				_heartParticleSystemRemainTime = 0.0f;

				OnOffHeartParticleSystem(false);
			}
		}
	}

	public void OnAttack(int attackPercent)
	{
		if (enemy1PetBattleInfo.IsDie() == false)
		{
			enemy1PetBattleInfo.OnAttack(attackPercent);
			if (enemy1PetBattleInfo.IsDie())
				_enemy1PetActor.animator.Play("Die");
		}
		if (enemy2PetBattleInfo.IsDie() == false)
		{
			enemy2PetBattleInfo.OnAttack(attackPercent);
			if (enemy2PetBattleInfo.IsDie())
				_enemy2PetActor.animator.Play("Die");
		}
	}

	#region Turn Result
	int _turnEndCount = 0;
	public void TurnEnd()
	{
		Debug.Log("Turn End");

		// 여기서 결과에 따라 나뉘어야한다.
		// 1. 몹을 둘 다 잡았다.
		// 2. 몹을 하나 잡거나 둘다 못잡았다.
		// 2-1. 추가 기회를 얻을지 굴린다. 무과금 5퍼. 추가 기회 5->30퍼. 얻는다고 판단되면 지그재그 토스트를 1초 띄우고, 터치하면 더 빨리 사라지게 한다.
		// 2-2. 배틀 스타트 대신 엑스트라 찬스를 띄우고 공격 진행.
		// 3. 한마리라도 쓰러졌으면 포획도구를 선택한다.
		// 3-1. 포획도구를 사용하면 Die중인 펫이 흔들흔들 흔들흔들 흔들흔들 후
		// 3-2. 실패하면 Idle로 일어나고 성공하면 가만히 있는다.
		// 4. 상하단 줄 그어있는 결과창 띄우고 얻었다. 표시.
		// 4-1. 터치하여 나가기 누를땐 진짜로 끝이기 때문에 컨페티 띄우고 종료.
		// 4-2. 뭔가 수상하다 누를땐 추가로 얻을 펫들을 배치시킨 후 화면 우측으로 돌리고 지그재그 토스트 텍스트 
		// 4-3. 하나도 없을때 불쌍해서인지 하나가 찾아왔다 텍스트. 주변에 따라오고 싶어하는 펫들이 동행했다. 텍스트
		// 4-4. 이후 무조건 컨페티 결과창.
		if (enemy1PetBattleInfo.IsDie() && enemy2PetBattleInfo.IsDie())
		{
			PetSearchCanvas.instance.ShowSelectCapture();
		}
		else
		{
			++_turnEndCount;

			// 두번째 턴은 항상 제공하기로 한다.
			if (_turnEndCount == 1)
			{
				Timing.RunCoroutine(StartSecondProcess());
				return;
			}
			else if (_turnEndCount == 2)
			{
				// 3턴에선 베이스와 펫 패스 둘다 검사해야한다.
				if (CheckExtraChance())
				{
					Timing.RunCoroutine(UseExtraChanceProcess());
					return;
				}
			}

			if (enemy1PetBattleInfo.IsDie() || enemy2PetBattleInfo.IsDie())
			{
				PetSearchCanvas.instance.ShowSelectCapture();
			}
			else
			{
				// 두마리 다 잡는데 실패했다면 펫을 획득하는데 실패하였습니다. 띄우고 extra gain은 무조건 발동시킨다.
				// extra gain중에 가장 낮은 1성 한개 얻는거 처리를 위해 
				List<ObscuredString> listExtraGainId = PetManager.instance.GetExtraGainIdList(true);
				PrepareExtraGain(true, listExtraGainId);
				_expectCount = listExtraGainId.Count;
				PlayFabApiManager.instance.RequestEndPet(0, listExtraGainId, OnRecvEndPet);
			}
		}
	}

	ObscuredInt _expectCount = 0;
	void OnRecvEndPetCommon(string itemGrantString)
	{
		if (itemGrantString == "")
			return;

		// prev 저장해둔다.
		PetSearchCanvas.instance.prevPowerValue = BattleInstanceManager.instance.playerActor.actorStatus.GetValue(ActorStatusDefine.eActorStatus.CombatPower);

		List<ItemInstance> listItemInstance = PetManager.instance.OnRecvItemGrantResult(itemGrantString, _expectCount);
		if (listItemInstance == null)
			return;

		MainCanvas.instance.RefreshMenuButton();
	}

	void OnRecvEndPet(string itemGrantString)
	{
		OnRecvEndPetCommon(itemGrantString);

		// 감화 자체에 실패한거라 아무런 처리 없이 result창으로 넘어가면 된다.
		PetSearchCanvas.instance.ShowResult(false, true);
	}
	
	bool extraChanceByPetPass { get; set; }
	bool CheckExtraChance()
	{
		extraChanceByPetPass = false;

		float baseValue = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetExtraChance") * 0.01f);
		if (Random.value < baseValue)
			return true;

		if (PetManager.instance.IsPetPass())
			baseValue = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetPassExtraChance") * 0.01f);
		if (Random.value < baseValue)
		{
			extraChanceByPetPass = true;
			return true;
		}
		return false;
	}

	IEnumerator<float> StartSecondProcess()
	{
		yield return Timing.WaitForSeconds(0.5f);
		PetSearchCanvas.instance.StartAttackPhase(false, true, false);
	}

	IEnumerator<float> UseExtraChanceProcess()
	{
		ToastZigzagCanvas.instance.ShowToast(UIString.instance.GetString("PetUI_UseExtraChanceToast"), 1.5f, 0.8f, true);
		if (extraChanceByPetPass) PetSearchCanvas.instance.petPassBonusCenterObject.SetActive(true);
		yield return Timing.WaitForSeconds(2.0f);

		PetSearchCanvas.instance.StartAttackPhase(false, false, true);
	}

	ObscuredInt _captureIndex;
	public void ApplyCapture(int captureIndex)
	{
		_captureIndex = captureIndex;
		Timing.RunCoroutine(CaptureProcess());
	}

	List<ObscuredString> _listCaptureId = new List<ObscuredString>();
	IEnumerator<float> CaptureProcess()
	{
		// 잠시 대기
		yield return Timing.WaitForSeconds(0.5f);

		// 3번 흔들흔들 할거다.
		for (int i = 0; i < 3; ++i)
		{
			// 다이 되어있는 몬스터
			if (enemy1PetBattleInfo.IsDie())
			{
				_enemy1PetActor.cachedTransform.DOShakePosition(0.6f, new Vector3(0.2f, 0.0f, 0.0f), 20);
				enemy1CaptureEffectObjectList[_captureIndex].SetActive(true);
			}
			if (enemy2PetBattleInfo.IsDie())
			{
				_enemy2PetActor.cachedTransform.DOShakePosition(0.6f, new Vector3(0.2f, 0.0f, 0.0f), 20);
				enemy2CaptureEffectObjectList[_captureIndex].SetActive(true);
			}
			yield return Timing.WaitForSeconds(1.0f);
			enemy1CaptureEffectObjectList[_captureIndex].SetActive(false);
			enemy2CaptureEffectObjectList[_captureIndex].SetActive(false);
		}

		_listCaptureId.Clear();

		int highestStar = 1;
		if (enemy1PetBattleInfo.IsDie())
		{
			// 현재 펫의 등급과 포획도구 확률표에 따라 확률을 구하고
			float captureRate = GetCaptureRate(enemy1PetBattleInfo.star, _captureIndex);
			if (Random.value <= captureRate)
			{
				_listCaptureId.Add(enemy1PetBattleInfo.petId);
				if (highestStar < enemy1PetBattleInfo.star)
					highestStar = enemy1PetBattleInfo.star;
			}
			else
				_enemy1Failure = true;
		}
		if (enemy2PetBattleInfo.IsDie())
		{
			float captureRate = GetCaptureRate(enemy2PetBattleInfo.star, _captureIndex);
			if (Random.value <= captureRate)
			{
				_listCaptureId.Add(enemy2PetBattleInfo.petId);
				if (highestStar < enemy2PetBattleInfo.star)
					highestStar = enemy2PetBattleInfo.star;
			}
			else
				_enemy2Failure = true;
		}

		_success = false;
		_existExtraGain = false;
		if (_listCaptureId.Count == 0)
		{
			// 모든 포획이 실패하면 아무것도 못잡았다고 판단해서 oneForFailure 엑스트라 획득으로 처리하고
			_existExtraGain = true;
			List<ObscuredString> listExtraGainId = PetManager.instance.GetExtraGainIdList(true);
			PrepareExtraGain(true, listExtraGainId);
			_expectCount = listExtraGainId.Count;
			PlayFabApiManager.instance.RequestEndPet(_captureIndex, listExtraGainId, OnRecvCapture);
		}
		else
		{
			// 하나라도 포획했다면 성공으로 하고
			// 
			_success = true;
			if (CheckExtraGain())
			{
				// 얻은거에서 제일 높은 등급을 구해 이거 이하의 동료들이 따라오게 해야한다.
				_existExtraGain = true;
				List<ObscuredString> listExtraGainId = PetManager.instance.GetExtraGainIdList(false, highestStar);
				PrepareExtraGain(false, listExtraGainId);
				for (int i = 0; i < listExtraGainId.Count; ++i)
					_listCaptureId.Add(listExtraGainId[i]);
				_expectCount = _listCaptureId.Count;
				PlayFabApiManager.instance.RequestEndPet(_captureIndex, _listCaptureId, OnRecvCapture);
			}
			else
			{
				_expectCount = _listCaptureId.Count;
				PlayFabApiManager.instance.RequestEndPet(_captureIndex, _listCaptureId, OnRecvCapture);
			}
		}
	}

	float GetCaptureRate(int star, int captureIndex)
	{
		PetCaptureTableData petCaptureTableData = TableDataManager.instance.FindPetCaptureTableDataByIndex(captureIndex);
		if (petCaptureTableData == null)
			return 0.0f;

		switch (star)
		{
			case 1:
			case 2:
			case 3:
				return petCaptureTableData.starProb_3;
			case 4:
				return petCaptureTableData.starProb_4;
			case 5:
				return petCaptureTableData.starProb_5;
		}
		return 0.0f;	
	}

	public bool extraGainByPetPass { get; private set; }
	bool CheckExtraGain()
	{
		extraGainByPetPass = false;

		float baseValue = BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetExtraGain") * 0.01f;
		if (Random.value < baseValue)
			return true;

		if (PetManager.instance.IsPetPass())
			baseValue = (BattleInstanceManager.instance.GetCachedGlobalConstantInt("PetPassExtraGain") * 0.01f);
		if (Random.value < baseValue)
		{
			extraGainByPetPass = true;
			return true;
		}
		return false;
	}

	bool _success = false;
	bool _enemy1Failure = false;
	bool _enemy2Failure = false;
	bool _existExtraGain = false;
	void OnRecvCapture(string itemGrantString)
	{
		OnRecvEndPetCommon(itemGrantString);
		Timing.RunCoroutine(CaptureResultProcess());
	}

	IEnumerator<float> CaptureResultProcess()
	{
		// 포획에서 풀려나는 펫이 있다면 그 연출을 하고 결과창을 띄워야한다.
		if (_enemy1Failure || _enemy2Failure)
		{
			if (_enemy1Failure)
			{
				_enemy1PetActor.animator.Play("Idle");
				enemy1CaptureFailEffectObject.SetActive(true);
			}
			if (_enemy2Failure)
			{
				_enemy2PetActor.animator.Play("Idle");
				enemy2CaptureFailEffectObject.SetActive(true);
			}
			yield return Timing.WaitForSeconds(1.0f);
		}

		// 감화 자체에 실패한거라 아무런 처리 없이 result창으로 넘어가면 된다.
		PetSearchCanvas.instance.ShowResult(_success, _existExtraGain);
	}

	ObscuredBool _oneForFailure = false;
	void PrepareExtraGain(bool oneForFailure, List<ObscuredString> listExtraGainId)
	{
		// 오른쪽에 새 펫들을 세워놔야한다.
		if (oneForFailure)
		{
			string petId = "";
			if (listExtraGainId.Count > 0)
				petId = listExtraGainId[0];
			else
				return;

			// 실패로 인한 1마리 보상일땐 1성 하나만 가운데 두고 
			SpawnPetActor(petId, (newPetActor) =>
			{
				newPetActor.cachedTransform.position = extraGain1PositionTransform.position;
				newPetActor.cachedTransform.rotation = extraGain1PositionTransform.rotation;

				extraGain1PetBattleInfo.SetInfo(petId);
				extraGain1PetBattleInfo.gameObject.SetActive(true);
			});
			_oneForFailure = oneForFailure;
		}
		else
		{
			// 순차로 만들어내야 버그 없이 만들어낼 수 있어서 Process 형태로 처리한다.
			Timing.RunCoroutine(SpawnExtraGainProcess(listExtraGainId));
		}
	}

	IEnumerator<float> SpawnExtraGainProcess(List<ObscuredString> listExtraGainId)
	{
		// 패스로 인한 여러마리 보상일땐 골고루 나오게
		if (listExtraGainId.Count == 1 || listExtraGainId.Count == 3)
		{
			bool spawnedFirstEnemy = false;
			string petId = listExtraGainId[0];
			SpawnPetActor(petId, (newPetActor) =>
			{
				newPetActor.cachedTransform.position = extraGain1PositionTransform.position;
				newPetActor.cachedTransform.rotation = extraGain1PositionTransform.rotation;
				spawnedFirstEnemy = true;

				extraGain1PetBattleInfo.SetInfo(petId);
				extraGain1PetBattleInfo.gameObject.SetActive(true);
			});

			if (listExtraGainId.Count == 3)
			{
				while (spawnedFirstEnemy == false)
					yield return Timing.WaitForOneFrame;

				bool spawnedSecondEnemy = false;
				petId = listExtraGainId[1];
				SpawnPetActor(petId, (newPetActor) =>
				{
					newPetActor.cachedTransform.position = extraGain2PositionTransform.position;
					newPetActor.cachedTransform.rotation = extraGain2PositionTransform.rotation;
					spawnedSecondEnemy = true;

					extraGain2PetBattleInfo.SetInfo(petId);
					extraGain2PetBattleInfo.gameObject.SetActive(true);
				});

				while (spawnedSecondEnemy == false)
					yield return Timing.WaitForOneFrame;

				petId = listExtraGainId[2];
				SpawnPetActor(petId, (newPetActor) =>
				{
					newPetActor.cachedTransform.position = extraGain3PositionTransform.position;
					newPetActor.cachedTransform.rotation = extraGain3PositionTransform.rotation;

					extraGain3PetBattleInfo.SetInfo(petId);
					extraGain3PetBattleInfo.gameObject.SetActive(true);
				});
			}
		}
		else if (listExtraGainId.Count == 2)
		{
			bool spawnedFirstEnemy = false;
			string petId = listExtraGainId[0];
			SpawnPetActor(petId, (newPetActor) =>
			{
				newPetActor.cachedTransform.position = extraGain2PositionTransform.position;
				newPetActor.cachedTransform.rotation = extraGain2PositionTransform.rotation;
				spawnedFirstEnemy = true;

				extraGain2PetBattleInfo.SetInfo(petId);
				extraGain2PetBattleInfo.gameObject.SetActive(true);
			});

			while (spawnedFirstEnemy == false)
				yield return Timing.WaitForOneFrame;

			petId = listExtraGainId[1];
			SpawnPetActor(petId, (newPetActor) =>
			{
				newPetActor.cachedTransform.position = extraGain3PositionTransform.position;
				newPetActor.cachedTransform.rotation = extraGain3PositionTransform.rotation;

				extraGain3PetBattleInfo.SetInfo(petId);
				extraGain3PetBattleInfo.gameObject.SetActive(true);
			});
		}
	}



	public void RotateCameraToExtraGain()
	{
		Timing.RunCoroutine(ExtraGainProcess());
	}

	IEnumerator<float> ExtraGainProcess()
	{
		ForceDirectionCamera(9.0f, 0.6f);
		yield return Timing.WaitForSeconds(0.6f);

		ToastZigzagCanvas.instance.ShowToast(UIString.instance.GetString(_oneForFailure ? "PetUI_FailureExtraGainToast" : "PetUI_ExtraGainToast"), 2.3f, 0.8f, true);
		yield return Timing.WaitForSeconds(2.3f);

		PetSearchCanvas.instance.ShowResult(true, false);
	}
	#endregion





	#region Pet Actor
	System.Action<PetActor> _completeCallback;
	public void SpawnPetActor(string petId, System.Action<PetActor> callback)
	{
		_completeCallback = callback;
		AddressableAssetLoadManager.GetAddressableGameObject(PetData.GetAddressByPetId(petId), "", OnLoadedPetActor);
	}

	void OnLoadedPetActor(GameObject prefab)
	{
		if (this == null) return;
		if (gameObject == null) return;

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

		if (_completeCallback != null) _completeCallback.Invoke(petActor);
	}
	#endregion
}