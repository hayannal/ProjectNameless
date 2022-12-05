using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetInfoGround : MonoBehaviour
{
	public static PetInfoGround instance;

	void Awake()
	{
		instance = this;
	}

	public PetBattleInfo petBattleInfo;

	public GameObject maxCountUpEffectPrefab;
	public Transform maxCountUpEffectRootTransform;

	public void ShowMaxCountUpEffect()
	{
		BattleInstanceManager.instance.GetCachedObject(maxCountUpEffectPrefab, maxCountUpEffectRootTransform);
	}
}