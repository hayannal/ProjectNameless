using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipGround : MonoBehaviour
{
	public static EquipGround instance;
	
	public EquipAltar[] equipAltarList;
	public GameObject reconstructRootObject;

	void Awake()
	{
		instance = this;
	}

	void OnEnable()
	{
		//reconstructRootObject.SetActive(ContentsManager.IsOpen(ContentsManager.eOpenContentsByChapter.Reconstruct));
	}

	#region AlarmObject
	public void RefreshAlarmObjectList()
	{
		for (int i = 0; i < equipAltarList.Length; ++i)
			equipAltarList[i].RefreshAlarmObject();
	}
	#endregion










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