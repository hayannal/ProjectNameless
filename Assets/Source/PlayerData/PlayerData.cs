using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeStage.AntiCheat.ObscuredTypes;
using PlayFab;
using PlayFab.ClientModels;
using PlayFab.DataModels;

public class PlayerData : MonoBehaviour
{
	public static PlayerData instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = (new GameObject("PlayerData")).AddComponent<PlayerData>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static PlayerData _instance = null;

	public bool loginned { get; private set; }
	public bool newlyCreated { get; private set; }
	public bool clientOnly { get; private set; }

#if UNITY_IOS
	// 심사빌드인지 체크해두는 변수
	public bool reviewVersion { get; set; }
#endif

	public void ResetData()
	{
		// 이게 가장 중요. 다른 것들은 받을때 알아서 다 비우고 다시 셋팅한다.
		loginned = false;
		newlyCreated = false;

		/*
		lobbyDownloadState = false;

		// OnRecvPlayerData 함수들 두번 받아도 아무 문제없게 짜두면 여기서 딱히 할일은 없을거다.
		// 두번 받는거 뿐만 아니라 모든 변수를 다 덮어서 기록하는지도 확인하면 완벽하다.(건너뛰면 이전값이 남을테니 위험)
		//
		// 대신 진입시에 앱구동처럼 처리하기 위해 재시작 플래그를 여기서 걸어둔다.
		checkRestartScene = true;
		*/
	}
}