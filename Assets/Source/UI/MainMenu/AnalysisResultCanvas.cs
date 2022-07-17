using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using MEC;

public class AnalysisResultCanvas : MonoBehaviour
{
	public static AnalysisResultCanvas instance;

	public RectTransform emptyRootRectTransform;
	public RectTransform levelUpRootRectTransform;
	public RectTransform goldDiaRootRectTransform;
	public GameObject exitObject;

	public Text levelValueText;
	public DOTweenAnimation levelValueTweenAnimation;
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

	void Awake()
	{
		instance = this;
	}

	void Update()
	{
		UpdateLevelText();
		UpdateGoldText();
		UpdateDiaText();
		UpdateEnergyText();
	}

	bool _showLevelUp = false;
	bool _showAnalysisResult = false;
	int _prevAnalysisLevel;
	public void RefreshInfo(bool showLevelUp, int prevLevel, bool showAnalysisResult)
	{
		_showLevelUp = showLevelUp;
		_showAnalysisResult = showAnalysisResult;
		_prevAnalysisLevel = prevLevel;

		// 둘다 없는데 왜 호출한거지
		if (showLevelUp == false && showAnalysisResult == false)
		{
			gameObject.SetActive(false);
			return;
		}

		// 캐릭터 결과창 영역이 빠지면서 그냥 항상 켜는게 나을거 같아서 수정해둔다.
		emptyRootRectTransform.gameObject.SetActive(true);

		// 나머지 처리는 항상 동일
		levelUpRootRectTransform.gameObject.SetActive(false);
		goldDiaRootRectTransform.gameObject.SetActive(false);
		exitObject.SetActive(false);

		Timing.RunCoroutine(RewardProcess());
	}

	int _addLevel;
	bool _goldBigSuccess;
	int _addGold;
	int _addDia;
	int _addEnergy;
	IEnumerator<float> RewardProcess()
	{
		// 0.1초 초기화 대기 후 시작
		yield return Timing.WaitForSeconds(0.1f);

		if (_showLevelUp)
		{
			// 레벨업 하기 전 값으로 셋팅 후 Show
			int prevMaxTime = 0;
			AnalysisTableData prevAnalysisTableData = TableDataManager.instance.FindAnalysisTableData(_prevAnalysisLevel);
			if (prevAnalysisTableData != null)
			{
				levelValueText.text = _prevAnalysisLevel.ToString();
				timeValueText.text = AnalysisLevelUpCanvas.GetMaxTimeText(prevAnalysisTableData.maxTime);
				prevMaxTime = prevAnalysisTableData.maxTime;
			}
			levelUpEffectTextObject.SetActive(false);
			levelUpRootRectTransform.gameObject.SetActive(true);
			AnalysisTableData analysisTableData = TableDataManager.instance.FindAnalysisTableData(AnalysisData.instance.analysisLevel);
			timeGroupRectTransform.gameObject.SetActive(analysisTableData != null && prevMaxTime != analysisTableData.maxTime);
			yield return Timing.WaitForSeconds(0.4f);

			// 숫자 변하는 
			_addLevel = AnalysisData.instance.analysisLevel - _prevAnalysisLevel;
			_levelChangeRemainTime = levelChangeTime;
			_levelChangeSpeed = _addLevel / _levelChangeRemainTime;
			_currentLevel = 0.0f;
			_updateLevelText = true;
			yield return Timing.WaitForSeconds(levelChangeTime);

			// 레벨 마지막으로 변하는 타이밍 맞춰서 시간도 변경
			if (timeGroupRectTransform.gameObject.activeSelf)
				timeValueText.text = AnalysisLevelUpCanvas.GetMaxTimeText(analysisTableData.maxTime);

			// 여긴 변경 후 잠시 대기
			yield return Timing.WaitForSeconds((_addLevel > 1) ? 0.3f : 0.05f);

			// 숫자 스케일 점프 애니
			levelValueTweenAnimation.DORestart();
			timeValueTweenAnimation.DORestart();
			yield return Timing.WaitForSeconds(0.6f);

			levelUpEffectTextObject.SetActive(true);
		}
		
		if (_showAnalysisResult)
		{
			yield return Timing.WaitForSeconds(0.2f);

			// 결과값들은 다 여기에 있다. 이 값 보고 판단해서 보여줄거 보여주면 된다.
			int resultGold = AnalysisData.instance.cachedResultGold;
			
			// SimpleResult에서 했던거처럼 값이 0보다 큰 것들만 보여주고 숫자가 증가하게 한다.
			_goldBigSuccess = false;
			_addGold = resultGold;
			_addDia = 0;
			_addEnergy = 0;
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
			yield return Timing.WaitForSeconds(0.6f);
		}

		yield return Timing.WaitForSeconds(0.5f);

		exitObject.SetActive(true);

		// 모든 표시가 끝나면 DropManager에 있는 정보를 강제로 초기화 시켜줘야한다.
		// DropManager.instance.ClearLobbyDropInfo(); 대신 
		AnalysisData.instance.ClearCachedInfo();
	}

	public void OnClickExitButton()
	{
		emptyRootRectTransform.gameObject.SetActive(false);
		levelUpRootRectTransform.gameObject.SetActive(false);
		goldDiaRootRectTransform.gameObject.SetActive(false);
		exitObject.SetActive(false);
		gameObject.SetActive(false);
	}


	#region Gold Dia Energy Increase
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