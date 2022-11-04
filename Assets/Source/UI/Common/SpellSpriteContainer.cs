using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellSpriteContainer : MonoBehaviour
{
	public static SpellSpriteContainer instance;

	void Awake()
	{
		instance = this;
	}

	public Sprite[] spriteList;

	public Sprite normalQuestionSprite;
	public Sprite goldenQuestionSprite;

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