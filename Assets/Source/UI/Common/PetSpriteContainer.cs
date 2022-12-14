using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PetSpriteContainer : MonoBehaviour
{
	public static PetSpriteContainer instance;

	void Awake()
	{
		instance = this;
	}

	public Sprite[] spriteList;

	public Sprite FindSprite(string name)
	{
		for (int i = 0; i < spriteList.Length; ++i)
		{
			if (spriteList[i].name == name)
				return spriteList[i];
		}
		return null;
	}
}