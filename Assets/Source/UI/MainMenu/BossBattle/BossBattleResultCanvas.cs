using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class BossBattleResultCanvas : MonoBehaviour
{
	public static BossBattleResultCanvas instance;

	public Text difficultyText;
	public Text resultText;

	public GameObject xpRewardGroupObject;
	public Text xpLevelText;
	public Text xpLevelExpText;
	public Image xpLevelExpImage;
	public GameObject xpLevelUpInfoObject;

	public GameObject pointRewardGroupObject;
	public Text pointText;
	public DOTweenAnimation pointTextTweenAnimation;
	public Text bonusTimesText;
	public Text remainCountText;
	public GameObject bonusRectObject;

	public Text kingBossResultText;

	public GameObject exitGroupObject;

	void Awake()
	{
		instance = this;
	}
	
	void OnEnable()
	{
		Time.timeScale = 0.0f;
	}

	void OnDestroy()
	{
		Time.timeScale = 1.0f;
	}

	void Update()
	{
		UpdatePointText();
	}

	bool _clear = false;
	int _selectedDifficulty;
	bool _firstClear;
	bool _dailyBonusApplied;
	bool _allKingClear;
	public void RefreshInfo(bool clear, int selectedDifficulty, bool firstClear)
	{
		_clear = clear;
		_selectedDifficulty = selectedDifficulty;
		_firstClear = firstClear;
		_dailyBonusApplied = (SubMissionData.instance.bossBattleDailyCount <= BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleDailyCount"));
		_allKingClear = false;
		if (SubMissionData.instance.bossBattleId == BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattle"))
		{
			if ((SubMissionData.instance.bossBattleClearId + 1) == SubMissionData.instance.bossBattleId)
				_allKingClear = true;
		}

		difficultyText.text = string.Format("DIFFICULTY {0}", selectedDifficulty);
		resultText.text = UIString.instance.GetString(clear ? "GameUI_Success" : "GameUI_Failure");
		resultText.color = clear ? new Color(0.0f, 0.733f, 0.792f) : new Color(0.792f, 0.152f, 0.0f);

		gameObject.SetActive(true);

		StartCoroutine(BgmProcess());
	}

	IEnumerator BgmProcess()
	{
		yield return new WaitForSecondsRealtime(_clear ? 0.1f : 1.5f);

		SoundManager.instance.PlaySFX(_clear ? "BattleWin" : "BattleLose");

		yield return new WaitForSecondsRealtime((_clear ? 12.0f : 11.0f) + 3.0f);

		SoundManager.instance.PlayBgm("BGM_BattleEnd", 3.0f);
	}

	public void OnEventSuccessResult()
	{
		RefreshBossBattleCount(BossBattleEnterCanvas.instance.GetXp());
		xpRewardGroupObject.SetActive(true);
	}
	
	#region Xp
	bool _maxXpLevel = false;
	float _nextPercent = 0.0f;
	bool _nextIsLevelUp = false;
	int _current = 0;
	int _max = 0;
	void RefreshBossBattleCount(int count)
	{
		// 현재 카운트가 속하는 테이블 구해와서 레벨 및 경험치로 표시.
		int maxXpLevel = BattleInstanceManager.instance.GetCachedGlobalConstantInt("MaxBossBattleXpLevel");
		int level = 0;
		float percent = 0.0f;
		int currentPeriodExp = 0;
		int currentPeriodExpMax = 0;
		for (int i = 1; i < TableDataManager.instance.bossExpTable.dataArray.Length; ++i)
		{
			if (count < TableDataManager.instance.bossExpTable.dataArray[i].requiredAccumulatedExp)
			{
				currentPeriodExp = count - TableDataManager.instance.bossExpTable.dataArray[i - 1].requiredAccumulatedExp;
				currentPeriodExpMax = TableDataManager.instance.bossExpTable.dataArray[i].requiredExp;
				percent = (float)currentPeriodExp / (float)currentPeriodExpMax;
				level = TableDataManager.instance.bossExpTable.dataArray[i].xpLevel - 1;
				break;
			}
			if (TableDataManager.instance.bossExpTable.dataArray[i].xpLevel >= maxXpLevel)
			{
				currentPeriodExp = count - TableDataManager.instance.bossExpTable.dataArray[i - 1].requiredAccumulatedExp;
				currentPeriodExpMax = TableDataManager.instance.bossExpTable.dataArray[i].requiredExp;
				level = maxXpLevel;
				percent = 1.0f;
				break;
			}
		}
		_current = currentPeriodExp;
		_max = currentPeriodExpMax;

		string xpLevelString = "";
		if (level == maxXpLevel)
		{
			_maxXpLevel = true;
			xpLevelString = UIString.instance.GetString("GameUI_Lv", "Max");
			xpLevelExpImage.color = MailCanvasListItem.GetGoldTextColor();
		}
		else
		{
			xpLevelString = UIString.instance.GetString("GameUI_Lv", level);
			xpLevelExpImage.color = Color.white;
			_nextPercent = (float)(currentPeriodExp + 1) / currentPeriodExpMax;
			_nextIsLevelUp = (currentPeriodExp + 1) == currentPeriodExpMax;
		}
		xpLevelText.text = string.Format("XP {0}", xpLevelString);
		xpLevelExpText.text = string.Format("{0} / {1}", currentPeriodExp, currentPeriodExpMax);
		xpLevelExpImage.fillAmount = percent;
	}
	#endregion

	public void OnEventIncreaseXp()
	{
		StartCoroutine(XpProcess());
	}

	IEnumerator XpProcess()
	{
		yield return new WaitForSecondsRealtime(0.2f);

		if (_maxXpLevel == false)
		{
			TweenerCore<float, float, FloatOptions> tweenRef = DOTween.To(() => xpLevelExpImage.fillAmount, x => xpLevelExpImage.fillAmount = x, _nextPercent, 0.4f).SetEase(Ease.Linear).SetUpdate(true);

			yield return new WaitForSecondsRealtime(0.4f);

			if (tweenRef != null)
				tweenRef.Kill();

			// _nextIsLevelUp 플래그가 간혹 동작하지 않는 버그가 있어서 fillAmount도 추가로 검사하기로 해본다.
			if (_nextIsLevelUp || xpLevelExpImage.fillAmount >= 1.0f)
			{
				RefreshBossBattleCount(BossBattleEnterCanvas.instance.GetXp() + 1);
				xpLevelUpInfoObject.SetActive(true);
			}
			else
				xpLevelExpText.text = string.Format("{0} / {1}", _current + 1, _max);

			yield return new WaitForSecondsRealtime(0.2f);
		}
		else
		{
			// max 상태일때는 잠시 대기 후 넘어간다.
			yield return new WaitForSecondsRealtime(0.5f);
		}

		SetPointRewardInfo();
		pointRewardGroupObject.SetActive(true);
	}

	#region Point
	int _targetPoint;
	void SetPointRewardInfo()
	{
		if (_clear)
		{
			_targetPoint = _selectedDifficulty;
		}
	}

	public void OnEventIncreasePoint()
	{
		if (_clear == false)
		{
			exitGroupObject.SetActive(true);
			return;
		}

		_pointChangeRemainTime = pointChangeTime;
		_pointChangeSpeed = _targetPoint / _pointChangeRemainTime;
		_currentPoint = 0.0f;
		_updatePointText = true;

		StartCoroutine(PointProcess());
	}

	

	const float pointChangeTime = 0.4f;
	float _pointChangeRemainTime;
	float _pointChangeSpeed;
	float _currentPoint;
	int _lastPoint;
	bool _updatePointText;
	void UpdatePointText()
	{
		if (_updatePointText == false)
			return;

		_currentPoint += _pointChangeSpeed * Time.unscaledDeltaTime;
		int currentPointInt = (int)_currentPoint;
		if (currentPointInt >= _targetPoint)
		{
			currentPointInt = _targetPoint;
			_updatePointText = false;
		}
		if (currentPointInt != _lastPoint)
		{
			_lastPoint = currentPointInt;
			pointText.text = _lastPoint.ToString("N0");
		}
	}
	#endregion

	IEnumerator PointProcess()
	{
		yield return new WaitForSecondsRealtime(pointChangeTime);

		if (_dailyBonusApplied == false)
		{
			pointTextTweenAnimation.DORestart();
			yield return new WaitForSecondsRealtime(0.6f);
		}

		// standby
		yield return new WaitForSecondsRealtime(0.2f);

		// 오늘의 보너스를 받을게 있다면
		if (_dailyBonusApplied)
		{
			int bonusTimes = BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleDailyBonusTimes");
			bonusTimesText.text = string.Format("X{0}", bonusTimes);
			int remainBonusCount = BattleInstanceManager.instance.GetCachedGlobalConstantInt("BossBattleDailyCount") - SubMissionData.instance.bossBattleDailyCount;
			if (remainBonusCount < 0) remainBonusCount = 0;
			remainCountText.text = remainBonusCount.ToString("N0");
			bonusRectObject.SetActive(true);
			yield return new WaitForSecondsRealtime(0.6f);

			_targetPoint *= bonusTimes;

			_pointChangeRemainTime = pointChangeTime;
			_pointChangeSpeed = (_targetPoint - _currentPoint) / _pointChangeRemainTime;
			//_currentPoint = 0.0f;
			_updatePointText = true;
			yield return new WaitForSecondsRealtime(pointChangeTime);

			pointTextTweenAnimation.DORestart();
			yield return new WaitForSecondsRealtime(0.6f);
		}

		ShowKingBossResultText();
		exitGroupObject.SetActive(true);
	}

	void ShowKingBossResultText()
	{
		// 평소에는 안해도 무방할거 같고, 마지막 왕관 보스를 잡았을때만 메세지 한줄 넣기로 한다.
		if (_allKingClear)
		{
			kingBossResultText.SetLocalizedText(UIString.instance.GetString("MissionUI_KingBossMaxCleared"));
			kingBossResultText.gameObject.SetActive(true);
		}
	}

	public void OnClickExitButton()
	{
		SubMissionData.instance.readyToPreloadBossBattleEnterCanvas = true;
		SubMissionData.instance.readyToReopenMissionListCanvas = true;
		SceneManager.LoadScene(0);
	}
}