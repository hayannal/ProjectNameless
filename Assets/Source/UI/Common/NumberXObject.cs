using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NumberXObject : MonoBehaviour
{
	public GameObject xObject;
	public GameObject[] numberObjectList;

	public void SetNumber(int number)
	{
		xObject.SetActive(number >= 1);

		for (int i = 0; i < numberObjectList.Length; ++i)
			numberObjectList[i].SetActive(i == number);
	}

	Transform _transform;
	public Transform cachedTransform
	{
		get
		{
			if (_transform == null)
				_transform = GetComponent<Transform>();
			return _transform;
		}
	}
}