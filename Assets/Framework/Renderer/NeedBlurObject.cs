﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// RFX4_PerPlatformSettings 와 달리 씬 오브젝트도 쓸 수 있게 만들었다.
[ExecuteInEditMode]
public class NeedBlurObject : MonoBehaviour
{
	public bool useStrongBlur;

	void OnEnable()
	{
		IncreaseRefCount();
	}

	void OnDisable()
	{
		DecreaseRefCount();
	}

	void IncreaseRefCount()
	{
		if (CustomRenderer.instance != null)
		{
			if (useStrongBlur)
				++CustomRenderer.instance.needStrongBlur_refCount;
			else
				++CustomRenderer.instance.needBlur_refCount;
		}
	}

	void DecreaseRefCount()
	{
		if (CustomRenderer.instance != null)
		{
			if (useStrongBlur)
				--CustomRenderer.instance.needStrongBlur_refCount;
			else
				--CustomRenderer.instance.needBlur_refCount;
		}
	}
}
