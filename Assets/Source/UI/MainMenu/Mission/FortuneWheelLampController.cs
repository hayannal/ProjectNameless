using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FortuneWheelLampController : MonoBehaviour
{
	List<RectTransform> _listNodeTransform;
	void OnEnable()
	{
		if (_listNodeTransform == null)
		{
			_listNodeTransform = new List<RectTransform>();
			for (int i = 0; i < transform.childCount; ++i)
				_listNodeTransform.Add(transform.GetChild(i).GetComponent<RectTransform>());
		}

		float angle = 360.0f / _listNodeTransform.Count;
		for (int i = 0; i < _listNodeTransform.Count; ++i)
			_listNodeTransform[i].localRotation = Quaternion.Euler(0.0f, 0.0f, angle * -i - 11.0f);
	}
}
