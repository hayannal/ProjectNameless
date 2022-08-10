using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GachaObjects : MonoBehaviour
{
	public static GachaObjects instance;
	
	public Transform effectRootTransform;

	public GameObject[] gachaResultObjectList;
	public GameObject[] gachaResultEffectObjectList;

	void Awake()
	{
		instance = this;
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