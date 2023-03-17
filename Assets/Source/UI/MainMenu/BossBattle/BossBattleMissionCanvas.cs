using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using MEC;
using Michsky.UI.Hexart;
using DG.Tweening;

public class BossBattleMissionCanvas : MonoBehaviour
{
	public static BossBattleMissionCanvas instance;

	public GameObject bossBattleMissionPrefab;

	public Text remainTimeText;
	public DOTweenAnimation tweenAnimation;

	public Button homeButton;
	public GameObject battlePauseSimpleMenuRoot;
	public SwitchAnim verticalSlotSwitch;

	void Awake()
	{
		instance = this;

		LoadPlayerPrefs();
	}

	Transform _groundTransform;
	void OnEnable()
	{
		bool restore = StackCanvas.Push(gameObject, false, null, OnPopStack);
		if (restore)
			return;

		if (_groundTransform == null)
			_groundTransform = BattleInstanceManager.instance.GetCachedObject(bossBattleMissionPrefab, StageManager.instance.GetSafeWorldOffset(), Quaternion.identity).transform;
		else
			_groundTransform.gameObject.SetActive(true);

		if (MainCanvas.instance != null)
		{
			MainCanvas.instance.OnClickCloseButton();
			// HideState를 풀어놔야 카메라 쉐이크가 동작한다.
			MainCanvas.instance.OnEnterCharacterMenu(false, true);
			MainCanvas.instance.challengeButtonObject.SetActive(false);
			MainCanvas.instance.inputRectObject.SetActive(false);
			MainCanvas.instance.FadeOutQuestInfoGroup(0.0f, 0.1f, false, true);
		}

		LoadOption();
	}

	void OnDisable()
	{
		// 씬을 종료하고 새 씬을 구축하러 나가는 로직으로 구현되어있기 때문에
		// 하단 라인들로 넘어갈 이유가 없다. 그러니 여기서 리턴시킨다.
		return;

		//MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	void OnDestroy()
	{
		if (Time.timeScale != 1.0f)
			Time.timeScale = 1.0f;
	}

	void OnPopStack()
	{
		if (StageManager.instance == null)
			return;
		if (MainSceneBuilder.instance == null)
			return;
		if (CustomFollowCamera.instance == null || CameraFovController.instance == null || MainCanvas.instance == null)
			return;
		if (CustomFollowCamera.instance.gameObject == null)
			return;

		//MainCanvas.instance.OnEnterCharacterMenu(false);
	}

	void Update()
	{
		UpdateTimer();
		UpdateRemainTime();
		UpdateHealRemainTime();
		UpdateEndProcess();
	}



	#region Pause Menu
	public void OnClickHomeButton()
	{
		homeButton.gameObject.SetActive(false);
		battlePauseSimpleMenuRoot.SetActive(true);
		PauseGame(true);
	}

	public void OnClickExitButton()
	{
		YesNoCanvas.instance.ShowCanvas(true, UIString.instance.GetString("GameUI_BackToLobby"), UIString.instance.GetString("GameUI_BackToLobbyDescription"), () => {
			PlayFabApiManager.instance.RequestCancelBossBattle();
			SavePlayerPrefs();
			SubMissionData.instance.readyToReopenMissionListCanvas = true;
			SceneManager.LoadScene(0);
		});
	}

	public void OnClickResumeButton()
	{
		SavePlayerPrefs();
		homeButton.gameObject.SetActive(true);
		battlePauseSimpleMenuRoot.SetActive(false);
		PauseGame(false);
	}

	PlayerActor _prevPlayerActor;
	bool _prevPlayerActorAnimatorUnscaledTime;
	float _prevTimeScale;
	void PauseGame(bool pause)
	{
		if (pause)
		{
			_prevPlayerActorAnimatorUnscaledTime = (BattleInstanceManager.instance.playerActor.actionController.animator.updateMode == AnimatorUpdateMode.UnscaledTime);
			if (_prevPlayerActorAnimatorUnscaledTime)
			{
				_prevPlayerActor = BattleInstanceManager.instance.playerActor;
				_prevPlayerActor.actionController.animator.updateMode = AnimatorUpdateMode.Normal;
			}
			_prevTimeScale = Time.timeScale;
			Time.timeScale = (BattleInstanceManager.instance.playerActor.actorId == "Actor1039") ? 0.000001f : 0.0f;
		}
		else
		{
			if (_prevPlayerActorAnimatorUnscaledTime && _prevPlayerActor != null)
				_prevPlayerActor.actionController.animator.updateMode = AnimatorUpdateMode.UnscaledTime;
			Time.timeScale = _prevTimeScale;
		}
	}
	#endregion

	#region GameOption
	public bool IsUseVerticalSkillSlot()
	{
		return _useVerticalSlot == 1;
	}

	int _useVerticalSlot = 0;
	static string OPTION_VERTICAL_SLOT_KEY = "_option_vertical_slot_key";
	void LoadPlayerPrefs()
	{
		if (PlayerPrefs.HasKey(OPTION_VERTICAL_SLOT_KEY))
		{
			_useVerticalSlot = PlayerPrefs.GetInt(OPTION_VERTICAL_SLOT_KEY);
		}
	}

	void SavePlayerPrefs()
	{
		PlayerPrefs.SetInt(OPTION_VERTICAL_SLOT_KEY, _useVerticalSlot);
	}

	void LoadOption()
	{
		verticalSlotSwitch.isOn = (_useVerticalSlot == 1);
	}

	public void OnSwitchOnVerticalSlot()
	{
		_useVerticalSlot = 1;

		if (EquipSkillSlotCanvas.instance != null && EquipSkillSlotCanvas.instance.gameObject.activeSelf)
			EquipSkillSlotCanvas.instance.RefreshVerticalPosition();
	}

	public void OnSwitchOffVerticalSlot()
	{
		_useVerticalSlot = 0;

		if (EquipSkillSlotCanvas.instance != null && EquipSkillSlotCanvas.instance.gameObject.activeSelf)
			EquipSkillSlotCanvas.instance.RefreshVerticalPosition();
	}
	#endregion






	#region Timer
	bool _timerStarted = false;
	float _timerRemainTime;
	bool _timeOut = false;
	public float remainTime { get { return _timerRemainTime; } }
	void UpdateTimer()
	{
		if (_timerStarted == false)
		{
			_timerStarted = true;
			_timerRemainTime = 60.0f;
			return;
		}

		if (_timerStarted == false)
			return;

		if (_endProcess)
			return;

		if (_timerRemainTime <= 0.0f)
			return;

		_timerRemainTime -= Time.deltaTime;
		if (_timerRemainTime <= 0.0f)
		{
			_timeOut = true;
			_endProcess = true;
			_endProcessWaitRemainTime = 1.0f;

			// 몬스터들이 timeOut 후 죽었더니 불공정하다고 느끼는 사람들이 있다
			AffectorValueLevelTableData invincibleAffectorValue = new AffectorValueLevelTableData();
			invincibleAffectorValue.fValue1 = -1.0f;
			invincibleAffectorValue.iValue3 = 1;    // noText
			List<MonsterActor> listMonsterActor = BattleInstanceManager.instance.GetLiveMonsterList();
			for (int i = 0; i < listMonsterActor.Count; ++i)
			{
				if (listMonsterActor[i].team.teamId != (int)Team.eTeamID.DefaultMonster || listMonsterActor[i].excludeMonsterCount)
					continue;
				listMonsterActor[i].affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Invincible, invincibleAffectorValue, listMonsterActor[i], false);
			}
		}
	}

	int _lastRemainTimeSecond = -1;
	void UpdateRemainTime()
	{
		if (remainTime > 0.0f)
		{
			if (_lastRemainTimeSecond != (int)remainTime)
			{
				int visualTime = (int)remainTime + 1;

				int minutes = visualTime / 60;
				int seconds = visualTime % 60;
				remainTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

				_lastRemainTimeSecond = (int)remainTime;

				if (_lastRemainTimeSecond >= 0 && _lastRemainTimeSecond < 10)
				{
					TweenRestart();
					remainTimeText.color = new Color(0.925f, 0.52f, 0.52f);
				}
			}
		}
		else
		{
			TweenRestart();
			remainTimeText.text = string.Format("{0:00}:{1:00}", 0, 0);
		}
	}

	void TweenRestart()
	{
		tweenAnimation.DORestart();
		remainTimeText.transform.localScale = new Vector3(1.7f, 1.7f, 1.7f);
		remainTimeText.transform.DOScale(1.0f, 0.5f);
	}
	#endregion

	#region Player Damage
	// Clear도 여기서 처리하는데 회복 로직은 못처리할게 어딨을까. 그러니 그냥 다 체크.
	float _prevHpRatio = 1.0f;
	public void OnChangedHP()
	{
		float hpRatio = BattleInstanceManager.instance.playerActor.actorStatus.GetHPRatio();
		if (_prevHpRatio < hpRatio)
		{

		}
		else
		{
			// damage
			_healRemainTime = BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleRegenDelay100") * 0.01f;

			// 한대 딱 맞을때 이후 들어오는 연타에 안죽게 하려면 0.1초 무적 처리도 해줘야한다.
			AffectorValueLevelTableData invincibleAffectorValue = new AffectorValueLevelTableData();
			invincibleAffectorValue.fValue1 = 0.6f;
			invincibleAffectorValue.iValue3 = 1;    // noText
			BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Invincible, invincibleAffectorValue, BattleInstanceManager.instance.playerActor, false);
		}
		_prevHpRatio = hpRatio;
	}

	float _healRemainTime;
	void UpdateHealRemainTime()
	{
		if (_healRemainTime > 0.0f)
		{
			_healRemainTime -= Time.deltaTime;
			if (_healRemainTime <= 0.0f)
			{
				AffectorValueLevelTableData healAffectorValue = new AffectorValueLevelTableData();
				healAffectorValue.fValue3 = BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleRegenHpRatio100") * 0.01f;
				BattleInstanceManager.instance.playerActor.affectorProcessor.ExecuteAffectorValueWithoutTable(eAffectorType.Heal, healAffectorValue, BattleInstanceManager.instance.playerActor, false);
				BattleInstanceManager.instance.GetCachedObject(BossBattleMissionGround.instance.healEffectPrefab, BattleInstanceManager.instance.playerActor.cachedTransform.position, Quaternion.identity);

				float hpRatio = BattleInstanceManager.instance.playerActor.actorStatus.GetHPRatio();
				_prevHpRatio = hpRatio;
				if (hpRatio >= 1.0f)
				{
					// full
					_healRemainTime = 0.0f;
				}
				else
				{
					_healRemainTime += BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleRegenTickDelay100") * 0.01f;
				}
			}
		}
	}
	#endregion

	#region Player Die
	public void OnDiePlayer()
	{
		// reset
		_healRemainTime = 0.0f;

		// 여기서 인풋은 막되
		homeButton.interactable = false;

		// 패킷 전달시간이 없다보니 1초 더 늘려둔다.
		_endProcess = true;
		_endProcessWaitRemainTime = 1.0f;
	}
	#endregion

	#region ClearMission
	public void ClearMission()
	{
		// boss clear
		homeButton.interactable = false;
		_endProcess = true;
		_endProcessWaitRemainTime = 1.5f;
	}
	#endregion

	#region EndGame
	bool _endProcess = false;
	float _endProcessWaitRemainTime = 0.0f; // 최소 대기타임
	void UpdateEndProcess()
	{
		if (_endProcess == false)
			return;

		if (_endProcessWaitRemainTime > 0.0f)
		{
			_endProcessWaitRemainTime -= Time.deltaTime;
			if (_endProcessWaitRemainTime <= 0.0f)
				_endProcessWaitRemainTime = 0.0f;
			return;
		}
		
		if (CheatingListener.detectedCheatTable)
		{
			_endProcess = false;
			return;
		}

		bool clear = false;
		if (BattleInstanceManager.instance.playerActor.actorStatus.IsDie() == false && _timeOut == false)
		{
			HitObject.EnableRigidbodyAndCollider(false, null, BattleInstanceManager.instance.playerActor.GetCollider());
			clear = true;
		}
		
		SoundManager.instance.StopBGM(3.0f);

		int price = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MissionEnergyBossBattle");

		// 패킷 준비
		_selectedDifficulty = BossBattleEnterCanvas.instance.selectedDifficulty;
		_clearDifficulty = BossBattleEnterCanvas.instance.clearDifficulty;
		_firstClear = false;
		if (_clearDifficulty == 0)
			_firstClear = true;
		if (_selectedDifficulty == (_clearDifficulty + 1))
			_firstClear = true;

		if (clear)
		{
			// 클리어 했다면 다음번 보스가 누구일지 미리 굴려서 End패킷에 보내야한다.
			// 왕관보스를 잡은거면 다음이 열리면 되고
			int nextBossId = -1;
			if (BossBattleEnterCanvas.instance.isKingMonster)
				nextBossId = SubMissionData.instance.GetNextKingBossId();
			else
				nextBossId = SubMissionData.instance.GetNextRandomBossId();

			// 첫 클리어라면 
			if (_firstClear)
			{
				//DropProcessor.Drop(BattleInstanceManager.instance.cachedTransform, _bossRewardTableData.firstDropId, "", true, true);
				//if (CheatingListener.detectedCheatTable)
				//	return;
			}

			PlayFabApiManager.instance.RequestEndBossBattle(clear, nextBossId, _selectedDifficulty, price, (result, nextId) =>
			{
				// 정보를 갱신하기 전에 먼저 BattleResult를 보여준다.
				UIInstanceManager.instance.ShowCanvasAsync("BossBattleResultCanvas", () =>
				{
					BossBattleResultCanvas.instance.RefreshInfo(true, _selectedDifficulty, _firstClear);
					OnRecvEndBossBattle(result, _firstClear, nextId);
				});
			});
		}
		else
		{
			PlayFabApiManager.instance.RequestEndBossBattle(false, 0, _selectedDifficulty, price, (result, nextId) =>
			{
				// 정보를 갱신하기 전에 먼저 BattleResult를 보여준다.
				UIInstanceManager.instance.ShowCanvasAsync("BossBattleResultCanvas", () =>
				{
					BossBattleResultCanvas.instance.RefreshInfo(false, _selectedDifficulty, false);
					OnRecvEndBossBattle(result, false, nextId);
				});
			});
		}

		_endProcess = false;
	}

	int _selectedDifficulty;
	int _clearDifficulty;
	bool _firstClear;
	void OnRecvEndBossBattle(bool clear, bool firstClear, int nextBossId)
	{
		// 반복클리어냐 아니냐에 따라 결과를 나누면 된다.
		SubMissionData.instance.AddBossBattleCount();

		if (clear)
		{
			SubMissionData.instance.OnClearBossBattle(_selectedDifficulty, _clearDifficulty, nextBossId);

			if (_firstClear)
			{
				//if (_bossRewardTableData.firstEnergy > 0)
				//	CurrencyData.instance.OnRecvRefillEnergy(_bossRewardTableData.firstEnergy);
			}
		}
	}
	#endregion







	void OnApplicationPause(bool pauseStatus)
	{
		OnApplicationPauseCanvas(pauseStatus);
	}

	void OnApplicationPauseCanvas(bool pauseStatus)
	{
		if (homeButton.gameObject.activeSelf == false)
			return;
		if (homeButton.interactable == false)
			return;
		if (FullscreenYesNoCanvas.IsShow())
			return;

		if (pauseStatus)
			OnClickHomeButton();
	}
}