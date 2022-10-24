using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreviewObject : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
		cachedTransform.eulerAngles = CustomFollowCamera.instance.cachedTransform.eulerAngles;
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
