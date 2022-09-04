using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using MEC;

public class GoldBoxRoomGround : MonoBehaviour
{
	public static GoldBoxRoomGround instance;

	public Animator[] boxAnimatorList;
	public Transform[] openTransformList;
	public GameObject[] fakeLimMeshObjectList;
	public GameObject[] existGoldObjectList;
	public GameObject[] noneGoldObjectList;

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
		for (int i = 0; i < worldButtonTransformList.Length; ++i)
			worldButtonTransformList[i].position = new Vector3(boxAnimatorList[i].transform.position.x, worldButtonTransformList[i].position.y, boxAnimatorList[i].transform.position.z);

		_defaultOpenTransformRotation = new Vector3[openTransformList.Length];
		for (int i = 0; i < openTransformList.Length; ++i)
			_defaultOpenTransformRotation[i] = openTransformList[i].localEulerAngles;
	}

	List<int> _listPercentIndex = new List<int>();
	int _openCount;
	ObscuredInt _resultGold;
	void OnEnable()
	{
		_openCount = 0;
		_resultGold = 0;
		for (int i = 0; i < boxAnimatorList.Length; ++i)
		{
			if (boxAnimatorList[i].enabled)
				boxAnimatorList[i].Play("Reset");
			_resetRemainTime = 0.75f;
		}

		// 현재 골드는 Betting쪽에 저장되어있을거다.
		// 이거 가져와서
		// 박스 4개중에 3개를 선별해서 66퍼 16퍼 16퍼를 넣어둔다.
		// 꽝인 박스는 일부 오브젝트를 숨겨서 비어있는척 한다.
		_listPercentIndex.Clear();
		for (int i = 0; i < boxAnimatorList.Length; ++i)
			_listPercentIndex.Add(i);
		ObjectUtil.Shuffle<int>(_listPercentIndex);

		int emptyBoxIndex = _listPercentIndex[_listPercentIndex.Count - 1];
		for (int i = 0; i < fakeLimMeshObjectList.Length; ++i)
		{
			fakeLimMeshObjectList[i].SetActive(emptyBoxIndex != i);
			existGoldObjectList[i].SetActive(emptyBoxIndex != i);
			noneGoldObjectList[i].SetActive(emptyBoxIndex == i);
		}
	}

	void OnDisable()
	{
		for (int i = 0; i < openTransformList.Length; ++i)
			openTransformList[i].localEulerAngles = _defaultOpenTransformRotation[i];
	}

	float _resetRemainTime;
	void Update()
	{
		if (_resetRemainTime > 0.0f)
		{
			_resetRemainTime -= Time.deltaTime;
			if (_resetRemainTime <= 0.0f)
			{
				_resetRemainTime = 0.0f;

				for (int i = 0; i < boxAnimatorList.Length; ++i)
				{
					if (boxAnimatorList[i].enabled)
						boxAnimatorList[i].enabled = false;
				}
			}
		}
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
		if (_openCount == 3)
			return;

		Debug.LogFormat("World Canvas Button Input : {0}", index);

		if (boxAnimatorList[index].enabled)
			return;
		boxAnimatorList[index].enabled = true;
		boxAnimatorList[index].Play("ChestOpenAnimation");

		int getGold = 0;
		if (_listPercentIndex[0] == index)
		{
			getGold = CurrencyData.instance.currentGoldBoxRoomReward - (int)(CurrencyData.instance.currentGoldBoxRoomReward * 0.1666f) - (int)(CurrencyData.instance.currentGoldBoxRoomReward * 0.1666f);
			Debug.LogFormat("66% : {0:N0}", getGold);
		}
		else if (_listPercentIndex[1] == index)
		{
			getGold = (int)(CurrencyData.instance.currentGoldBoxRoomReward * 0.1666f);
			Debug.LogFormat("16% : {0:N0}", getGold);
		}
		else if (_listPercentIndex[2] == index)
		{
			getGold = (int)(CurrencyData.instance.currentGoldBoxRoomReward * 0.1666f);
			Debug.LogFormat("16% : {0:N0}", getGold);
		}
		_resultGold += getGold;
		++_openCount;
		if (GoldBoxRoomCanvas.instance != null)
			GoldBoxRoomCanvas.instance.UseKey();
		if (getGold > 0 && GoldBoxRoomCanvas.instance != null)
			GoldBoxRoomCanvas.instance.SetStoleValue(_resultGold);
		if (getGold > 0)
			ToastNumberCanvas.instance.ShowToast(getGold.ToString("N0"), 2.0f);

		if (_openCount == 3)
		{
			int betRate = GachaInfoCanvas.instance.GetBetRate();

			// 3번째 클릭하자마자 패킷을 보내서 결과 제대로 저장되면
			PlayFabApiManager.instance.RequestEndBettingRoom(_resultGold * betRate, () =>
			{
				Timing.RunCoroutine(ReturnProcess());
			});
		}
	}

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
			GoldBoxRoomCanvas.instance.PunchAnimateWinText();
			yield return Timing.WaitForSeconds(0.8f);

			GoldBoxRoomCanvas.instance.ScaleZeroWinText();
			GoldBoxRoomCanvas.instance.SetStoleValue(_resultGold * betRate, true);
			yield return Timing.WaitForSeconds(1.5f);
		}

		FadeCanvas.instance.FadeOut(0.2f, 1.0f, true);
		yield return Timing.WaitForSeconds(0.2f);

		GoldBoxRoomCanvas.instance.gameObject.SetActive(false);

		yield return Timing.WaitForSeconds(0.1f);
		
		// 가차 캔버스로 돌아가야한다.
		GachaCanvas.instance.gameObject.SetActive(true);

		// 
		FadeCanvas.instance.FadeIn(0.5f);

		yield return Timing.WaitForSeconds(0.3f);
		GachaInfoCanvas.instance.OnPostProcess();
	}
}