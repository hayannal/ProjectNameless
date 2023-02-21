using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MEC;

public class AnalysisResultCanvas : MonoBehaviour
{
	public static AnalysisResultCanvas instance;

	public RectTransform analysisRootRectTransform;
	public RectTransform levelUpRootRectTransform;
	public RectTransform goldDiaRootRectTransform;
	public GameObject exitObject;

	public Text expValueText;
	public RectTransform expGroupRectTransform;
	public GameObject expBoostedObject;
	public Text boostValueText;
	public RectTransform boostRootRectTransform;

	public Text levelValueText;
	public DOTweenAnimation levelValueTweenAnimation;
	public RectTransform atkGroupRectTransform;
	public Text atkValueText;
	public DOTweenAnimation atkValueTweenAnimation;
	public RectTransform timeGroupRectTransform;
	public Text timeValueText;
	public DOTweenAnimation timeValueTweenAnimation;
	public GameObject levelUpEffectTextObject;

	public RectTransform goldGroupRectTransform;
	public DOTweenAnimation goldGroupTweenAnimation;
	public Text goldValueText;
	public GameObject goldBigSuccessObject;
	public RectTransform diaGroupRectTransform;
	public Text diaValueText;
	public RectTransform energyGroupRectTransform;
	public Text energyValueText;

	public GameObject contentItemPrefab;
	public RectTransform contentRootRectTransform;

	public class CustomItemContainer : CachedItemHave<RewardIcon>
	{
	}
	CustomItemContainer _container = new CustomItemContainer();

	void Awake()
	{
		instance = this;
	}

	void Start()
	{
		contentItemPrefab.SetActive(false);
	}

	void Update()
	{
		UpdateExpText();
		UpdateBoostText();
		UpdateLevelText();
		UpdateGoldText();
		UpdateDiaText();
		UpdateEnergyText();
	}

	bool _showLevelUp = false;
	int _prevAnalysisLevel;
	public void RefreshInfo(bool showLevelUp, int prevLevel)
	{
		_showLevelUp = showLevelUp;
		_prevAnalysisLevel = prevLevel;

		// 나머지 처리는 항상 동일
		analysisRootRectTransform.gameObject.SetActive(false);
		expBoostedObject.SetActive(false);
		boostRootRectTransform.gameObject.SetActive(false);
		levelUpRootRectTransform.gameObject.SetActive(false);
		goldDiaRootRectTransform.gameObject.SetActive(false);
		for (int i = 0; i < _listRewardIcon.Count; ++i)
			_listRewardIcon[i].gameObject.SetActive(false);
		_listRewardIcon.Clear();
		exitObject.SetActive(false);

		Timing.RunCoroutine(RewardProcess());
	}

	public static string GetMaxTimeText(int tableMaxTime)
	{
		int maxTimeMinute = tableMaxTime / 60;
		if (maxTimeMinute < 60)
			return string.Format("{0}m", maxTimeMinute);
		else
			return string.Format("{0}h", maxTimeMinute / 60);
	}

	int _addExp;
	int _addLevel;
	bool _goldBigSuccess;
	int _addGold;
	int _addDia;
	int _addEnergy;
	List<RewardIcon> _listRewardIcon = new List<RewardIcon>();
	IEnumerator<float> RewardProcess()
	{
		// 0.1초 초기화 대기 후 시작
		yield return Timing.WaitForSeconds(0.1f);

		expValueText.text = "0s";
		analysisRootRectTransform.gameObject.SetActive(true);
		AnalysisTableData prevAnalysisTableData = TableDataManager.instance.FindAnalysisTableData(_prevAnalysisLevel);
		AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(AnalysisData.instance.analysisLevel);
		if (_showLevelUp)
		{
			// 레벨업 하기 전 값으로 셋팅 후 Show
			int prevMaxTime = 0;
			if (prevAnalysisTableData != null)
			{
				levelValueText.text = _prevAnalysisLevel.ToString();
				atkValueText.text = prevAnalysisTableData.accumulatedAtk.ToString("N0");
				timeValueText.text = GetMaxTimeText(prevAnalysisTableData.maxTime);
				prevMaxTime = prevAnalysisTableData.maxTime;
			}
			levelUpEffectTextObject.SetActive(false);
			levelUpRootRectTransform.gameObject.SetActive(true);
			atkGroupRectTransform.gameObject.SetActive(true);
			timeGroupRectTransform.gameObject.SetActive(analysisTableData != null && prevMaxTime != analysisTableData.maxTime);
		}
		levelUpRootRectTransform.gameObject.SetActive(_showLevelUp);
		yield return Timing.WaitForSeconds(0.1f);

		// exp 연출을 제일 먼저 한다.
		_addExp = ResearchInfoAnalysisCanvas.instance.cachedExpSecond;
		_expChangeRemainTime = expChangeTime;
		_expChangeSpeed = _addExp / _expChangeRemainTime;
		_currentExp = 0.0f;
		_updateExpText = true;
		yield return Timing.WaitForSeconds(expChangeTime);

		// 경험치 부스터 영역은 항상 나온다.
		int remainBoost = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.AnalysisBoost);
		remainBoost += ResearchInfoAnalysisCanvas.instance.cachedBoostUses;
		boostValueText.text = GetTimeString(remainBoost);
		boostValueText.color = (remainBoost == 0) ? new Color(0.8f, 0.0f, 0.0f) : new Color(0.0f, 0.895f, 0.895f);
		boostRootRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForSeconds(0.3f);

		// 적용해야할 부스터가 있다면 처리를 하고 없으면 바로 다음으로 넘어간다.
		if (ResearchInfoAnalysisCanvas.instance.cachedBoostUses > 0)
		{
			// 부스트 표시
			expBoostedObject.SetActive(true);
			yield return Timing.WaitForSeconds(0.6f);

			// 이후 
			_addExp = ResearchInfoAnalysisCanvas.instance.cachedExpSecondBoosted;
			_expChangeRemainTime = expChangeTime;
			_expChangeSpeed = (_addExp - _currentExp) / _expChangeRemainTime;
			_updateExpText = true;

			_targetBoost = CashShopData.instance.GetCashItemCount(CashShopData.eCashItemCountType.AnalysisBoost);
			_boostChangeSpeed = ResearchInfoAnalysisCanvas.instance.cachedBoostUses / _expChangeRemainTime;
			_currentBoost = remainBoost;
			_updateBoostText = true;
			yield return Timing.WaitForSeconds(expChangeTime);
		}

		if (_showLevelUp)
		{
			// 숫자 변하는 
			_addLevel = AnalysisData.instance.analysisLevel - _prevAnalysisLevel;
			_levelChangeRemainTime = levelChangeTime;
			_levelChangeSpeed = _addLevel / _levelChangeRemainTime;
			_currentLevel = 0.0f;
			_updateLevelText = true;
			yield return Timing.WaitForSeconds(levelChangeTime);

			// 레벨 마지막으로 변하는 타이밍 맞춰서 시간도 변경
			if (atkGroupRectTransform.gameObject.activeSelf)
				atkValueText.text = analysisTableData.accumulatedAtk.ToString("N0");
			if (timeGroupRectTransform.gameObject.activeSelf)
				timeValueText.text = GetMaxTimeText(analysisTableData.maxTime);

			// 여긴 변경 후 잠시 대기
			yield return Timing.WaitForSeconds((_addLevel > 1) ? 0.3f : 0.05f);

			// 숫자 스케일 점프 애니
			levelValueTweenAnimation.DORestart();
			atkValueTweenAnimation.DORestart();
			timeValueTweenAnimation.DORestart();
			yield return Timing.WaitForSeconds(0.6f);

			levelUpEffectTextObject.SetActive(true);
		}

		// 재화쪽은 언제나 나온다.
		yield return Timing.WaitForSeconds(0.2f);

		// 결과값들은 다 여기에 있다. 이 값 보고 판단해서 보여줄거 보여주면 된다.
		int resultGold = ResearchInfoAnalysisCanvas.instance.cachedResultGold;
		int resultDia = ResearchInfoAnalysisCanvas.instance.cachedResultDia;
		int resultEnergy = ResearchInfoAnalysisCanvas.instance.cachedResultEnergy;

		// SimpleResult에서 했던거처럼 값이 0보다 큰 것들만 보여주고 숫자가 증가하게 한다.
		_goldBigSuccess = false;
		_addGold = resultGold;
		_addDia = resultDia;
		_addEnergy = resultEnergy;
		goldGroupRectTransform.gameObject.SetActive(_addGold > 0);
		diaGroupRectTransform.gameObject.SetActive(_addDia > 0);
		energyGroupRectTransform.gameObject.SetActive(_addEnergy > 0);

		if (_addGold > 0)
		{
			goldValueText.text = "0";
			_goldChangeRemainTime = goldChangeTime;
			_goldChangeSpeed = _addGold / _goldChangeRemainTime;
			_currentGold = 0.0f;

			goldBigSuccessObject.SetActive(false);
		}

		if (_addDia > 0)
		{
			diaValueText.text = "0";
			_diaChangeRemainTime = diaChangeTime;
			_diaChangeSpeed = _addDia / _diaChangeRemainTime;
			_currentDia = 0.0f;
		}

		if (_addEnergy > 0)
		{
			energyValueText.text = "0";
			_energyChangeRemainTime = energyChangeTime;
			_energyChangeSpeed = _addEnergy / _energyChangeRemainTime;
			_currentEnergy = 0.0f;
		}

		goldDiaRootRectTransform.localScale = Vector3.zero;
		goldDiaRootRectTransform.gameObject.SetActive(true);
		yield return Timing.WaitForOneFrame;
		goldGroupTweenAnimation.DORestart();
		yield return Timing.WaitForSeconds(0.4f);

		if (_addGold > 0) _updateGoldText = true;
		if (_addDia > 0) _updateDiaText = true;
		if (_addEnergy > 0) _updateEnergyText = true;
		yield return Timing.WaitForSeconds(0.2f);

		// list
		for (int i = 0; i < ResearchInfoAnalysisCanvas.instance.cachedResultItemValue.Count; ++i)
		{
			RewardIcon rewardIconItem = _container.GetCachedItem(contentItemPrefab, contentRootRectTransform);
			rewardIconItem.RefreshReward("it", ResearchInfoAnalysisCanvas.instance.cachedResultItemValue[i], ResearchInfoAnalysisCanvas.instance.cachedResultItemCount[i]);
			_listRewardIcon.Add(rewardIconItem);
			yield return Timing.WaitForSeconds(0.1f);
		}

		ResearchInfoAnalysisCanvas.instance.CheckShowChangePower();
		yield return Timing.WaitForSeconds(0.8f);

		exitObject.SetActive(true);

		// 모든 표시가 끝나면 DropManager에 있는 정보를 강제로 초기화 시켜줘야한다.
		// DropManager.instance.ClearLobbyDropInfo(); 대신 
		ResearchInfoAnalysisCanvas.instance.ClearCachedInfo();
	}

	public void OnClickExitButton()
	{
		analysisRootRectTransform.gameObject.SetActive(false);
		boostRootRectTransform.gameObject.SetActive(false);
		levelUpRootRectTransform.gameObject.SetActive(false);
		goldDiaRootRectTransform.gameObject.SetActive(false);
		for (int i = 0; i < _listRewardIcon.Count; ++i)
			_listRewardIcon[i].gameObject.SetActive(false);
		_listRewardIcon.Clear();
		exitObject.SetActive(false);
		gameObject.SetActive(false);

		ResearchInfoAnalysisCanvas.instance.RefreshBoostInfo();
		if (ResearchInfoAnalysisCanvas.instance.cachedResultItemValue != null && ResearchInfoAnalysisCanvas.instance.cachedResultItemValue.Count > 0 &&
			ResearchInfoAnalysisCanvas.instance.cachedResultItemValue.Count == ResearchInfoAnalysisCanvas.instance.cachedResultItemCount.Count)
		{
			ConsumeProductProcessor.instance.ConsumeGacha(ResearchInfoAnalysisCanvas.instance.cachedResultItemValue, ResearchInfoAnalysisCanvas.instance.cachedResultItemCount);
		}
	}


	#region Gold Dia Energy Increase
	const float expChangeTime = 0.6f;
	float _expChangeRemainTime;
	float _expChangeSpeed;
	float _currentExp;
	int _lastExp;
	bool _updateExpText;
	void UpdateExpText()
	{
		if (_updateExpText == false)
			return;

		_currentExp += _expChangeSpeed * Time.unscaledDeltaTime;
		int currentExpInt = (int)_currentExp;
		if (currentExpInt >= _addExp)
		{
			currentExpInt = _addExp;
			_updateExpText = false;
		}
		if (currentExpInt != _lastExp)
		{
			_lastExp = currentExpInt;
			expValueText.text = GetTimeString(_lastExp);
		}
	}

	int _targetBoost;
	float _boostChangeSpeed;
	float _currentBoost;
	int _lastBoost;
	bool _updateBoostText;
	void UpdateBoostText()
	{
		if (_updateBoostText == false)
			return;

		_currentBoost -= _boostChangeSpeed * Time.unscaledDeltaTime;
		int currentBoostInt = (int)_currentBoost;
		if (currentBoostInt <= _targetBoost)
		{
			currentBoostInt = _targetBoost;
			_updateBoostText = false;
		}
		if (currentBoostInt != _lastBoost)
		{
			_lastBoost = currentBoostInt;
			boostValueText.text = GetTimeString(_lastBoost);
			boostValueText.color = (_lastBoost == 0) ? new Color(0.8f, 0.0f, 0.0f) : new Color(0.0f, 0.895f, 0.895f);
		}
	}

	public static string GetTimeString(int remainTime, bool shopItem = false)
	{
		string result = "";
		int min = remainTime / 60;
		int hour = min / 60;
		int day = hour / 24;
		int sec = remainTime % 60;
		min = min % 60;
		hour = hour % 24;

		if (shopItem)
		{
			if (day > 0) result = string.Format("{0}d", day);
			if (hour > 0) result = string.Format("{0} {1}h", result, hour);
			if (min > 0) result = string.Format("{0} {1}m", result, min);
			if (sec > 0) result = string.Format("{0} {1}m", result, sec);
			return result;
		}

		if (day > 0) result = string.Format("{0}d {1}h {2}m {3}s", day, hour, min, sec);
		else if (hour > 0) result = string.Format("{0}h {1}m {2}s", hour, min, sec);
		else result = string.Format("{0}m {1}s", min, sec);
		return result;
	}

	const float levelChangeTime = 0.6f;
	float _levelChangeRemainTime;
	float _levelChangeSpeed;
	float _currentLevel;
	int _lastLevel;
	bool _updateLevelText;
	void UpdateLevelText()
	{
		if (_updateLevelText == false)
			return;

		_currentLevel += _levelChangeSpeed * Time.unscaledDeltaTime;
		int currentLevelInt = (int)_currentLevel;
		if (currentLevelInt >= _addLevel)
		{
			currentLevelInt = _addLevel;
			_updateLevelText = false;
		}
		if (currentLevelInt != _lastLevel)
		{
			_lastLevel = currentLevelInt;
			levelValueText.text = (_lastLevel + _prevAnalysisLevel).ToString("N0");
		}
	}


	const float diaChangeTime = 0.6f;
	float _diaChangeRemainTime;
	float _diaChangeSpeed;
	float _currentDia;
	int _lastDia;
	bool _updateDiaText;
	void UpdateDiaText()
	{
		if (_updateDiaText == false)
			return;

		_currentDia += _diaChangeSpeed * Time.deltaTime;
		int currentDiaInt = (int)_currentDia;
		if (currentDiaInt >= _addDia)
		{
			currentDiaInt = _addDia;
			_updateDiaText = false;
		}
		if (currentDiaInt != _lastDia)
		{
			_lastDia = currentDiaInt;
			diaValueText.text = _lastDia.ToString("N0");
		}
	}

	const float goldChangeTime = 0.6f;
	float _goldChangeRemainTime;
	float _goldChangeSpeed;
	float _currentGold;
	int _lastGold;
	bool _updateGoldText;
	void UpdateGoldText()
	{
		if (_updateGoldText == false)
			return;

		_currentGold += _goldChangeSpeed * Time.unscaledDeltaTime;
		int currentGoldInt = (int)_currentGold;
		if (currentGoldInt >= _addGold)
		{
			currentGoldInt = _addGold;
			_updateGoldText = false;

			if (_goldBigSuccess) goldBigSuccessObject.SetActive(true);
		}
		if (currentGoldInt != _lastGold)
		{
			_lastGold = currentGoldInt;
			goldValueText.text = _lastGold.ToString("N0");
		}
	}

	const float energyChangeTime = 0.6f;
	float _energyChangeRemainTime;
	float _energyChangeSpeed;
	float _currentEnergy;
	int _lastEnergy;
	bool _updateEnergyText;
	void UpdateEnergyText()
	{
		if (_updateEnergyText == false)
			return;

		_currentEnergy += _energyChangeSpeed * Time.unscaledDeltaTime;
		int currentEnergyInt = (int)_currentEnergy;
		if (currentEnergyInt >= _addEnergy)
		{
			currentEnergyInt = _addEnergy;
			_updateEnergyText = false;
		}
		if (currentEnergyInt != _lastEnergy)
		{
			_lastEnergy = currentEnergyInt;
			energyValueText.text = _lastEnergy.ToString("N0");
		}
	}
	#endregion
}