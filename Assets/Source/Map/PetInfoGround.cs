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
}