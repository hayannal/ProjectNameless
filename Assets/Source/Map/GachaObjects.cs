using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GachaObjects : MonoBehaviour
{
	public static GachaObjects instance;
	
	public GameObject[] effectRootObjectList;
	public float[] effectWaitTimeList;

	public GameObject[] gachaResultObjectList;
	public GameObject[] gachaResultEffectObjectList;

	void Awake()
	{
		instance = this;
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
		GachaTypeTableData gachaTypeTableData = TableDataManager.instance.FindeGachaTypeTableData(stringId);
		if (gachaTypeTableData == null)
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
				case 0: weight = gachaTypeTableData.effectLen_1; break;
				case 1: weight = gachaTypeTableData.effectLen_2; break;
				case 2: weight = gachaTypeTableData.effectLen_3; break;
				case 3: weight = gachaTypeTableData.effectLen_4; break;
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

	public void ShowSummonResultObject(bool show, GachaInfoCanvas.eGachaResult gachaResult)
	{
		int index = (int)gachaResult;
		if (index < gachaResultObjectList.Length && gachaResultObjectList[index] != null)
		{
			gachaResultObjectList[index].SetActive(show);
		}
	}

	public void ShowSummonResultEffectObject(GachaInfoCanvas.eGachaResult gachaResult)
	{
		int index = (int)gachaResult;
		if (index < gachaResultEffectObjectList.Length && gachaResultEffectObjectList[index] != null)
			gachaResultEffectObjectList[index].SetActive(true);
	}

	public void GetObject(GachaInfoCanvas.eGachaResult gachaResult)
	{
		int index = (int)gachaResult;
		if (index < gachaResultObjectList.Length && gachaResultObjectList[index] != null)
		{
			gachaResultObjectList[index].transform.DOScale(0.05f, 0.7f);
			gachaResultObjectList[index].transform.DOLocalJump(new Vector3(0.2f, -1.0f, -7.0f), 2.0f, 1, 1.2f).OnComplete(() =>
			{
				gachaResultObjectList[index].transform.localScale = Vector3.one;
				gachaResultObjectList[index].transform.localPosition = Vector3.zero;
				gachaResultObjectList[index].SetActive(false);
			});
		}
	}
}