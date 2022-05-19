using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageFloorInfoCanvas : MonoBehaviour
{
	public static StageFloorInfoCanvas instance;

	public Text stageText;
	public GameObject repeatTextObject;
	public GameObject challengeTextObject;

	void Awake()
	{
		instance = this;
	}

	public void RefreshStageInfo(int stage, bool repeat)
	{
		stageText.text = string.Format("STAGE {0:N0}", stage);
		repeatTextObject.SetActive(repeat);
		challengeTextObject.SetActive(!repeat);
	}




	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = transform;
			return _transform;
		}
	}
}