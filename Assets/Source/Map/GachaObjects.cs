using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class GachaObjects : MonoBehaviour
{
	public static GachaObjects instance;
	
	public GameObject[] effectRootObjectList;
	public float[] effectWaitTimeList;
	public Transform eventPointObjectRootTransform;

	public GameObject[] gachaResultObjectList;
	public GameObject[] gachaResultEffectObjectList;

	public NumberXObject eventPointNumberXObject;
	public NumberXObject brokenEnergyNumberXObject;

	public GameObject objectDisappearEffectObject;

	public Text gachaResultText;
	public DOTweenAnimation gachaResultTweenAnimation;
	public Transform gachaResultTransform;

	public Text stageText;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		stageText.text = string.Format("STAGE <size=18>{0}</size>", StageManager.instance.currentFloor);
	}

	class RandomGachaEffectInfo
	{
		public int effectLengthIndex;
		public float sumWeight;
	}
	List<RandomGachaEffectInfo> _listGachaEffectInfo = null;

	public GameObject ShowSummonWaitEffect(GachaInfoCanvas.eGachaResult gachaResult, ref float effectWaitTime)
	{
		string stringId = ((int)gachaResult).ToString();
		SummonTypeTableData summonTypeTableData = TableDataManager.instance.FindeSummonTypeTableData(stringId);
		if (summonTypeTableData == null)
			return null;

		if (_listGachaEffectInfo == null)
			_listGachaEffectInfo = new List<RandomGachaEffectInfo>();
		_listGachaEffectInfo.Clear();

		float sumWeight = 0.0f;
		for (int i = 0; i < 4; ++i)
		{
			float weight = 0.0f;
			switch (i)
			{
				case 0: weight = summonTypeTableData.effectLen_1; break;
				case 1: weight = summonTypeTableData.effectLen_2; break;
				case 2: weight = summonTypeTableData.effectLen_3; break;
				case 3: weight = summonTypeTableData.effectLen_4; break;
			}
			if (weight <= 0.0f)
				continue;

			sumWeight += weight;
			RandomGachaEffectInfo newInfo = new RandomGachaEffectInfo();
			newInfo.effectLengthIndex = i;
			newInfo.sumWeight = sumWeight;
			_listGachaEffectInfo.Add(newInfo);
		}

		if (_listGachaEffectInfo.Count == 0)
			return effectRootObjectList[0];

		int index = -1;
		float random = UnityEngine.Random.Range(0.0f, _listGachaEffectInfo[_listGachaEffectInfo.Count - 1].sumWeight);
		for (int i = 0; i < _listGachaEffectInfo.Count; ++i)
		{
			if (random <= _listGachaEffectInfo[i].sumWeight)
			{
				index = i;
				break;
			}
		}
		if (index == -1)
			return effectRootObjectList[0];

		int effectLengthIndex = _listGachaEffectInfo[index].effectLengthIndex;
		if (effectLengthIndex < effectRootObjectList.Length)
		{
			effectWaitTime = effectWaitTimeList[effectLengthIndex];
			effectRootObjectList[effectLengthIndex].SetActive(true);
			return effectRootObjectList[effectLengthIndex];
		}
		return effectRootObjectList[0];
	}

	GameObject _eventPointObject;
	public void SetEventPointPrefab(GameObject prefab)
	{
		if (_eventPointObject != null)
			_eventPointObject.SetActive(false);

		_eventPointObject = BattleInstanceManager.instance.GetCachedObject(prefab, eventPointObjectRootTransform);
	}

	public void ShowSummonResultObject(bool show, GachaInfoCanvas.eGachaResult gachaResult)
	{
		int index = (int)gachaResult;
		if (index < gachaResultObjectList.Length && gachaResultObjectList[index] != null)
		{
			gachaResultObjectList[index].SetActive(show);
		}

		if (show)
		{
			switch (gachaResult)
			{
				case GachaInfoCanvas.eGachaResult.EventPoint1: eventPointNumberXObject.cachedTransform.localScale = Vector3.one; eventPointNumberXObject.SetNumber(1); break;
				case GachaInfoCanvas.eGachaResult.EventPoint2: eventPointNumberXObject.cachedTransform.localScale = Vector3.one; eventPointNumberXObject.SetNumber(2); break;
				case GachaInfoCanvas.eGachaResult.EventPoint9: eventPointNumberXObject.cachedTransform.localScale = Vector3.one; eventPointNumberXObject.SetNumber(5); break;
				case GachaInfoCanvas.eGachaResult.BrokenEnergy1: brokenEnergyNumberXObject.SetNumber(1); break;
				case GachaInfoCanvas.eGachaResult.BrokenEnergy2: brokenEnergyNumberXObject.SetNumber(2); break;
				case GachaInfoCanvas.eGachaResult.BrokenEnergy3: brokenEnergyNumberXObject.SetNumber(3); break;
			}
		}
	}

	public void ShowSummonResultEffectObject(GachaInfoCanvas.eGachaResult gachaResult)
	{
		int index = (int)gachaResult;
		if (index < gachaResultEffectObjectList.Length && gachaResultEffectObjectList[index] != null)
			gachaResultEffectObjectList[index].SetActive(true);
	}

	public void HideNumberXObject(GachaInfoCanvas.eGachaResult gachaResult)
	{
		switch (gachaResult)
		{
			case GachaInfoCanvas.eGachaResult.EventPoint1:
			case GachaInfoCanvas.eGachaResult.EventPoint2:
			case GachaInfoCanvas.eGachaResult.EventPoint9: eventPointNumberXObject.cachedTransform.DOScale(0.0f, 0.2f); break;
		}
	}

	public void GetObject(GachaInfoCanvas.eGachaResult gachaResult)
	{
		switch (gachaResult)
		{
			// 골드는 전용 획득 이펙트와 함께 그자리에서 사라진다.
			case GachaInfoCanvas.eGachaResult.Gold1:
			case GachaInfoCanvas.eGachaResult.Gold2:
			case GachaInfoCanvas.eGachaResult.Gold10:
			// 에너지랑 나머지 기타 등등은 다 사라지는 이펙트로 간단하게 때우기로 한다.
			case GachaInfoCanvas.eGachaResult.Energy10:
			case GachaInfoCanvas.eGachaResult.BrokenEnergy1:
			case GachaInfoCanvas.eGachaResult.BrokenEnergy2:
			case GachaInfoCanvas.eGachaResult.BrokenEnergy3:
			case GachaInfoCanvas.eGachaResult.Junk1:
			case GachaInfoCanvas.eGachaResult.Junk2:
			case GachaInfoCanvas.eGachaResult.Junk3:
				gachaResultObjectList[(int)gachaResult].SetActive(false);
				objectDisappearEffectObject.SetActive(true);
				return;
		}

		int index = (int)gachaResult;
		if (index < gachaResultObjectList.Length && gachaResultObjectList[index] != null)
		{
			float targetScale = 1.0f;
			Vector3 targetPosition = new Vector3(0.2f, -1.0f, -5.0f);
			switch (gachaResult)
			{
				case GachaInfoCanvas.eGachaResult.EventPoint1:
				case GachaInfoCanvas.eGachaResult.EventPoint2:
				case GachaInfoCanvas.eGachaResult.EventPoint9:
					targetPosition = new Vector3(2.0f, -3.0f, 0.0f);
					targetScale = 0.05f;
					break;
			}
			if (targetScale != 1.0f)
				gachaResultObjectList[index].transform.DOScale(targetScale, 0.7f);
			gachaResultObjectList[index].transform.DOLocalJump(targetPosition, 2.0f, 1, 1.2f).OnComplete(() =>
			{
				gachaResultObjectList[index].transform.localScale = Vector3.one;
				gachaResultObjectList[index].transform.localPosition = Vector3.zero;
				gachaResultObjectList[index].SetActive(false);
			});
		}
	}

	public void SetResultText(bool show, int value, Color color)
	{
		if (show)
		{
			gachaResultText.text = value.ToString("N0");
			gachaResultText.color = color;
			gachaResultTweenAnimation.DORestart();
		}
		else
		{
			gachaResultTransform.DOScale(0.0f, 0.2f);
		}
	}
}