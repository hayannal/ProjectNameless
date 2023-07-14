using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VictoryResultCanvas : MonoBehaviour
{
	public static VictoryResultCanvas instance;

	public Text victoryText;

	void Awake()
	{
		instance = this;
	}
}